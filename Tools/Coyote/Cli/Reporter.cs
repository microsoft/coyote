// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using Microsoft.Coyote.Actors.Coverage;
using Microsoft.Coyote.SystematicTesting;

namespace Microsoft.Coyote.Cli
{
    /// <summary>
    /// The Coyote testing reporter.
    /// </summary>
    internal static class Reporter
    {
        internal static string OutputDirectory = string.Empty;

        /// <summary>
        /// Emits the testing coverage report.
        /// </summary>
        /// <param name="report">TestReport.</param>
        internal static void EmitTestingCoverageReport(TestReport report)
        {
            string file = Path.GetFileNameWithoutExtension(report.Configuration.AssemblyToBeAnalyzed);
            EmitTestingCoverageOutputFiles(report, OutputDirectory, file);
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
            OutputDirectory = GetOutputDirectory(configuration.OutputFilePath, configuration.AssemblyToBeAnalyzed,
                "CoyoteOutput", createDir: !makeHistory);
            if (!makeHistory)
            {
                return;
            }

            // Now create the new directory.
            Directory.CreateDirectory(OutputDirectory);
        }

        /// <summary>
        /// Returns (and creates if it does not exist) the output directory with an optional suffix.
        /// </summary>
        internal static string GetOutputDirectory(string userOutputDir, string assemblyPath,
                string suffix = "", bool createDir = true)
        {
            string directoryPath;

            if (!string.IsNullOrEmpty(userOutputDir))
            {
                directoryPath = userOutputDir + Path.DirectorySeparatorChar;
            }
            else
            {
                var subpath = Path.GetDirectoryName(assemblyPath);
                if (subpath.Length is 0)
                {
                    subpath = ".";
                }

                directoryPath = subpath +
                    Path.DirectorySeparatorChar + "Output" + Path.DirectorySeparatorChar +
                    Path.GetFileName(assemblyPath) + Path.DirectorySeparatorChar;
            }

            if (suffix.Length > 0)
            {
                directoryPath += suffix + Path.DirectorySeparatorChar;
            }

            if (createDir)
            {
                Directory.CreateDirectory(directoryPath);
            }

            return directoryPath;
        }

        /// <summary>
        /// Emits all the testing coverage related output files.
        /// </summary>
        /// <param name="report">TestReport containing CoverageInfo.</param>
        /// <param name="directory">Output directory name, unique for this run.</param>
        /// <param name="file">Output file name.</param>
        private static void EmitTestingCoverageOutputFiles(TestReport report, string directory, string file)
        {
            var codeCoverageReporter = new ActivityCoverageReporter(report.CoverageInfo);
            var filePath = $"{directory}{file}";

            string graphFilePath = $"{filePath}.dgml";
            Console.WriteLine($"..... Writing {graphFilePath}");
            codeCoverageReporter.EmitVisualizationGraph(graphFilePath);

            string coverageFilePath = $"{filePath}.coverage.txt";
            Console.WriteLine($"..... Writing {coverageFilePath}");
            codeCoverageReporter.EmitCoverageReport(coverageFilePath);

            string serFilePath = $"{filePath}.sci";
            Console.WriteLine($"..... Writing {serFilePath}");
            report.CoverageInfo.Save(serFilePath);
        }
    }
}
