// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

using SystemCompiler = System.Runtime.CompilerServices;

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
        /// The assembly direct dependencies.
        /// </summary>
        private readonly HashSet<AssemblyInfo> Dependencies;

        /// <summary>
        /// The resolver of this assembly.
        /// </summary>
        private readonly IAssemblyResolver Resolver;

        /// <summary>
        /// The rewriting options.
        /// </summary>
        private readonly RewritingOptions Options;

        /// <summary>
        /// True if the assembly has been rewritten, else false.
        /// </summary>
        internal bool IsRewritten { get; private set; }

        /// <summary>
        /// True if the assembly has been disposed, else false.
        /// </summary>
        private bool IsDisposed;

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
            this.IsDisposed = false;

            // TODO: can we reuse it, or do we need a new one for each assembly?
            var assemblyResolver = new DefaultAssemblyResolver();

            // Add known search directories for resolving assemblies.
            assemblyResolver.AddSearchDirectory(
                Path.GetDirectoryName(typeof(Types.Threading.Tasks.Task).Assembly.Location));
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
        /// Loads and returns the topological sorted list of unique assemblies to rewrite.
        /// </summary>
        internal static IEnumerable<AssemblyInfo> LoadAssembliesToRewrite(RewritingOptions options,
            AssemblyResolveEventHandler handler)
        {
            // Add all explicitly requested assemblies.
            var assemblies = new HashSet<AssemblyInfo>();
            foreach (string path in options.AssemblyPaths)
            {
                if (!assemblies.Any(assembly => assembly.FilePath == path))
                {
                    var name = Path.GetFileName(path);
                    if (options.IsAssemblyIgnored(name))
                    {
                        throw new InvalidOperationException($"Rewriting assembly '{name}' ({path}) that is in the ignore list.");
                    }

                    assemblies.Add(new AssemblyInfo(name, path, options, handler));
                }
            }

            // Find direct dependencies to each assembly and load them, if the corresponding option is enabled.
            foreach (var assembly in assemblies)
            {
                assembly.LoadDependencies(assemblies, handler);
            }

            // Validate that all assemblies are eligible for rewriting.
            foreach (var assembly in assemblies)
            {
                assembly.ValidateAssembly();
            }

            return SortAssemblies(assemblies);
        }

        private readonly HashSet<TypeDefinition> IAsyncStateMachineTypesSet = new HashSet<TypeDefinition>();

        /// <summary>
        /// Invokes the specified analysis or transformation pass on the assembly.
        /// </summary>
        internal void Invoke(Pass pass)
        {
            pass.VisitAssembly(this);
            foreach (var module in this.Definition.Modules)
            {
                Debug.WriteLine($"....... Module: {module.Name} ({module.FileName})");
                pass.VisitModule(module);
                foreach (var type in module.GetTypes())
                {
                    Debug.WriteLine($"......... Type: {type.FullName}");

                    // Finding whether type is an object of class IAsyncStateMachine.
                    bool isAsyncStateMachineType = type.Interfaces.Any(i => i.InterfaceType.FullName == typeof(SystemCompiler.IAsyncStateMachine).FullName);
                    if (isAsyncStateMachineType)
                    {
                        // Making sure that this IAsyncStateMachine class is not already instrumented.
                        if (!this.IAsyncStateMachineTypesSet.Contains(type))
                        {
                            this.IAsyncStateMachineTypesSet.Add(type);

                            // Iterating over all methods of this IAsyncStateMachine class.
                            Collection<MethodDefinition> methods = type.Methods;
                            foreach (MethodDefinition method in methods)
                            {
                                // Instrumenting the MoveNext method of this IAsyncStateMachine class.
                                if (method.FullName.Contains("MoveNext"))
                                {
                                    var instructionList = method.Body.Instructions.ToList();
                                    var processor = method.Body.GetILProcessor();

                                    // Finding the AsyncTaskMethodBuilder field reference.
                                    FieldReference asyncTaskMethodBuilderFieldRef = null;
                                    string builderType = "AsyncTaskMethodBuilder";
                                    foreach (FieldReference field in type.Fields)
                                    {
                                        if (field.FieldType.FullName.Contains("AsyncTaskMethodBuilder"))
                                        {
                                            asyncTaskMethodBuilderFieldRef = field;
                                            break;
                                        }
                                        else if (field.FieldType.FullName.Contains("AsyncValueTaskMethodBuilder"))
                                        {
                                            asyncTaskMethodBuilderFieldRef = field;
                                            builderType = "AsyncValueTaskMethodBuilder";
                                            break;
                                        }
                                        else if (field.FieldType.FullName.Contains("AsyncVoidMethodBuilder"))
                                        {
                                            asyncTaskMethodBuilderFieldRef = field;
                                            builderType = "AsyncVoidMethodBuilder";
                                            break;
                                        }
                                    }

                                    if (asyncTaskMethodBuilderFieldRef == null)
                                    {
                                        // TODO: Fix this isses/error of non-existence of AsyncTaskMethodBuilder field reference in this IAsyncStateMachine class.
                                        // Specification.Assert(false, $"EYRYRYOR: in FN_REWRITING for type: {type}");
                                        Debug.WriteLine($"EYRYRYOR: in FN_REWRITING for type: {type}");
                                    }
                                    else
                                    {
                                        Debug.WriteLine($"SUCCESS: in FN_REWRITING for type: {type}");

                                        asyncTaskMethodBuilderFieldRef = asyncTaskMethodBuilderFieldRef.Resolve();
                                        asyncTaskMethodBuilderFieldRef = method.Module.ImportReference(asyncTaskMethodBuilderFieldRef);

                                        // Inserting call to the OnMoveNext method of AsyncTaskMethodBuilder field at the befining of the MoveNext method of this IAsyncStateMachine class.
                                        TypeDefinition asyncTaskMethodBuilderType = null;
                                        MethodReference onMoveNextMethod = null;
                                        if (builderType == "AsyncTaskMethodBuilder")
                                        {
                                            asyncTaskMethodBuilderType = method.Module.ImportReference(typeof(AsyncTaskMethodBuilder)).Resolve();
                                            onMoveNextMethod = asyncTaskMethodBuilderType.Methods.FirstOrDefault(m => m.Name is nameof(AsyncTaskMethodBuilder.OnMoveNext));
                                            onMoveNextMethod = method.Module.ImportReference(onMoveNextMethod);
                                        }
                                        else if (builderType == "AsyncValueTaskMethodBuilder")
                                        {
                                            asyncTaskMethodBuilderType = method.Module.ImportReference(typeof(AsyncValueTaskMethodBuilder)).Resolve();
                                            onMoveNextMethod = asyncTaskMethodBuilderType.Methods.FirstOrDefault(m => m.Name is nameof(AsyncValueTaskMethodBuilder.OnMoveNext));
                                            onMoveNextMethod = method.Module.ImportReference(onMoveNextMethod);
                                        }
                                        else
                                        {
                                            asyncTaskMethodBuilderType = method.Module.ImportReference(typeof(AsyncVoidMethodBuilder)).Resolve();
                                            onMoveNextMethod = asyncTaskMethodBuilderType.Methods.FirstOrDefault(m => m.Name is nameof(AsyncVoidMethodBuilder.OnMoveNext));
                                            onMoveNextMethod = method.Module.ImportReference(onMoveNextMethod);
                                        }

                                        processor.InsertBefore(method.Body.Instructions[0], processor.Create(OpCodes.Call, onMoveNextMethod));
                                        processor.InsertBefore(method.Body.Instructions[0], processor.Create(OpCodes.Ldflda, asyncTaskMethodBuilderFieldRef));
                                        processor.InsertBefore(method.Body.Instructions[0], processor.Create(OpCodes.Ldarg_0));

                                        instructionList = method.Body.Instructions.ToList();
                                    }
                                }
                            }
                        }
                    }

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
                        if (pass is RewritingPass rewritingPass && rewritingPass.IsMethodBodyModified)
                        {
                            RewritingPass.FixInstructionOffsets(method);
                            rewritingPass.IsMethodBodyModified = false;
                        }
                    }
                }
            }

            pass.CompleteVisit();
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

            this.Definition.Write(outputPath, writerParameters);
        }

        /// <summary>
        /// Applies the <see cref="RewritingSignatureAttribute"/> attribute to the assembly. This attribute
        /// indicates that the assembly has been rewritten with the current version of Coyote and contains
        /// a signature identifying the parameters used during binary rewriting of the assembly.
        /// </summary>
        internal void ApplyRewritingSignatureAttribute(Version rewriterVersion)
        {
            var signature = new AssemblySignature(this, this.Dependencies, rewriterVersion, this.Options);
            var signatureHash = signature.ComputeHash();

            CustomAttribute attribute = this.GetCustomAttribute(typeof(RewritingSignatureAttribute));
            var versionAttributeArgument = new CustomAttributeArgument(
                this.Definition.MainModule.ImportReference(typeof(string)), rewriterVersion.ToString());
            var idAttributeArgument = new CustomAttributeArgument(
                this.Definition.MainModule.ImportReference(typeof(string)), signatureHash);
            if (attribute is null)
            {
                MethodReference attributeConstructor = this.Definition.MainModule.ImportReference(
                    typeof(RewritingSignatureAttribute).GetConstructor(new Type[] { typeof(string), typeof(string) }));
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

            this.IsRewritten = true;
        }

        /// <summary>
        /// Checks if this assembly has been rewritten and, if yes, returns its version and signature.
        /// </summary>
        /// <returns>True if the assembly has been rewritten with the same signature, else false.</returns>
        private bool IsAssemblyRewritten(out string version, out string signatureHash)
        {
            CustomAttribute attribute = this.GetCustomAttribute(typeof(RewritingSignatureAttribute));
            if (attribute != null)
            {
                version = attribute.ConstructorArguments[0].Value as string;
                signatureHash = attribute.ConstructorArguments[1].Value as string;
                return true;
            }

            version = string.Empty;
            signatureHash = string.Empty;
            return false;
        }

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
        private void ValidateAssembly()
        {
            if (this.IsAssemblyRewritten(out string version, out string signatureHash))
            {
                // The assembly has been already rewritten so check if the signatures match.
                var newVersion = Assembly.GetExecutingAssembly().GetName().Version;
                var newSignature = new AssemblySignature(this, this.Dependencies, newVersion, this.Options);
                var newSignatureHash = newSignature.ComputeHash();
                if (version != newVersion.ToString())
                {
                    throw new InvalidOperationException(
                        $"Assembly '{this.Name}' has been rewritten with a different coyote version.");
                }
                else if (signatureHash != newSignatureHash)
                {
                    throw new InvalidOperationException(
                        $"Assembly '{this.Name}' has been rewritten with a different rewriting configuration.");
                }

                this.IsRewritten = true;
            }
            else if (this.IsMixedModeAssembly())
            {
                // Mono.Cecil does not support writing mixed-mode assemblies.
                throw new InvalidOperationException($"Rewriting mixed-mode assembly '{this.Name}' is not supported.");
            }
        }

        /// <summary>
        /// Loads all dependent assemblies in the local assembly path.
        /// </summary>
        private void LoadDependencies(HashSet<AssemblyInfo> assemblies, AssemblyResolveEventHandler handler)
        {
            // Get the directory associated with this assembly.
            var assemblyDir = Path.GetDirectoryName(this.FilePath);

            // Perform a non-recursive depth-first search to find all dependencies.
            var stack = new Stack<AssemblyInfo>();
            stack.Push(this);
            while (stack.Count > 0)
            {
                var assembly = stack.Pop();
                foreach (var reference in assembly.Definition.Modules.SelectMany(module => module.AssemblyReferences))
                {
                    var fileName = reference.Name + ".dll";
                    var path = Path.Combine(assemblyDir, fileName);
                    if (File.Exists(path) && !this.Options.IsAssemblyIgnored(fileName))
                    {
                        AssemblyInfo dependency = assemblies.FirstOrDefault(assembly => assembly.FilePath == path);
                        if (dependency is null && this.Options.IsRewritingDependencies)
                        {
                            var name = Path.GetFileName(path);
                            dependency = new AssemblyInfo(name, path, this.Options, handler);
                            stack.Push(dependency);
                            assemblies.Add(dependency);
                        }

                        if (dependency != null)
                        {
                            this.Dependencies.Add(dependency);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sorts the specified assemblies in topological ordering.
        /// </summary>
        private static IEnumerable<AssemblyInfo> SortAssemblies(HashSet<AssemblyInfo> assemblies)
        {
            var sortedAssemblies = new List<AssemblyInfo>();

            // Assemblies that have zero or visited dependencies.
            var nextAssemblies = new HashSet<AssemblyInfo>(
                assemblies.Where(assembly => assembly.Dependencies.Count is 0));

            // Sort the assemblies in topological ordering.
            while (nextAssemblies.Count > 0)
            {
                var nextAssembly = nextAssemblies.First();
                nextAssemblies.Remove(nextAssembly);
                sortedAssemblies.Add(nextAssembly);

                // Add all assemblies that have not been sorted yet and have all their dependencies visited
                // to the set of next assemblies to sort.
                foreach (var assembly in assemblies.Where(assembly => !sortedAssemblies.Contains(assembly)))
                {
                    if (assembly.Dependencies.IsSubsetOf(sortedAssemblies))
                    {
                        nextAssemblies.Add(assembly);
                    }
                }
            }

            if (sortedAssemblies.Count != assemblies.Count)
            {
                // There are cycles in the assembly dependencies. This should normally never
                // happen because C# does not allow cycles in assembly references.
                throw new InvalidOperationException("Detected circular assembly dependencies.");
            }

            return sortedAssemblies;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj) => obj is AssemblyInfo info && this.FullName == info.FullName;

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode() => this.FullName.GetHashCode();

        /// <summary>
        /// Returns a string that represents the current assembly.
        /// </summary>
        public override string ToString() => this.FullName;

        /// <summary>
        /// Disposes the resources held by this object.
        /// </summary>
        public void Dispose()
        {
            if (!this.IsDisposed)
            {
                this.Definition?.Dispose();
                this.Resolver?.Dispose();
                this.IsDisposed = true;
            }
        }
    }
}
