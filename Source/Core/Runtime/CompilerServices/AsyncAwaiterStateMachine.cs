// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using SystemCompiler = System.Runtime.CompilerServices;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Runtime.CompilerServices
{
    /// <summary>
    /// Implements an abstract state machine that can be used to asynchronously pause controlled operations.
    /// </summary>
    /// <remarks>
    /// We should be able to replace this in certain instances with the "AsyncMethodBuilder override" feature in C# 10.
    /// See: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-10.0/async-method-builders.
    /// </remarks>
    internal abstract class AsyncAwaiterStateMachine<TResult> : SystemCompiler.IAsyncStateMachine
    {
        /// <summary>
        /// The status of the state machine.
        /// </summary>
        protected enum Status
        {
            Running,
            Waiting,
            Completed
        }

        /// <summary>
        /// Responsible for controlling the execution of this state machine.
        /// </summary>
        protected readonly CoyoteRuntime Runtime;

        /// <summary>
        /// Used to signal the completion of this state machine.
        /// </summary>
        protected readonly SystemTasks.TaskCompletionSource<TResult> CompletionSource;

        /// <summary>
        /// The current status of this state machine.
        /// </summary>
        protected Status CurrentStatus;

        /// <summary>
        /// True if the continuation must execute asynchronously, else false.
        /// </summary>
        protected readonly bool RunContinuationAsynchronously;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncAwaiterStateMachine{TResult}"/> class.
        /// </summary>
        protected AsyncAwaiterStateMachine(CoyoteRuntime runtime, bool runContinuationAsynchronously)
        {
            this.Runtime = runtime;
            this.CompletionSource = new SystemTasks.TaskCompletionSource<TResult>();
            this.CurrentStatus = Status.Running;
            this.RunContinuationAsynchronously = runContinuationAsynchronously;
        }

        /// <summary>
        /// Moves the state machine to its next state.
        /// </summary>
        public abstract void MoveNext();

        /// <summary>
        /// Configures the state machine with a heap-allocated replica.
        /// </summary>
        void SystemCompiler.IAsyncStateMachine.SetStateMachine(SystemCompiler.IAsyncStateMachine stateMachine) =>
            throw new InvalidOperationException("This method should never be called.");
    }
}
