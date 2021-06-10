// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SystemCompiler = System.Runtime.CompilerServices;

namespace Microsoft.Coyote.Rewriting
{
    /// <summary>
    /// Rewriting pass that ensures user defined try/catch blocks do not consume runtime exceptions.
    /// </summary>
    internal class ExceptionFilterRewriter : AssemblyRewriter
    {
        /// <summary>
        /// True if the visited type is a generated async state machine.
        /// </summary>
        private bool IsAsyncStateMachineType;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExceptionFilterRewriter"/> class.
        /// </summary>
        internal ExceptionFilterRewriter(ILogger logger)
            : base(logger)
        {
        }

        /// <inheritdoc/>
        internal override void VisitType(TypeDefinition type)
        {
            this.IsAsyncStateMachineType = type.Interfaces.Any(
                i => i.InterfaceType.FullName == typeof(SystemCompiler.IAsyncStateMachine).FullName);
            base.VisitType(type);
        }

        /// <inheritdoc/>
        internal override void VisitMethod(MethodDefinition method)
        {
            this.Method = method;

            if (method.Body.HasExceptionHandlers)
            {
                foreach (var handler in method.Body.ExceptionHandlers)
                {
                    this.VisitExceptionHandler(handler);
                }
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

            if (handler.FilterStart is null)
            {
                if (handler.CatchType is null)
                {
                    // This is a finally block, which we can skip.
                    return;
                }

                var name = handler.CatchType.FullName;
                if (name == typeof(object).FullName ||
                    name == typeof(System.Exception).FullName ||
                    name == typeof(RuntimeException).FullName)
                {
                    this.AddThrowIfExecutionCanceledException(handler);
                }
            }
            else
            {
                // If there is an existing filter, then just insert a check.
                this.AddThrowIfExecutionCanceledException(handler);
            }
        }

        private void AddThrowIfExecutionCanceledException(ExceptionHandler handler)
        {
            if (!this.ModifiedMethodBody)
            {
                // A previous transform may have replaced some instructions, and if so, we need to recompute
                // the instruction indexes before we operate on the try catch.
                FixInstructionOffsets(this.Method);
                this.ModifiedMethodBody = true;
            }

            Debug.WriteLine($"............. [+] rewriting catch block to rethrow an {nameof(ExecutionCanceledException)}");

            var providerType = this.Method.Module.ImportReference(typeof(ExceptionProvider)).Resolve();
            MethodReference providerMethod = providerType.Methods.FirstOrDefault(
                m => m.Name is nameof(ExceptionProvider.ThrowIfExecutionCanceledException));
            providerMethod = this.Method.Module.ImportReference(providerMethod);

            var processor = this.Method.Body.GetILProcessor();
            var newStart = Instruction.Create(OpCodes.Dup);
            var previousStart = handler.HandlerStart;
            processor.InsertBefore(handler.HandlerStart, newStart);
            processor.InsertBefore(handler.HandlerStart, Instruction.Create(OpCodes.Call, providerMethod));
            handler.HandlerStart = newStart;

            // Fix up any other handler end position that points to previousStart instruction.
            foreach (var other in this.Method.Body.ExceptionHandlers)
            {
                // The first (or most nested) try/catch block.
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
            storeCode is Code.Stloc_0 ? loadCode is Code.Ldloc_0 :
            storeCode is Code.Stloc_1 ? loadCode is Code.Ldloc_1 :
            storeCode is Code.Stloc_2 ? loadCode is Code.Ldloc_2 :
            storeCode is Code.Stloc_3 && loadCode is Code.Ldloc_3;

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
                    if ((type.Namespace == CachedNameProvider.InterceptionNamespace ||
                        type.Namespace == CachedNameProvider.SystemCompilerNamespace) &&
                        (type.Name == CachedNameProvider.AsyncTaskMethodBuilderName ||
                        type.Name.StartsWith("AsyncTaskMethodBuilder`")) &&
                        method.Name is nameof(SystemCompiler.AsyncTaskMethodBuilder.SetException))
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
