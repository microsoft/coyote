// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Coyote.Runtime.CompilerServices
{
    /// <summary>
    /// Provides an awaitable object that pauses the executing operation until a signal happens.
    /// </summary>
    /// <remarks>This type is intended for compiler use only.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public readonly struct SignalAwaitable
{
        /// <summary>
        /// The paused operation awaiter.
        /// </summary>
        private readonly SignalAwaiter Awaiter;

        /// <summary>
        /// Initializes a new instance of the <see cref="SignalAwaitable"/> struct.
        /// </summary>
        internal SignalAwaitable(CoyoteRuntime runtime, ControlledOperation op, string name) =>
            this.Awaiter = new SignalAwaiter(runtime, op, name);

        /// <summary>
        /// Returns an awaiter for this awaitable object.
        /// </summary>
        /// <returns>The awaiter.</returns>
        public SignalAwaiter GetAwaiter() => this.Awaiter;

        /// <summary>
        /// Provides an awaiter that that pauses the this.Operationly executing operation until a signal happens.
        /// </summary>
        /// <remarks>This type is intended for compiler use only.</remarks>
        public struct SignalAwaiter : IControllableAwaiter, ICriticalNotifyCompletion, INotifyCompletion
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
            /// The name of the signal being awaited.
            /// </summary>
            private readonly string Name;

            /// <summary>
            /// The yield awaiter.
            /// </summary>
            private readonly YieldAwaitable.YieldAwaiter Awaiter;

            /// <summary>
            /// True if the awaiter has completed, else false.
            /// </summary>
#pragma warning disable CA1822 // Mark members as static
            public bool IsCompleted => false;
#pragma warning restore CA1822 // Mark members as static

            /// <inheritdoc/>
            bool IControllableAwaiter.IsControlled => true;

            /// <summary>
            /// Initializes a new instance of the <see cref="SignalAwaiter"/> struct.
            /// </summary>
            internal SignalAwaiter(CoyoteRuntime runtime, ControlledOperation op, string name)
            {
                this.Runtime = runtime;
                this.Operation = op;
                this.Name = name;
                this.Awaiter = default;
            }

            /// <summary>
            /// Ends asynchronously waiting for the completion of the awaiter.
            /// </summary>
            public void GetResult() => this.Awaiter.GetResult();

            /// <summary>
            /// Sets the action to perform when the controlled task completes.
            /// </summary>
            public void OnCompleted(Action continuation)
            {
                if (this.Runtime is null || this.Operation is null)
                {
                    this.Awaiter.OnCompleted(continuation);
                }
                else
                {
                    this.Runtime.Schedule(continuation, preCondition: this.WaitSignal);
                }
            }

            /// <summary>
            /// Schedules the continuation action that is invoked when the controlled task completes.
            /// </summary>
            public void UnsafeOnCompleted(Action continuation)
            {
                if (this.Runtime is null || this.Operation is null)
                {
                    this.Awaiter.UnsafeOnCompleted(continuation);
                }
                else
                {
                    this.Runtime.Schedule(continuation, preCondition: this.WaitSignal);
                }
            }

            private void WaitSignal() => SchedulingPoint.Wait(this.Name);
        }
    }
}
