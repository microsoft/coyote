// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;

namespace Microsoft.Coyote.Threading.Tasks
{
    /// <summary>
    /// Implements an awaitable that asynchronously yields back to the current context when awaited.
    /// This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public readonly struct ControlledYieldAwaitable
    {
        /// <summary>
        /// Responsible for controlling the execution of tasks during systematic testing.
        /// </summary>
        private readonly ITaskController TaskController;

        /// <summary>
        /// Gets an awaiter for this awaitable.
        /// </summary>
        public ControlledYieldAwaiter GetAwaiter()
        {
            this.TaskController?.OnGetControlledAwaiter();
            return new ControlledYieldAwaiter(this.TaskController, default);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledYieldAwaitable"/> struct.
        /// </summary>
        internal ControlledYieldAwaitable(ITaskController taskController)
        {
            this.TaskController = taskController;
        }

        /// <summary>
        /// Provides an awaiter that switches into a target environment.
        /// This type is intended for compiler use only.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
        public readonly struct ControlledYieldAwaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
            /// <summary>
            /// Responsible for controlling the execution of tasks during systematic testing.
            /// </summary>
            private readonly ITaskController TaskController;

            /// <summary>
            /// The internal yield awaiter.
            /// </summary>
            private readonly YieldAwaitable.YieldAwaiter Awaiter;

            /// <summary>
            /// Gets a value that indicates whether a yield is not required.
            /// </summary>
#pragma warning disable CA1822 // Mark members as static
            public bool IsCompleted => false;
#pragma warning restore CA1822 // Mark members as static

            /// <summary>
            /// Initializes a new instance of the <see cref="ControlledYieldAwaiter"/> struct.
            /// </summary>
            internal ControlledYieldAwaiter(ITaskController taskController, YieldAwaitable.YieldAwaiter awaiter)
            {
                this.TaskController = taskController;
                this.Awaiter = awaiter;
            }

            /// <summary>
            /// Ends the await operation.
            /// </summary>
            public void GetResult()
            {
                this.TaskController?.OnControlledYieldAwaiterGetResult();
                this.Awaiter.GetResult();
            }

            /// <summary>
            /// Posts the continuation action back to the current context.
            /// </summary>
            public void OnCompleted(Action continuation)
            {
                if (this.TaskController is null)
                {
                    this.Awaiter.OnCompleted(continuation);
                }
                else
                {
                    this.TaskController.ScheduleYieldAwaiterContinuation(continuation);
                }
            }

            /// <summary>
            /// Posts the continuation action back to the current context.
            /// </summary>
            public void UnsafeOnCompleted(Action continuation)
            {
                if (this.TaskController is null)
                {
                    this.Awaiter.UnsafeOnCompleted(continuation);
                }
                else
                {
                    this.TaskController.ScheduleYieldAwaiterContinuation(continuation);
                }
            }
        }
    }
}
