// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Coyote.Interception;
using Microsoft.Coyote.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// Contains information for an assembly that is being rewritten.
    /// </summary>
    internal sealed class AssemblyInfo : IDisposable
    {
        /// <summary>
        /// The full name of the assembly.
        /// </summary>
        internal readonly string FullName;

        /// <summary>
        /// The name of the assembly.
        /// </summary>
        internal readonly string Name;

        /// <summary>
        /// The path to the assembly file.
        /// </summary>
        internal readonly string FilePath;

        /// <summary>
        /// The assembly definition.
        /// </summary>
        internal readonly AssemblyDefinition Definition;

        /// <summary>
        /// The assembly dependencies.
        /// </summary>
        private readonly HashSet<AssemblyInfo> Dependencies;

        /// <summary>
        /// The resolver of this assembly.
        /// </summary>
        private readonly IAssemblyResolver Resolver;

        /// <summary>
        /// The writing options.
        /// </summary>
        private readonly RewritingOptions Options;

        /// <summary>
        /// True if the assembly has been rewritten, else false.
        /// </summary>
        internal bool IsRewritten { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyInfo"/> class.
        /// </summary>
        private AssemblyInfo(string name, string path, RewritingOptions options, AssemblyResolveEventHandler handler)
        {
            this.Name = name;
            this.FilePath = path;
            this.Dependencies = new HashSet<AssemblyInfo>();
            this.Options = options;
            this.IsRewritten = false;

            // TODO: can we reuse it, or do we need a new one for each assembly?
            var assemblyResolver = new DefaultAssemblyResolver();

            // Add known search directories for resolving assemblies.
            assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(typeof(ControlledTask).Assembly.Location));
            assemblyResolver.AddSearchDirectory(this.Options.AssembliesDirectory);
            if (this.Options.DependencySearchPaths != null)
            {
                foreach (var dependencySearchPath in this.Options.DependencySearchPaths)
                {
                    assemblyResolver.AddSearchDirectory(dependencySearchPath);
                }
            }

            // Add the assembly resolution error handler.
            assemblyResolver.ResolveFailure += handler;

            this.Resolver = assemblyResolver;
            var readerParameters = new ReaderParameters()
            {
                AssemblyResolver = assemblyResolver,
                ReadSymbols = this.IsSymbolFileAvailable()
            };

            this.Definition = AssemblyDefinition.ReadAssembly(this.FilePath, readerParameters);
            this.FullName = this.Definition.FullName;
        }

        /// <summary>
        /// Loads and returns the set of assemblies to rewrite.
        /// </summary>
        internal static HashSet<AssemblyInfo> LoadAssembliesToRewrite(RewritingOptions options,
            AssemblyResolveEventHandler handler)
        {
            var result = new HashSet<AssemblyInfo>();
            var visitedPaths = new HashSet<string>();

            // Add all explicitly requested assemblies.
            foreach (string path in options.AssemblyPaths)
            {
                if (!visitedPaths.Contains(path))
                {
                    var name = Path.GetFileName(path);
                    if (options.IsAssemblyIgnored(name))
                    {
                        throw new InvalidOperationException($"Rewriting assembly '{name}' ({path}) that is in the ignore list.");
                    }

                    result.Add(new AssemblyInfo(name, path, options, handler));
                    visitedPaths.Add(path);
                }
            }

            // Add all implicit assembly dependencies, if the corresponding option is enabled.
            if (options.IsRewritingDependencies)
            {
                foreach (var assembly in result)
                {
                    result.UnionWith(assembly.LoadDependencies(handler, visitedPaths));
                }
            }

            return result;
        }

        /// <summary>
        /// Rewrites the assembly definition using the specified pass.
        /// </summary>
        internal void Rewrite(AssemblyRewriter pass)
        {
            Debug.WriteLine($"..... Applying the '{pass.GetType().Name}' pass");
            foreach (var module in this.Definition.Modules)
            {
                Debug.WriteLine($"....... Module: {module.Name} ({module.FileName})");
                pass.VisitModule(module);
                foreach (var type in module.GetTypes())
                {
                    Debug.WriteLine($"......... Type: {type.FullName}");
                    pass.VisitType(type);
                    foreach (var field in type.Fields.ToArray())
                    {
                        Debug.WriteLine($"........... Field: {field.FullName}");
                        pass.VisitField(field);
                    }

                    foreach (var method in type.Methods.ToArray())
                    {
                        if (method.Body is null)
                        {
                            continue;
                        }

                        Debug.WriteLine($"........... Method {method.FullName}");
                        pass.VisitMethod(method);
                        if (pass.ModifiedMethodBody)
                        {
                            AssemblyRewriter.FixInstructionOffsets(method);
                            pass.ModifiedMethodBody = false;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Writes the assembly to the specified output path.
        /// </summary>
        internal void Write(string outputPath)
        {
            var writerParameters = new WriterParameters()
            {
                WriteSymbols = this.IsSymbolFileAvailable(),
                SymbolWriterProvider = new PortablePdbWriterProvider()
            };

            if (!string.IsNullOrEmpty(this.Options.StrongNameKeyFile))
            {
                using FileStream fs = File.Open(this.Options.StrongNameKeyFile, FileMode.Open);
                writerParameters.StrongNameKeyPair = new StrongNameKeyPair(fs);
            }

            this.Definition.Write(outputPath, writerParameters);
        }

        /// <summary>
        /// Loads all dependent assemblies in the local assembly path.
        /// </summary>
        private HashSet<AssemblyInfo> LoadDependencies(AssemblyResolveEventHandler handler, HashSet<string> visitedPaths)
        {
            var assemblyDir = Path.GetDirectoryName(this.FilePath);
            var result = new HashSet<AssemblyInfo>();

            // Add this assembly to the visited paths.
            visitedPaths.Add(this.FilePath);

            // Perform a non-recursive depth-first search to find all dependencies.
            var stack = new Stack<AssemblyInfo>();
            stack.Push(this);
            while (stack.Count > 0)
            {
                var assembly = stack.Pop();
                foreach (var module in assembly.Definition.Modules)
                {
                    foreach (var ar in module.AssemblyReferences)
                    {
                        var fileName = ar.Name + ".dll";
                        var path = Path.Combine(assemblyDir, fileName);
                        if (!visitedPaths.Contains(path) && File.Exists(path) && !this.Options.IsAssemblyIgnored(fileName))
                        {
                            var name = Path.GetFileName(path);
                            var dependency = new AssemblyInfo(name, path, this.Options, handler);
                            result.Add(dependency);
                            stack.Push(dependency);
                            visitedPaths.Add(path);
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Applies the <see cref="CoyoteVersionAttribute"/> attribute to the assembly. This attribute
        /// indicates that the assembly has been rewritten with the current version of Coyote.
        /// </summary>
        internal void ApplyCoyoteVersionAttribute(Version rewriterVersion, Guid rewriterIdentifier)
        {
            CustomAttribute attribute = this.GetCustomAttribute(typeof(CoyoteVersionAttribute));
            var versionAttributeArgument = new CustomAttributeArgument(
                this.Definition.MainModule.ImportReference(typeof(string)), rewriterVersion.ToString());
            var idAttributeArgument = new CustomAttributeArgument(
                this.Definition.MainModule.ImportReference(typeof(string)), rewriterIdentifier.ToString());
            if (attribute is null)
            {
                MethodReference attributeConstructor = this.Definition.MainModule.ImportReference(
                    typeof(CoyoteVersionAttribute).GetConstructor(new Type[] { typeof(string), typeof(string) }));
                attribute = new CustomAttribute(attributeConstructor);
                attribute.ConstructorArguments.Add(versionAttributeArgument);
                attribute.ConstructorArguments.Add(idAttributeArgument);
                this.Definition.CustomAttributes.Add(attribute);
            }
            else
            {
                attribute.ConstructorArguments[0] = versionAttributeArgument;
                attribute.ConstructorArguments[1] = idAttributeArgument;
            }
        }

        /// <summary>
        /// Checks if the specified assembly has been already rewritten.
        /// </summary>
        /// <returns>True if the assembly has been rewritten, else false.</returns>
        internal bool IsAssemblyRewritten() => this.GetCustomAttribute(typeof(CoyoteVersionAttribute)) != null;

        /// <summary>
        /// Checks if the specified assembly is a mixed-mode assembly.
        /// </summary>
        /// <returns>True if the assembly only contains IL, else false.</returns>
        private bool IsMixedModeAssembly() =>
            this.Definition.Modules.Any(m => (m.Attributes & ModuleAttributes.ILOnly) is 0);

        /// <summary>
        /// Checks if the symbol file for the specified assembly is available.
        /// </summary>
        internal bool IsSymbolFileAvailable() => File.Exists(Path.ChangeExtension(this.FilePath, "pdb"));

        /// <summary>
        /// Returns the first found custom attribute with the specified type, if such an attribute
        /// is applied to the assembly, else null.
        /// </summary>
        private CustomAttribute GetCustomAttribute(Type attributeType) =>
            this.Definition.CustomAttributes.FirstOrDefault(
                attr => attr.AttributeType.Namespace == attributeType.Namespace &&
                attr.AttributeType.Name == attributeType.Name);

        /// <summary>
        /// Validates that the assembly can be rewritten.
        /// </summary>
        internal void ValidateAssemblyBeforeRewriting()
        {
            if (this.IsAssemblyRewritten())
            {
                // The assembly has been already rewritten by this version of Coyote, so exit with an error.
                throw new InvalidOperationException($"Assembly '{this.Name}' has been already rewritten.");
            }
            else if (this.IsMixedModeAssembly())
            {
                // Mono.Cecil does not support writing mixed-mode assemblies.
                throw new InvalidOperationException($"Rewriting mixed-mode assembly '{this.Name}' is not supported.");
            }
        }

        /// <summary>
        /// Disposes the resources held by this object.
        /// </summary>
        public void Dispose()
        {
            this.Definition?.Dispose();
            this.Resolver?.Dispose();
        }
    }
}
