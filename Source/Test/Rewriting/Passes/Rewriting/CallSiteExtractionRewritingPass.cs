// Copyright (c) Microsoft Corporation.
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
    /// Rewriting pass that injects callbacks to the runtime for extracting call-site information.
    /// </summary>
    internal sealed class CallSiteExtractionRewritingPass : RewritingPass
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CallSiteExtractionRewritingPass"/> class.
        /// </summary>
        internal CallSiteExtractionRewritingPass(IEnumerable<AssemblyInfo> visitedAssemblies, LogWriter logWriter)
            : base(visitedAssemblies, logWriter)
        {
        }

        /// <inheritdoc/>
        protected override void VisitMethodBody(MethodBody body)
        {
            if (this.Method is null || this.Method.IsConstructor)
            {
                return;
            }

            if (this.IsAsyncStateMachineType && this.Method.Name != "MoveNext")
            {
                // For asynchronous state machine types, only instrument the 'MoveNext' method.
                return;
            }

            // Get the first instruction in the body.
            Instruction nextInstruction = body.Instructions.FirstOrDefault();

            // Construct the instructions for notifying the runtime which method is executing.
            string methodName = GetFullyQualifiedMethodName(this.Method);
            Instruction loadStrInstruction = this.Processor.Create(OpCodes.Ldstr, methodName);

            TypeDefinition providerType = this.Module.ImportReference(typeof(Operation)).Resolve();
            MethodReference notificationMethod = providerType.Methods.FirstOrDefault(m => m.Name == nameof(Operation.RegisterCallSite));
            notificationMethod = this.Module.ImportReference(notificationMethod);
            Instruction callInstruction = this.Processor.Create(OpCodes.Call, notificationMethod);

            this.Processor.InsertBefore(nextInstruction, this.Processor.Create(OpCodes.Nop));
            this.Processor.InsertBefore(nextInstruction, loadStrInstruction);
            this.Processor.InsertBefore(nextInstruction, callInstruction);
            if (nextInstruction.OpCode != OpCodes.Nop)
            {
                this.Processor.InsertBefore(nextInstruction, this.Processor.Create(OpCodes.Nop));
            }

            this.IsMethodBodyModified = true;
        }
    }
}
