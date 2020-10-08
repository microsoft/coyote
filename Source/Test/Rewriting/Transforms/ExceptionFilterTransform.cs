// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.SystematicTesting.Interception;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using SystemCompiler = System.Runtime.CompilerServices;

namespace Microsoft.Coyote.Rewriting
{
    internal class ExceptionFilterTransform : AssemblyTransform
    {
        /// <summary>
        /// The type being transformed.
        /// </summary>
        private TypeDefinition TypeDef;

        /// <summary>
        /// Is part of an async state machine.
        /// </summary>
        private bool IsStateMachine;

        /// <summary>
        /// The current method being transformed.
        /// </summary>
        private MethodDefinition Method;

        /// <summary>
        /// Whether the current method has modified handlers.
        /// </summary>
        private bool ModifiedHandlers;

        internal ExceptionFilterTransform(ILogger log)

            : base(log)
        {
        }

        /// <inheritdoc/>
        internal override void VisitType(TypeDefinition typeDef)
        {
            this.TypeDef = typeDef;
            this.IsStateMachine = typeDef.Interfaces.Any(
                i => i.InterfaceType.FullName == typeof(System.Runtime.CompilerServices.IAsyncStateMachine).FullName);
        }

        /// <inheritdoc/>
        internal override void VisitMethod(MethodDefinition method)
        {
            this.Method = method;
            this.ModifiedHandlers = false;

            // Do exception handlers before the method instructions because they are a
            // higher level concept and it's handy to pre-process them before seeing the
            // raw instructions.
            if (method.Body.HasExceptionHandlers)
            {
                foreach (var handler in method.Body.ExceptionHandlers)
                {
                    this.VisitExceptionHandler(handler);
                }
            }

            if (this.ModifiedHandlers)
            {
                this.FixupInstructionOffsets();
            }
        }

        /// <summary>
        /// Visits the specified <see cref="ExceptionHandler"/> inside the body of the <see cref="MethodDefinition"/>
        /// that was visited by the last <see cref="VisitMethod"/>.
        /// </summary>
        /// <remarks>
        /// In the case of nested try/catch blocks the inner block is visited first before the outer block.
        /// </remarks>
        internal void VisitExceptionHandler(ExceptionHandler handler)
        {
            var handlerInstructions = GetHandlerInstructions(handler);
            if (handlerInstructions.Count == 2 && handlerInstructions[0].OpCode.Code == Code.Pop &&
                handlerInstructions[1].OpCode.Code == Code.Rethrow)
            {
                // Trivial case, the exception handler is just a rethrow.
                return;
            }

            if (this.IsStateMachine && handlerInstructions.Any(instruction => IsAsyncStateMachineInstruction(instruction)))
            {
                // We do not want to instrument the compiler generated
                // catch block of an async state machine.
                return;
            }

            if (handler.FilterStart == null)
            {
                if (handler.CatchType == null)
                {
                    // then this is a finally block, which is ok...
                    return;
                }

                var name = handler.CatchType.FullName;
                if (name == "System.Object" || name == "System.Exception" || name == "Microsoft.Coyote.RuntimeException")
                {
                    this.AddThrowIfExecutionCanceledException(handler);
                }
            }
            else
            {
                // Oh, it has a filter, then we don't care what it is we can insert a check for
                // ExecutionCanceledException at the top of this handler.
                this.AddThrowIfExecutionCanceledException(handler);
            }
        }

        private void AddThrowIfExecutionCanceledException(ExceptionHandler handler)
        {
            if (!this.ModifiedHandlers)
            {
                // A previous transform may have replaced some instructions, and if so, we need to recompute
                // the instruction indexes before we operate on the try catch.
                this.FixupInstructionOffsets();
                this.ModifiedHandlers = true;
            }

            Debug.WriteLine($"............. [+] inserting ExecutionCanceledException check into catch block");

            var handlerType = this.Method.Module.ImportReference(typeof(Microsoft.Coyote.SystematicTesting.Interception.ExceptionHandlers)).Resolve();
            MethodReference handlerMethod = (from m in handlerType.Methods where m.Name == "ThrowIfCoyoteRuntimeException" select m).FirstOrDefault();
            handlerMethod = this.Method.Module.ImportReference(handlerMethod);

            var processor = this.Method.Body.GetILProcessor();
            var newStart = Instruction.Create(OpCodes.Dup);
            var previousStart = handler.HandlerStart;
            processor.InsertBefore(handler.HandlerStart, newStart);
            processor.InsertBefore(handler.HandlerStart, Instruction.Create(OpCodes.Call, handlerMethod));
            handler.HandlerStart = newStart;

            // fix up any other handler end position that points to previousStart instruction.
            foreach (var other in this.Method.Body.ExceptionHandlers)
            {
                // we are the first (or most nested) try/catch
                if (other.TryEnd == previousStart)
                {
                    other.TryEnd = newStart;
                }

                if (other.HandlerEnd == previousStart)
                {
                    other.HandlerEnd = newStart;
                }
            }
        }

        private void FixupInstructionOffsets()
        {
            // Now because we have now inserted new code into this method, it is possible some short branch instructions
            // are now out of range, and need to be switch to long branches.  This fixes that and it also
            // recomputes instruction indexes which is also needed for valid write assembly operation.
            this.Method.Body.SimplifyMacros();
            this.Method.Body.OptimizeMacros();
        }

        private static List<Instruction> GetHandlerInstructions(ExceptionHandler handler)
        {
            if (handler.HandlerStart == null)
            {
                return null;
            }

            List<Instruction> result = new List<Instruction>();
            for (var i = handler.HandlerStart; i != handler.HandlerEnd; i = i.Next)
            {
                if (i.OpCode.Code != Code.Nop)
                {
                    result.Add(i);
                }
            }

            return result;
        }

        /// <summary>
        /// Checks if the specified instruction executes an async state machine method.
        /// </summary>
        private static bool IsAsyncStateMachineInstruction(Instruction instruction)
        {
            if (instruction.Operand is MethodReference method)
            {
                TypeReference type = method.DeclaringType;
                if ((type.FullName == typeof(AsyncTaskMethodBuilder).FullName ||
                    type.FullName == typeof(SystemCompiler.AsyncTaskMethodBuilder).FullName) &&
                    method.Name == nameof(SystemCompiler.AsyncTaskMethodBuilder.SetException))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
