// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.Coyote.SystematicTesting.Interception;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SystemCompiler = System.Runtime.CompilerServices;

namespace Microsoft.Coyote.Rewriting
{
    internal class AsyncStateMachineTransform : AssemblyTransform
    {
        /// <summary>
        /// The type being transformed.
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
        /// Initializes a new instance of the <see cref="AsyncStateMachineTransform"/> class.
        /// </summary>
        internal AsyncStateMachineTransform(ILogger logger)
            : base(logger)
        {
        }

        /// <inheritdoc/>
        internal override void VisitType(TypeDefinition typeDef)
        {
            if (typeDef.Interfaces.Any(i => i.InterfaceType.FullName == typeof(SystemCompiler.IAsyncStateMachine).FullName))
            {
                this.TypeDef = typeDef;
            }
        }

        /// <inheritdoc/>
        internal override void VisitMethod(MethodDefinition method)
        {
            if (this.TypeDef is null || method.Name != "MoveNext")
            {
                return;
            }

            this.Method = method;
            this.Processor = method.Body.GetILProcessor();

            // Rewrite the method body instructions.
            this.VisitInstructions(method);

            FixInstructionOffsets(method);
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
                    instruction.Operand is MethodReference method)
                {
                    TypeReference type = method.DeclaringType;
                    if ((type.Namespace == CachedNameProvider.SystematicTestingNamespace ||
                        type.Namespace == CachedNameProvider.SystemTasksNamespace) &&
                        (type.Name == CachedNameProvider.ControlledTaskName || type.Name.StartsWith("ControlledTask`") ||
                        type.Name == CachedNameProvider.TaskName || type.Name.StartsWith("Task`")) &&
                        method.Name is nameof(ControlledTask.GetAwaiter))
                    {
                        Debug.WriteLine($"............. [+] insert check for uncontrolled tasks");

                        var providerType = this.Method.Module.ImportReference(typeof(ExceptionProvider)).Resolve();
                        MethodReference providerMethod = providerType.Methods.FirstOrDefault(
                            m => m.Name is nameof(ExceptionProvider.ThrowIfAwaitedTaskNotControlled));
                        providerMethod = this.Method.Module.ImportReference(providerMethod);

                        this.Processor.InsertBefore(instruction, Instruction.Create(OpCodes.Dup));
                        this.Processor.InsertBefore(instruction, Instruction.Create(OpCodes.Call, providerMethod));

                        FixInstructionOffsets(this.Method);
                    }
                }
            }
            catch (AssemblyResolutionException)
            {
                // Skip this instruction, we are only interested in types that can be resolved.
            }

            return instruction;
        }
    }
}
