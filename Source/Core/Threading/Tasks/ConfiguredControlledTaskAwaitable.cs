// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Threading.Tasks
{
    /// <summary>
    /// Provides an awaitable object that is the outcome of invoking <see cref="ControlledTask.ConfigureAwait"/>.
    /// This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    public struct ConfiguredControlledTaskAwaitable
    {
        /// <summary>
        /// The task awaiter.
        /// </summary>
        private readonly ConfiguredControlledTaskAwaiter Awaiter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfiguredControlledTaskAwaitable"/> struct.
        /// </summary>
        internal ConfiguredControlledTaskAwaitable(ControlledTask task, Task awaiterTask, bool continueOnCapturedContext)
        {
            this.Awaiter = new ConfiguredControlledTaskAwaiter(task, awaiterTask, continueOnCapturedContext);
        }

        /// <summary>
        /// Returns an awaiter for this awaitable object.
        /// </summary>
        /// <returns>The awaiter.</returns>
        public ConfiguredControlledTaskAwaiter GetAwaiter() => this.Awaiter;

        /// <summary>
        /// Provides an awaiter for an awaitable object. This type is intended for compiler use only.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
        public struct ConfiguredControlledTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
            /// <summary>
            /// The controlled task being awaited.
            /// </summary>
            private readonly ControlledTask ControlledTask;

            /// <summary>
            /// The task awaiter.
            /// </summary>
            private readonly ConfiguredTaskAwaitable.ConfiguredTaskAwaiter Awaiter;

            /// <summary>
            /// Gets a value that indicates whether the controlled task has completed.
            /// </summary>
            public bool IsCompleted => this.ControlledTask.IsCompleted;

            /// <summary>
            /// Initializes a new instance of the <see cref="ConfiguredControlledTaskAwaiter"/> struct.
            /// </summary>
            internal ConfiguredControlledTaskAwaiter(ControlledTask task, Task awaiterTask, bool continueOnCapturedContext)
            {
                this.ControlledTask = task;
                this.Awaiter = awaiterTask.ConfigureAwait(continueOnCapturedContext).GetAwaiter();
            }

            /// <summary>
            /// Ends the await on the completed task.
            /// </summary>
            public void GetResult() => this.ControlledTask.GetResult(this.Awaiter);

            /// <summary>
            /// Schedules the continuation action for the task associated with this awaiter.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            public void OnCompleted(Action continuation) =>
                this.ControlledTask.OnCompleted(continuation, this.Awaiter);

            /// <summary>
            /// Schedules the continuation action for the task associated with this awaiter.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            public void UnsafeOnCompleted(Action continuation) =>
                this.ControlledTask.UnsafeOnCompleted(continuation, this.Awaiter);
        }
    }

    /// <summary>
    /// Provides an awaitable object that enables configured awaits on a <see cref="ControlledTask{TResult}"/>.
    /// This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    public struct ConfiguredControlledTaskAwaitable<TResult>
    {
        /// <summary>
        /// The task awaiter.
        /// </summary>
        private readonly ConfiguredControlledTaskAwaiter Awaiter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfiguredControlledTaskAwaitable{TResult}"/> struct.
        /// </summary>
        internal ConfiguredControlledTaskAwaitable(ControlledTask<TResult> task, Task<TResult> awaiterTask,
            bool continueOnCapturedContext)
        {
            this.Awaiter = new ConfiguredControlledTaskAwaiter(task, awaiterTask, continueOnCapturedContext);
        }

        /// <summary>
        /// Returns an awaiter for this awaitable object.
        /// </summary>
        /// <returns>The awaiter.</returns>
        public ConfiguredControlledTaskAwaiter GetAwaiter() => this.Awaiter;

        /// <summary>
        /// Provides an awaiter for an awaitable object. This type is intended for compiler use only.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
        public struct ConfiguredControlledTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
            /// <summary>
            /// The controlled task being awaited.
            /// </summary>
            private readonly ControlledTask<TResult> ControlledTask;

            /// <summary>
            /// The task awaiter.
            /// </summary>
            private readonly ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter Awaiter;

            /// <summary>
            /// Gets a value that indicates whether the controlled task has completed.
            /// </summary>
            public bool IsCompleted => this.ControlledTask.IsCompleted;

            /// <summary>
            /// Initializes a new instance of the <see cref="ConfiguredControlledTaskAwaiter"/> struct.
            /// </summary>
            internal ConfiguredControlledTaskAwaiter(ControlledTask<TResult> task, Task<TResult> awaiterTask,
                bool continueOnCapturedContext)
            {
                this.ControlledTask = task;
                this.Awaiter = awaiterTask.ConfigureAwait(continueOnCapturedContext).GetAwaiter();
            }

            /// <summary>
            /// Ends the await on the completed task.
            /// </summary>
            public TResult GetResult() => this.ControlledTask.GetResult(this.Awaiter);

            /// <summary>
            /// Schedules the continuation action for the task associated with this awaiter.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            public void OnCompleted(Action continuation) =>
                this.ControlledTask.OnCompleted(continuation, this.Awaiter);

            /// <summary>
            /// Schedules the continuation action for the task associated with this awaiter.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
            public void UnsafeOnCompleted(Action continuation) =>
                this.ControlledTask.UnsafeOnCompleted(continuation, this.Awaiter);
        }
    }
}
