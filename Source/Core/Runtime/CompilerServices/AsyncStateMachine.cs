// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using SystemCompiler = System.Runtime.CompilerServices;

namespace Microsoft.Coyote.Runtime.CompilerServices
{
    /// <summary>
    /// Implements an asynchronous state machine that can be used to control
    /// and drive the execution of asynchronous methods during testing.
    /// </summary>
    /// <remarks>
    /// We should be able to replace this with the "AsyncMethodBuilder override" feature in C# 10.
    /// See: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-10.0/async-method-builders.
    /// </remarks>
    internal abstract class AsyncStateMachine<TResult> : SystemCompiler.IAsyncStateMachine
    {
        /// <summary>
        /// The status of the state machine.
        /// </summary>
        private enum Status
        {
            Running,
            Waiting,
            Completed
        }

        /// <summary>
        /// Responsible for controlling the execution of tasks during systematic testing.
        /// </summary>
        protected readonly CoyoteRuntime Runtime;

        /// <summary>
        /// The state machine builder associated with this state machine.
        /// </summary>
        internal AsyncTaskMethodBuilder<TResult> Builder;

        /// <summary>
        /// The current status of this state machine.
        /// </summary>
        private Status CurrentStatus;

        /// <summary>
        /// The number of states that were already executed.
        /// </summary>
        private uint NumExecutedStates;

        /// <summary>
        /// The max number of states to execute.
        /// </summary>
        private uint NumMaxStates;

        /// <summary>
        /// Used to await for the completion of the current state of this state machine.
        /// </summary>
        protected SystemCompiler.ICriticalNotifyCompletion Awaiter;

        /// <summary>
        /// The result of executing the state machine to completion.
        /// </summary>
        protected TResult Result;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncStateMachine{T}"/> class.
        /// </summary>
        internal AsyncStateMachine(CoyoteRuntime runtime, uint numStates)
        {
            this.Runtime = runtime;
            this.Builder = new AsyncTaskMethodBuilder<TResult>(runtime);
            this.CurrentStatus = Status.Running;
            this.NumExecutedStates = 0;
            this.NumMaxStates = numStates;
        }

        /// <summary>
        /// Configures the state machine with a heap-allocated replica.
        /// </summary>
        void SystemCompiler.IAsyncStateMachine.SetStateMachine(SystemCompiler.IAsyncStateMachine stateMachine) =>
            this.Builder.SetStateMachine(stateMachine);

        /// <summary>
        /// Moves the state machine to its next state.
        /// </summary>
        void SystemCompiler.IAsyncStateMachine.MoveNext()
        {
            try
            {
                while (this.NumExecutedStates < this.NumMaxStates)
                {
                    if (this.CurrentStatus is Status.Running)
                    {
                        // Execute the next state of the state machine.
                        if (!this.TryExecuteState(this.NumExecutedStates))
                        {
                            // Schedule the continuation to execute after the current state completes.
                            this.CurrentStatus = Status.Waiting;
                            AsyncStateMachine<TResult> stateMachine = this;
                            this.Builder.AwaitUnsafeOnCompleted(ref this.Awaiter, ref stateMachine);
                            return;
                        }
                    }
                    else if (this.CurrentStatus is Status.Waiting)
                    {
                        this.CurrentStatus = Status.Running;
                    }

                    this.CompleteState(this.NumExecutedStates);
                    this.NumExecutedStates++;
                }
            }
            catch (Exception exception)
            {
                this.CurrentStatus = Status.Completed;
                this.Builder.SetException(exception);
                return;
            }

            this.CurrentStatus = Status.Completed;
            this.Builder.SetResult(this.Result);
        }

        /// <summary>
        /// Executes the current state.
        /// </summary>
        protected abstract bool TryExecuteState(uint state);

        /// <summary>
        /// Completes the execution of the specified state.
        /// </summary>
        protected abstract void CompleteState(uint state);
    }
}
