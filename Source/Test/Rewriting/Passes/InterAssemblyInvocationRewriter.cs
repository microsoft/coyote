// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;
using ControlledTasks = Microsoft.Coyote.Interception;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// Rewriting pass for invocations between assemblies.
    /// </summary>
    internal class InterAssemblyInvocationRewriter : AssemblyRewriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InterAssemblyInvocationRewriter"/> class.
        /// </summary>
        internal InterAssemblyInvocationRewriter(IEnumerable<AssemblyInfo> rewrittenAssemblies, ILogger logger)
            : base(rewrittenAssemblies, logger)
        {
        }

        /// <inheritdoc/>
        internal override void VisitType(TypeDefinition type)
        {
            this.Method = null;
            this.Processor = null;
        }

        /// <inheritdoc/>
        internal override void VisitMethod(MethodDefinition method)
        {
            this.Method = null;

            // Only non-abstract method bodies can be rewritten.
            if (method.IsAbstract)
            {
                return;
            }

            this.Method = method;
            this.Processor = method.Body.GetILProcessor();

            // Rewrite the method body instructions.
            this.VisitInstructions(method);
        }

        /// <inheritdoc/>
        protected override Instruction VisitInstruction(Instruction instruction)
        {
            if (this.Method is null)
            {
                return instruction;
            }

            try
            {
                if ((instruction.OpCode == OpCodes.Call || instruction.OpCode == OpCodes.Callvirt) &&
                    instruction.Operand is MethodReference methodReference &&
                    this.IsForeignType(methodReference.DeclaringType.Resolve()))
                {
                    TypeDefinition resolvedReturnType = methodReference.ReturnType.Resolve();
                    if (IsTaskType(resolvedReturnType, CachedNameProvider.TaskName, CachedNameProvider.SystemTasksNamespace))
                    {
                        string methodName = GetFullyQualifiedMethodName(methodReference);
                        Debug.WriteLine($"............. [+] injected returned uncontrolled task assertion for method '{methodName}'");

                        var providerType = this.Module.ImportReference(typeof(ExceptionProvider)).Resolve();
                        MethodReference providerMethod = providerType.Methods.FirstOrDefault(
                            m => m.Name is nameof(ExceptionProvider.ThrowIfReturnedTaskNotControlled));
                        providerMethod = this.Module.ImportReference(providerMethod);

                        this.Processor.InsertAfter(instruction, Instruction.Create(OpCodes.Call, providerMethod));
                        this.Processor.InsertAfter(instruction, Instruction.Create(OpCodes.Ldstr, methodName));
                        this.Processor.InsertAfter(instruction, Instruction.Create(OpCodes.Dup));

                        this.ModifiedMethodBody = true;
                    }
                    else if (methodReference.Name is "GetAwaiter" && IsTaskType(resolvedReturnType,
                        CachedNameProvider.TaskAwaiterName, CachedNameProvider.SystemCompilerNamespace))
                    {
                        var returnType = methodReference.ReturnType;
                        TypeDefinition providerType = this.Module.ImportReference(typeof(ControlledTasks.TaskAwaiter)).Resolve();
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
                                m => m.Name == "Wrap" && m.HasGenericParameters);
                            MethodReference wrapReference = this.Module.ImportReference(genericMethod);
                            wrapMethod = MakeGenericMethod(wrapReference, argType);
                        }
                        else
                        {
                            wrapMethod = providerType.Methods.FirstOrDefault(
                               m => m.Name is nameof(ControlledTasks.TaskAwaiter.Wrap));
                        }

                        wrapMethod = this.Module.ImportReference(wrapMethod);

                        Instruction newInstruction = Instruction.Create(OpCodes.Call, wrapMethod);
                        Debug.WriteLine($"............. [+] {newInstruction}");

                        this.Processor.InsertAfter(instruction, newInstruction);

                        this.ModifiedMethodBody = true;
                    }
                }
            }
            catch (AssemblyResolutionException)
            {
                // Skip this instruction, we are only interested in types that can be resolved.
            }

            return instruction;
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
