// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Interception;
using Microsoft.Coyote.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Microsoft.Coyote.Rewriting
{
    internal class DataRaceCheckingRewriter : AssemblyRewriter
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataRaceCheckingRewriter"/> class.
        /// </summary>
        internal DataRaceCheckingRewriter(ILogger logger)
            : base(logger)
        {
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

                // Rewrite the method body instructions.
                this.VisitInstructions(method);

                // TODO: is this required?
                FixInstructionOffsets(method);
            }
        }

        /// <inheritdoc/>
        protected override Instruction VisitInstruction(Instruction instruction)
        {
            if (this.Method is null)
            {
                return instruction;
            }

            if (instruction.OpCode == OpCodes.Newobj)
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
        private Instruction VisitNewobjInstruction(Instruction instruction)
        {
            MethodReference constructor = instruction.Operand as MethodReference;
            MethodReference newMethod = this.RewriteMethodReference(constructor, this.Module, "Create");
            if (constructor.FullName == newMethod.FullName ||
                !this.TryResolve(constructor, out MethodDefinition _))
            {
                // There is nothing to rewrite, return the original instruction.
                return instruction;
            }

            // Create and return the new instruction.
            Instruction newInstruction = Instruction.Create(OpCodes.Call, newMethod);
            newInstruction.Offset = instruction.Offset;

            Debug.WriteLine($"............. [-] {instruction}");
            this.Processor.Replace(instruction, newInstruction);
            Debug.WriteLine($"............. [+] {newInstruction}");
            return newInstruction;
        }

        /// <summary>
        /// Rewrites the specified non-generic <see cref="OpCodes.Call"/> or <see cref="OpCodes.Callvirt"/> instruction.
        /// </summary>
        /// <returns>The unmodified instruction, or the newly replaced instruction.</returns>
        private Instruction VisitCallInstruction(Instruction instruction, MethodReference method)
        {
            MethodReference newMethod = this.RewriteMethodReference(method, this.Module);
            if (method.FullName == newMethod.FullName ||
                !this.TryResolve(method, out MethodDefinition _))
            {
                // There is nothing to rewrite, return the original instruction.
                return instruction;
            }

            // Create and return the new instruction.
            Instruction newInstruction = Instruction.Create(OpCodes.Call, newMethod);
            newInstruction.Offset = instruction.Offset;

            Debug.WriteLine($"............. [-] {instruction}");
            this.Processor.Replace(instruction, newInstruction);
            Debug.WriteLine($"............. [+] {newInstruction}");
            return newInstruction;
        }

        /// <inheritdoc/>
        protected override TypeReference RewriteDeclaringTypeReference(MethodReference method)
        {
            TypeReference type = method.DeclaringType;
            if (type is GenericInstanceType genericType)
            {
                string fullName = genericType.ElementType.FullName;
                if (fullName == CachedNameProvider.GenericListFullName)
                {
                    type = this.Module.ImportReference(typeof(ControlledList));
                }
                else if (fullName == CachedNameProvider.GenericDictionaryFullName)
                {
                    type = this.Module.ImportReference(typeof(ControlledDictionary));
                }
            }

            return type;
        }
    }
}
