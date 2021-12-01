// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;
using Mono.Cecil;

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
        /// List of passes to invoke while rewriting IL.
        /// </summary>
        private readonly LinkedList<Pass> Passes;

        /// <summary>
        /// Simple cache to reduce redundant warnings.
        /// </summary>
        private readonly HashSet<string> ResolveWarnings;

        /// <summary>
        /// The installed logger.
        /// </summary>
        private readonly ILogger Logger;

        /// <summary>
        /// The installed profiler.
        /// </summary>
        private readonly Profiler Profiler;

        /// <summary>
        /// Initializes a new instance of the <see cref="RewritingEngine"/> class.
        /// </summary>
        private RewritingEngine(RewritingOptions options, Configuration configuration, ILogger logger, Profiler profiler)
        {
            this.Options = options.Sanitize();
            this.Configuration = configuration;
            this.Passes = new LinkedList<Pass>();
            this.ResolveWarnings = new HashSet<string>();
            this.Logger = logger;
            this.Profiler = profiler;
        }

        /// <summary>
        /// Runs the engine using the specified rewriting options.
        /// </summary>
        internal static void Run(RewritingOptions options, Configuration configuration, Profiler profiler)
        {
            var logger = new ConsoleLogger() { LogLevel = configuration.LogLevel };
            var engine = new RewritingEngine(options, configuration, logger, profiler);
            engine.Run();
        }

        /// <summary>
        /// Runs the rewriting engine.
        /// </summary>
        private void Run()
        {
            this.Profiler.StartMeasuringExecutionTime();

            // Create the output directory and copy any necessary files.
            string outputDirectory = this.CreateOutputDirectoryAndCopyFiles();

            try
            {
                // Get the set of assemblies to rewrite.
                var assemblies = AssemblyInfo.LoadAssembliesToRewrite(this.Options, this.OnResolveAssemblyFailure);
                this.InitializePasses(assemblies);
                foreach (var assembly in assemblies)
                {
                    string outputPath = Path.Combine(outputDirectory, assembly.Name);
                    this.RewriteAssembly(assembly, outputPath);
                }
            }
            catch (Exception ex)
            {
                ExceptionDispatchInfo.Capture(ex).Throw();
            }
            finally
            {
                if (this.Options.IsReplacingAssemblies())
                {
                    // If we are replacing the original assemblies, then delete the temporary output directory.
                    Directory.Delete(outputDirectory, true);
                }

                this.Profiler.StopMeasuringExecutionTime();
            }
        }

        /// <summary>
        /// Initializes the passes to invoke during rewriting.
        /// </summary>
        private void InitializePasses(IEnumerable<AssemblyInfo> assemblies)
        {
            this.Passes.AddFirst(new TaskRewritingPass(assemblies, this.Logger));
            this.Passes.AddLast(new MonitorRewritingPass(assemblies, this.Logger));
            this.Passes.AddLast(new ExceptionFilterRewritingPass(assemblies, this.Logger));

            if (this.Options.IsRewritingThreads)
            {
                this.Passes.AddLast(new ThreadingRewritingPass(assemblies, this.Logger));
            }

            if (this.Options.IsRewritingConcurrentCollections)
            {
                this.Passes.AddLast(new ConcurrentCollectionRewritingPass(assemblies, this.Logger));
            }

            if (this.Options.IsDataRaceCheckingEnabled)
            {
                this.Passes.AddLast(new DataRaceCheckingRewritingPass(assemblies, this.Logger));
            }

#if !NETSTANDARD2_0 && !NETFRAMEWORK
            this.Passes.AddLast(new AspNetRewritingPass(assemblies, this.Logger));
#endif

            if (this.Options.IsRewritingUnitTests)
            {
                // We are running this pass last, as we are rewriting the original method, and
                // we need the other rewriting passes to happen before this pass.
                this.Passes.AddLast(new MSTestRewritingPass(this.Configuration, assemblies, this.Logger));
            }

            this.Passes.AddLast(new InterAssemblyInvocationRewritingPass(assemblies, this.Logger));
            this.Passes.AddLast(new UncontrolledInvocationRewritingPass(assemblies, this.Logger));

            if (this.Options.IsLoggingAssemblyContents || this.Options.IsDiffingAssemblyContents)
            {
                // Parsing the contents of an assembly must happen before and after any other pass.
                this.Passes.AddFirst(new AssemblyDiffingPass(assemblies, this.Logger));
                this.Passes.AddLast(new AssemblyDiffingPass(assemblies, this.Logger));
            }
        }

        /// <summary>
        /// Rewrites the specified assembly.
        /// </summary>
        private void RewriteAssembly(AssemblyInfo assembly, string outputPath)
        {
            try
            {
                this.Logger.WriteLine($"... Rewriting the '{assembly.Name}' assembly ({assembly.FullName})");
                if (assembly.IsRewritten)
                {
                    this.Logger.WriteLine($"..... Skipping as assembly is already rewritten with matching signature");
                    return;
                }

                // Traverse the assembly to invoke each pass.
                foreach (var pass in this.Passes)
                {
                    Debug.WriteLine($"..... Invoking the '{pass.GetType().Name}' pass");
                    assembly.Invoke(pass);
                }

                // Apply the rewriting signature to the assembly metadata.
                assembly.ApplyRewritingSignatureAttribute(GetAssemblyRewriterVersion());

                // Write the binary in the output path with portable symbols enabled.
                string resolvedOutputPath = this.Options.IsReplacingAssemblies() ? assembly.FilePath : outputPath;
                this.Logger.WriteLine($"..... Writing the modified '{assembly.Name}' assembly to {resolvedOutputPath}");
                assembly.Write(outputPath);

                if (this.Options.IsLoggingAssemblyContents)
                {
                    // Write the IL before and after rewriting to a JSON file.
                    this.WriteILToJson(assembly, false, resolvedOutputPath);
                    this.WriteILToJson(assembly, true, resolvedOutputPath);
                }

                if (this.Options.IsDiffingAssemblyContents)
                {
                    // Write the IL diff before and after rewriting to a JSON file.
                    this.WriteILDiffToJson(assembly, resolvedOutputPath);
                }
            }
            finally
            {
                assembly.Dispose();
            }

            if (this.Options.IsReplacingAssemblies())
            {
                string targetPath = Path.Combine(this.Options.AssembliesDirectory, assembly.Name);
                this.CopyWithRetriesAsync(outputPath, assembly.FilePath).Wait();
                if (assembly.IsSymbolFileAvailable())
                {
                    string pdbFile = Path.ChangeExtension(outputPath, "pdb");
                    string targetPdbFile = Path.ChangeExtension(targetPath, "pdb");
                    this.CopyWithRetriesAsync(pdbFile, targetPdbFile).Wait();
                }
            }
        }

        /// <summary>
        /// Writes the original or rewritten IL to a JSON file in the specified output path.
        /// </summary>
        internal void WriteILToJson(AssemblyInfo assembly, bool isRewritten, string outputPath)
        {
            var diffingPass = (isRewritten ? this.Passes.Last : this.Passes.First).Value as AssemblyDiffingPass;
            if (diffingPass != null)
            {
                string json = diffingPass.GetJson(assembly);
                if (!string.IsNullOrEmpty(json))
                {
                    string jsonFile = Path.ChangeExtension(outputPath, $".{(isRewritten ? "rw" : "il")}.json");
                    this.Logger.WriteLine($"..... Writing the {(isRewritten ? "rewritten" : "original")} IL " +
                        $"of '{assembly.Name}' as JSON to {jsonFile}");
                    File.WriteAllText(jsonFile, json);
                }
            }
        }

        /// <summary>
        /// Writes the IL diff to a JSON file in the specified output path.
        /// </summary>
        internal void WriteILDiffToJson(AssemblyInfo assembly, string outputPath)
        {
            var originalDiffingPass = this.Passes.First.Value as AssemblyDiffingPass;
            var rewrittenDiffingPass = this.Passes.Last.Value as AssemblyDiffingPass;
            if (originalDiffingPass != null && rewrittenDiffingPass != null)
            {
                // Compute the diff between the original and rewritten IL and dump it to JSON.
                string diffJson = originalDiffingPass.GetDiffJson(assembly, rewrittenDiffingPass);
                if (!string.IsNullOrEmpty(diffJson))
                {
                    string jsonFile = Path.ChangeExtension(outputPath, ".diff.json");
                    this.Logger.WriteLine($"..... Writing the IL diff of '{assembly.Name}' as JSON to {jsonFile}");
                    File.WriteAllText(jsonFile, diffJson);
                }
            }
        }

        /// <summary>
        /// Checks if the specified assembly has been already rewritten with the current version.
        /// </summary>
        /// <param name="assembly">The assembly to check.</param>
        /// <returns>True if the assembly has been rewritten with the current version, else false.</returns>
        public static bool IsAssemblyRewritten(Assembly assembly) =>
            assembly.GetCustomAttribute(typeof(RewritingSignatureAttribute)) is RewritingSignatureAttribute attribute &&
            attribute.Version == GetAssemblyRewriterVersion().ToString();

        /// <summary>
        /// Returns the version of the assembly rewriter.
        /// </summary>
        private static Version GetAssemblyRewriterVersion() => Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// Creates the output directory, if it does not already exists, and copies all necessary files.
        /// </summary>
        /// <returns>The output directory path.</returns>
        private string CreateOutputDirectoryAndCopyFiles()
        {
            string sourceDirectory = this.Options.AssembliesDirectory;
            string outputDirectory = Directory.CreateDirectory(this.Options.IsReplacingAssemblies() ?
                Path.Combine(this.Options.OutputDirectory, TempDirectory) : this.Options.OutputDirectory).FullName;
            if (!this.Options.IsReplacingAssemblies())
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
                        string path = Path.Combine(outputDirectory, directoryPath.Remove(0, sourceDirectory.Length)
                            .TrimStart('\\', '/'));
                        Directory.CreateDirectory(path);
                        foreach (string filePath in Directory.GetFiles(directoryPath, "*"))
                        {
                            Debug.WriteLine($"....... Copying the '{filePath}' file");
                            CopyFile(filePath, path);
                        }
                    }
                }
            }

            // Copy all the dependent assemblies.
            foreach (var type in new Type[]
                {
                    typeof(CoyoteRuntime),
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
        /// Copies the specified file to the destination with retries.
        /// </summary>
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

        /// <summary>
        /// Handles an assembly resolution error.
        /// </summary>
        private AssemblyDefinition OnResolveAssemblyFailure(object sender, AssemblyNameReference reference)
        {
            if (!this.ResolveWarnings.Contains(reference.FullName))
            {
                this.Logger.WriteLine(LogSeverity.Warning, "Unable to resolve assembly: '{0}'", reference.FullName);
                this.ResolveWarnings.Add(reference.FullName);
            }

            return null;
        }
    }
}
