// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Coyote.Interception;
using Microsoft.Coyote.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// Rewrites Monitor.Enter, Monitor.Wait, Monitor.Pulse, Monitor.Exit to use the
    /// Coyote ControlledMonitor instead which allows systematic testing of code that
    /// uses monitors.
    /// </summary>
    internal class MonitorRewriter : AssemblyRewriter
    {
        /// <summary>
        /// The cached imported <see cref="ControlledMonitor"/> type.
        /// </summary>
        private TypeDefinition ControlledMonitorType;

        private const string MonitorClassName = "System.Threading.Monitor";

        /// <summary>
        /// Initializes a new instance of the <see cref="MonitorRewriter"/> class.
        /// </summary>
        internal MonitorRewriter(IEnumerable<AssemblyInfo> rewrittenAssemblies, ILogger logger)
            : base(rewrittenAssemblies, logger)
        {
        }

        /// <inheritdoc/>
        internal override void VisitModule(ModuleDefinition module)
        {
            this.ControlledMonitorType = null;
            base.VisitModule(module);
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
            }
        }

        /// <inheritdoc/>
        protected override Instruction VisitInstruction(Instruction instruction)
        {
            if (this.Method is null)
            {
                return instruction;
            }

            if (instruction.OpCode == OpCodes.Call &&
                instruction.Operand is MethodReference method &&
                method.DeclaringType.FullName == MonitorClassName)
            {
                var newMethod = this.GetControlledMonitorMethod(method);
                if (newMethod != null)
                {
                    // Since the method parameters match there's no need to modify the parameter setup code
                    // we can simply switch out the call.
                    Debug.WriteLine($"............. [-] call '{method}'");
                    var newInstruction = Instruction.Create(OpCodes.Call, newMethod);
                    Debug.WriteLine($"............. [+] call '{newMethod}'");
                    this.Replace(instruction, newInstruction);
                    instruction = newInstruction;
                }
            }

            return instruction;
        }

        private MethodReference GetControlledMonitorMethod(MethodReference monitorMethod)
        {
            if (this.ControlledMonitorType is null)
            {
                var tr = this.Module.ImportReference(typeof(Interception.ControlledMonitor));
                this.ControlledMonitorType = tr.Resolve();
                if (this.ControlledMonitorType is null)
                {
                    throw new InvalidOperationException("The rewriter is not finding the right version of Microsoft.Coyote.");
                }
            }

            // Ensure that we have full parameter names!
            var methodDef = monitorMethod.Resolve();

            foreach (var method in this.ControlledMonitorType.Methods)
            {
                if (method.Name == monitorMethod.Name && CheckMethodParametersMatch(method, methodDef))
                {
                    return this.Module.ImportReference(method);
                }
            }

            Debug.WriteLine($"............. [?] Monitor method not found '{monitorMethod}'");
            return null;
        }
    }
}
