// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Microsoft.Coyote.Rewriting
{
    internal class ExceptionFilterTransform : AssemblyTransform
    {
        /// <summary>
        /// Console output writer.
        /// </summary>
        private readonly ConsoleLogger Log;

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

        internal ExceptionFilterTransform(ConsoleLogger log)
        {
            this.Log = log;
        }

        /// <inheritdoc/>
        internal override void VisitType(TypeDefinition typeDef)
        {
            this.TypeDef = typeDef;
            this.IsStateMachine = (from i in typeDef.Interfaces where i.InterfaceType.FullName == "System.Runtime.CompilerServices.IAsyncStateMachine" select i).Any();
        }

        /// <inheritdoc/>
        internal override void VisitMethod(MethodDefinition method)
        {
            this.Method = method;
        }

        /// <inheritdoc/>
        internal override void VisitExceptionHandler(ExceptionHandler handler)
        {
            if (this.IsStateMachine)
            {
                // these have try catch blocks that forward the caught exception over to the AsyncTaskMethodBuilder.SetException
                // and these are ok, Coyote knows about this.
                return;
            }

            // Trivial case, if the exception handler is just a rethrow!
            var handlerInstruction = GetHandlerInstructions(handler);
            if (handlerInstruction.Count == 2 && handlerInstruction[0].OpCode.Code == Code.Pop &&
                handlerInstruction[1].OpCode.Code == Code.Rethrow)
            {
                // ok then, doesn't matter what the filter is doing since it is just rethrowing anyway.
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
            Debug.WriteLine($"............. [+] inserting ExecutionCanceledException check into existing handler.");

            var handlerType = this.Method.Module.ImportReference(typeof(Microsoft.Coyote.SystematicTesting.Interception.ExceptionHandlers)).Resolve();
            MethodReference handlerMethod = (from m in handlerType.Methods where m.Name == "ThrowIfCoyoteRuntimeException" select m).FirstOrDefault();
            handlerMethod = this.Method.Module.ImportReference(handlerMethod);

            var processor = this.Method.Body.GetILProcessor();
            var newStart = Instruction.Create(OpCodes.Dup);
            processor.InsertBefore(handler.HandlerStart, newStart);
            processor.InsertBefore(handler.HandlerStart, Instruction.Create(OpCodes.Call, handlerMethod));
            handler.HandlerStart = newStart;
            if (handler.FilterStart == null)
            {
                handler.TryEnd = handler.HandlerStart;
            }
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
    }
}
