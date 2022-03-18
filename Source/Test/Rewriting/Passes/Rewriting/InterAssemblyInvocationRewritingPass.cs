// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Rewriting.Types;
using Microsoft.Coyote.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;
#if NET || NETCOREAPP3_1
using HttpClient = Microsoft.Coyote.Rewriting.Types.Net.Http.HttpClient;
#endif
using RuntimeCompiler = Microsoft.Coyote.Runtime.CompilerServices;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// Rewriting pass for invocations between assemblies.
    /// </summary>
    internal class InterAssemblyInvocationRewritingPass : RewritingPass
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InterAssemblyInvocationRewritingPass"/> class.
        /// </summary>
        internal InterAssemblyInvocationRewritingPass(IEnumerable<AssemblyInfo> visitedAssemblies, ILogger logger)
            : base(visitedAssemblies, logger)
        {
        }

        /// <inheritdoc/>
        protected override Instruction VisitInstruction(Instruction instruction)
        {
            if (this.Method is null)
            {
                return instruction;
            }

            if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                instruction.Operand is MethodReference methodReference &&
                this.IsForeignType(methodReference.DeclaringType))
            {
                TypeDefinition resolvedReturnType = methodReference.ReturnType.Resolve();
                if (IsTaskType(resolvedReturnType, NameCache.TaskName, NameCache.SystemTasksNamespace))
                {
                    string methodName = GetFullyQualifiedMethodName(methodReference);
                    Debug.WriteLine($"............. [+] returned uncontrolled task assertion for method '{methodName}'");

                    var providerType = this.Module.ImportReference(typeof(ExceptionProvider)).Resolve();
                    MethodReference providerMethod = providerType.Methods.FirstOrDefault(
                        m => m.Name is nameof(ExceptionProvider.ThrowIfReturnedTaskNotControlled));
                    providerMethod = this.Module.ImportReference(providerMethod);

                    this.Processor.InsertAfter(instruction, Instruction.Create(OpCodes.Call, providerMethod));
                    this.Processor.InsertAfter(instruction, Instruction.Create(OpCodes.Ldstr, methodName));
                    this.Processor.InsertAfter(instruction, Instruction.Create(OpCodes.Dup));
                    this.IsMethodBodyModified = true;
                }
                else if (IsTaskType(resolvedReturnType, NameCache.ValueTaskName, NameCache.SystemTasksNamespace))
                {
                    string methodName = GetFullyQualifiedMethodName(methodReference);
                    Debug.WriteLine($"............. [+] returned uncontrolled value task assertion for method '{methodName}'");

                    var providerType = this.Module.ImportReference(typeof(ExceptionProvider)).Resolve();
                    MethodReference providerMethod = providerType.Methods.FirstOrDefault(
                        m => m.Name is nameof(ExceptionProvider.ThrowIfReturnedValueTaskNotControlled));
                    providerMethod = this.Module.ImportReference(providerMethod);

                    this.Processor.InsertAfter(instruction, Instruction.Create(OpCodes.Call, providerMethod));
                    this.Processor.InsertAfter(instruction, Instruction.Create(OpCodes.Ldstr, methodName));
                    this.Processor.InsertAfter(instruction, Instruction.Create(OpCodes.Dup));
                    this.IsMethodBodyModified = true;
                }
                else if (methodReference.Name is "GetAwaiter" && IsTaskType(resolvedReturnType,
                    NameCache.TaskAwaiterName, NameCache.SystemCompilerNamespace))
                {
                    MethodReference interceptionMethod = this.CreateInterceptionMethod(
                        typeof(RuntimeCompiler.TaskAwaiter), methodReference,
                        nameof(RuntimeCompiler.TaskAwaiter.Wrap));
                    Instruction newInstruction = Instruction.Create(OpCodes.Call, interceptionMethod);
                    Debug.WriteLine($"............. [+] {newInstruction}");

                    this.Processor.InsertAfter(instruction, newInstruction);
                    this.IsMethodBodyModified = true;
                }
                else if (methodReference.Name is "GetAwaiter" && IsTaskType(resolvedReturnType,
                    NameCache.ValueTaskAwaiterName, NameCache.SystemCompilerNamespace))
                {
                    MethodReference interceptionMethod = this.CreateInterceptionMethod(
                        typeof(RuntimeCompiler.ValueTaskAwaiter), methodReference,
                        nameof(RuntimeCompiler.ValueTaskAwaiter.Wrap));
                    Instruction newInstruction = Instruction.Create(OpCodes.Call, interceptionMethod);
                    Debug.WriteLine($"............. [+] {newInstruction}");

                    this.Processor.InsertAfter(instruction, newInstruction);
                    this.IsMethodBodyModified = true;
                }
#if NET || NETCOREAPP3_1
                else if (IsSystemType(resolvedReturnType) && resolvedReturnType.FullName == NameCache.HttpClient)
                {
                    MethodReference interceptionMethod = this.CreateInterceptionMethod(
                        typeof(HttpClient), methodReference, nameof(HttpClient.Control));
                    Instruction newInstruction = Instruction.Create(OpCodes.Call, interceptionMethod);
                    Debug.WriteLine($"............. [+] {newInstruction}");

                    this.Processor.InsertAfter(instruction, newInstruction);
                    this.IsMethodBodyModified = true;
                }
#endif
            }

            return instruction;
        }

        /// <summary>
        /// Creates an interception method for the specified task awaiter type.
        /// </summary>
        private MethodReference CreateInterceptionMethod(Type type, MethodReference methodReference,
            string interceptionMethodName)
        {
            var returnType = methodReference.ReturnType;
            TypeDefinition providerType = this.Module.ImportReference(type).Resolve();
            MethodReference wrapMethod = null;
            if (returnType is GenericInstanceType rgt)
            {
                TypeReference argType;
                if (methodReference.DeclaringType is GenericInstanceType dgt)
                {
                    var returnArgType = rgt.GenericArguments.FirstOrDefault().GetElementType();
                    argType = GetGenericParameterTypeFromNamedIndex(dgt, returnArgType.FullName);
                }
                else
                {
                    argType = rgt.GenericArguments.FirstOrDefault().GetElementType();
                }

                MethodDefinition genericMethod = providerType.Methods.FirstOrDefault(
                    m => m.Name == interceptionMethodName && m.HasGenericParameters);
                MethodReference wrapReference = this.Module.ImportReference(genericMethod);
                wrapMethod = MakeGenericMethod(wrapReference, argType);
            }
            else
            {
                wrapMethod = providerType.Methods.FirstOrDefault(
                    m => m.Name == interceptionMethodName);
            }

            return this.Module.ImportReference(wrapMethod);
        }

        /// <summary>
        /// Checks if the specified type is the expected task type.
        /// </summary>
        private static bool IsTaskType(TypeDefinition type, string expectedName, string expectedNamespace)
        {
            if (type != null)
            {
                if (IsSystemType(type) && type.Namespace == expectedNamespace &&
                    (type.Name == expectedName || type.Name.StartsWith(expectedName + "`")))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the specified type is a task-like type.
        /// </summary>
        private static bool IsTaskLikeType(TypeDefinition type)
        {
            if (type is null)
            {
                return false;
            }

            var interfaceTypes = type.Interfaces.Select(i => i.InterfaceType);
            if (!interfaceTypes.Any(
                i => i.FullName is "System.Runtime.CompilerServices.INotifyCompletion" ||
                i.FullName is "System.Runtime.CompilerServices.INotifyCompletion"))
            {
                return false;
            }

            if (type.Methods.Any(m => m.Name is "get_IsCompleted"))
            {
                return true;
            }

            return false;
        }
    }
}
