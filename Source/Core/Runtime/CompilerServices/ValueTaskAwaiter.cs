// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using SystemCompiler = System.Runtime.CompilerServices;
using SystemTasks = System.Threading.Tasks;
using SystemValueTask = System.Threading.Tasks.ValueTask;

namespace Microsoft.Coyote.Runtime.CompilerServices
{
    /// <summary>
    /// Implements a <see cref="ValueTask"/> awaiter. This type is intended for compiler use only.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public readonly struct ValueTaskAwaiter : ICriticalNotifyCompletion, INotifyCompletion
    {
        // WARNING: The layout must remain the same, as the struct is used to access
        // the generic ValueTaskAwaiter<> as ValueTaskAwaiter.

        /// <summary>
        /// The value task being awaited.
        /// </summary>
        private readonly SystemValueTask AwaitedTask;

        /// <summary>
        /// The value task awaiter.
        /// </summary>
        private readonly SystemCompiler.ValueTaskAwaiter Awaiter;

        /// <summary>
        /// Gets a value that indicates whether the controlled value task has completed.
        /// </summary>
        public bool IsCompleted => this.Awaiter.IsCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTaskAwaiter"/> struct.
        /// </summary>
        internal ValueTaskAwaiter(in SystemValueTask awaitedTask)
        {
            this.AwaitedTask = awaitedTask;
            this.Awaiter = awaitedTask.GetAwaiter();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTaskAwaiter"/> struct.
        /// </summary>
        private ValueTaskAwaiter(in SystemValueTask awaitedTask, SystemCompiler.ValueTaskAwaiter awaiter)
        {
            this.AwaitedTask = awaitedTask;
            this.Awaiter = awaiter;
        }

        /// <summary>
        /// Ends the wait for the completion of the controlled value task.
        /// </summary>
        public void GetResult()
        {
            if (SynchronizationContext.Current is ControlledSynchronizationContext context)
            {
                context.Runtime?.WaitUntilTaskCompletes(this.AwaitedTask.AsTask());
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
            return new ValueTaskAwaiter(in awaitedTask, awaiter);
        }

        /// <summary>
        /// Wraps the specified generic value task awaiter.
        /// </summary>
        public static ValueTaskAwaiter<TResult> Wrap<TResult>(SystemCompiler.ValueTaskAwaiter<TResult> awaiter)
        {
            // Access the generic task being awaited through reflection.
            var field = awaiter.GetType().GetField("_value", BindingFlags.NonPublic | BindingFlags.Instance);
            var awaitedTask = (ValueTask<TResult>)field?.GetValue(awaiter);
            return new ValueTaskAwaiter<TResult>(in awaitedTask, awaiter);
        }
    }

    /// <summary>
    /// Implements a <see cref="ValueTask"/> awaiter. This type is intended for compiler use only.
    /// </summary>
    /// <typeparam name="TResult">The type of the produced result.</typeparam>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public readonly struct ValueTaskAwaiter<TResult> : ICriticalNotifyCompletion, INotifyCompletion
    {
        // WARNING: The layout must remain the same, as the struct is used to access
        // the generic ValueTaskAwaiter<> as ValueTaskAwaiter.

        /// <summary>
        /// The value task being awaited.
        /// </summary>
        private readonly SystemTasks.ValueTask<TResult> AwaitedTask;

        /// <summary>
        /// The value task awaiter.
        /// </summary>
        private readonly SystemCompiler.ValueTaskAwaiter<TResult> Awaiter;

        /// <summary>
        /// Gets a value that indicates whether the controlled value task has completed.
        /// </summary>
        public bool IsCompleted => this.Awaiter.IsCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTaskAwaiter{TResult}"/> struct.
        /// </summary>
        internal ValueTaskAwaiter(in SystemTasks.ValueTask<TResult> awaitedTask)
        {
            this.AwaitedTask = awaitedTask;
            this.Awaiter = awaitedTask.GetAwaiter();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ValueTaskAwaiter{TResult}"/> struct.
        /// </summary>
        internal ValueTaskAwaiter(in SystemTasks.ValueTask<TResult> awaitedTask,
            SystemCompiler.ValueTaskAwaiter<TResult> awaiter)
        {
            this.AwaitedTask = awaitedTask;
            this.Awaiter = awaiter;
        }

        /// <summary>
        /// Ends the wait for the completion of the controlled value task.
        /// </summary>
        public TResult GetResult()
        {
            if (SynchronizationContext.Current is ControlledSynchronizationContext context)
            {
                context.Runtime?.WaitUntilTaskCompletes(this.AwaitedTask.AsTask());
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
