// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Coyote.IO;

namespace Microsoft.Coyote.SystematicTesting.Utilities
{
    internal class DependencyGraph
    {
        private readonly Dictionary<string, string> AssemblyNameToFullPathMap = new Dictionary<string, string>();

        public DependencyGraph(string rootFolder, HashSet<string> additionalAssemblies)
        {
            // here we assume the dependencies we need to instrument for assemblyUnderTest all live in the
            // same folder, or the user provides them with --instrument and --instrument-list options.
            var allNames = new HashSet<string>(Directory.GetFiles(rootFolder, "*.dll"));
            allNames.UnionWith(additionalAssemblies);

            // Because Assembly.GetReferencedAssemblies does not yet have the path (assembly resolution is complex), we will assume that
            // any assembly that matches a name in the executing directory is the referenced assembly that we need to also instrument.
            foreach (var path in allNames)
            {
                // Note: we cannot use allNames.ToDictionary because in some cases we have a *.exe and *.dll with the same name.
                var name = Path.GetFileNameWithoutExtension(path);
                if (!this.AssemblyNameToFullPathMap.ContainsKey(name))
                {
                    this.AssemblyNameToFullPathMap[name] = path;
                }
                else
                {
                    Debug.WriteLine("Skipping {0}", path);
                }
            }
        }

        internal string[] GetDependencies(string assemblyUnderTest)
        {
            // Get the case-normalized directory name
            var result = new HashSet<string>();
            this.GetDependencies(assemblyUnderTest, result);
            return result.ToArray();
        }

        private void GetDependencies(string assemblyPath, HashSet<string> visited)
        {
            assemblyPath = Path.GetFullPath(assemblyPath);
            var assembly = Assembly.LoadFrom(assemblyPath);
            visited.Add(assemblyPath);

            foreach (var assemblyName in assembly.GetReferencedAssemblies())
            {
                if (assemblyName.Name != "Microsoft.Coyote")
                {
                    if (this.AssemblyNameToFullPathMap.ContainsKey(assemblyName.Name))
                    {
                        var dependencyPath = this.AssemblyNameToFullPathMap[assemblyName.Name];
                        if (!visited.Contains(dependencyPath))
                        {
                            this.GetDependencies(dependencyPath, visited);
                        }
                    }
                    else if (assemblyName.Name != "mscorlib" && !assemblyName.Name.StartsWith("System"))
                    {
                        Error.Report($"Could not find dependent assembly '{assemblyName.ToString()}'");
                    }
                }
            }
        }
    }
}
