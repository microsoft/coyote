// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Coyote.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using RuntimeCompiler = Microsoft.Coyote.Runtime.CompilerServices;
using SystemCompiler = System.Runtime.CompilerServices;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Rewriting
{
    internal class TaskRewritingPass : RewritingPass
    {
        private static readonly Dictionary<string, Type> SupportedTypes = new Dictionary<string, Type>()
        {
            { CachedNameProvider.AsyncTaskMethodBuilderFullName, typeof(RuntimeCompiler.AsyncTaskMethodBuilder) },
            { CachedNameProvider.GenericAsyncTaskMethodBuilderFullName, typeof(RuntimeCompiler.AsyncTaskMethodBuilder<>) },
            { CachedNameProvider.TaskAwaiterFullName, typeof(RuntimeCompiler.TaskAwaiter) },
            { CachedNameProvider.GenericTaskAwaiterFullName, typeof(RuntimeCompiler.TaskAwaiter<>) },
            { CachedNameProvider.ConfiguredTaskAwaitableFullName, typeof(RuntimeCompiler.ConfiguredTaskAwaitable) },
            { CachedNameProvider.GenericConfiguredTaskAwaitableFullName, typeof(RuntimeCompiler.ConfiguredTaskAwaitable<>) },
            { CachedNameProvider.ConfiguredTaskAwaiterFullName, typeof(RuntimeCompiler.ConfiguredTaskAwaitable.ConfiguredTaskAwaiter) },
            { CachedNameProvider.GenericConfiguredTaskAwaiterFullName, typeof(RuntimeCompiler.ConfiguredTaskAwaitable<>.ConfiguredTaskAwaiter) },
            { CachedNameProvider.TaskExtensionsFullName, typeof(Types.TaskExtensions) },
            { CachedNameProvider.TaskFactoryFullName, typeof(Types.TaskFactory) },
            { CachedNameProvider.GenericTaskFactoryFullName, typeof(Types.TaskFactory<>) },
            { CachedNameProvider.TaskParallelFullName, typeof(Types.Parallel) },
            { CachedNameProvider.ThreadPoolFullName, typeof(Types.ThreadPool) },
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskRewritingPass"/> class.
        /// </summary>
        internal TaskRewritingPass(IEnumerable<AssemblyInfo> visitedAssemblies, ILogger logger)
            : base(visitedAssemblies, logger)
        {
        }

        /// <inheritdoc/>
        protected internal override void VisitField(FieldDefinition field)
        {
            if (this.TryRewriteCompilerType(field.FieldType, out TypeReference newFieldType))
            {
                Debug.WriteLine($"............. [-] field '{field}'");
                field.FieldType = newFieldType;
                Debug.WriteLine($"............. [+] field '{field}'");
            }
        }

        /// <inheritdoc/>
        protected internal override void VisitMethod(MethodDefinition method)
        {
            base.VisitMethod(method);

            // TODO: what if this is an override of an inherited virtual method?  For example, what if there
            // is an external base class that is a Task like type that implements a virtual GetAwaiter() that
            // is overridden by this method?
            // I don't think we can really support task-like types, as their semantics can be arbitrary,
            // but good to understand what that entails and give warnings/errors perhaps: work item #4678
            if (this.TryRewriteCompilerType(method.ReturnType, out TypeReference newReturnType))
            {
                Debug.WriteLine($"............. [-] return type '{method.ReturnType}'");
                method.ReturnType = newReturnType;
                Debug.WriteLine($"............. [+] return type '{method.ReturnType}'");
            }
        }

        /// <inheritdoc/>
        protected override void VisitVariable(VariableDefinition variable)
        {
            if (this.Method is null)
            {
                return;
            }

            if (this.TryRewriteCompilerType(variable.VariableType, out TypeReference newVariableType))
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
                    this.TryRewriteCompilerType(fd.FieldType, out TypeReference newFieldType))
                {
                    Debug.WriteLine($"............. [-] {instruction}");
                    fd.FieldType = newFieldType;
                    this.IsMethodBodyModified = true;
                    Debug.WriteLine($"............. [+] {instruction}");
                }
                else if (instruction.Operand is FieldReference fr &&
                    this.TryRewriteCompilerType(fr.FieldType, out newFieldType))
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
            if (this.TryRewriteCompilerType(type, out TypeReference newType))
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
        /// Rewrites the specified non-generic <see cref="OpCodes.Call"/> or <see cref="OpCodes.Callvirt"/> instruction.
        /// </summary>
        /// <returns>The unmodified instruction, or the newly replaced instruction.</returns>
        private Instruction VisitCallInstruction(Instruction instruction, MethodReference method)
        {
            MethodReference newMethod = this.RewriteMethodReference(method, this.Module);
            if (method.FullName == newMethod.FullName ||
                !this.TryResolve(newMethod, out MethodDefinition resolvedMethod))
            {
                // There is nothing to rewrite, return the original instruction.
                return instruction;
            }

            // Create and return the new instruction.
            Instruction newInstruction = Instruction.Create(resolvedMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, newMethod);
            newInstruction.Offset = instruction.Offset;

            Debug.WriteLine($"............. [-] {instruction}");
            this.Replace(instruction, newInstruction);
            Debug.WriteLine($"............. [+] {newInstruction}");
            return newInstruction;
        }

        /// <inheritdoc/>
        protected override ParameterDefinition RewriteParameterDefinition(ParameterDefinition parameter) =>
            new ParameterDefinition(parameter.Name, parameter.Attributes, this.RewriteTypeReference(parameter.ParameterType));

        /// <inheritdoc/>
        protected override TypeReference RewriteMethodDeclaringTypeReference(MethodReference method)
        {
            TypeReference type = method.DeclaringType;
            if (IsSupportedTaskType(type))
            {
                // Special rules apply for `Task` and `TaskCompletionSource` methods, which are replaced
                // with their `ControlledTask` and `ControlledTaskCompletionSource` counterparts.
                if (IsSupportedTaskMethod(type.Name, method.Name) ||
                    type.Name.StartsWith(CachedNameProvider.GenericTaskCompletionSourceName))
                {
                    type = this.RewriteTaskType(type);
                }
            }
            else
            {
                type = this.RewriteCompilerType(type);
            }

            return type;
        }

        /// <inheritdoc/>
        protected override TypeReference RewriteTypeReference(TypeReference type) => this.RewriteCompilerType(type);

        /// <summary>
        /// Returns the rewritten type for the specified <see cref="SystemTasks"/> type, or returns the original
        /// if there is nothing to rewrite.
        /// </summary>
        private TypeReference RewriteTaskType(TypeReference type, bool isRoot = true)
        {
            TypeReference result = type;

            string fullName = type.FullName;
            if (type is GenericInstanceType genericType)
            {
                TypeReference elementType = this.RewriteTaskType(genericType.ElementType, false);
                result = this.RewriteCompilerType(genericType, elementType);
            }
            else if (fullName == CachedNameProvider.TaskFullName)
            {
                result = this.Module.ImportReference(typeof(Types.ControlledTask));
            }
            else if (fullName == CachedNameProvider.GenericTaskFullName)
            {
                result = this.Module.ImportReference(typeof(Types.ControlledTask<>), type);
            }
            else if (fullName == CachedNameProvider.GenericTaskCompletionSourceFullName)
            {
                result = this.Module.ImportReference(typeof(Types.ControlledTaskCompletionSource<>), type);
            }

            if (isRoot && result != type)
            {
                // Try resolve the new type.
                Resolve(result);
            }

            return result;
        }

        /// <summary>
        /// Tries to return the rewritten type for the specified <see cref="SystemCompiler"/> type, or returns
        /// false if there is nothing to rewrite.
        /// </summary>
        private bool TryRewriteCompilerType(TypeReference type, out TypeReference result)
        {
            result = this.RewriteCompilerType(type);
            return result.FullName != type.FullName;
        }

        /// <summary>
        /// Returns the rewritten type for the specified <see cref="SystemCompiler"/> type, or returns the original
        /// if there is nothing to rewrite.
        /// </summary>
        private TypeReference RewriteCompilerType(TypeReference type, bool isRoot = true)
        {
            TypeReference result = type;

            string fullName = type.FullName;
            if (type is GenericInstanceType genericType)
            {
                TypeReference elementType = this.RewriteCompilerType(genericType.ElementType, false);
                result = this.RewriteCompilerType(genericType, elementType);
                result = this.Module.ImportReference(result);
            }
            else
            {
                if (SupportedTypes.TryGetValue(fullName, out Type coyoteType))
                {
                    if (coyoteType.IsGenericType)
                    {
                        result = this.Module.ImportReference(coyoteType, type);
                    }
                    else
                    {
                        result = this.Module.ImportReference(coyoteType);
                    }
                }
            }

            if (isRoot && result != type)
            {
                // Try resolve the new type.
                Resolve(result);
            }

            return result;
        }

        /// <summary>
        /// Returns the rewritten type for the specified generic <see cref="SystemCompiler"/> type, or returns
        /// the original if there is nothing to rewrite.
        /// </summary>
        private TypeReference RewriteCompilerType(GenericInstanceType type, TypeReference elementType)
        {
            GenericInstanceType result = type;
            if (type.ElementType.FullName != elementType.FullName)
            {
                // Try to rewrite the arguments of the generic type.
                result = this.Module.ImportReference(elementType) as GenericInstanceType;
                for (int idx = 0; idx < type.GenericArguments.Count; idx++)
                {
                    result.GenericArguments[idx] = this.RewriteCompilerType(type.GenericArguments[idx], false);
                }
            }

            return this.Module.ImportReference(result);
        }

        /// <summary>
        /// Checks if the specified type is a supported task type.
        /// </summary>
        private static bool IsSupportedTaskType(TypeReference type) =>
            type.Namespace == CachedNameProvider.SystemTasksNamespace &&
            (type.Name == CachedNameProvider.TaskName || type.Name.StartsWith("Task`") ||
            type.Name.StartsWith("TaskCompletionSource`"));

        /// <summary>
        /// Checks if the <see cref="SystemTasks.Task"/> method with the specified name is supported.
        /// </summary>
        private static bool IsSupportedTaskMethod(string typeName, string methodName) =>
            typeName.StartsWith(CachedNameProvider.TaskName) &&
            (methodName == "get_Factory" ||
            methodName == "get_Result" ||
            methodName == nameof(Types.ControlledTask.Run) ||
            methodName == nameof(Types.ControlledTask.Delay) ||
            methodName == nameof(Types.ControlledTask.WhenAll) ||
            methodName == nameof(Types.ControlledTask.WhenAny) ||
            methodName == nameof(Types.ControlledTask.WaitAll) ||
            methodName == nameof(Types.ControlledTask.WaitAny) ||
            methodName == nameof(Types.ControlledTask.Wait) ||
            methodName == nameof(Types.ControlledTask.GetAwaiter) ||
            methodName == nameof(Types.ControlledTask.ConfigureAwait));
    }
}
