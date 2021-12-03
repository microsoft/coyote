// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Rewriting.Types;
using Mono.Cecil;
using Mono.Cecil.Cil;
using RuntimeCompiler = Microsoft.Coyote.Runtime.CompilerServices;
using SystemCompiler = System.Runtime.CompilerServices;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Rewriting
{
    internal sealed class MethodBodyTypeRewritingPass : TypeRewritingPass
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MethodBodyTypeRewritingPass"/> class.
        /// </summary>
        internal MethodBodyTypeRewritingPass(RewritingOptions options, IEnumerable<AssemblyInfo> visitedAssemblies,
            ILogger logger)
            : base(options, visitedAssemblies, logger)
        {
        }

        /// <inheritdoc/>
        protected override void VisitVariable(VariableDefinition variable)
        {
            if (this.Method is null)
            {
                return;
            }

            if (this.TryRewriteType(variable.VariableType, out TypeReference newVariableType) &&
                this.TryResolve(newVariableType, out TypeDefinition newVariableDefinition) &&
                !IsStaticType(newVariableDefinition))
            {
                Debug.WriteLine($"............. [-] variable '{variable.VariableType}'");
                variable.VariableType = newVariableType;
                Debug.WriteLine($"............. [+] variable '{variable.VariableType}'");
            }
        }

        /// <inheritdoc/>
        protected override Instruction VisitInstruction(Instruction instruction)
        {
            if (this.Method is null)
            {
                return instruction;
            }

            // Note that the C# compiler is not generating `OpCodes.Calli` instructions:
            // https://docs.microsoft.com/en-us/archive/blogs/shawnfa/calli-is-not-verifiable.
            // TODO: what about ldsfld, for static fields?
            if (instruction.OpCode == OpCodes.Stfld || instruction.OpCode == OpCodes.Ldfld || instruction.OpCode == OpCodes.Ldflda)
            {
                if (instruction.Operand is FieldDefinition fd &&
                    this.TryRewriteType(fd.FieldType, out TypeReference newFieldType) &&
                    this.TryResolve(newFieldType, out TypeDefinition _))
                {
                    Debug.WriteLine($"............. [-] {instruction}");
                    fd.FieldType = newFieldType;
                    this.IsMethodBodyModified = true;
                    Debug.WriteLine($"............. [+] {instruction}");
                }
                else if (instruction.Operand is FieldReference fr &&
                    this.TryRewriteType(fr.FieldType, out newFieldType) &&
                    this.TryResolve(newFieldType, out TypeDefinition _))
                {
                    Debug.WriteLine($"............. [-] {instruction}");
                    fr.FieldType = newFieldType;
                    this.IsMethodBodyModified = true;
                    Debug.WriteLine($"............. [+] {instruction}");
                }
            }
            else if (instruction.OpCode == OpCodes.Initobj)
            {
                instruction = this.VisitInitobjInstruction(instruction);
            }
            else if (instruction.OpCode == OpCodes.Newobj)
            {
                instruction = this.VisitNewobjInstruction(instruction);
            }
            else if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                instruction.Operand is MethodReference methodReference)
            {
                instruction = this.VisitCallInstruction(instruction, methodReference);
            }

            return instruction;
        }

        /// <summary>
        /// Rewrites the specified <see cref="OpCodes.Initobj"/> instruction.
        /// </summary>
        /// <returns>The unmodified instruction, or the newly replaced instruction.</returns>
        private Instruction VisitInitobjInstruction(Instruction instruction)
        {
            TypeReference type = instruction.Operand as TypeReference;
            if (this.TryRewriteType(type, out TypeReference newType) &&
                this.TryResolve(newType, out TypeDefinition _))
            {
                var newInstruction = Instruction.Create(instruction.OpCode, newType);
                newInstruction.Offset = instruction.Offset;

                Debug.WriteLine($"............. [-] {instruction}");
                this.Replace(instruction, newInstruction);
                Debug.WriteLine($"............. [+] {newInstruction}");

                instruction = newInstruction;
            }

            return instruction;
        }

        /// <summary>
        /// Rewrites the specified <see cref="OpCodes.Newobj"/> instruction.
        /// </summary>
        /// <returns>The unmodified instruction, or the newly replaced instruction.</returns>
        private Instruction VisitNewobjInstruction(Instruction instruction)
        {
            MethodReference constructor = instruction.Operand as MethodReference;
            MethodReference newMethod = this.RewriteMethodReference(constructor, this.Module, "Create");
            if (constructor.FullName != newMethod.FullName &&
                this.TryResolve(constructor, out MethodDefinition _))
            {
                // Create and return the new instruction.
                Instruction newInstruction = Instruction.Create(OpCodes.Call, newMethod);
                newInstruction.Offset = instruction.Offset;

                Debug.WriteLine($"............. [-] {instruction}");
                this.Replace(instruction, newInstruction);
                Debug.WriteLine($"............. [+] {newInstruction}");

                instruction = newInstruction;
            }

            return instruction;
        }

        /// <summary>
        /// Rewrites the specified non-generic <see cref="OpCodes.Call"/> or <see cref="OpCodes.Callvirt"/> instruction.
        /// </summary>
        /// <returns>The unmodified instruction, or the newly replaced instruction.</returns>
        private Instruction VisitCallInstruction(Instruction instruction, MethodReference method)
        {
            MethodReference newMethod = this.RewriteMethodReference(method, this.Module);
            if (method.FullName != newMethod.FullName &&
                this.TryResolve(newMethod, out MethodDefinition resolvedMethod))
            {
                // Create and return the new instruction.
                Instruction newInstruction = Instruction.Create(resolvedMethod.IsVirtual ?
                    OpCodes.Callvirt : OpCodes.Call, newMethod);
                newInstruction.Offset = instruction.Offset;

                Debug.WriteLine($"............. [-] {instruction}");
                this.Replace(instruction, newInstruction);
                Debug.WriteLine($"............. [+] {newInstruction}");

                instruction = newInstruction;
            }

            return instruction;
        }

        /// <summary>
        /// Rewrites the specified <see cref="MethodReference"/>.
        /// </summary>
        /// <param name="method">The method reference to rewrite.</param>
        /// <param name="module">The module definition that is being visited.</param>
        /// <param name="matchName">Optional method name to match.</param>
        /// <returns>The rewritten method, or the original if it was not changed.</returns>
        private MethodReference RewriteMethodReference(MethodReference method, ModuleDefinition module, string matchName = null)
        {
            MethodReference result = method;
            TypeDefinition resolvedDeclaringType = method.DeclaringType.Resolve();
            if (!this.IsRewritableType(resolvedDeclaringType))
            {
                return result;
            }

            if (!this.TryResolve(method, out MethodDefinition resolvedMethod))
            {
                // Check if this method signature has been rewritten and, if it has, find the
                // rewritten method. The signature does not include the return type according
                // to C# rules, but the return type may have also been rewritten which is why
                // it is imperative here that we find the correct new definition.
                List<TypeReference> paramTypes = new List<TypeReference>();
                for (int i = 0; i < method.Parameters.Count; i++)
                {
                    var p = method.Parameters[i];
                    paramTypes.Add(this.RewriteTypeReference(p.ParameterType));
                }

                var newMethod = FindMatchingMethodInDeclaringType(resolvedDeclaringType, method.Name, paramTypes.ToArray());
                if (!this.TryResolve(newMethod, out resolvedMethod))
                {
                    // Unable to resolve the method or a rewritten version of this method.
                    return result;
                }
            }

            // Try to rewrite the declaring type.
            TypeReference newDeclaringType = this.RewriteMethodDeclaringTypeReference(method);
            if (!this.TryResolve(newDeclaringType, out TypeDefinition resolvedNewDeclaringType))
            {
                // Unable to resolve the declaring type of the method.
                return result;
            }

            // if (resolvedNewDeclaringType.FullName == resolvedDeclaringType.FullName &&
            //     IsSystemType(resolvedDeclaringType))
            // {
            //     // Cannot rewrite the signature of a system method.
            //     return result;
            // }

            MethodDefinition match = FindMatchingMethodInDeclaringType(resolvedNewDeclaringType, resolvedMethod, matchName);
            if (match is null)
            {
                // No matching method found.
                return result;
            }

            result = module.ImportReference(match);
            Console.WriteLine($"Method: {method.FullName}");
            Console.WriteLine($"Match: {result.FullName}");
            Console.WriteLine($">> Method: {method.Name} ({method.DeclaringType.FullName})");
            Console.WriteLine($">> Match: {result.Name} ({result.DeclaringType.FullName})");

            if (!result.HasThis && !newDeclaringType.IsGenericInstance &&
                method.HasThis && method.DeclaringType.IsGenericInstance)
            {
                // We are converting from a generic type to a non generic static type, and from a non-generic
                // method to a generic method, so we need to instantiate the generic method.
                GenericInstanceMethod genericMethodInstance = new GenericInstanceMethod(result);

                var genericArgs = new List<TypeReference>();
                if (method.DeclaringType is GenericInstanceType genericDeclaringType)
                {
                    // Populate the generic arguments with the generic declaring type arguments.
                    genericArgs.AddRange(genericDeclaringType.GenericArguments);
                    foreach (var genericArg in genericArgs)
                    {
                        genericMethodInstance.GenericArguments.Add(genericArg);
                    }
                }

                result = genericMethodInstance;
            }
            else
            {
                // This is an extra initial parameter that we have when converting an instance to a static method.
                ParameterDefinition instanceParameter = null;
                if (resolvedMethod.Parameters.Count != match.Parameters.Count)
                {
                    // We are converting from an instance method to a static method, so store the instance parameter.
                    instanceParameter = result.Parameters[0];
                }

                Console.WriteLine($">> Return: {method.ReturnType} ({method.ReturnType.IsGenericParameter})");
                Console.WriteLine($">> Return: {result.ReturnType} ({result.ReturnType.IsGenericParameter})");
                Console.WriteLine($">> Return: {method.ReturnType} ({method.ReturnType.IsGenericInstance})");
                Console.WriteLine($">> Return: {result.ReturnType} ({result.ReturnType.IsGenericInstance})");

                // Try to rewrite the return type only if it matches the original method return type.
                TypeReference newReturnType = (result.ReturnType is GenericInstanceType genericReturnType &&
                    method.ReturnType is GenericInstanceType genericMethodReturnType &&
                    genericReturnType.ElementType.FullName == genericMethodReturnType.ElementType.FullName) ||
                    result.ReturnType.FullName == method.ReturnType.FullName ?
                    this.RewriteTypeReference(method.ReturnType) : result.ReturnType;

                // TypeReference newReturnType = this.RewriteTypeReference(method.ReturnType);
                // TypeReference newReturnType = this.RewriteTypeReference(result.ReturnType);

                // Instantiate the method reference to set its generic arguments and parameters, if any.
                result = new MethodReference(result.Name, newReturnType, newDeclaringType)
                {
                    HasThis = result.HasThis,
                    ExplicitThis = result.ExplicitThis,
                    CallingConvention = result.CallingConvention
                };

                if (resolvedMethod.HasGenericParameters)
                {
                    // We need to instantiate the generic method.
                    GenericInstanceMethod genericMethodInstance = new GenericInstanceMethod(result);

                    var genericArgs = new List<TypeReference>();
                    int genericArgOffset = 0;

                    if (newDeclaringType is GenericInstanceType genericDeclaringType)
                    {
                        // Populate the generic arguments with the generic declaring type arguments.
                        genericArgs.AddRange(genericDeclaringType.GenericArguments);
                        genericArgOffset = genericDeclaringType.GenericArguments.Count;
                    }

                    if (method is GenericInstanceMethod genericInstanceMethod)
                    {
                        // Populate the generic arguments with the generic instance method arguments.
                        genericArgs.AddRange(genericInstanceMethod.GenericArguments);
                    }

                    for (int i = 0; i < resolvedMethod.GenericParameters.Count; i++)
                    {
                        var p = resolvedMethod.GenericParameters[i];
                        var j = p.Position + genericArgOffset;
                        if (j >= genericArgs.Count)
                        {
                            throw new InvalidOperationException($"Not enough generic arguments to instantiate method {method}");
                        }

                        GenericParameter parameter = new GenericParameter(p.Name, genericMethodInstance);
                        result.GenericParameters.Add(parameter);
                        genericMethodInstance.GenericParameters.Add(parameter);
                        genericMethodInstance.GenericArguments.Add(this.RewriteTypeReference(genericArgs[j]));
                    }

                    result = genericMethodInstance;
                }

                // Set the instance parameter of the method, if any.
                if (instanceParameter != null)
                {
                    result.Parameters.Add(instanceParameter);
                }

                // Set the remaining parameters of the method, if any.
                foreach (var parameter in method.Parameters)
                {
                    result.Parameters.Add(this.RewriteParameterDefinition(parameter));
                }
            }

            return module.ImportReference(result);
        }

        /// <summary>
        /// Rewrites the specified <see cref="ParameterDefinition"/>.
        /// </summary>
        /// <param name="parameter">The parameter definition to rewrite.</param>
        /// <returns>The rewritten parameter definition, or the original if it was not changed.</returns>
        private ParameterDefinition RewriteParameterDefinition(ParameterDefinition parameter) =>
            new ParameterDefinition(parameter.Name, parameter.Attributes, this.RewriteTypeReference(parameter.ParameterType));

        /// <summary>
        /// Rewrites the declaring <see cref="TypeReference"/> of the specified <see cref="MethodReference"/>.
        /// </summary>
        /// <param name="method">The method with the declaring type to rewrite.</param>
        /// <returns>The rewritten declaring type, or the original if it was not changed.</returns>
        private TypeReference RewriteMethodDeclaringTypeReference(MethodReference method)
        {
            TypeReference type = method.DeclaringType;
            // if (IsSupportedTaskType(type))
            // {
            //     // Special rules apply for `Task` and `TaskCompletionSource` methods, which are replaced
            //     // with their `Task` and `ControlledTaskCompletionSource` counterparts.
            //     if (IsSupportedTaskMethod(type.Name, method.Name) ||
            //         type.Name.StartsWith(NameCache.GenericTaskCompletionSourceName))
            //     {
            //         type = this.RewriteTaskType(type);
            //     }
            // }
            // else
            if (this.TryRewriteType(type, out TypeReference newDeclaringType) &&
                this.TryResolve(newDeclaringType, out TypeDefinition _))
            {
                type = newDeclaringType;
            }

            return type;
        }

        /// <summary>
        /// Rewrites the specified <see cref="TypeReference"/>.
        /// </summary>
        /// <param name="type">The type reference to rewrite.</param>
        /// <returns>The rewritten type reference, or the original if it was not changed.</returns>
        private TypeReference RewriteTypeReference(TypeReference type)
        {
            if (this.TryRewriteType(type, out TypeReference newType) &&
                this.TryResolve(newType, out TypeDefinition _))
            {
                type = newType;
            }

            return type;
        }

        // /// <summary>
        // /// Returns the rewritten type for the specified <see cref="SystemTasks"/> type, or returns the original
        // /// if there is nothing to rewrite.
        // /// </summary>
        // private TypeReference RewriteTaskType(TypeReference type, bool isRoot = true)
        // {
        //     TypeReference result = type;
        //     string fullName = type.FullName;
        //     if (type is GenericInstanceType genericType)
        //     {
        //         TypeReference elementType = this.RewriteTaskType(genericType.ElementType, false);
        //         result = this.RewriteCompilerType(genericType, elementType);
        //     }
        //     else if (fullName == NameCache.TaskFullName)
        //     {
        //         result = this.Module.ImportReference(typeof(Types.Task));
        //     }
        //     else if (fullName == NameCache.GenericTaskFullName)
        //     {
        //         result = this.Module.ImportReference(typeof(Types.Task<>), type);
        //     }
        //     else if (fullName == NameCache.GenericTaskCompletionSourceFullName)
        //     {
        //         result = this.Module.ImportReference(typeof(Types.ControlledTaskCompletionSource<>), type);
        //     }
        //     if (isRoot && result != type)
        //     {
        //         // Try resolve the new type.
        //         Resolve(result);
        //     }
        //     return result;
        // }

        // /// <summary>
        // /// Tries to return the rewritten type for the specified <see cref="SystemCompiler"/> type, or returns
        // /// false if there is nothing to rewrite.
        // /// </summary>
        // private bool TryRewriteCompilerType(TypeReference type, out TypeReference result)
        // {
        //     result = this.RewriteCompilerType(type);
        //     return result.FullName != type.FullName;
        // }

        // /// <summary>
        // /// Returns the rewritten type for the specified <see cref="SystemCompiler"/> type, or returns the original
        // /// if there is nothing to rewrite.
        // /// </summary>
        // private TypeReference RewriteCompilerType(TypeReference type, bool isRoot = true)
        // {
        //     TypeReference result = type;
        //     string fullName = type.FullName;
        //     if (type is GenericInstanceType genericType)
        //     {
        //         TypeReference elementType = this.RewriteCompilerType(genericType.ElementType, false);
        //         result = this.RewriteCompilerType(genericType, elementType);
        //         result = this.Module.ImportReference(result);
        //     }
        //     else
        //     {
        //         if (SupportedTypes.TryGetValue(fullName, out Type coyoteType))
        //         {
        //             if (coyoteType.IsGenericType)
        //             {
        //                 result = this.Module.ImportReference(coyoteType, type);
        //             }
        //             else
        //             {
        //                 result = this.Module.ImportReference(coyoteType);
        //             }
        //         }
        //     }
        //     if (isRoot && result != type)
        //     {
        //         // Try resolve the new type.
        //         Resolve(result);
        //     }
        //     return result;
        // }

        // /// <summary>
        // /// Returns the rewritten type for the specified generic <see cref="SystemCompiler"/> type, or returns
        // /// the original if there is nothing to rewrite.
        // /// </summary>
        // private TypeReference RewriteCompilerType(GenericInstanceType type, TypeReference elementType)
        // {
        //     GenericInstanceType result = type;
        //     if (type.ElementType.FullName != elementType.FullName)
        //     {
        //         // Try to rewrite the arguments of the generic type.
        //         result = this.Module.ImportReference(elementType) as GenericInstanceType;
        //         for (int idx = 0; idx < type.GenericArguments.Count; idx++)
        //         {
        //             result.GenericArguments[idx] = this.RewriteCompilerType(type.GenericArguments[idx], false);
        //         }
        //     }
        //     return this.Module.ImportReference(result);
        // }

        // /// <summary>
        // /// Checks if the specified type is a supported task type.
        // /// </summary>
        // private static bool IsSupportedTaskType(TypeReference type) =>
        //     type.Namespace == NameCache.SystemTasksNamespace &&
        //     (type.Name == NameCache.TaskName || type.Name.StartsWith("Task`") ||
        //     type.Name.StartsWith("TaskCompletionSource`"));

        // /// <summary>
        // /// Checks if the <see cref="SystemTasks.Task"/> method with the specified name is supported.
        // /// </summary>
        // private static bool IsSupportedTaskMethod(string typeName, string methodName) =>
        //     typeName.StartsWith(NameCache.TaskName) &&
        //     (methodName == "get_Factory" ||
        //     methodName == "get_Result" ||
        //     methodName == nameof(Types.Task.Run) ||
        //     methodName == nameof(Types.Task.Delay) ||
        //     methodName == nameof(Types.Task.WhenAll) ||
        //     methodName == nameof(Types.Task.WhenAny) ||
        //     methodName == nameof(Types.Task.WaitAll) ||
        //     methodName == nameof(Types.Task.WaitAny) ||
        //     methodName == nameof(Types.Task.Wait) ||
        //     methodName == nameof(Types.Task.GetAwaiter) ||
        //     methodName == nameof(Types.Task.ConfigureAwait));
    }
}
