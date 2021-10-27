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
using Microsoft.Coyote.Interception;
using Microsoft.Coyote.IO;
using Mono.Cecil;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// Engine that can rewrite a set of assemblies for systematic testing.
    /// </summary>
    /// <remarks>
    /// See <see href="/coyote/get-started/rewriting">rewriting</see> for more information.
    /// </remarks>
    internal class RewritingEngine
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
        /// Unique identifier applied to the rewritten assemblies.
        /// </summary>
        private readonly Guid Identifier;

        /// <summary>
        /// List of passes applied while rewriting.
        /// </summary>
        private readonly List<AssemblyRewriter> RewritingPasses;

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
            this.Identifier = Guid.NewGuid();
            this.Options = options.Sanitize();
            this.Configuration = configuration;
            this.RewritingPasses = new List<AssemblyRewriter>();
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
                this.InitializeRewritingPasses(assemblies);
                foreach (var assembly in assemblies)
                {
                    if (!assembly.IsRewritten)
                    {
                        string outputPath = Path.Combine(outputDirectory, assembly.Name);
                        this.RewriteAssembly(assembly, outputPath);
                    }
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
        /// Initializes the passes to run during rewriting.
        /// </summary>
        private void InitializeRewritingPasses(HashSet<AssemblyInfo> assemblies)
        {
            this.RewritingPasses.Add(new TaskRewriter(assemblies, this.Logger));
            this.RewritingPasses.Add(new MonitorRewriter(assemblies, this.Logger));
            this.RewritingPasses.Add(new ExceptionFilterRewriter(assemblies, this.Logger));

            if (this.Options.IsRewritingThreads)
            {
                this.RewritingPasses.Add(new ThreadingRewriter(assemblies, this.Logger));
            }

            if (this.Options.IsRewritingConcurrentCollections)
            {
                this.RewritingPasses.Add(new ConcurrentCollectionRewriter(assemblies, this.Logger));
            }

            if (this.Options.IsDataRaceCheckingEnabled)
            {
                this.RewritingPasses.Add(new DataRaceCheckingRewriter(assemblies, this.Logger));
            }

            if (this.Options.IsRewritingUnitTests)
            {
                // We are running this pass last, as we are rewriting the original method, and
                // we need the other rewriting passes to happen before this pass.
                this.RewritingPasses.Add(new MSTestRewriter(this.Configuration, assemblies, this.Logger));
            }

            this.RewritingPasses.Add(new InterAssemblyInvocationRewriter(assemblies, this.Logger));
            this.RewritingPasses.Add(new UncontrolledInvocationRewriter(assemblies, this.Logger));
        }

        /// <summary>
        /// Rewrites the specified assembly.
        /// </summary>
        private void RewriteAssembly(AssemblyInfo assembly, string outputPath)
        {
            try
            {
                this.Logger.WriteLine($"... Rewriting the '{assembly.Name}' assembly ({assembly.FullName})");
                assembly.ValidateAssemblyBeforeRewriting();
                assembly.ApplyCoyoteVersionAttribute(GetAssemblyRewriterVersion(), this.Identifier);

                // Traverse the assembly to apply each rewriting pass.
                foreach (var pass in this.RewritingPasses)
                {
                    assembly.Rewrite(pass);
                }

                // Write the binary in the output path with portable symbols enabled.
                this.Logger.WriteLine($"... Writing the modified '{assembly.Name}' assembly to " +
                    $"{(this.Options.IsReplacingAssemblies() ? assembly.FilePath : outputPath)}");
                assembly.Write(outputPath);
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
        /// Checks if the specified assembly has been already rewritten with the current version.
        /// </summary>
        /// <param name="assembly">The assembly to check.</param>
        /// <returns>True if the assembly has been rewritten with the current version, else false.</returns>
        internal static bool IsAssemblyRewrittenWithCurrentVersion(Assembly assembly) =>
            assembly.GetCustomAttribute(typeof(CoyoteVersionAttribute)) is CoyoteVersionAttribute attribute &&
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
