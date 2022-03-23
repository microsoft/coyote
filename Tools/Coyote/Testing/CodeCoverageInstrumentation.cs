// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Instruments a binary for code coverage.
    /// </summary>
    internal static class CodeCoverageInstrumentation
    {
        internal static string OutputDirectory = string.Empty;

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
