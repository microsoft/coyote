// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Microsoft.Coyote.IO;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// Options for rewriting binaries.
    /// </summary>
    public class RewritingOptions
    {
        /// <summary>
        /// The directory with the assemblies to rewrite.
        /// </summary>
        public string AssembliesDirectory { get; private set; }

        /// <summary>
        /// The output directory where rewritten assemblies are placed.
        /// </summary>
        public string OutputDirectory { get; private set; }

        /// <summary>
        /// The path to the assemblies to rewrite.
        /// </summary>
        public HashSet<string> AssemblyPaths { get; private set; }

        /// <summary>
        /// True if the input assemblies are being replaced by the rewritten ones.
        /// </summary>
        public bool IsReplacingAssemblies => this.AssembliesDirectory == this.OutputDirectory;

        /// <summary>
        /// The .NET platform version that Coyote was compiled for.
        /// </summary>
        private string DotnetVersion;

        /// <summary>
        /// Path of strong name key to use for signing new assembly.
        /// </summary>
        internal string StrongNameKeyFile;

        /// <summary>
        /// The .NET platform version that Coyote was compiled for.
        /// </summary>
        internal string PlatformVersion
        {
            get => this.DotnetVersion;

            set
            {
                this.DotnetVersion = value;
                this.ResolveVariables();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RewritingOptions"/> class.
        /// </summary>
        private RewritingOptions()
        {
        }

        /// <summary>
        /// Creates a <see cref="RewritingOptions"/> instance from the specified parameters.
        /// </summary>
        public static RewritingOptions Create(string assembliesDirectory, string outputDirectory, HashSet<string> assemblyPaths) =>
            new RewritingOptions()
            {
                AssembliesDirectory = assembliesDirectory,
                OutputDirectory = outputDirectory,
                AssemblyPaths = assemblyPaths
            };

        /// <summary>
        /// Parses the <see cref="RewritingOptions"/> from the specified JSON configuration file.
        /// </summary>
        public static RewritingOptions ParseFromJSON(string configurationPath)
        {
            // TODO: replace with the new 'System.Text.Json' when .NET 5 comes out.

            var assembliesDirectory = string.Empty;
            var outputDirectory = string.Empty;
            string strongNameKeyFile = null;
            var assemblyPaths = new HashSet<string>();

            string workingDirectory = Path.GetDirectoryName(Path.GetFullPath(configurationPath)) + Path.DirectorySeparatorChar;

            try
            {
                using (FileStream fs = new FileStream(configurationPath, FileMode.Open, FileAccess.Read))
                {
                    var serializer = new DataContractJsonSerializer(typeof(JsonConfiguration));
                    JsonConfiguration configuration = (JsonConfiguration)serializer.ReadObject(fs);

                    Uri baseUri = new Uri(workingDirectory);
                    Uri resolvedUri = new Uri(baseUri, configuration.AssembliesPath);
                    assembliesDirectory = resolvedUri.LocalPath;
                    strongNameKeyFile = configuration.StrongNameKeyFile;

                    if (string.IsNullOrEmpty(configuration.OutputPath))
                    {
                        outputDirectory = assembliesDirectory;
                    }
                    else
                    {
                        resolvedUri = new Uri(baseUri, configuration.OutputPath);
                        outputDirectory = resolvedUri.LocalPath;
                    }

                    foreach (string assembly in configuration.Assemblies)
                    {
                        resolvedUri = new Uri(Path.Combine(assembliesDirectory, assembly));
                        string assemblyFileName = resolvedUri.LocalPath;
                        assemblyPaths.Add(assemblyFileName);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw new InvalidOperationException($"Unexpected JSON format in the '{configurationPath}' configuration file.\n{ex.Message}");
            }

            return new RewritingOptions()
            {
                AssembliesDirectory = assembliesDirectory,
                OutputDirectory = outputDirectory,
                AssemblyPaths = assemblyPaths,
                StrongNameKeyFile = strongNameKeyFile
            };
        }

        internal void ResolveVariables()
        {
            this.AssembliesDirectory = this.ResolvePath(this.AssembliesDirectory);
            this.OutputDirectory = this.ResolvePath(this.OutputDirectory);

            foreach (string path in this.AssemblyPaths.ToArray())
            {
                var newPath = this.ResolvePath(path);
                if (newPath != path)
                {
                    this.AssemblyPaths.Remove(path);
                    this.AssemblyPaths.Add(newPath);
                }
            }
        }

        private string ResolvePath(string path) => path.Replace("$(Platform)", this.PlatformVersion);

        /// <summary>
        /// Implements a JSON configuration object.
        /// </summary>
        /// <example>
        /// The JSON schema is:
        /// <code>
        /// {
        ///     // The directory with the assemblies to rewrite. This path is relative
        ///     // to this configuration file.
        ///     "AssembliesPath": "./bin/netcoreapp3.1",
        ///     // The output directory where rewritten assemblies are placed. This path
        ///     // is relative to this configuration file.
        ///     "OutputPath": "./bin/netcoreapp3.1/RewrittenBinaries",
        ///     // The assemblies to rewrite. The paths are relative to 'AssembliesPath'.
        ///     "Assemblies": [
        ///         "Example.exe"
        ///     ]
        /// }
        /// </code>
        /// </example>
        [DataContract]
        private class JsonConfiguration
        {
            [DataMember(Name = "AssembliesPath", IsRequired = true)]
            public string AssembliesPath { get; set; }

            [DataMember(Name = "OutputPath")]
            public string OutputPath { get; set; }

            [DataMember(Name = "Assemblies", IsRequired = true)]
            public IList<string> Assemblies { get; set; }

            [DataMember(Name = "StrongNameKeyFile")]
            public string StrongNameKeyFile { get; set; }
        }
    }
}
