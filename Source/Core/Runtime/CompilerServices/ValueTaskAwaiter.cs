// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using SystemCompiler = System.Runtime.CompilerServices;
using SystemTask = System.Threading.Tasks.Task;
using SystemTasks = System.Threading.Tasks;
using SystemValueTask = System.Threading.Tasks.ValueTask;

namespace Microsoft.Coyote.Runtime.CompilerServices
{
    /// <summary>
    /// Implements a <see cref="ValueTask"/> awaiter. This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public readonly struct ValueTaskAwaiter : IControllableAwaiter, ICriticalNotifyCompletion, INotifyCompletion
    {
        // WARNING: The layout must remain the same, as the struct is used to access
        // the generic ValueTaskAwaiter<> as ValueTaskAwaiter.

        /// <summary>
        /// The inner task being awaited.
        /// </summary>
        private readonly SystemTask AwaitedTask;

        /// <summary>
        /// The value task awaiter.
        /// </summary>
        private readonly SystemCompiler.ValueTaskAwaiter Awaiter;

        /// <summary>
        /// The runtime controlling this awaiter.
        /// </summary>
        private readonly CoyoteRuntime Runtime;

        /// <summary>
        /// Gets a value that indicates whether the controlled value task has completed.
        /// </summary>
        public bool IsCompleted => this.AwaitedTask?.IsCompleted ?? this.Awaiter.IsCompleted;

        /// <inheritdoc/>
        bool IControllableAwaiter.IsControlled =>
            !this.Runtime?.IsTaskUncontrolled(this.AwaitedTask) ?? false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTaskAwaiter"/> struct.
        /// </summary>
        internal ValueTaskAwaiter(ref SystemValueTask awaitedTask)
        {
            this.AwaitedTask = ValueTaskAwaiter.TryGetTask(ref awaitedTask, out Task innerTask) ?
                innerTask : null;
            this.Awaiter = awaitedTask.GetAwaiter();
            RuntimeProvider.TryGetFromSynchronizationContext(out CoyoteRuntime runtime);
            this.Runtime = runtime;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTaskAwaiter"/> struct.
        /// </summary>
        private ValueTaskAwaiter(ref SystemValueTask awaitedTask, ref SystemCompiler.ValueTaskAwaiter awaiter)
        {
            this.AwaitedTask = ValueTaskAwaiter.TryGetTask(ref awaitedTask, out Task innerTask) ?
                innerTask : null;
            this.Awaiter = awaiter;
            RuntimeProvider.TryGetFromSynchronizationContext(out CoyoteRuntime runtime);
            this.Runtime = runtime;
        }

        /// <summary>
        /// Ends the wait for the completion of the controlled value task.
        /// </summary>
        public void GetResult()
        {
            if (this.AwaitedTask != null)
            {
                this.Runtime?.WaitUntilTaskCompletes(this.AwaitedTask);
            }

            this.Awaiter.GetResult();
        }

        /// <summary>
        /// Sets the action to perform when the controlled value task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action continuation) => this.Awaiter.OnCompleted(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the controlled value task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeOnCompleted(Action continuation) => this.Awaiter.UnsafeOnCompleted(continuation);

        /// <summary>
        /// Wraps the specified value task awaiter.
        /// </summary>
        public static ValueTaskAwaiter Wrap(SystemCompiler.ValueTaskAwaiter awaiter)
        {
            // Access the task being awaited through reflection.
            var field = awaiter.GetType().GetField("_value", BindingFlags.NonPublic | BindingFlags.Instance);
            var awaitedTask = (ValueTask)field?.GetValue(awaiter);
            return new ValueTaskAwaiter(ref awaitedTask, ref awaiter);
        }

        /// <summary>
        /// Wraps the specified generic value task awaiter.
        /// </summary>
        public static ValueTaskAwaiter<TResult> Wrap<TResult>(SystemCompiler.ValueTaskAwaiter<TResult> awaiter)
        {
            // Access the generic task being awaited through reflection.
            var field = awaiter.GetType().GetField("_value", BindingFlags.NonPublic | BindingFlags.Instance);
            var awaitedTask = (ValueTask<TResult>)field?.GetValue(awaiter);
            return new ValueTaskAwaiter<TResult>(ref awaitedTask, ref awaiter);
        }

        /// <summary>
        /// Tries to safely retrieve the payload of a value task if that payload is an asynchronous task.
        /// If the payload is a <see cref="SystemTasks.Sources.IValueTaskSource"/>, then it returns null.
        /// </summary>
        internal static bool TryGetTask(ref SystemValueTask task, out SystemTask payload)
        {
            // Access the payload through reflection.
            var field = task.GetType().GetField("_obj", BindingFlags.NonPublic | BindingFlags.Instance);
            payload = field?.GetValueDirect(__makeref(task)) as SystemTask;
            return payload != null;
        }

        /// <summary>
        /// Tries to safely retrieve the payload of a value task if that payload is an asynchronous task.
        /// If the payload is a <see cref="SystemTasks.Sources.IValueTaskSource"/>, then it returns null.
        /// </summary>
        internal static bool TryGetTask<TResult>(ref SystemTasks.ValueTask<TResult> task,
            out SystemTasks.Task<TResult> payload)
        {
            // Access the payload through reflection.
            var field = task.GetType().GetField("_obj", BindingFlags.NonPublic | BindingFlags.Instance);
            payload = field?.GetValueDirect(__makeref(task)) as SystemTasks.Task<TResult>;
            return payload != null;
        }
    }

    /// <summary>
    /// Implements a <see cref="ValueTask"/> awaiter. This type is intended for compiler use only.
    /// </summary>
    /// <typeparam name="TResult">The type of the produced result.</typeparam>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public readonly struct ValueTaskAwaiter<TResult> : IControllableAwaiter, ICriticalNotifyCompletion, INotifyCompletion
    {
        // WARNING: The layout must remain the same, as the struct is used to access
        // the generic ValueTaskAwaiter<> as ValueTaskAwaiter.

        /// <summary>
        /// The inner task being awaited.
        /// </summary>
        private readonly SystemTasks.Task<TResult> AwaitedTask;

        /// <summary>
        /// The value task awaiter.
        /// </summary>
        private readonly SystemCompiler.ValueTaskAwaiter<TResult> Awaiter;

        /// <summary>
        /// The runtime controlling this awaiter.
        /// </summary>
        private readonly CoyoteRuntime Runtime;

        /// <summary>
        /// Gets a value that indicates whether the controlled value task has completed.
        /// </summary>
        public bool IsCompleted => this.AwaitedTask?.IsCompleted ?? this.Awaiter.IsCompleted;

        /// <inheritdoc/>
        bool IControllableAwaiter.IsControlled =>
            !this.Runtime?.IsTaskUncontrolled(this.AwaitedTask) ?? false;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTaskAwaiter{TResult}"/> struct.
        /// </summary>
        internal ValueTaskAwaiter(ref SystemTasks.ValueTask<TResult> awaitedTask)
        {
            this.AwaitedTask = ValueTaskAwaiter.TryGetTask<TResult>(ref awaitedTask, out Task<TResult> innerTask) ?
                innerTask : null;
            this.Awaiter = awaitedTask.GetAwaiter();
            RuntimeProvider.TryGetFromSynchronizationContext(out CoyoteRuntime runtime);
            this.Runtime = runtime;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTaskAwaiter{TResult}"/> struct.
        /// </summary>
        internal ValueTaskAwaiter(ref SystemTasks.ValueTask<TResult> awaitedTask,
            ref SystemCompiler.ValueTaskAwaiter<TResult> awaiter)
        {
            this.AwaitedTask = ValueTaskAwaiter.TryGetTask<TResult>(ref awaitedTask, out Task<TResult> innerTask) ?
                innerTask : null;
            this.Awaiter = awaiter;
            RuntimeProvider.TryGetFromSynchronizationContext(out CoyoteRuntime runtime);
            this.Runtime = runtime;
        }

        /// <summary>
        /// Ends the wait for the completion of the controlled value task.
        /// </summary>
        public TResult GetResult()
        {
            if (this.AwaitedTask != null)
            {
                this.Runtime?.WaitUntilTaskCompletes(this.AwaitedTask);
            }

            return this.Awaiter.GetResult();
        }

        /// <summary>
        /// Sets the action to perform when the controlled value task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void OnCompleted(Action continuation) => this.Awaiter.OnCompleted(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the controlled value task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void UnsafeOnCompleted(Action continuation) => this.Awaiter.UnsafeOnCompleted(continuation);
    }
}
