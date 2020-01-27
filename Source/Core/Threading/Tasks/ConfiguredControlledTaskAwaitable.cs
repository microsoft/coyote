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
        /// Responsible for controlling the execution of tasks during systematic testing.
        /// </summary>
        private readonly ITaskController TaskController;

        /// <summary>
        /// The task awaiter.
        /// </summary>
        private readonly ConfiguredControlledTaskAwaiter Awaiter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfiguredControlledTaskAwaitable"/> struct.
        /// </summary>
        internal ConfiguredControlledTaskAwaitable(ITaskController taskController, Task awaitedTask,
            bool continueOnCapturedContext)
        {
            this.TaskController = taskController;
            this.Awaiter = new ConfiguredControlledTaskAwaiter(taskController, awaitedTask, continueOnCapturedContext);
        }

        /// <summary>
        /// Returns an awaiter for this awaitable object.
        /// </summary>
        /// <returns>The awaiter.</returns>
        public ConfiguredControlledTaskAwaiter GetAwaiter()
        {
            this.TaskController?.OnGetControlledAwaiter();
            return this.Awaiter;
        }

        /// <summary>
        /// Provides an awaiter for an awaitable object. This type is intended for compiler use only.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
        public struct ConfiguredControlledTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
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
            private readonly ConfiguredTaskAwaitable.ConfiguredTaskAwaiter Awaiter;

            /// <summary>
            /// Gets a value that indicates whether the controlled task has completed.
            /// </summary>
            public bool IsCompleted => this.AwaitedTask.IsCompleted;

            /// <summary>
            /// Initializes a new instance of the <see cref="ConfiguredControlledTaskAwaiter"/> struct.
            /// </summary>
            internal ConfiguredControlledTaskAwaiter(ITaskController taskController, Task awaitedTask,
                bool continueOnCapturedContext)
            {
                this.TaskController = taskController;
                this.AwaitedTask = awaitedTask;
                this.Awaiter = awaitedTask.ConfigureAwait(continueOnCapturedContext).GetAwaiter();
            }

            /// <summary>
            /// Ends the await on the completed task.
            /// </summary>
            public void GetResult()
            {
                this.TaskController?.OnWaitTask(this.AwaitedTask);
                this.Awaiter.GetResult();
            }

            /// <summary>
            /// Schedules the continuation action for the task associated with this awaiter.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
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
            /// Schedules the continuation action for the task associated with this awaiter.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
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

    /// <summary>
    /// Provides an awaitable object that enables configured awaits on a <see cref="ControlledTask{TResult}"/>.
    /// This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    public struct ConfiguredControlledTaskAwaitable<TResult>
    {
        /// <summary>
        /// Responsible for controlling the execution of tasks during systematic testing.
        /// </summary>
        private readonly ITaskController TaskController;

        /// <summary>
        /// The task awaiter.
        /// </summary>
        private readonly ConfiguredControlledTaskAwaiter Awaiter;

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfiguredControlledTaskAwaitable{TResult}"/> struct.
        /// </summary>
        internal ConfiguredControlledTaskAwaitable(ITaskController taskController, Task<TResult> awaitedTask,
            bool continueOnCapturedContext)
        {
            this.TaskController = taskController;
            this.Awaiter = new ConfiguredControlledTaskAwaiter(taskController, awaitedTask, continueOnCapturedContext);
        }

        /// <summary>
        /// Returns an awaiter for this awaitable object.
        /// </summary>
        /// <returns>The awaiter.</returns>
        public ConfiguredControlledTaskAwaiter GetAwaiter()
        {
            this.TaskController?.OnGetControlledAwaiter();
            return this.Awaiter;
        }

        /// <summary>
        /// Provides an awaiter for an awaitable object. This type is intended for compiler use only.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
        public struct ConfiguredControlledTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
        {
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
            private readonly ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter Awaiter;

            /// <summary>
            /// Gets a value that indicates whether the controlled task has completed.
            /// </summary>
            public bool IsCompleted => this.AwaitedTask.IsCompleted;

            /// <summary>
            /// Initializes a new instance of the <see cref="ConfiguredControlledTaskAwaiter"/> struct.
            /// </summary>
            internal ConfiguredControlledTaskAwaiter(ITaskController taskController, Task<TResult> awaitedTask,
                bool continueOnCapturedContext)
            {
                this.TaskController = taskController;
                this.AwaitedTask = awaitedTask;
                this.Awaiter = awaitedTask.ConfigureAwait(continueOnCapturedContext).GetAwaiter();
            }

            /// <summary>
            /// Ends the await on the completed task.
            /// </summary>
            public TResult GetResult()
            {
                this.TaskController?.OnWaitTask(this.AwaitedTask);
                return this.Awaiter.GetResult();
            }

            /// <summary>
            /// Schedules the continuation action for the task associated with this awaiter.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
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
            /// Schedules the continuation action for the task associated with this awaiter.
            /// </summary>
            /// <param name="continuation">The action to invoke when the await operation completes.</param>
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
}
