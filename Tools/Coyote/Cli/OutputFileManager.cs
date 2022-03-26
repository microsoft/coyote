// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Microsoft.Coyote.Cli
{
    /// <summary>
    /// The manager for output files.
    /// </summary>
    internal static class OutputFileManager
    {
        /// <summary>
        /// Creates the output directory at either the user-specified <see cref="Configuration.OutputFilePath"/>
        /// or at the same directory as the <see cref="Configuration.AssemblyToBeAnalyzed"/>.
        /// </summary>
        internal static string CreateOutputDirectory(Configuration configuration)
        {
            string directory;
            if (!string.IsNullOrEmpty(configuration.OutputFilePath))
            {
                directory = configuration.OutputFilePath + Path.DirectorySeparatorChar;
            }
            else
            {
                var subpath = Path.GetDirectoryName(configuration.AssemblyToBeAnalyzed);
                if (subpath.Length is 0)
                {
                    subpath = ".";
                }

                directory = subpath +
                    Path.DirectorySeparatorChar + "Output" + Path.DirectorySeparatorChar +
                    Path.GetFileName(configuration.AssemblyToBeAnalyzed) + Path.DirectorySeparatorChar;
            }

            string suffix = "CoyoteOutput";
            if (suffix.Length > 0)
            {
                directory += suffix + Path.DirectorySeparatorChar;
            }

            Directory.CreateDirectory(directory);
            return directory;
        }

        /// <summary>
        /// Returns the filename with the next available index appended to it.
        /// </summary>
        internal static string GetResolvedFileName(string fileName, string directory)
        {
            int index = 0;
            Regex match = new Regex("^(.*)_([0-9]+)");
            fileName = Path.GetFileNameWithoutExtension(fileName);
            foreach (var path in Directory.GetFiles(directory))
            {
                string name = Path.GetFileName(path);
                if (name.StartsWith(fileName))
                {
                    var result = match.Match(name);
                    if (result.Success)
                    {
                        string value = result.Groups[2].Value;
                        if (int.TryParse(value, out int i))
                        {
                            index = Math.Max(index, i + 1);
                        }
                    }
                }
            }

            return fileName + "_" + index;
        }
    }
}
