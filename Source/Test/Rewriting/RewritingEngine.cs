// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Coyote.Interception;
using Microsoft.Coyote.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// Engine that can rewrite a set of assemblies for systematic testing.
    /// </summary>
    /// <remarks>
    /// See <see href="/coyote/get-started/rewriting">rewriting</see> for more information.
    /// </remarks>
    public class RewritingEngine
    {
        /// <summary>
        /// Temporary directory that is used to write the rewritten assemblies
        /// in the case that they are replacing the original ones.
        /// </summary>
        /// <remarks>
        /// We need this because it seems Mono.Cecil does not allow to rewrite in-place.
        /// </remarks>
        private const string TempDirectory = "__temp_coyote__";

        /// <summary>
        /// Options for rewriting assemblies.
        /// </summary>
        private readonly RewritingOptions Options;

        /// <summary>
        /// The test configuration to use when rewriting unit tests.
        /// </summary>
        private readonly Configuration Configuration;

        /// <summary>
        /// List of assemblies that are not allowed to be rewritten.
        /// </summary>
        private readonly Regex DisallowedAssemblies;

        private readonly string[] DefaultDisallowedList = new string[]
        {
            @"Newtonsoft\.Json\.dll",
            @"Microsoft\.Coyote\.dll",
            @"Microsoft\.Coyote.Test\.dll",
            @"Microsoft\.VisualStudio\.TestPlatform.*",
            @"Microsoft\.TestPlatform.*",
            @"System\.Private\.CoreLib\.dll",
            @"mscorlib\.dll"
        };

        /// <summary>
        /// List of transforms we are applying while rewriting.
        /// </summary>
        private readonly List<AssemblyTransform> Transforms;

        /// <summary>
        /// Map from assembly name to full name definition of the rewritten assemblies.
        /// </summary>
        private readonly Dictionary<string, AssemblyNameDefinition> RewrittenAssemblies;

        /// <summary>
        /// List of assemblies to be rewritten.
        /// </summary>
        private Queue<string> Pending = new Queue<string>();

        /// <summary>
        /// The installed logger.
        /// </summary>
        private readonly ILogger Logger;

        /// <summary>
        /// The profiler.
        /// </summary>
        private readonly Profiler Profiler;

        /// <summary>
        /// Initializes a new instance of the <see cref="RewritingEngine"/> class.
        /// </summary>
        /// <param name="configuration">The test configuration to use when rewriting unit tests.</param>
        /// <param name="options">The <see cref="RewritingOptions"/> for this rewriter.</param>
        private RewritingEngine(Configuration configuration, RewritingOptions options)
        {
            this.Configuration = configuration;
            this.Options = options;
            this.Logger = options.Logger ?? new ConsoleLogger() { LogLevel = options.LogLevel };
            this.Profiler = new Profiler();

            this.RewrittenAssemblies = new Dictionary<string, AssemblyNameDefinition>();
            var ignoredAssemblies = options.IgnoredAssemblies ?? Array.Empty<string>();
            StringBuilder combined = new StringBuilder();
            foreach (var e in this.DefaultDisallowedList.Concat(ignoredAssemblies))
            {
                combined.Append(combined.Length is 0 ? "(" : "|");
                combined.Append(e);
            }

            combined.Append(")");
            try
            {
                this.DisallowedAssemblies = new Regex(combined.ToString());
            }
            catch (Exception ex)
            {
                throw new Exception("DisallowedAssemblies not a valid regular expression\n" + ex.Message);
            }

            this.Transforms = new List<AssemblyTransform>()
            {
                new TaskTransform(this.Logger),
                new MonitorTransform(this.Logger),
                new ExceptionFilterTransform(this.Logger)
            };

            if (this.Options.IsRewritingThreads)
            {
                this.Transforms.Add(new ThreadTransform(this.Logger));
            }

            if (this.Options.IsRewritingUnitTests)
            {
                // We are running this pass last, as we are rewriting the original method, and
                // we need the other rewriting passes to happen before this pass.
                this.Transforms.Add(new MSTestTransform(this.Configuration, this.Logger));
            }

            this.Transforms.Add(new AssertionInjectionTransform(this.Logger));
            this.Transforms.Add(new NotSupportedInvocationTransform(this.Logger));

            // expand folder
            if (this.Options.AssemblyPaths is null || this.Options.AssemblyPaths.Count is 0)
            {
                // Expand to include all .dll files in AssemblyPaths.
                foreach (var file in Directory.GetFiles(this.Options.AssembliesDirectory, "*.dll"))
                {
                    if (this.IsDisallowed(Path.GetFileName(file)))
                    {
                        this.Options.AssemblyPaths.Add(file);
                    }
                    else
                    {
                        Debug.WriteLine("Skipping " + file);
                    }
                }
            }
        }

        /// <summary>
        /// Runs the engine using the specified rewriting options.
        /// </summary>
        public static void Run(Configuration configuration, RewritingOptions options)
        {
            if (string.IsNullOrEmpty(options.AssembliesDirectory))
            {
                throw new Exception("Please provide RewritingOptions.AssembliesDirectory");
            }

            if (string.IsNullOrEmpty(options.OutputDirectory))
            {
                throw new Exception("Please provide RewritingOptions.OutputDirectory");
            }

            if (options.AssemblyPaths is null || options.AssemblyPaths.Count is 0)
            {
                throw new Exception("Please provide RewritingOptions.AssemblyPaths");
            }

            var engine = new RewritingEngine(configuration, options);
            engine.Run();
        }

        /// <summary>
        /// Runs the engine.
        /// </summary>
        private void Run()
        {
            this.Profiler.StartMeasuringExecutionTime();

            // Create the output directory and copy any necessary files.
            string outputDirectory = this.CreateOutputDirectoryAndCopyFiles();

            this.Pending = new Queue<string>();

            int errors = 0;
            // Rewrite the assembly files to the output directory.
            foreach (string assemblyPath in this.Options.AssemblyPaths)
            {
                this.Pending.Enqueue(assemblyPath);
            }

            while (this.Pending.Count > 0)
            {
                var assemblyPath = this.Pending.Dequeue();

                try
                {
                    this.RewriteAssembly(assemblyPath, outputDirectory);
                }
                catch (Exception ex)
                {
                    if (!this.Options.IsReplacingAssemblies)
                    {
                        // Make sure to copy the original assembly to avoid any corruptions.
                        CopyFile(assemblyPath, outputDirectory);
                    }

                    if (ex is AggregateException ae && ae.InnerException != null)
                    {
                        this.Logger.WriteLine(LogSeverity.Error, ae.InnerException.Message);
                    }
                    else
                    {
                        this.Logger.WriteLine(LogSeverity.Error, ex.Message);
                    }

                    errors++;
                }
            }

            if (errors is 0 && this.Options.IsReplacingAssemblies)
            {
                // If we are replacing the original assemblies, then delete the temporary output directory.
                Directory.Delete(outputDirectory, true);
            }

            this.Profiler.StopMeasuringExecutionTime();
            Console.WriteLine($". Done rewriting in {this.Profiler.Results()} sec");
        }

        /// <summary>
        /// Rewrites the specified assembly definition.
        /// </summary>
        private void RewriteAssembly(string assemblyPath, string outputDirectory)
        {
            string assemblyName = Path.GetFileName(assemblyPath);
            if (this.IsDisallowed(assemblyName))
            {
                throw new InvalidOperationException($"Rewriting the '{assemblyName}' assembly ({assemblyPath}) is not allowed.");
            }
            else if (this.RewrittenAssemblies.ContainsKey(Path.GetFileNameWithoutExtension(assemblyPath)))
            {
                // The assembly is already rewritten, so skip it.
                return;
            }

            string outputPath = Path.Combine(outputDirectory, assemblyName);

            using var assemblyResolver = this.GetAssemblyResolver();
            var readParams = new ReaderParameters()
            {
                AssemblyResolver = assemblyResolver,
                ReadSymbols = IsSymbolFileAvailable(assemblyPath)
            };

            using (var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, readParams))
            {
                this.Logger.WriteLine($"... Rewriting the '{assemblyName}' assembly ({assembly.FullName})");
                if (this.Options.IsRewritingDependencies)
                {
                    // Check for dependencies and if those are found in the same folder then rewrite them also,
                    // and fix up the version numbers so the rewritten assemblies are bound to these versions.
                    this.AddLocalDependencies(assemblyPath, assembly);
                }

                this.RewrittenAssemblies[assembly.Name.Name] = assembly.Name;

                if (IsAssemblyRewritten(assembly))
                {
                    // The assembly has been already rewritten by this version of Coyote, so skip it.
                    this.Logger.WriteLine(LogSeverity.Warning, $"..... Skipping assembly with reason: already rewritten by Coyote v{GetAssemblyRewritterVersion()}");
                    return;
                }
                else if (IsMixedModeAssembly(assembly))
                {
                    // Mono.Cecil does not support writing mixed-mode assemblies.
                    this.Logger.WriteLine(LogSeverity.Warning, $"..... Skipping assembly with reason: rewriting a mixed-mode assembly is not supported");
                    return;
                }

                this.FixAssemblyReferences(assembly);

                ApplyIsAssemblyRewrittenAttribute(assembly);
                foreach (var transform in this.Transforms)
                {
                    // Traverse the assembly to apply each transformation pass.
                    Debug.WriteLine($"..... Applying the '{transform.GetType().Name}' transform");
                    foreach (var module in assembly.Modules)
                    {
                        RewriteModule(module, transform);
                    }
                }

                // Write the binary in the output path with portable symbols enabled.
                this.Logger.WriteLine($"... Writing the modified '{assemblyName}' assembly to " +
                    $"{(this.Options.IsReplacingAssemblies ? assemblyPath : outputPath)}");

                var writeParams = new WriterParameters()
                {
                    WriteSymbols = readParams.ReadSymbols,
                    SymbolWriterProvider = new PortablePdbWriterProvider()
                };

                if (!string.IsNullOrEmpty(this.Options.StrongNameKeyFile))
                {
                    using FileStream fs = File.Open(this.Options.StrongNameKeyFile, FileMode.Open);
                    writeParams.StrongNameKeyPair = new StrongNameKeyPair(fs);
                }

                assembly.Write(outputPath, writeParams);
            }

            if (this.Options.IsReplacingAssemblies)
            {
                string targetPath = Path.Combine(this.Options.AssembliesDirectory, assemblyName);
                this.CopyWithRetriesAsync(outputPath, assemblyPath).Wait();
                if (readParams.ReadSymbols)
                {
                    string pdbFile = Path.ChangeExtension(outputPath, "pdb");
                    string targetPdbFile = Path.ChangeExtension(targetPath, "pdb");
                    this.CopyWithRetriesAsync(pdbFile, targetPdbFile).Wait();
                }
            }
        }

        private async Task CopyWithRetriesAsync(string srcFile, string targetFile)
        {
            for (int retries = 10; retries >= 0; retries--)
            {
                try
                {
                    File.Copy(srcFile, targetFile, true);
                }
                catch (Exception)
                {
                    if (retries is 0)
                    {
                        throw;
                    }

                    await Task.Delay(100);
                    this.Logger.WriteLine(LogSeverity.Warning, $"... Retrying write to {targetFile}");
                }
            }
        }

        private void FixAssemblyReferences(AssemblyDefinition assembly)
        {
            foreach (var module in assembly.Modules)
            {
                for (int i = 0, n = module.AssemblyReferences.Count; i < n; i++)
                {
                    var ar = module.AssemblyReferences[i];
                    var name = ar.Name;
                    if (this.RewrittenAssemblies.TryGetValue(name, out AssemblyNameDefinition rewrittenName))
                    {
                        // rewrite this reference to point to the newly rewritten assembly.
                        var refName = AssemblyNameReference.Parse(rewrittenName.FullName);
                        module.AssemblyReferences[i] = refName;
                    }
                }
            }
        }

        /// <summary>
        /// Enqueue any dependent assemblies that also exist in the assemblyPath and have not
        /// already been rewritten.
        /// </summary>
        private void AddLocalDependencies(string assemblyPath, AssemblyDefinition assembly)
        {
            var assemblyDir = Path.GetDirectoryName(assemblyPath);
            foreach (var module in assembly.Modules)
            {
                foreach (var ar in module.AssemblyReferences)
                {
                    var name = ar.Name + ".dll";
                    var localName = Path.Combine(assemblyDir, name);
                    if (!this.IsDisallowed(name) &&
                        File.Exists(localName) && !this.Pending.Contains(localName))
                    {
                        this.Pending.Enqueue(localName);
                    }
                }
            }
        }

        /// <summary>
        /// Rewrites the specified module definition using the specified transform.
        /// </summary>
        private static void RewriteModule(ModuleDefinition module, AssemblyTransform transform)
        {
            Debug.WriteLine($"....... Module: {module.Name} ({module.FileName})");
            transform.VisitModule(module);
            foreach (var type in module.GetTypes())
            {
                RewriteType(type, transform);
            }
        }

        /// <summary>
        /// Rewrites the specified type definition using the specified transform.
        /// </summary>
        private static void RewriteType(TypeDefinition type, AssemblyTransform transform)
        {
            Debug.WriteLine($"......... Type: {type.FullName}");
            transform.VisitType(type);
            foreach (var field in type.Fields.ToArray())
            {
                Debug.WriteLine($"........... Field: {field.FullName}");
                transform.VisitField(field);
            }

            foreach (var method in type.Methods.ToArray())
            {
                RewriteMethod(method, transform);
            }
        }

        /// <summary>
        /// Rewrites the specified method definition using the specified transform.
        /// </summary>
        private static void RewriteMethod(MethodDefinition method, AssemblyTransform transform)
        {
            if (method.Body is null)
            {
                return;
            }

            Debug.WriteLine($"........... Method {method.FullName}");
            transform.VisitMethod(method);
        }

        /// <summary>
        /// Applies the <see cref="IsAssemblyRewrittenAttribute"/> attribute to the specified assembly. This attribute
        /// indicates that the assembly has been rewritten with the current version of Coyote.
        /// </summary>
        private static void ApplyIsAssemblyRewrittenAttribute(AssemblyDefinition assembly)
        {
            CustomAttribute attribute = GetCustomAttribute(assembly, typeof(IsAssemblyRewrittenAttribute));
            var attributeArgument = new CustomAttributeArgument(
                assembly.MainModule.ImportReference(typeof(string)),
                GetAssemblyRewritterVersion().ToString());

            if (attribute is null)
            {
                MethodReference attributeConstructor = assembly.MainModule.ImportReference(
                    typeof(IsAssemblyRewrittenAttribute).GetConstructor(new Type[] { typeof(string) }));
                attribute = new CustomAttribute(attributeConstructor);
                attribute.ConstructorArguments.Add(attributeArgument);
                assembly.CustomAttributes.Add(attribute);
            }
            else
            {
                attribute.ConstructorArguments[0] = attributeArgument;
            }
        }

        /// <summary>
        /// Creates the output directory, if it does not already exists, and copies all necessery files.
        /// </summary>
        /// <returns>The output directory path.</returns>
        private string CreateOutputDirectoryAndCopyFiles()
        {
            string sourceDirectory = this.Options.AssembliesDirectory;
            string outputDirectory = Directory.CreateDirectory(this.Options.IsReplacingAssemblies ?
                Path.Combine(this.Options.OutputDirectory, TempDirectory) : this.Options.OutputDirectory).FullName;

            if (!this.Options.IsReplacingAssemblies)
            {
                this.Logger.WriteLine($"... Copying all files to the '{outputDirectory}' directory");

                // Copy all files to the output directory, skipping any nested directory files.
                foreach (string filePath in Directory.GetFiles(sourceDirectory, "*"))
                {
                    Debug.WriteLine($"..... Copying the '{filePath}' file");
                    CopyFile(filePath, outputDirectory);
                }

                // Copy all nested directories to the output directory, while preserving directory structure.
                foreach (string directoryPath in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
                {
                    // Avoid copying the output directory itself.
                    if (!directoryPath.StartsWith(outputDirectory))
                    {
                        Debug.WriteLine($"..... Copying the '{directoryPath}' directory");
                        string path = Path.Combine(outputDirectory, directoryPath.Remove(0, sourceDirectory.Length).TrimStart('\\', '/'));
                        Directory.CreateDirectory(path);
                        foreach (string filePath in Directory.GetFiles(directoryPath, "*"))
                        {
                            Debug.WriteLine($"....... Copying the '{filePath}' file");
                            CopyFile(filePath, path);
                        }
                    }
                }
            }

            // Copy all the dependent assemblies
            foreach (var type in new Type[]
                {
                    typeof(ControlledTask),
                    typeof(RewritingEngine),
                    typeof(TelemetryConfiguration),
                    typeof(EventTelemetry),
                    typeof(ITelemetry),
                    typeof(TelemetryClient)
                })
            {
                string assemblyPath = type.Assembly.Location;
                CopyFile(assemblyPath, this.Options.OutputDirectory);
            }

            return outputDirectory;
        }

        /// <summary>
        /// Copies the specified file to the destination.
        /// </summary>
        private static void CopyFile(string filePath, string destination) =>
            File.Copy(filePath, Path.Combine(destination, Path.GetFileName(filePath)), true);

        /// <summary>
        /// Checks if the assembly with the specified name is not allowed.
        /// </summary>
        private bool IsDisallowed(string assemblyName) => this.DisallowedAssemblies is null ? false :
            this.DisallowedAssemblies.IsMatch(assemblyName);

        /// <summary>
        /// Checks if the specified assembly has been already rewritten with the current version of Coyote.
        /// </summary>
        /// <param name="assembly">The assembly to check.</param>
        /// <returns>True if the assembly has been rewritten, else false.</returns>
        public static bool IsAssemblyRewritten(Assembly assembly) =>
            assembly.GetCustomAttribute(typeof(IsAssemblyRewrittenAttribute)) is IsAssemblyRewrittenAttribute attribute &&
            attribute.Version == GetAssemblyRewritterVersion().ToString();

        /// <summary>
        /// Checks if the specified assembly has been already rewritten with the current version of Coyote.
        /// </summary>
        /// <param name="assembly">The assembly to check.</param>
        /// <returns>True if the assembly has been rewritten, else false.</returns>
        private static bool IsAssemblyRewritten(AssemblyDefinition assembly)
        {
            var attribute = GetCustomAttribute(assembly, typeof(IsAssemblyRewrittenAttribute));
            return attribute != null && (string)attribute.ConstructorArguments[0].Value == GetAssemblyRewritterVersion().ToString();
        }

        /// <summary>
        /// Checks if the specified assembly is a mixed-mode assembly.
        /// </summary>
        /// <param name="assembly">The assembly to check.</param>
        /// <returns>True if the assembly only contains IL, else false.</returns>
        private static bool IsMixedModeAssembly(AssemblyDefinition assembly)
        {
            foreach (var module in assembly.Modules)
            {
                if ((module.Attributes & ModuleAttributes.ILOnly) is 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the symbol file for the specified assembly is available.
        /// </summary>
        private static bool IsSymbolFileAvailable(string assemblyPath) =>
            File.Exists(Path.ChangeExtension(assemblyPath, "pdb"));

        /// <summary>
        /// Returns the first found custom attribute with the specified type, if such an attribute
        /// is applied to the specified assembly, else null.
        /// </summary>
        private static CustomAttribute GetCustomAttribute(AssemblyDefinition assembly, Type attributeType) =>
            assembly.CustomAttributes.FirstOrDefault(
                attr => attr.AttributeType.Namespace == attributeType.Namespace &&
                attr.AttributeType.Name == attributeType.Name);

        /// <summary>
        /// Returns the version of the assembly rewritter.
        /// </summary>
        private static Version GetAssemblyRewritterVersion() => Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// Returns a new assembly resolver.
        /// </summary>
        private IAssemblyResolver GetAssemblyResolver()
        {
            // TODO: can we reuse it, or do we need a new one for each assembly?
            var assemblyResolver = new DefaultAssemblyResolver();

            // Add known search directories for resolving assemblies.
            assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(typeof(ControlledTask).Assembly.Location));
            assemblyResolver.AddSearchDirectory(this.Options.AssembliesDirectory);
            if (this.Options.DependencySearchPaths != null)
            {
                foreach (var path in this.Options.DependencySearchPaths)
                {
                    assemblyResolver.AddSearchDirectory(path);
                }
            }

            // Add the assembly resolution error handler.
            assemblyResolver.ResolveFailure += this.OnResolveAssemblyFailure;
            return assemblyResolver;
        }

        /// <summary>
        /// Handles an assembly resolution error.
        /// </summary>
        private AssemblyDefinition OnResolveAssemblyFailure(object sender, AssemblyNameReference reference)
        {
            this.Logger.WriteLine(LogSeverity.Warning, "Unable to resolve assembly: " + reference.FullName);
            return null;
        }
    }
}
