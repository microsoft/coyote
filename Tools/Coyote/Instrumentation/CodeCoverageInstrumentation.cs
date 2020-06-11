// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.SystematicTesting.Utilities;

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Instruments a binary for code coverage.
    /// </summary>
    internal static class CodeCoverageInstrumentation
    {
        internal static string OutputDirectory = string.Empty;

        internal static void Instrument(Configuration configuration)
        {
            if (string.IsNullOrEmpty(OutputDirectory))
            {
                throw new Exception("Please set the OutputDirectory before calling Instrument");
            }

            // HashSet in case of duplicate file specifications.
            var assembliesToInstrument = new HashSet<string>(GetInstrumentAssemblies(configuration));
            assembliesToInstrument.UnionWith(GetDependencies(configuration.AssemblyToBeAnalyzed, assembliesToInstrument));

            foreach (var assemblyPath in assembliesToInstrument)
            {
                string newAssembly = Instrument(assemblyPath);
                if (string.IsNullOrEmpty(newAssembly))
                {
                    Error.ReportAndExit($"Terminating due to VSInstr error.");
                }

                if (assemblyPath == configuration.AssemblyToBeAnalyzed)
                {
                    // Remember the location of the original assembly so we can add this to the PATH
                    // when we run the test out of proc.
                    configuration.AdditionalPaths = Path.GetDirectoryName(configuration.AssemblyToBeAnalyzed);
                    configuration.AssemblyToBeAnalyzed = newAssembly;
                }
            }
        }

        private static IEnumerable<string> GetDependencies(string assemblyToBeAnalyzed, HashSet<string> additionalAssemblies)
        {
            var fullPath = Path.GetFullPath(assemblyToBeAnalyzed);
            DependencyGraph graph = new DependencyGraph(Path.GetDirectoryName(fullPath), additionalAssemblies);
            return graph.GetDependencies(fullPath);
        }

        private static IEnumerable<string> GetInstrumentAssemblies(Configuration configuration)
        {
            var fullPath = Path.GetFullPath(configuration.AssemblyToBeAnalyzed);
            var testAssemblyPath = Path.GetDirectoryName(fullPath);
            yield return fullPath;

            Uri baseUri = new Uri(fullPath);

            IEnumerable<string> ResolveFileSpec(string spec)
            {
                var localPath = Path.GetFullPath(spec);
                if (!File.Exists(localPath))
                {
                    // If not rooted, the file path might be relative to testAssemblyPath.
                    var resolved = new Uri(baseUri, spec);
                    localPath = resolved.LocalPath;

                    if (!File.Exists(localPath))
                    {
                        Error.ReportAndExit($"Cannot find specified file for code-coverage instrumentation: '{spec}'.");
                    }
                }

                yield return localPath;
            }

            IEnumerable<string> ResolveAdditionalFiles(KeyValuePair<string, bool> kvp)
            {
                if (!kvp.Value)
                {
                    foreach (var file in ResolveFileSpec(kvp.Key))
                    {
                        yield return file;
                    }

                    yield break;
                }

                var dir = Path.GetDirectoryName(kvp.Key);
                var fullDir = Path.GetFullPath(dir.Length > 0 ? dir : testAssemblyPath);
                var listFile = Path.Combine(fullDir, Path.GetFileName(kvp.Key));
                if (!File.Exists(listFile))
                {
                    Error.ReportAndExit($"Cannot find specified list file for code-coverage instrumentation: '{kvp.Key}'.");
                }

                foreach (var spec in File.ReadAllLines(listFile))
                {
                    var trimmed = spec.Trim();
                    if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("//"))
                    {
                        foreach (var file in ResolveFileSpec(trimmed))
                        {
                            yield return file;
                        }
                    }
                }
            }

            // Note: Resolution has been deferred to here so that all empty path qualifiations, including to the list
            // file, will resolve to testAssemblyPath (as config coverage parameters may be specified before /test).
            // Return .ToList() to force iteration and return errors before we start instrumenting.
            foreach (var kvp in configuration.AdditionalCodeCoverageAssemblies)
            {
                foreach (var file in ResolveAdditionalFiles(kvp))
                {
                    yield return file;
                }
            }
        }

        private static string Instrument(string assemblyName)
        {
            int exitCode;
            string error;
            Console.WriteLine($"Instrumenting {assemblyName}");

            using (var instrProc = new Process())
            {
                instrProc.StartInfo.FileName = GetToolPath("VSInstrToolPath", "VSInstr");
                instrProc.StartInfo.Arguments = $"\"{assemblyName}\" /COVERAGE \"/OUTPUTPATH:{OutputDirectory}\"";
                instrProc.StartInfo.UseShellExecute = false;
                instrProc.StartInfo.RedirectStandardOutput = true;
                instrProc.StartInfo.RedirectStandardError = true;
                instrProc.Start();

                error = instrProc.StandardError.ReadToEnd();

                instrProc.WaitForExit();
                exitCode = instrProc.ExitCode;
            }

            if (error.StartsWith("Error"))
            {
                // sometimes VSInstr fails, provides error message and returns 0 ?!
                if (error.Contains("VSP1014"))
                {
                    Error.Report($"[Coyote] 'VSInstr' requires you build with '<DebugType>full</DebugType>' and '<DebugSymbols>true</DebugSymbols>'");
                }

                exitCode = 1;
            }

            // Exit code 0 means that the file was instrumented successfully.
            // Exit code 4 means that the file was already instrumented.
            if (exitCode != 0 && exitCode != 4)
            {
                Error.Report($"[Coyote] 'VSInstr' failed to instrument '{assemblyName}'. " + error);
                return null;
            }

            string fileName = Path.GetFileName(assemblyName);
            string newFileName = Path.Combine(OutputDirectory, fileName);
            if (!File.Exists(newFileName))
            {
                Error.Report($"[Coyote] 'VSInstr' did not produce output assembly '{newFileName}'. " + error);
                return null;
            }

            return newFileName;
        }

        /// <summary>
        /// Returns the tool path to the code coverage instrumentor.
        /// </summary>
        /// <param name="settingName">The name of the setting; also used to query the environment variables.</param>
        /// <param name="toolName">The name of the tool; used in messages only.</param>
        internal static string GetToolPath(string settingName, string toolName)
        {
            string toolPath = string.Empty;
            try
            {
                toolPath = Environment.GetEnvironmentVariable(settingName);
                if (string.IsNullOrEmpty(toolPath))
                {
#if NETFRAMEWORK
                    toolPath = ConfigurationManager.AppSettings[settingName];
#else
                    if (settingName == "VSInstrToolPath")
                    {
                        toolPath = @"$(DevEnvDir)..\..\Team Tools\Performance Tools\x64\vsinstr.exe";
                    }
                    else
                    {
                        toolPath = @"$(DevEnvDir)..\..\..\..\Shared\Common\VSPerfCollectionTools\vs2019\x64\VSPerfCmd.exe";
                    }
#endif
                }
                else
                {
                    Console.WriteLine($"{toolName} overriding app settings path with environment variable");
                }
            }
            catch (Exception)
            {
                Error.ReportAndExit($"[Coyote] required '{settingName}' value is not set in configuration file.");
            }

            if (toolPath.Contains("$(DevEnvDir)"))
            {
                var devenvDir = Environment.GetEnvironmentVariable("DevEnvDir");
                if (string.IsNullOrEmpty(devenvDir))
                {
                    Error.ReportAndExit($"[Coyote] '{toolName}' tool needs DevEnvDir variable to be set.");
                }

                toolPath = toolPath.Replace("$(DevEnvDir)", devenvDir);
            }

            if (!File.Exists(toolPath))
            {
                Error.ReportAndExit($"[Coyote] '{toolName}' tool '{toolPath}' not found.");
            }

            return toolPath;
        }

        /// <summary>
        /// Set the <see cref="OutputDirectory"/> to either the user-specified <see cref="Configuration.OutputFilePath"/>
        /// or to a unique output directory name in the same directory as <see cref="Configuration.AssemblyToBeAnalyzed"/>
        /// and starting with its name.
        /// </summary>
        internal static void SetOutputDirectory(Configuration configuration, bool makeHistory)
        {
            if (OutputDirectory.Length > 0)
            {
                return;
            }

            // Do not create the output directory yet if we have to scroll back the history first.
            OutputDirectory = Reporter.GetOutputDirectory(configuration.OutputFilePath, configuration.AssemblyToBeAnalyzed,
                "CoyoteOutput", createDir: !makeHistory);
            if (!makeHistory)
            {
                return;
            }

            // Now create the new directory.
            Directory.CreateDirectory(OutputDirectory);
        }
    }
}
