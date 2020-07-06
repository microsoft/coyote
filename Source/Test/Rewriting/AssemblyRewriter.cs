// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Coyote.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using ControlledTasks = Microsoft.Coyote.SystematicTesting.Interception;
using CoyoteTasks = Microsoft.Coyote.Tasks;
using SystemCompiler = System.Runtime.CompilerServices;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// Rewrites an assembly for systematic testing.
    /// </summary>
    internal class AssemblyRewriter
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
        /// Configuration for rewriting assemblies.
        /// </summary>
        private readonly Configuration Configuration;

        /// <summary>
        /// Cache from <see cref="SystemTasks"/> type names to types being replaced
        /// in the module that is currently being rewritten.
        /// </summary>
        private readonly Dictionary<string, TypeReference> TaskTypeCache;

        /// <summary>
        /// Cache from <see cref="SystemCompiler"/> type names to types being replaced
        /// in the module that is currently being rewritten.
        /// </summary>
        private readonly Dictionary<string, TypeReference> CompilerTypeCache;

        /// <summary>
        /// Cached generic task type name prefix.
        /// </summary>
        private const string GenericTaskTypeNamePrefix = "Task`";

        /// <summary>
        /// Initializes a new instance of the <see cref="AssemblyRewriter"/> class.
        /// </summary>
        /// <param name="configuration">The configuration for this rewriter.</param>
        private AssemblyRewriter(Configuration configuration)
        {
            this.Configuration = configuration;
            this.TaskTypeCache = new Dictionary<string, TypeReference>();
            this.CompilerTypeCache = new Dictionary<string, TypeReference>();
        }

        /// <summary>
        /// Rewrites the assemblies specified in the configuration file.
        /// </summary>
        internal static void Rewrite(string configurationFile) => Rewrite(Configuration.ParseFromJSON(configurationFile));

        /// <summary>
        /// Rewrites the assemblies specified in the configuration.
        /// </summary>
        internal static void Rewrite(Configuration configuration)
        {
            var binaryRewriter = new AssemblyRewriter(configuration);
            binaryRewriter.Rewrite();
        }

        /// <summary>
        /// Performs the rewriting.
        /// </summary>
        private void Rewrite()
        {
            string coyotePath = typeof(CoyoteTasks.AsyncTaskMethodBuilder).Assembly.Location;
            string outputDirectory = this.Configuration.IsReplacingAssemblies ?
                Path.Combine(this.Configuration.OutputDirectory, TempDirectory) : this.Configuration.OutputDirectory;
            Directory.CreateDirectory(outputDirectory);

            // make sure target path also has Microsoft.Coyote.dll
            string coyoteOutputPath = Path.Combine(this.Configuration.OutputDirectory, Path.GetFileName(coyotePath));
            if (!File.Exists(coyoteOutputPath) || File.GetLastWriteTime(coyoteOutputPath) != File.GetLastWriteTime(coyotePath))
            {
                File.Copy(coyotePath, coyoteOutputPath, true);
            }

            // Rewrite the assembly files to the output directory.
            foreach (string assemblyPath in this.Configuration.AssemblyPaths)
            {
                // Specify the search directory for resolving assemblies.
                var assemblyResolver = new DefaultAssemblyResolver();
                assemblyResolver.AddSearchDirectory(this.Configuration.AssembliesDirectory);
                assemblyResolver.AddSearchDirectory(Path.GetDirectoryName(coyotePath));
                assemblyResolver.ResolveFailure += OnResolveAssemblyFailure;

                var assembly = AssemblyDefinition.ReadAssembly(assemblyPath, new ReaderParameters()
                {
                    AssemblyResolver = assemblyResolver,
                    ReadSymbols = true
                });

                string assemblyName = Path.GetFileName(assemblyPath);
                string outputPath = Path.Combine(outputDirectory, assemblyName);

                Console.WriteLine($"... Rewriting the '{assemblyName}' assembly ({assembly.FullName})");
                foreach (var module in assembly.Modules)
                {
                    Debug.WriteLine($"..... Rewriting the '{module.Name}' module ({module.FileName})");
                    this.RewriteModule(module);
                }

                // Write the binary in the output path with portable symbols enabled.
                Console.WriteLine($"... Writing the modified '{assemblyName}' assembly to " +
                    $"{(this.Configuration.IsReplacingAssemblies ? assemblyPath : outputPath)}");
                assembly.Write(outputPath, new WriterParameters()
                {
                    WriteSymbols = true,
                    SymbolWriterProvider = new PortablePdbWriterProvider()
                });

                assembly.Dispose();
                if (this.Configuration.IsReplacingAssemblies)
                {
                    File.Copy(outputPath, assemblyPath, true);
                    string pdbFile = Path.Combine(Path.GetDirectoryName(outputPath), Path.GetFileNameWithoutExtension(outputPath) + ".pdb");
                    string targetPdbFile = Path.Combine(Path.GetDirectoryName(assemblyPath), Path.GetFileNameWithoutExtension(assemblyPath) + ".pdb");
                    File.Copy(pdbFile, targetPdbFile, true);
                }
            }

            if (this.Configuration.IsReplacingAssemblies)
            {
                // Delete the temporary output directory.
                Directory.Delete(outputDirectory, true);
            }
            else
            {
                // Copy the dependency files to the output directory.
                foreach (string dependencyPath in this.Configuration.DependencyPaths)
                {
                    string dependencyName = Path.GetFileName(dependencyPath);
                    Console.WriteLine($"... Copying the '{dependencyName}' dependency file");
                    File.Copy(dependencyPath, Path.Combine(outputDirectory, dependencyName), true);
                }
            }

            Console.WriteLine($". Done rewriting");
        }

        private static AssemblyDefinition OnResolveAssemblyFailure(object sender, AssemblyNameReference reference)
        {
            Console.WriteLine("Error resolving assembly: " + reference.FullName);
            return null;
        }

        /// <summary>
        /// Rewrites the specified module definition.
        /// </summary>
        private void RewriteModule(ModuleDefinition module)
        {
            this.TaskTypeCache.Clear();
            this.CompilerTypeCache.Clear();
            foreach (var type in module.GetTypes())
            {
                this.RewriteType(type);
            }
        }

        /// <summary>
        /// Rewrites the specified type definition.
        /// </summary>
        private void RewriteType(TypeDefinition type)
        {
            Debug.WriteLine($"....... Rewriting type '{type.FullName}'");
            foreach (var field in type.Fields)
            {
                if (this.TryGetCompilerTypeReplacement(field.FieldType, null, out TypeReference newFieldType))
                {
                    Debug.WriteLine($"......... [-] field '{field}'");
                    field.FieldType = newFieldType;
                    Debug.WriteLine($"......... [+] field '{field}'");
                }
            }

            foreach (var method in type.Methods)
            {
                this.RewriteMethod(method);
            }
        }

        /// <summary>
        /// Rewrites the specified method definition.
        /// </summary>
        private void RewriteMethod(MethodDefinition method)
        {
            Debug.WriteLine($"......... Rewriting method '{method.FullName}'");

            if (this.TryGetCompilerTypeReplacement(method.ReturnType, method, out TypeReference newReturnType))
            {
                Debug.WriteLine($"........... [-] return type '{method.ReturnType}'");
                method.ReturnType = newReturnType;
                Debug.WriteLine($"........... [+] return type '{method.ReturnType}'");
            }

            // Only non-abstract method bodies can be rewritten.
            if (!method.IsAbstract)
            {
                // TODO: Check if method.Body.ExceptionHandlers needs to be rewritten.

                ILProcessor processor = method.Body.GetILProcessor();
                foreach (var variable in method.Body.Variables)
                {
                    if (this.TryGetCompilerTypeReplacement(variable.VariableType, method, out TypeReference newVariableType))
                    {
                        Debug.WriteLine($"........... [-] variable '{variable.VariableType}'");
                        variable.VariableType = newVariableType;
                        Debug.WriteLine($"........... [+] variable '{variable.VariableType}'");
                    }
                }

                Instruction instruction = method.Body.Instructions.FirstOrDefault();
                while (instruction != null)
                {
                    if (instruction.OpCode == OpCodes.Stfld || instruction.OpCode == OpCodes.Ldfld || instruction.OpCode == OpCodes.Ldflda)
                    {
                        if (instruction.Operand is FieldDefinition fd &&
                            this.TryGetCompilerTypeReplacement(fd.FieldType, method, out TypeReference newFieldType))
                        {
                            Debug.WriteLine($"........... [-] {instruction}");
                            fd.FieldType = newFieldType;
                            Debug.WriteLine($"........... [+] {instruction}");
                        }
                        else if (instruction.Operand is FieldReference fr &&
                            this.TryGetCompilerTypeReplacement(fr.FieldType, method, out newFieldType))
                        {
                            Debug.WriteLine($"........... [-] {instruction}");
                            fr.FieldType = newFieldType;
                            Debug.WriteLine($"........... [+] {instruction}");
                        }
                    }
                    else
                    {
                        ILProcessingOperation operation = this.GetILProcessingOperation(instruction, method);
                        if (operation.Type is ILProcessingOperationType.Replace)
                        {
                            Debug.WriteLine($"........... [-] {instruction}");
                            operation.Instructions[0].Offset = instruction.Offset;
                            processor.Replace(instruction, operation.Instructions[0]);
                            instruction = operation.Instructions[0];

                            Debug.WriteLine($"........... [+] {instruction}");
                            for (int idx = 1; idx < operation.Instructions.Length; idx++)
                            {
                                Debug.WriteLine($"........... [+] {operation.Instructions[idx]}");
                                processor.InsertAfter(instruction, operation.Instructions[idx]);
                                instruction = instruction.Next;
                            }
                        }
                    }

                    instruction = instruction.Next;
                }
            }
        }

        /// <summary>
        /// Returns an <see cref="ILProcessingOperation"/> for the specified instruction.
        /// </summary>
        /// <remarks>
        /// If the returned operation has type <see cref="ILProcessingOperationType.None"/>, then there is nothing to rewrite.
        /// </remarks>
        private ILProcessingOperation GetILProcessingOperation(Instruction instruction, MethodDefinition method)
        {
            // TODO: check what we need to deal with `OpCodes.Calli`, and if we need to.
            ILProcessingOperation operation = ILProcessingOperation.None;
            if (instruction.OpCode == OpCodes.Initobj)
            {
                operation = this.GetInitobjProcessingOperation(instruction, method);
            }
            else if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                instruction.Operand is MethodReference methodReference)
            {
                operation = this.GetCallProcessingOperation(instruction.OpCode, methodReference, method);
            }

            return operation;
        }

        /// <summary>
        /// Returns an <see cref="ILProcessingOperation"/> for the specified <see cref="OpCodes.Initobj"/> instruction.
        /// </summary>
        /// <remarks>
        /// If the returned operation has type <see cref="ILProcessingOperationType.None"/>, then there is nothing to rewrite.
        /// </remarks>
        private ILProcessingOperation GetInitobjProcessingOperation(Instruction instruction, MethodDefinition method)
        {
            ILProcessingOperation operation = ILProcessingOperation.None;
            TypeReference type = instruction.Operand as TypeReference;
            if (this.TryGetCompilerTypeReplacement(type, method, out TypeReference newType))
            {
                var newInstruction = Instruction.Create(instruction.OpCode, newType);
                operation = new ILProcessingOperation(ILProcessingOperationType.Replace, newInstruction);
            }

            return operation;
        }

        /// <summary>
        /// Returns an <see cref="ILProcessingOperation"/> for the specified non-generic <see cref="OpCodes.Call"/>
        /// or <see cref="OpCodes.Callvirt"/> instruction.
        /// </summary>
        /// <remarks>
        /// If the returned operation has type <see cref="ILProcessingOperationType.None"/>, then there is nothing to rewrite.
        /// </remarks>
        private ILProcessingOperation GetCallProcessingOperation(OpCode opCode, MethodReference method, IGenericParameterProvider provider)
        {
            TypeReference newType = null;
            if (IsSystemTaskType(method.DeclaringType))
            {
                // Special rules apply for methods under the Task namespace.
                if (method.Name == nameof(SystemTasks.Task.Run) ||
                    method.Name == nameof(SystemTasks.Task.Delay) ||
                    method.Name == nameof(SystemTasks.Task.WhenAll) ||
                    method.Name == nameof(SystemTasks.Task.WhenAny) ||
                    method.Name == nameof(SystemTasks.Task.Yield) ||
                    method.Name == nameof(SystemTasks.Task.GetAwaiter))
                {
                    newType = this.GetTaskTypeReplacement(method.DeclaringType);
                }
            }
            else
            {
                newType = this.GetCompilerTypeReplacement(method.DeclaringType, provider);
                if (newType == method.DeclaringType)
                {
                    newType = null;
                }
            }

            // TODO: check if "new type is null" check is required.
            if (newType is null || !this.TryGetReplacementMethod(newType, method, out MethodReference newMethod))
            {
                // There is nothing to rewrite, return with the none operation.
                return ILProcessingOperation.None;
            }

            OpCode newOpCode = opCode;
            if (newMethod.Name == nameof(ControlledTasks.ControlledTask.GetAwaiter))
            {
                // The OpCode must change for the GetAwaiter method.
                newOpCode = OpCodes.Call;
            }

            // Create and return the new instruction.
            Instruction newInstruction = Instruction.Create(newOpCode, newMethod);
            return new ILProcessingOperation(ILProcessingOperationType.Replace, newInstruction);
        }

        /// <summary>
        /// Returns a method from the specified type that can replace the original method, if any.
        /// </summary>
        private bool TryGetReplacementMethod(TypeReference replacementType, MethodReference originalMethod, out MethodReference result)
        {
            result = null;

            TypeDefinition replacementTypeDef = replacementType.Resolve();
            if (replacementTypeDef == null)
            {
                throw new Exception(string.Format("Error resolving type: {0}", replacementType.FullName));
            }

            MethodDefinition original = originalMethod.Resolve();

            bool isGetControlledAwaiter = false;
            foreach (var replacement in replacementTypeDef.Methods)
            {
                // TODO: make sure all necessery checks are in place!
                if (!(!replacement.IsConstructor &&
                    replacement.Name == original.Name &&
                    replacement.ReturnType.IsGenericInstance == original.ReturnType.IsGenericInstance &&
                    replacement.IsPublic == original.IsPublic &&
                    replacement.IsPrivate == original.IsPrivate &&
                    replacement.IsAssembly == original.IsAssembly &&
                    replacement.IsFamilyAndAssembly == original.IsFamilyAndAssembly))
                {
                    continue;
                }

                isGetControlledAwaiter = replacement.DeclaringType.Namespace == KnownNamespaces.ControlledTasksName &&
                    replacement.Name == nameof(ControlledTasks.ControlledTask.GetAwaiter);
                if (!isGetControlledAwaiter)
                {
                    // Only check that the parameters match for non-controlled awaiter methods.
                    // That is because we do special rewriting for this method.
                    if (!CheckMethodParametersMatch(replacement, original))
                    {
                        continue;
                    }
                }

                // Import the method in the module that is being rewritten.
                result = originalMethod.Module.ImportReference(replacement);
                break;
            }

            if (result is null)
            {
                // TODO: raise an error.
                return false;
            }

            result.DeclaringType = replacementType;
            if (originalMethod is GenericInstanceMethod genericMethod)
            {
                var newGenericMethod = new GenericInstanceMethod(result);

                // The method is generic, so populate it with generic argument types and parameters.
                foreach (var arg in genericMethod.GenericArguments)
                {
                    TypeReference newArgumentType = this.GetCompilerTypeReplacement(arg, newGenericMethod);
                    newGenericMethod.GenericArguments.Add(newArgumentType);
                }

                result = newGenericMethod;
            }
            else if (isGetControlledAwaiter && originalMethod.DeclaringType is GenericInstanceType genericType)
            {
                // Special processing applies in this case, because we are converting the `Task<T>.GetAwaiter`
                // non-generic instance method to the `ControlledTask.GetAwaiter<T>` generic static method.
                var newGenericMethod = new GenericInstanceMethod(result);

                // There is only a single argument type, which must be added to ma.
                newGenericMethod.GenericArguments.Add(this.GetCompilerTypeReplacement(genericType.GenericArguments[0], newGenericMethod));

                // The single generic argument type in the task parameter must be rewritten to the same
                // generic argument type as the one in the return type of `GetAwaiter<T>`.
                var parameterType = newGenericMethod.Parameters[0].ParameterType as GenericInstanceType;
                parameterType.GenericArguments[0] = (newGenericMethod.ReturnType as GenericInstanceType).GenericArguments[0];

                result = newGenericMethod;
            }

            // Rewrite the parameters of the method, if any.
            for (int idx = 0; idx < originalMethod.Parameters.Count; idx++)
            {
                ParameterDefinition parameter = originalMethod.Parameters[idx];
                TypeReference newParameterType = this.GetCompilerTypeReplacement(parameter.ParameterType, result);
                ParameterDefinition newParameter = new ParameterDefinition(parameter.Name, parameter.Attributes, newParameterType);
                result.Parameters[idx] = newParameter;
            }

            if (result.ReturnType.Namespace != KnownNamespaces.ControlledTasksName)
            {
                result.ReturnType = this.GetCompilerTypeReplacement(originalMethod.ReturnType, result);
            }

            return originalMethod.FullName != result.FullName;
        }

        /// <summary>
        /// Checks if the the parameters of the two methods match.
        /// </summary>
        private static bool CheckMethodParametersMatch(MethodDefinition left, MethodDefinition right)
        {
            if (left.Parameters.Count != right.Parameters.Count)
            {
                return false;
            }

            for (int idx = 0; idx < right.Parameters.Count; idx++)
            {
                var originalParam = right.Parameters[0];
                var replacementParam = left.Parameters[0];
                // TODO: make sure all necessery checks are in place!
                if ((replacementParam.ParameterType.FullName != originalParam.ParameterType.FullName) ||
                    (replacementParam.Name != originalParam.Name) ||
                    (replacementParam.IsIn && !originalParam.IsIn) ||
                    (replacementParam.IsOut && !originalParam.IsOut))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns the replacement type for the specified <see cref="SystemTasks"/> type, else null.
        /// </summary>
        private TypeReference GetTaskTypeReplacement(TypeReference type)
        {
            TypeReference result;

            string fullName = type.FullName;
            if (this.TaskTypeCache.ContainsKey(fullName))
            {
                result = this.TaskTypeCache[fullName];
                if (result.Module != type.Module)
                {
                    result = type.Module.ImportReference(result);
                    this.TaskTypeCache[fullName] = result;
                }
            }
            else
            {
                result = type.Module.ImportReference(typeof(ControlledTasks.ControlledTask));
                this.TaskTypeCache[fullName] = result;
            }

            return result;
        }

        /// <summary>
        /// Tries to return the replacement type for the specified <see cref="SystemCompiler"/> type, if such a type exists.
        /// </summary>
        private bool TryGetCompilerTypeReplacement(TypeReference type, IGenericParameterProvider provider, out TypeReference result)
        {
            result = this.GetCompilerTypeReplacement(type, provider);
            return result.FullName != type.FullName;
        }

        /// <summary>
        /// Returns the replacement type for the specified <see cref="SystemCompiler"/> type, else null.
        /// </summary>
        private TypeReference GetCompilerTypeReplacement(TypeReference type, IGenericParameterProvider provider)
        {
            TypeReference result = type;

            string fullName = type.FullName;
            if (this.CompilerTypeCache.ContainsKey(fullName))
            {
                result = this.CompilerTypeCache[fullName];
                if (result.Module != type.Module)
                {
                    result = type.Module.ImportReference(result);
                    this.CompilerTypeCache[fullName] = result;
                }
            }
            else if (type.IsGenericInstance &&
                (type.Name == KnownSystemTypes.GenericAsyncTaskMethodBuilderName ||
                type.Name == KnownSystemTypes.GenericTaskAwaiterName))
            {
                result = this.GetGenericTypeReplacement(type as GenericInstanceType, provider);
                if (result.FullName != fullName)
                {
                    result = type.Module.ImportReference(result);
                    this.CompilerTypeCache[fullName] = result;
                }
            }
            else if (fullName == KnownSystemTypes.AsyncTaskMethodBuilderFullName)
            {
                result = this.ImportCompilerTypeReplacement(type, typeof(ControlledTasks.AsyncTaskMethodBuilder));
            }
            else if (fullName == KnownSystemTypes.GenericAsyncTaskMethodBuilderFullName)
            {
                result = this.ImportCompilerTypeReplacement(type, typeof(ControlledTasks.AsyncTaskMethodBuilder<>));
            }
            else if (fullName == KnownSystemTypes.TaskAwaiterFullName)
            {
                result = this.ImportCompilerTypeReplacement(type, typeof(CoyoteTasks.TaskAwaiter));
            }
            else if (fullName == KnownSystemTypes.GenericTaskAwaiterFullName)
            {
                result = this.ImportCompilerTypeReplacement(type, typeof(CoyoteTasks.TaskAwaiter<>));
            }
            else if (fullName == KnownSystemTypes.YieldAwaitableFullName)
            {
                result = this.ImportCompilerTypeReplacement(type, typeof(CoyoteTasks.YieldAwaitable));
            }
            else if (fullName == KnownSystemTypes.YieldAwaiterFullName)
            {
                result = this.ImportCompilerTypeReplacement(type, typeof(CoyoteTasks.YieldAwaitable.YieldAwaiter));
            }

            return result;
        }

        /// <summary>
        /// Import the replacement coyote type and cache it in the CompilerTypeCache and make sure the type can be
        /// fully resolved.
        /// </summary>
        private TypeReference ImportCompilerTypeReplacement(TypeReference originalType, Type coyoteType)
        {
            var result = originalType.Module.ImportReference(coyoteType);
            this.CompilerTypeCache[originalType.FullName] = result;

            TypeDefinition coyoteTypeDef = result.Resolve();
            if (coyoteTypeDef == null)
            {
                throw new Exception(string.Format("Unexpected error resolving type: {0}", coyoteType.FullName));
            }

            return result;
        }

        /// <summary>
        /// Returns the replacement type for the specified generic type, else null.
        /// </summary>
        private GenericInstanceType GetGenericTypeReplacement(GenericInstanceType type, IGenericParameterProvider provider)
        {
            GenericInstanceType result = type;
            TypeReference genericType = this.GetCompilerTypeReplacement(type.ElementType, null);
            if (type.ElementType.FullName != genericType.FullName)
            {
                // The generic type must be rewritten.
                result = new GenericInstanceType(type.Module.ImportReference(genericType));
                foreach (var arg in type.GenericArguments)
                {
                    TypeReference newArgumentType;
                    if (arg.IsGenericParameter)
                    {
                        GenericParameter parameter = new GenericParameter(arg.Name, provider ?? result);
                        result.GenericParameters.Add(parameter);
                        newArgumentType = parameter;
                    }
                    else
                    {
                        newArgumentType = this.GetCompilerTypeReplacement(arg, provider);
                    }

                    result.GenericArguments.Add(newArgumentType);
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if the specified type is the <see cref="SystemTasks.Task"/> type.
        /// </summary>
        private static bool IsSystemTaskType(TypeReference type) => type.Namespace == KnownNamespaces.SystemTasksName &&
            (type.Name == typeof(SystemTasks.Task).Name || type.Name.StartsWith(GenericTaskTypeNamePrefix));

        /// <summary>
        /// Cache of known <see cref="SystemCompiler"/> type names.
        /// </summary>
        private static class KnownSystemTypes
        {
            internal static string AsyncTaskMethodBuilderFullName { get; } = typeof(SystemCompiler.AsyncTaskMethodBuilder).FullName;
            internal static string GenericAsyncTaskMethodBuilderName { get; } = typeof(SystemCompiler.AsyncTaskMethodBuilder<>).Name;
            internal static string GenericAsyncTaskMethodBuilderFullName { get; } = typeof(SystemCompiler.AsyncTaskMethodBuilder<>).FullName;
            internal static string TaskAwaiterFullName { get; } = typeof(SystemCompiler.TaskAwaiter).FullName;
            internal static string GenericTaskAwaiterName { get; } = typeof(SystemCompiler.TaskAwaiter<>).Name;
            internal static string GenericTaskAwaiterFullName { get; } = typeof(SystemCompiler.TaskAwaiter<>).FullName;
            internal static string YieldAwaitableFullName { get; } = typeof(SystemCompiler.YieldAwaitable).FullName;
            internal static string YieldAwaiterFullName { get; } = typeof(SystemCompiler.YieldAwaitable).FullName + "/YieldAwaiter";
        }

        /// <summary>
        /// Cache of known namespace names.
        /// </summary>
        private static class KnownNamespaces
        {
            internal static string ControlledTasksName { get; } = typeof(ControlledTasks.ControlledTask).Namespace;
            internal static string SystemTasksName { get; } = typeof(SystemTasks.Task).Namespace;
        }
    }
}
