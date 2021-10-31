// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Coyote.Interception;
using Microsoft.Coyote.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// A pass that rewrites the concurrent collection types to their controlled versions
    /// to enable Coyote explore interleavings during testing.
    /// </summary>
    internal class ConcurrentCollectionRewritingPass : RewritingPass
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConcurrentCollectionRewritingPass"/> class.
        /// </summary>
        internal ConcurrentCollectionRewritingPass(IEnumerable<AssemblyInfo> visitedAssemblies, ILogger logger)
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
                instruction.Operand is MethodReference methodReference)
            {
                instruction = this.VisitCallInstruction(instruction, methodReference);
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
            if (method.FullName == newMethod.FullName || !this.TryResolve(newMethod, out MethodDefinition _))
            {
                // There is nothing to rewrite, return the original instruction.
                return instruction;
            }

            // Create and return the new instruction.
            Instruction newInstruction = Instruction.Create(OpCodes.Call, newMethod);
            newInstruction.Offset = instruction.Offset;

            Debug.WriteLine($"............. [-] {instruction}");
            this.Replace(instruction, newInstruction);
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
                if (fullName == CachedNameProvider.ConcurrentBagFullName)
                {
                    type = this.Module.ImportReference(typeof(ControlledConcurrentBag));
                }
                else if (fullName == CachedNameProvider.ConcurrentDictionaryFullName)
                {
                    type = this.Module.ImportReference(typeof(ControlledConcurrentDictionary));
                }
                else if (fullName == CachedNameProvider.ConcurrentQueueFullName)
                {
                    type = this.Module.ImportReference(typeof(ControlledConcurrentQueue));
                }
                else if (fullName == CachedNameProvider.ConcurrentStackFullName)
                {
                    type = this.Module.ImportReference(typeof(ControlledConcurrentStack));
                }
            }

            return type;
        }
    }
}
