﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using ControlledTasks = Microsoft.Coyote.SystematicTesting.Interception;
using CoyoteTasks = Microsoft.Coyote.Tasks;
using SystemCompiler = System.Runtime.CompilerServices;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Rewriting
{
    internal class TaskTransform : AssemblyTransform
    {
        /// <summary>
        /// The current module being transformed.
        /// </summary>
        private ModuleDefinition Module;

        /// <summary>
        /// The current type being transformed.
        /// </summary>
        private TypeDefinition TypeDef;

        /// <summary>
        /// The current method being transformed.
        /// </summary>
        private MethodDefinition Method;

        /// <summary>
        /// A helper class for editing method body.
        /// </summary>
        private ILProcessor Processor;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskTransform"/> class.
        /// </summary>
        internal TaskTransform(ILogger logger)
            : base(logger)
        {
        }

        /// <inheritdoc/>
        internal override void VisitModule(ModuleDefinition module)
        {
            this.Module = module;
        }

        /// <inheritdoc/>
        internal override void VisitType(TypeDefinition type)
        {
            this.TypeDef = type;
            this.Method = null;
            this.Processor = null;
        }

        /// <inheritdoc/>
        internal override void VisitField(FieldDefinition field)
        {
            if (this.TryRewriteCompilerType(field.FieldType, out TypeReference newFieldType))
            {
                Debug.WriteLine($"............. [-] field '{field}'");
                field.FieldType = newFieldType;
                Debug.WriteLine($"............. [+] field '{field}'");
            }
        }

        /// <inheritdoc/>
        internal override void VisitMethod(MethodDefinition method)
        {
            this.Method = null;

            // Only non-abstract method bodies can be rewritten.
            if (!method.IsAbstract)
            {
                this.Method = method;
                this.Processor = method.Body.GetILProcessor();

                // rewrite the variable declarations.
                this.VisitVariables(method);

                // Rewrite the method body instructions.
                this.VisitInstructions(method);
            }

            // bugbug: what if this is an override of an inherited virtual method?  For example, what if there
            // is an external base class that is a Task like type that implements a virtual GetAwaiter() that
            // is overridden by this method?
            // answer: I don't think we can really support task-like types, as their semantics can be arbitrary,
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
                    Debug.WriteLine($"............. [+] {instruction}");
                }
                else if (instruction.Operand is FieldReference fr &&
                    this.TryRewriteCompilerType(fr.FieldType, out newFieldType))
                {
                    Debug.WriteLine($"............. [-] {instruction}");
                    fr.FieldType = newFieldType;
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
        /// Transforms the specified <see cref="OpCodes.Initobj"/> instruction.
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
                this.Processor.Replace(instruction, newInstruction);
                Debug.WriteLine($"............. [+] {newInstruction}");
                instruction = newInstruction;
            }

            return instruction;
        }

        /// <summary>
        /// Transforms the specified non-generic <see cref="OpCodes.Call"/> or <see cref="OpCodes.Callvirt"/> instruction.
        /// </summary>
        /// <returns>The unmodified instruction, or the newly replaced instruction.</returns>
        private Instruction VisitCallInstruction(Instruction instruction, MethodReference method)
        {
            MethodReference newMethod = this.RewriteMethodReference(method, this.Module);
            if (method.FullName == newMethod.FullName ||
                !this.TryResolve(method, out MethodDefinition resolvedMethod))
            {
                // There is nothing to rewrite, return the original instruction.
                return instruction;
            }

            // Create and return the new instruction.
            Instruction newInstruction = Instruction.Create(resolvedMethod.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, newMethod);
            newInstruction.Offset = instruction.Offset;

            Debug.WriteLine($"............. [-] {instruction}");
            this.Processor.Replace(instruction, newInstruction);
            Debug.WriteLine($"............. [+] {newInstruction}");
            return newInstruction;
        }

        /// <inheritdoc/>
        protected override ParameterDefinition RewriteParameterDefinition(ParameterDefinition parameter) =>
            new ParameterDefinition(parameter.Name, parameter.Attributes, this.RewriteTypeReference(parameter.ParameterType));

        /// <inheritdoc/>
        protected override TypeReference RewriteDeclaringTypeReference(MethodReference method)
        {
            TypeReference result = method.DeclaringType;
            if (IsSystemTaskType(result))
            {
                // Special rules apply for `Task` methods, which are replaced with their `ControlledTask` counterparts.
                if (IsSupportedTaskMethod(method.Name))
                {
                    result = this.RewriteTaskType(result);
                }
            }
            else
            {
                result = this.RewriteCompilerType(result);
            }

            return result;
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
                result = this.Module.ImportReference(typeof(ControlledTasks.ControlledTask));
            }
            else if (fullName == CachedNameProvider.GenericTaskFullName)
            {
                result = this.Module.ImportReference(typeof(ControlledTasks.ControlledTask<>), type);
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
            else if (fullName == CachedNameProvider.AsyncTaskMethodBuilderFullName)
            {
                result = this.Module.ImportReference(typeof(ControlledTasks.AsyncTaskMethodBuilder));
            }
            else if (fullName == CachedNameProvider.GenericAsyncTaskMethodBuilderFullName)
            {
                result = this.Module.ImportReference(typeof(ControlledTasks.AsyncTaskMethodBuilder<>), type);
            }
            else if (fullName == CachedNameProvider.TaskAwaiterFullName)
            {
                result = this.Module.ImportReference(typeof(CoyoteTasks.TaskAwaiter));
            }
            else if (fullName == CachedNameProvider.GenericTaskAwaiterFullName)
            {
                result = this.Module.ImportReference(typeof(CoyoteTasks.TaskAwaiter<>), type);
            }
            else if (fullName == CachedNameProvider.YieldAwaitableFullName)
            {
                result = this.Module.ImportReference(typeof(CoyoteTasks.YieldAwaitable));
            }
            else if (fullName == CachedNameProvider.YieldAwaiterFullName)
            {
                result = this.Module.ImportReference(typeof(CoyoteTasks.YieldAwaitable.YieldAwaiter));
            }
            else if (fullName == CachedNameProvider.TaskExtensionsFullName)
            {
                result = this.Module.ImportReference(typeof(ControlledTasks.TaskExtensions), type);
            }
            else if (fullName == CachedNameProvider.TaskFactoryFullName)
            {
                result = this.Module.ImportReference(typeof(ControlledTasks.TaskFactory));
            }
            else if (fullName == CachedNameProvider.GenericTaskFactoryFullName)
            {
                result = this.Module.ImportReference(typeof(ControlledTasks.TaskFactory<>), type);
            }
            else if (fullName == CachedNameProvider.TaskParallelFullName)
            {
                result = this.Module.ImportReference(typeof(ControlledTasks.Parallel));
            }
            else if (fullName == CachedNameProvider.ThreadPoolFullName)
            {
                result = this.Module.ImportReference(typeof(ControlledTasks.ThreadPool));
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
        /// Checks if the specified type is the <see cref="SystemTasks.Task"/> type.
        /// </summary>
        private static bool IsSystemTaskType(TypeReference type) => type.Namespace == CachedNameProvider.SystemTasksNamespace &&
            (type.Name == typeof(SystemTasks.Task).Name || type.Name.StartsWith("Task`"));

        /// <summary>
        /// Checks if the <see cref="SystemTasks.Task"/> method with the specified name is supported.
        /// </summary>
        private static bool IsSupportedTaskMethod(string name) =>
            name == "get_Factory" ||
            name == "get_Result" ||
            name == nameof(ControlledTasks.ControlledTask.Run) ||
            name == nameof(ControlledTasks.ControlledTask.Delay) ||
            name == nameof(ControlledTasks.ControlledTask.WhenAll) ||
            name == nameof(ControlledTasks.ControlledTask.WhenAny) ||
            name == nameof(ControlledTasks.ControlledTask.WaitAll) ||
            name == nameof(ControlledTasks.ControlledTask.WaitAny) ||
            name == nameof(ControlledTasks.ControlledTask.Wait) ||
            name == nameof(ControlledTasks.ControlledTask.Yield) ||
            name == nameof(ControlledTasks.ControlledTask.GetAwaiter);
    }
}
