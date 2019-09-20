// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Threading.Tasks
{
    /// <summary>
    /// Implements a <see cref="ControlledTask"/> awaiter. This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    public readonly struct ControlledTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
    {
        // WARNING: The layout must remain the same, as the struct is used to access
        // the generic ControlledTaskAwaiter<> as ControlledTaskAwaiter.

        /// <summary>
        /// The controlled task being awaited.
        /// </summary>
        private readonly ControlledTask ControlledTask;

        /// <summary>
        /// The task awaiter.
        /// </summary>
        private readonly TaskAwaiter Awaiter;

        /// <summary>
        /// Gets a value that indicates whether the controlled task has completed.
        /// </summary>
        public bool IsCompleted => this.ControlledTask.IsCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledTaskAwaiter"/> struct.
        /// </summary>
        [DebuggerStepThrough]
        internal ControlledTaskAwaiter(ControlledTask task, Task awaiterTask)
        {
            this.ControlledTask = task;
            this.Awaiter = awaiterTask.GetAwaiter();
        }

        /// <summary>
        /// Ends the wait for the completion of the controlled task.
        /// </summary>
        [DebuggerHidden]
        public void GetResult() => this.ControlledTask.GetResult(this.Awaiter);

        /// <summary>
        /// Sets the action to perform when the controlled task completes.
        /// </summary>
        [DebuggerHidden]
        public void OnCompleted(Action continuation) =>
            this.ControlledTask.OnCompleted(continuation, this.Awaiter);

        /// <summary>
        /// Schedules the continuation action that is invoked when the controlled task completes.
        /// </summary>
        [DebuggerHidden]
        public void UnsafeOnCompleted(Action continuation) =>
            this.ControlledTask.UnsafeOnCompleted(continuation, this.Awaiter);
    }

    /// <summary>
    /// Implements a <see cref="ControlledTask"/> awaiter. This type is intended for compiler use only.
    /// </summary>
    /// <typeparam name="TResult">The type of the produced result.</typeparam>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    public readonly struct ControlledTaskAwaiter<TResult> : ICriticalNotifyCompletion, INotifyCompletion
    {
        // WARNING: The layout must remain the same, as the struct is used to access
        // the generic ControlledTaskAwaiter<> as ControlledTaskAwaiter.

        /// <summary>
        /// The controlled task being awaited.
        /// </summary>
        private readonly ControlledTask<TResult> ControlledTask;

        /// <summary>
        /// The task awaiter.
        /// </summary>
        private readonly TaskAwaiter<TResult> Awaiter;

        /// <summary>
        /// Gets a value that indicates whether the controlled task has completed.
        /// </summary>
        public bool IsCompleted => this.ControlledTask.IsCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledTaskAwaiter{TResult}"/> struct.
        /// </summary>
        [DebuggerStepThrough]
        internal ControlledTaskAwaiter(ControlledTask<TResult> task, Task<TResult> awaiterTask)
        {
            this.ControlledTask = task;
            this.Awaiter = awaiterTask.GetAwaiter();
        }

        /// <summary>
        /// Ends the wait for the completion of the controlled task.
        /// </summary>
        [DebuggerHidden]
        public TResult GetResult() => this.ControlledTask.GetResult(this.Awaiter);

        /// <summary>
        /// Sets the action to perform when the controlled task completes.
        /// </summary>
        [DebuggerHidden]
        public void OnCompleted(Action continuation) =>
            this.ControlledTask.OnCompleted(continuation, this.Awaiter);

        /// <summary>
        /// Schedules the continuation action that is invoked when the controlled task completes.
        /// </summary>
        [DebuggerHidden]
        public void UnsafeOnCompleted(Action continuation) =>
            this.ControlledTask.UnsafeOnCompleted(continuation, this.Awaiter);
    }
}
