// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Coyote.Runtime.CompilerServices
{
    /// <summary>
    /// Provides an awaitable object that pauses the currently executing operation until
    /// the specified condition gets satisfied.
    /// </summary>
    /// <remarks>This type is intended for compiler use only.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class PausedOperationAwaitable
{
        /// <summary>
        /// The paused operation awaiter.
        /// </summary>
        private readonly PausedOperationAwaiter Awaiter;

        /// <summary>
        /// Initializes a new instance of the <see cref="PausedOperationAwaitable"/> struct.
        /// </summary>
        internal PausedOperationAwaitable(CoyoteRuntime runtime, ControlledOperation op, Func<bool> condition,
            bool resumeAsynchronously) =>
            this.Awaiter = new PausedOperationAwaiter(runtime, op, condition, resumeAsynchronously);

        /// <summary>
        /// Returns an awaiter for this awaitable object.
        /// </summary>
        /// <returns>The awaiter.</returns>
        public PausedOperationAwaiter GetAwaiter() => this.Awaiter;

        /// <summary>
        /// Provides an awaiter that that pauses the currently executing operation until
        /// the specified condition gets satisfied.
        /// </summary>
        /// <remarks>This type is intended for compiler use only.</remarks>
        public class PausedOperationAwaiter : IControllableAwaiter, ICriticalNotifyCompletion, INotifyCompletion
        {
            /// <summary>
            /// The runtime managing the paused operation.
            /// </summary>
            private readonly CoyoteRuntime Runtime;

            /// <summary>
            /// The paused operation.
            /// </summary>
            private readonly ControlledOperation Operation;

            /// <summary>
            /// The condition being awaited.
            /// </summary>
            private readonly Func<bool> Condition;

            /// <summary>
            /// True if the continuation must execute asynchronously, else false.
            /// </summary>
            private readonly bool ResumeAsynchronously;

            /// <summary>
            /// True if the awaiter has completed, else false.
            /// </summary>
            public bool IsCompleted
            {
                get
                {
                    IO.Debug.WriteLine($">>> PausedOperationAwaiter::IsCompleted {this.Condition()} '{this.Operation}'.");
                    return this.Operation != null ? this.Condition() : true;
                }
            }

            /// <inheritdoc/>
            bool IControllableAwaiter.IsControlled => true;

            /// <summary>
            /// Initializes a new instance of the <see cref="PausedOperationAwaiter"/> struct.
            /// </summary>
            internal PausedOperationAwaiter(CoyoteRuntime runtime, ControlledOperation op, Func<bool> condition,
                bool resumeAsynchronously)
            {
                this.Runtime = runtime;
                this.Operation = op;
                this.Condition = condition;
                this.ResumeAsynchronously = resumeAsynchronously;
            }

            /// <summary>
            /// Ends asynchronously waiting for the completion of the awaiter.
            /// </summary>
            public void GetResult() => this.Runtime.PauseOperationUntil(this.Condition);

            /// <summary>
            /// Sets the action to perform when the controlled task completes.
            /// </summary>
            public void OnCompleted(Action continuation) => this.UnsafeOnCompleted(continuation);

            /// <summary>
            /// Schedules the continuation action that is invoked when the controlled task completes.
            /// </summary>
            public void UnsafeOnCompleted(Action continuation)
            {
                IO.Debug.WriteLine($">>> Scheduling continuation for operation '{this.Operation}'.");
                if (this.ResumeAsynchronously)
                {
                    this.Runtime.Schedule(continuation);
                }
                else
                {
                    this.Operation.SetContinuationCallback(continuation);
                }
            }
        }
    }
}
