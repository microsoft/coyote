// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Coyote.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// An abstract implementation of a pass that rewrites IL.
    /// </summary>
    internal abstract class RewritingPass : Pass
    {
        /// <summary>
        /// True if the current method body has been modified, else false.
        /// </summary>
        internal bool IsMethodBodyModified { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RewritingPass"/> class.
        /// </summary>
        protected RewritingPass(IEnumerable<AssemblyInfo> visitedAssemblies, ILogger logger)
            : base(visitedAssemblies, logger)
        {
        }

        protected static TypeReference MakeGenericType(TypeReference self, params TypeReference[] arguments)
        {
            if (self.GenericParameters.Count != arguments.Length)
            {
                throw new ArgumentException();
            }

            var instance = new GenericInstanceType(self);
            foreach (var argument in arguments)
            {
                instance.GenericArguments.Add(argument);
            }

            return instance;
        }

        protected static MethodReference MakeGenericMethod(MethodReference self, params TypeReference[] arguments)
        {
            if (self.GenericParameters.Count != arguments.Length)
            {
                throw new ArgumentException();
            }

            var instance = new GenericInstanceMethod(self);
            foreach (var argument in arguments)
            {
                instance.GenericArguments.Add(argument);
            }

            return instance;
        }

        /// <summary>
        /// Fixes the instruction offsets of the specified method.
        /// </summary>
        internal static void FixInstructionOffsets(MethodDefinition method)
        {
            // By inserting new code into the visited method, it is possible some short branch
            // instructions are now out of range, and need to be switch to long branches. This
            // fixes that and it also recomputes instruction indexes which is also needed for
            // valid write assembly operation.
            method.Body.SimplifyMacros();
            method.Body.OptimizeMacros();
        }

        /// <summary>
        /// Replaces the existing instruction with a new instruction and fixes up any
        /// branch references to the old instruction so they point to the new instruction.
        /// </summary>
        /// <param name="instruction">The instruction to be replaced.</param>
        /// <param name="newInstruction">The new instruction to be inserted.</param>
        protected void Replace(Instruction instruction, Instruction newInstruction)
        {
            this.IsMethodBodyModified = true;
            this.Processor.Replace(instruction, newInstruction);
            foreach (var i in this.Processor.Body.Instructions)
            {
                if (this.Processor.Body.HasExceptionHandlers)
                {
                    foreach (var handler in this.Processor.Body.ExceptionHandlers)
                    {
                        if (handler.TryStart == instruction)
                        {
                            handler.TryStart = newInstruction;
                        }

                        if (handler.TryEnd == instruction)
                        {
                            handler.TryEnd = newInstruction;
                        }

                        if (handler.FilterStart == instruction)
                        {
                            handler.FilterStart = newInstruction;
                        }

                        if (handler.HandlerStart == instruction)
                        {
                            handler.HandlerStart = newInstruction;
                        }

                        if (handler.HandlerEnd == instruction)
                        {
                            handler.HandlerEnd = newInstruction;
                        }
                    }
                }

                // Fix up branch instructions so they branch to the new instruction instead.
                if (i.Operand is Instruction target && target == instruction)
                {
                    i.Operand = newInstruction;
                }
                else if (i.Operand is Instruction[] targets)
                {
                    for (var j = 0; j < targets.Length; ++j)
                    {
                        if (targets[j] == instruction)
                        {
                            targets[j] = newInstruction;
                        }
                    }
                }
            }
        }
    }
}
