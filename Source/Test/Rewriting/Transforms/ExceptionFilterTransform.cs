// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
        /// The current method being transformed.
        /// </summary>
        private MethodDefinition Method;

        /// <summary>
        /// True if the visited type is a generated async state machine.
        /// </summary>
        private bool IsAsyncStateMachineType;

        /// <summary>
        /// True if the current method has modified handlers.
        /// </summary>
        private bool ModifiedHandlers;

        internal ExceptionFilterTransform(ILogger logger)
            : base(logger)
        {
        }

        /// <inheritdoc/>
        internal override void VisitType(TypeDefinition typeDef)
        {
            this.TypeDef = typeDef;
            this.IsAsyncStateMachineType = typeDef.Interfaces.Any(
                i => i.InterfaceType.FullName == typeof(SystemCompiler.IAsyncStateMachine).FullName);
        }

        /// <inheritdoc/>
        internal override void VisitMethod(MethodDefinition method)
        {
            this.Method = method;
            this.ModifiedHandlers = false;

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
            if ((this.IsAsyncStateMachineType && IsAsyncStateMachineHandler(handler)) ||
                IsRethrowHandler(handler))
            {
                // Do not instrument the compiler generated catch block of an async state machine,
                // or an exception handler that is just a rethrow.
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

            Debug.WriteLine($"............. [+] rewriting catch block to rethrow an ExecutionCanceledException");

            var handlerType = this.Method.Module.ImportReference(typeof(ExceptionHandlers)).Resolve();
            MethodReference handlerMethod = handlerType.Methods.FirstOrDefault(m => m.Name == "ThrowIfCoyoteRuntimeException");
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
            // By inserting new code into the visited method, it is possible some short branch
            // instructions are now out of range, and need to be switch to long branches. This
            // fixes that and it also recomputes instruction indexes which is also needed for
            // valid write assembly operation.
            this.Method.Body.SimplifyMacros();
            this.Method.Body.OptimizeMacros();
        }

        /// <summary>
        /// Checks if the specified handler is only rethrowing an exception.
        /// </summary>
        private static bool IsRethrowHandler(ExceptionHandler handler)
        {
            Code previousOpCode = Code.Nop;
            bool isRethrowing = false;
            Instruction instruction = handler.HandlerStart;
            while (instruction != handler.HandlerEnd)
            {
                Code opCode = instruction.OpCode.Code;
                if (opCode is Code.Throw || opCode is Code.Rethrow)
                {
                    isRethrowing = true;
                    break;
                }

                if (opCode != Code.Nop && opCode != Code.Pop)
                {
                    // TODO: optimize for generated async state machine cases.
                    if (previousOpCode != Code.Nop && !IsStoreLoadOpCodeMatching(previousOpCode, opCode))
                    {
                        break;
                    }

                    previousOpCode = opCode;
                }

                instruction = instruction.Next;
            }

            return isRethrowing;
        }

        /// <summary>
        /// Checks if the specified store and load op codes are matching.
        /// </summary>
        private static bool IsStoreLoadOpCodeMatching(Code storeCode, Code loadCode) =>
            storeCode == Code.Stloc_0 ? loadCode == Code.Ldloc_0 :
            storeCode == Code.Stloc_1 ? loadCode == Code.Ldloc_1 :
            storeCode == Code.Stloc_2 ? loadCode == Code.Ldloc_2 :
            storeCode == Code.Stloc_3 ? loadCode == Code.Ldloc_3 : false;

        /// <summary>
        /// Checks if the specified handler is generated for the async state machine.
        /// </summary>
        private static bool IsAsyncStateMachineHandler(ExceptionHandler handler)
        {
            Instruction instruction = handler.HandlerStart;
            while (instruction != handler.HandlerEnd)
            {
                if (instruction.Operand is MethodReference method)
                {
                    TypeReference type = method.DeclaringType;
                    if ((type.Namespace == CachedNameProvider.SystematicTestingNamespace ||
                        type.Namespace == CachedNameProvider.SystemCompilerNamespace) &&
                        (type.Name == CachedNameProvider.AsyncTaskMethodBuilderName ||
                        type.Name.StartsWith("AsyncTaskMethodBuilder`")) &&
                        method.Name == nameof(SystemCompiler.AsyncTaskMethodBuilder.SetException))
                    {
                        return true;
                    }
                }

                instruction = instruction.Next;
            }

            return false;
        }
    }
}
