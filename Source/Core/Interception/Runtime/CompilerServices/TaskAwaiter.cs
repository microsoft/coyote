// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using SystemCompiler = System.Runtime.CompilerServices;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Interception
{
    /// <summary>
    /// Implements a <see cref="Task"/> awaiter. This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public readonly struct TaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
    {
        // WARNING: The layout must remain the same, as the struct is used to access
        // the generic TaskAwaiter<> as TaskAwaiter.

        /// <summary>
        /// Responsible for controlling the execution of tasks during systematic testing.
        /// </summary>
        private readonly CoyoteRuntime Runtime;

        /// <summary>
        /// The task being awaited.
        /// </summary>
        private readonly SystemTasks.Task AwaitedTask;

        /// <summary>
        /// The task awaiter.
        /// </summary>
        private readonly SystemCompiler.TaskAwaiter Awaiter;

        /// <summary>
        /// Gets a value that indicates whether the controlled task has completed.
        /// </summary>
        public bool IsCompleted => this.AwaitedTask?.IsCompleted ?? this.Awaiter.IsCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskAwaiter"/> struct.
        /// </summary>
        internal TaskAwaiter(CoyoteRuntime runtime, SystemTasks.Task awaitedTask)
        {
            this.Runtime = runtime;
            this.AwaitedTask = awaitedTask;
            this.Awaiter = awaitedTask.GetAwaiter();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskAwaiter"/> struct.
        /// </summary>
        internal TaskAwaiter(CoyoteRuntime runtime, SystemCompiler.TaskAwaiter awaiter)
        {
            this.Runtime = runtime;
            this.AwaitedTask = null;
            this.Awaiter = awaiter;
        }

        /// <summary>
        /// Ends the wait for the completion of the controlled task.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void GetResult()
        {
            this.Runtime?.OnWaitTask(this.AwaitedTask);
            this.Awaiter.GetResult();
        }

        /// <summary>
        /// Sets the action to perform when the controlled task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action continuation)
        {
            // if (this.Runtime != null && this.Runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            // {
            //     this.Runtime.ScheduleTaskAwaiterContinuation(this.AwaitedTask, continuation);
            // }
            // else
            {
                this.Awaiter.OnCompleted(continuation);
            }
        }

        /// <summary>
        /// Schedules the continuation action that is invoked when the controlled task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeOnCompleted(Action continuation)
        {
            // if (this.Runtime != null && this.Runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            // {
            //     this.Runtime.ScheduleTaskAwaiterContinuation(this.AwaitedTask, continuation);
            // }
            // else
            {
                this.Awaiter.UnsafeOnCompleted(continuation);
            }
        }

        /// <summary>
        /// Wraps the specified task awaiter.
        /// </summary>
        public static TaskAwaiter Wrap(SystemCompiler.TaskAwaiter awaiter) => new TaskAwaiter(null, awaiter);

        /// <summary>
        /// Wraps the specified task awaiter.
        /// </summary>
        public static TaskAwaiter<TResult> Wrap<TResult>(SystemCompiler.TaskAwaiter<TResult> awaiter) => new TaskAwaiter<TResult>(null, awaiter);
    }

    /// <summary>
    /// Implements a <see cref="Task"/> awaiter. This type is intended for compiler use only.
    /// </summary>
    /// <typeparam name="TResult">The type of the produced result.</typeparam>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public readonly struct TaskAwaiter<TResult> : ICriticalNotifyCompletion, INotifyCompletion
    {
        // WARNING: The layout must remain the same, as the struct is used to access
        // the generic TaskAwaiter<> as TaskAwaiter.

        /// <summary>
        /// Responsible for controlling the execution of tasks during systematic testing.
        /// </summary>
        private readonly CoyoteRuntime Runtime;

        /// <summary>
        /// The task being awaited.
        /// </summary>
        private readonly SystemTasks.Task<TResult> AwaitedTask;

        /// <summary>
        /// The task awaiter.
        /// </summary>
        private readonly SystemCompiler.TaskAwaiter<TResult> Awaiter;

        /// <summary>
        /// Gets a value that indicates whether the controlled task has completed.
        /// </summary>
        public bool IsCompleted => this.AwaitedTask?.IsCompleted ?? this.Awaiter.IsCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskAwaiter{TResult}"/> struct.
        /// </summary>
        internal TaskAwaiter(CoyoteRuntime runtime, SystemTasks.Task<TResult> awaitedTask)
        {
            this.Runtime = runtime;
            this.AwaitedTask = awaitedTask;
            this.Awaiter = awaitedTask.GetAwaiter();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskAwaiter{TResult}"/> struct.
        /// </summary>
        internal TaskAwaiter(CoyoteRuntime runtime, SystemCompiler.TaskAwaiter<TResult> awaiter)
        {
            this.Runtime = runtime;
            this.AwaitedTask = null;
            this.Awaiter = awaiter;
        }

        /// <summary>
        /// Ends the wait for the completion of the controlled task.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public TResult GetResult()
        {
            this.Runtime?.OnWaitTask(this.AwaitedTask);
            return this.Awaiter.GetResult();
        }

        /// <summary>
        /// Sets the action to perform when the controlled task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action continuation)
        {
            // if (this.Runtime != null && this.Runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            // {
            //     this.Runtime.ScheduleTaskAwaiterContinuation(this.AwaitedTask, continuation);
            // }
            // else
            {
                this.Awaiter.OnCompleted(continuation);
            }
        }

        /// <summary>
        /// Schedules the continuation action that is invoked when the controlled task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeOnCompleted(Action continuation)
        {
            // if (this.Runtime != null && this.Runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            // {
            //     this.Runtime.ScheduleTaskAwaiterContinuation(this.AwaitedTask, continuation);
            // }
            // else
            {
                this.Awaiter.UnsafeOnCompleted(continuation);
            }
        }
    }
}
