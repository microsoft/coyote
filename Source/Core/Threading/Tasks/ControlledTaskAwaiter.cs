// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public readonly struct ControlledTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
    {
        // WARNING: The layout must remain the same, as the struct is used to access
        // the generic ControlledTaskAwaiter<> as ControlledTaskAwaiter.

        /// <summary>
        /// Responsible for controlling the execution of tasks during systematic testing.
        /// </summary>
        private readonly ITaskController TaskController;

        /// <summary>
        /// The task being awaited.
        /// </summary>
        private readonly Task AwaitedTask;

        /// <summary>
        /// The task awaiter.
        /// </summary>
        private readonly TaskAwaiter Awaiter;

        /// <summary>
        /// Gets a value that indicates whether the controlled task has completed.
        /// </summary>
        public bool IsCompleted => this.AwaitedTask.IsCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledTaskAwaiter"/> struct.
        /// </summary>
        [DebuggerStepThrough]
        internal ControlledTaskAwaiter(ITaskController taskController, Task awaitedTask)
        {
            this.TaskController = taskController;
            this.AwaitedTask = awaitedTask;
            this.Awaiter = awaitedTask.GetAwaiter();
        }

        /// <summary>
        /// Ends the wait for the completion of the controlled task.
        /// </summary>
        [DebuggerHidden]
        public void GetResult()
        {
            this.TaskController?.OnWaitTask(this.AwaitedTask);
            this.Awaiter.GetResult();
        }

        /// <summary>
        /// Sets the action to perform when the controlled task completes.
        /// </summary>
        [DebuggerHidden]
        public void OnCompleted(Action continuation)
        {
            if (this.TaskController is null)
            {
                this.Awaiter.OnCompleted(continuation);
            }
            else
            {
                this.TaskController.ScheduleTaskAwaiterContinuation(this.AwaitedTask, continuation);
            }
        }

        /// <summary>
        /// Schedules the continuation action that is invoked when the controlled task completes.
        /// </summary>
        [DebuggerHidden]
        public void UnsafeOnCompleted(Action continuation)
        {
            if (this.TaskController is null)
            {
                this.Awaiter.UnsafeOnCompleted(continuation);
            }
            else
            {
                this.TaskController.ScheduleTaskAwaiterContinuation(this.AwaitedTask, continuation);
            }
        }
    }

    /// <summary>
    /// Implements a <see cref="ControlledTask"/> awaiter. This type is intended for compiler use only.
    /// </summary>
    /// <typeparam name="TResult">The type of the produced result.</typeparam>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public readonly struct ControlledTaskAwaiter<TResult> : ICriticalNotifyCompletion, INotifyCompletion
    {
        // WARNING: The layout must remain the same, as the struct is used to access
        // the generic ControlledTaskAwaiter<> as ControlledTaskAwaiter.

        /// <summary>
        /// Responsible for controlling the execution of tasks during systematic testing.
        /// </summary>
        private readonly ITaskController TaskController;

        /// <summary>
        /// The task being awaited.
        /// </summary>
        private readonly Task<TResult> AwaitedTask;

        /// <summary>
        /// The task awaiter.
        /// </summary>
        private readonly TaskAwaiter<TResult> Awaiter;

        /// <summary>
        /// Gets a value that indicates whether the controlled task has completed.
        /// </summary>
        public bool IsCompleted => this.AwaitedTask.IsCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledTaskAwaiter{TResult}"/> struct.
        /// </summary>
        [DebuggerStepThrough]
        internal ControlledTaskAwaiter(ITaskController taskController, Task<TResult> awaitedTask)
        {
            this.TaskController = taskController;
            this.AwaitedTask = awaitedTask;
            this.Awaiter = awaitedTask.GetAwaiter();
        }

        /// <summary>
        /// Ends the wait for the completion of the controlled task.
        /// </summary>
        [DebuggerHidden]
        public TResult GetResult()
        {
            this.TaskController?.OnWaitTask(this.AwaitedTask);
            return this.Awaiter.GetResult();
        }

        /// <summary>
        /// Sets the action to perform when the controlled task completes.
        /// </summary>
        [DebuggerHidden]
        public void OnCompleted(Action continuation)
        {
            if (this.TaskController is null)
            {
                this.Awaiter.OnCompleted(continuation);
            }
            else
            {
                this.TaskController.ScheduleTaskAwaiterContinuation(this.AwaitedTask, continuation);
            }
        }

        /// <summary>
        /// Schedules the continuation action that is invoked when the controlled task completes.
        /// </summary>
        [DebuggerHidden]
        public void UnsafeOnCompleted(Action continuation)
        {
            if (this.TaskController is null)
            {
                this.Awaiter.UnsafeOnCompleted(continuation);
            }
            else
            {
                this.TaskController.ScheduleTaskAwaiterContinuation(this.AwaitedTask, continuation);
            }
        }
    }
}
