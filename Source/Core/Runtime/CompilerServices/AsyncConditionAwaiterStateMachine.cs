// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Runtime.CompilerServices
{
    /// <summary>
    /// Implements a state machine that can be used to control and asynchronously wait
    /// for a condition to get resolved during testing.
    /// </summary>
    /// <remarks>
    /// We should be able to replace this in certain instances with the "AsyncMethodBuilder override" feature in C# 10.
    /// See: https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/proposals/csharp-10.0/async-method-builders.
    /// </remarks>
    internal class AsyncConditionAwaiterStateMachine : AsyncAwaiterStateMachine<bool>
    {
        /// <summary>
        /// Condition that must get resolved to complete this state machine.
        /// </summary>
        private readonly Func<bool> Condition;

        /// <summary>
        /// The debug message to print while waiting for the condition to get resolved.
        /// </summary>
        private readonly string DebugMsg;

        /// <summary>
        /// Initializes a new instance of the <see cref="AsyncConditionAwaiterStateMachine"/> class.
        /// </summary>
        private AsyncConditionAwaiterStateMachine(CoyoteRuntime runtime, Func<bool> condition,
            bool runContinuationAsynchronously, string debugMsg)
            : base(runtime, runContinuationAsynchronously)
        {
            this.Condition = condition;
            this.DebugMsg = debugMsg;
        }

        /// <summary>
        /// Runs an asynchronous state machine that will pause the specified operation
        /// until the condition gets resolved and return the result.
        /// </summary>
        internal static SystemTasks.Task<bool> RunAsync(CoyoteRuntime runtime, Func<bool> condition,
            bool runContinuationAsynchronously = true, string debugMsg = null)
        {
            var stateMachine = new AsyncConditionAwaiterStateMachine(runtime, condition, runContinuationAsynchronously, debugMsg);
            stateMachine.MoveNext();
            runtime.RegisterKnownControlledTask(stateMachine.CompletionSource.Task);
            return stateMachine.CompletionSource.Task;
        }

        /// <summary>
        /// Moves the state machine to its next state.
        /// </summary>
        public override void MoveNext()
        {
            if (this.CurrentStatus != Status.Completed)
            {
                try
                {
                    ControlledOperation current = this.Runtime.GetExecutingOperation();
                    if (this.CurrentStatus is Status.Running && !this.Condition())
                    {
                        // Schedule the continuation to execute asynchronously after the current state completes.
                        this.CurrentStatus = Status.Waiting;
                        if (this.RunContinuationAsynchronously)
                        {
                            var stateMachine = this;
                            this.Runtime.Schedule(this.MoveNext);
                        }
                        else
                        {
                            current.SetContinuationCallback(this.MoveNext);
                        }

                        return;
                    }

                    // Wait for the condition to get resolved synchronously.
                    this.Runtime.PauseOperationUntil(current, this.Condition, debugMsg: this.DebugMsg);

                    // Complete the state machine with the result.
                    this.CurrentStatus = Status.Completed;
                    this.CompletionSource.SetResult(true);
                }
                catch (Exception exception)
                {
                    this.CurrentStatus = Status.Completed;
                    this.CompletionSource.SetException(exception);
                }
            }
        }
    }
}
