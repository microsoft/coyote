// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Coyote.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Microsoft.Coyote.Rewriting
{
    internal class AspNetRewritingPass : RewritingPass
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AspNetRewritingPass"/> class.
        /// </summary>
        internal AspNetRewritingPass(IEnumerable<AssemblyInfo> visitedAssemblies, ILogger logger)
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
        protected override TypeReference RewriteMethodDeclaringTypeReference(MethodReference method)
        {
            TypeReference type = method.DeclaringType;
            if (type is GenericInstanceType genericType)
            {
                string fullName = genericType.ElementType.FullName;
                if (fullName == CachedNameProvider.WebApplicationFactoryFullName)
                {
                    type = this.Module.ImportReference(typeof(Types.WebApplicationFactory));
                }
            }

            return type;
        }

        /// <inheritdoc/>
        protected override bool IsRewritableType(TypeDefinition type)
        {
            if (type != null)
            {
                string modulePath = Path.GetFileName(type.Module.FileName);
                if (modulePath is "Microsoft.AspNetCore.Mvc.Testing.dll")
                {
                    return true;
                }
            }

            return false;
        }
    }
}
