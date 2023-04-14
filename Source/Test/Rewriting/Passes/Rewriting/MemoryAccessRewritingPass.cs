﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// Rewriting pass that adds scheduling points at memory-access locations.
    /// </summary>
    internal class MemoryAccessRewritingPass : RewritingPass
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryAccessRewritingPass"/> class.
        /// </summary>
        internal MemoryAccessRewritingPass(IEnumerable<AssemblyInfo> visitedAssemblies, LogWriter logWriter)
            : base(visitedAssemblies, logWriter)
        {
        }

        /// <inheritdoc/>
        protected internal override void VisitMethod(MethodDefinition method)
        {
            if (this.IsAsyncStateMachineType ||
                method is null || method.IsConstructor ||
                method.IsGetter || method.IsSetter)
            {
                return;
            }

            base.VisitMethod(method);
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
                if (instruction.OpCode == OpCodes.Ldfld || instruction.OpCode == OpCodes.Stfld)
                {
                    this.LogWriter.LogDebug("............. [+] injected scheduling point at field-access instruction");

                    TypeDefinition providerType = this.Method.Module.ImportReference(typeof(SchedulingPoint)).Resolve();
                    MethodReference providerMethod = providerType.Methods.FirstOrDefault(m => m.Name is nameof(SchedulingPoint.InterleaveMemoryAccess));
                    providerMethod = this.Method.Module.ImportReference(providerMethod);

                    if (instruction.Previous != null && instruction.Previous.OpCode == OpCodes.Volatile)
                    {
                        this.Processor.InsertBefore(instruction.Previous, Instruction.Create(OpCodes.Call, providerMethod));
                    }
                    else
                    {
                        this.Processor.InsertBefore(instruction, Instruction.Create(OpCodes.Call, providerMethod));
                    }

                    this.IsMethodBodyModified = true;
                }
                else if (instruction.OpCode == OpCodes.Brfalse || instruction.OpCode == OpCodes.Brfalse_S ||
                    instruction.OpCode == OpCodes.Brtrue || instruction.OpCode == OpCodes.Brtrue_S)
                {
                    this.LogWriter.LogDebug("............. [+] injected scheduling point at branching instruction");

                    TypeDefinition providerType = this.Method.Module.ImportReference(typeof(SchedulingPoint)).Resolve();
                    MethodReference providerMethod = providerType.Methods.FirstOrDefault(m => m.Name is nameof(SchedulingPoint.InterleaveControlFlow));
                    providerMethod = this.Method.Module.ImportReference(providerMethod);
                    this.Processor.InsertBefore(instruction, Instruction.Create(OpCodes.Call, providerMethod));

                    this.IsMethodBodyModified = true;
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
