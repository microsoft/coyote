// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using SystemCompiler = System.Runtime.CompilerServices;
using SystemTask = System.Threading.Tasks.Task;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Runtime.CompilerServices
{
    /// <summary>
    /// Implements a task awaiter. This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public readonly struct TaskAwaiter : IControlledAwaiter, ICriticalNotifyCompletion, INotifyCompletion
    {
        // WARNING: The layout must remain the same, as the struct is used to access
        // the generic TaskAwaiter<> as TaskAwaiter.

        /// <summary>
        /// The task being awaited.
        /// </summary>
        private readonly SystemTask AwaitedTask;

        /// <summary>
        /// The task awaiter.
        /// </summary>
        private readonly SystemCompiler.TaskAwaiter Awaiter;

        /// <summary>
        /// The runtime controlling this awaiter.
        /// </summary>
        private readonly CoyoteRuntime Runtime;

        /// <summary>
        /// Gets a value that indicates whether the controlled task has completed.
        /// </summary>
        public bool IsCompleted => this.AwaitedTask?.IsCompleted ?? this.Awaiter.IsCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskAwaiter"/> struct.
        /// </summary>
        internal TaskAwaiter(SystemTask awaitedTask)
        {
            this.AwaitedTask = awaitedTask;
            this.Awaiter = awaitedTask.GetAwaiter();
            RuntimeProvider.TryGetFromSynchronizationContext(out CoyoteRuntime runtime);
            this.Runtime = runtime;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskAwaiter"/> struct.
        /// </summary>
        private TaskAwaiter(SystemTask awaitedTask, ref SystemCompiler.TaskAwaiter awaiter)
        {
            this.AwaitedTask = awaitedTask;
            this.Awaiter = awaiter;
            RuntimeProvider.TryGetFromSynchronizationContext(out CoyoteRuntime runtime);
            this.Runtime = runtime;
        }

        /// <summary>
        /// Ends the wait for the completion of the controlled task.
        /// </summary>
        public void GetResult()
        {
            this.Runtime?.WaitUntilTaskCompletes(this.AwaitedTask);
            this.Awaiter.GetResult();
        }

        /// <summary>
        /// Sets the action to perform when the controlled task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action continuation) => this.Awaiter.OnCompleted(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the controlled task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeOnCompleted(Action continuation) => this.Awaiter.UnsafeOnCompleted(continuation);

        /// <inheritdoc/>
        bool IControlledAwaiter.IsTaskControlled() =>
            !this.Runtime?.IsTaskUncontrolled(this.AwaitedTask) ?? false;

        /// <summary>
        /// Wraps the specified task awaiter.
        /// </summary>
        public static TaskAwaiter Wrap(SystemCompiler.TaskAwaiter awaiter)
        {
            // Access the task being awaited through reflection.
            var field = awaiter.GetType().GetField("m_task", BindingFlags.NonPublic | BindingFlags.Instance);
            var awaitedTask = (SystemTask)field?.GetValue(awaiter);
            return new TaskAwaiter(awaitedTask, ref awaiter);
        }

        /// <summary>
        /// Wraps the specified generic task awaiter.
        /// </summary>
        public static TaskAwaiter<TResult> Wrap<TResult>(SystemCompiler.TaskAwaiter<TResult> awaiter)
        {
            // Access the generic task being awaited through reflection.
            var field = awaiter.GetType().GetField("m_task", BindingFlags.NonPublic | BindingFlags.Instance);
            var awaitedTask = (SystemTasks.Task<TResult>)field?.GetValue(awaiter);
            return new TaskAwaiter<TResult>(awaitedTask, ref awaiter);
        }
    }

    /// <summary>
    /// Implements a generic task awaiter. This type is intended for compiler use only.
    /// </summary>
    /// <typeparam name="TResult">The type of the produced result.</typeparam>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public readonly struct TaskAwaiter<TResult> : IControlledAwaiter, ICriticalNotifyCompletion, INotifyCompletion
    {
        // WARNING: The layout must remain the same, as the struct is used to access
        // the generic TaskAwaiter<> as TaskAwaiter.

        /// <summary>
        /// The task being awaited.
        /// </summary>
        private readonly SystemTasks.Task<TResult> AwaitedTask;

        /// <summary>
        /// The task awaiter.
        /// </summary>
        private readonly SystemCompiler.TaskAwaiter<TResult> Awaiter;

        /// <summary>
        /// The runtime controlling this awaiter.
        /// </summary>
        private readonly CoyoteRuntime Runtime;

        /// <summary>
        /// Gets a value that indicates whether the controlled task has completed.
        /// </summary>
        public bool IsCompleted => this.AwaitedTask?.IsCompleted ?? this.Awaiter.IsCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskAwaiter{TResult}"/> struct.
        /// </summary>
        internal TaskAwaiter(SystemTasks.Task<TResult> awaitedTask)
        {
            this.AwaitedTask = awaitedTask;
            this.Awaiter = awaitedTask.GetAwaiter();
            RuntimeProvider.TryGetFromSynchronizationContext(out CoyoteRuntime runtime);
            this.Runtime = runtime;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskAwaiter{TResult}"/> struct.
        /// </summary>
        internal TaskAwaiter(SystemTasks.Task<TResult> awaitedTask, ref SystemCompiler.TaskAwaiter<TResult> awaiter)
        {
            this.AwaitedTask = awaitedTask;
            this.Awaiter = awaiter;
            RuntimeProvider.TryGetFromSynchronizationContext(out CoyoteRuntime runtime);
            this.Runtime = runtime;
        }

        /// <summary>
        /// Ends the wait for the completion of the controlled task.
        /// </summary>
        public TResult GetResult()
        {
            this.Runtime?.WaitUntilTaskCompletes(this.AwaitedTask);
            return this.Awaiter.GetResult();
        }

        /// <summary>
        /// Sets the action to perform when the controlled task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action continuation) => this.Awaiter.OnCompleted(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the controlled task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeOnCompleted(Action continuation) => this.Awaiter.UnsafeOnCompleted(continuation);

        /// <inheritdoc/>
        bool IControlledAwaiter.IsTaskControlled() =>
            !this.Runtime?.IsTaskUncontrolled(this.AwaitedTask) ?? false;
    }
}
