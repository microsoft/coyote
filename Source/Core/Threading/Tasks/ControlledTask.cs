// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Threading.Tasks
{
    /// <summary>
    /// Represents an asynchronous operation. Each <see cref="ControlledTask"/> is a thin wrapper
    /// over <see cref="Task"/> and each call simply invokes the wrapped task. During testing, a
    /// <see cref="ControlledTask"/> is controlled by the runtime and systematically interleaved
    /// with other asynchronous operations to find bugs.
    /// </summary>
    [AsyncMethodBuilder(typeof(AsyncControlledTaskMethodBuilder))]
    public class ControlledTask : IDisposable
    {
        /// <summary>
        /// A <see cref="ControlledTask"/> that has completed successfully.
        /// </summary>
        public static ControlledTask CompletedTask { get; } = new ControlledTask(Task.CompletedTask);

        /// <summary>
        /// Returns the id of the currently executing <see cref="ControlledTask"/>.
        /// </summary>
        public static int? CurrentId => MachineRuntime.Current.CurrentTaskId;

        /// <summary>
        /// Internal task used to execute the work.
        /// </summary>
        private protected readonly Task InternalTask;

        /// <summary>
        /// The id of this task.
        /// </summary>
        public int Id => this.InternalTask.Id;

        /// <summary>
        /// Task that provides access to the completed work.
        /// </summary>
        internal Task AwaiterTask => this.InternalTask;

        /// <summary>
        /// Value that indicates whether the task has completed.
        /// </summary>
        public bool IsCompleted => this.InternalTask.IsCompleted;

        /// <summary>
        /// Value that indicates whether the task completed execution due to being canceled.
        /// </summary>
        public bool IsCanceled => this.InternalTask.IsCanceled;

        /// <summary>
        /// Value that indicates whether the task completed due to an unhandled exception.
        /// </summary>
        public bool IsFaulted => this.InternalTask.IsFaulted;

        /// <summary>
        /// Gets the <see cref="System.AggregateException"/> that caused the task
        /// to end prematurely. If the task completed successfully or has not yet
        /// thrown any exceptions, this will return null.
        /// </summary>
        public AggregateException Exception => this.InternalTask.Exception;

        /// <summary>
        /// The status of this task.
        /// </summary>
        public TaskStatus Status => this.InternalTask.Status;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledTask"/> class.
        /// </summary>
        internal ControlledTask(Task task)
        {
            this.InternalTask = task ?? throw new ArgumentNullException(nameof(task));
        }

        /// <summary>
        /// Creates a <see cref="ControlledTask{TResult}"/> that is completed successfully with the specified result.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
        /// <param name="result">The result to store into the completed task.</param>
        /// <returns>The successfully completed task.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask<TResult> FromResult<TResult>(TResult result) =>
            new ControlledTask<TResult>(Task.FromResult(result));

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that is completed due to
        /// cancellation with a specified cancellation token.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token with which to complete the task.</param>
        /// <returns>The canceled task.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask FromCanceled(CancellationToken cancellationToken) =>
            new ControlledTask(Task.FromCanceled(cancellationToken));

        /// <summary>
        /// Creates a <see cref="ControlledTask{TResult}"/> that is completed due to
        /// cancellation with a specified cancellation token.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
        /// <param name="cancellationToken">The cancellation token with which to complete the task.</param>
        /// <returns>The canceled task.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask<TResult> FromCanceled<TResult>(CancellationToken cancellationToken) =>
            new ControlledTask<TResult>(Task.FromCanceled<TResult>(cancellationToken));

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that is completed with a specified exception.
        /// </summary>
        /// <param name="exception">The exception with which to complete the task.</param>
        /// <returns>The faulted task.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask FromException(Exception exception) =>
            new ControlledTask(Task.FromException(exception));

        /// <summary>
        /// Creates a <see cref="ControlledTask{TResult}"/> that is completed with a specified exception.
        /// </summary>
        /// <typeparam name="TResult">The type of the result returned by the task.</typeparam>
        /// <param name="exception">The exception with which to complete the task.</param>
        /// <returns>The faulted task.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask<TResult> FromException<TResult>(Exception exception) =>
            new ControlledTask<TResult>(Task.FromException<TResult>(exception));

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a <see cref="ControlledTask"/>
        /// object that represents that work. A cancellation token allows the work to be cancelled.
        /// </summary>
        /// <param name="action">The work to execute asynchronously.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask Run(Action action) => MachineRuntime.Current.CreateControlledTask(action, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a <see cref="ControlledTask"/>
        /// object that represents that work.
        /// </summary>
        /// <param name="action">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask Run(Action action, CancellationToken cancellationToken) =>
            MachineRuntime.Current.CreateControlledTask(action, cancellationToken);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a <see cref="ControlledTask"/>
        /// object that represents that work.
        /// </summary>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask Run(Func<Task> function) => MachineRuntime.Current.CreateControlledTask(function, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a <see cref="ControlledTask"/>
        /// object that represents that work. A cancellation token allows the work to be cancelled.
        /// </summary>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask Run(Func<Task> function, CancellationToken cancellationToken) =>
            MachineRuntime.Current.CreateControlledTask(function, cancellationToken);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a <see cref="ControlledTask"/>
        /// object that represents that work.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask<TResult> Run<TResult>(Func<TResult> function) =>
            MachineRuntime.Current.CreateControlledTask(function, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a <see cref="ControlledTask"/>
        /// object that represents that work. A cancellation token allows the work to be cancelled.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask<TResult> Run<TResult>(Func<TResult> function, CancellationToken cancellationToken) =>
            MachineRuntime.Current.CreateControlledTask(function, cancellationToken);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a <see cref="ControlledTask"/>
        /// object that represents that work.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask<TResult> Run<TResult>(Func<Task<TResult>> function) =>
            MachineRuntime.Current.CreateControlledTask(function, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a <see cref="ControlledTask"/>
        /// object that represents that work. A cancellation token allows the work to be cancelled.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask<TResult> Run<TResult>(Func<Task<TResult>> function, CancellationToken cancellationToken) =>
            MachineRuntime.Current.CreateControlledTask(function, cancellationToken);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that completes after a time delay.
        /// </summary>
        /// <param name="millisecondsDelay">
        /// The number of milliseconds to wait before completing the returned task, or -1 to wait indefinitely.
        /// </param>
        /// <returns>Task that represents the time delay.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask Delay(int millisecondsDelay) =>
            MachineRuntime.Current.CreateControlledTaskDelay(millisecondsDelay, default);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that completes after a time delay.
        /// </summary>
        /// <param name="millisecondsDelay">
        /// The number of milliseconds to wait before completing the returned task, or -1 to wait indefinitely.
        /// </param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the delay.</param>
        /// <returns>Task that represents the time delay.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask Delay(int millisecondsDelay, CancellationToken cancellationToken) =>
            MachineRuntime.Current.CreateControlledTaskDelay(millisecondsDelay, cancellationToken);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that completes after a specified time interval.
        /// </summary>
        /// <param name="delay">
        /// The time span to wait before completing the returned task, or TimeSpan.FromMilliseconds(-1)
        /// to wait indefinitely.
        /// </param>
        /// <returns>Task that represents the time delay.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask Delay(TimeSpan delay) =>
            MachineRuntime.Current.CreateControlledTaskDelay(delay, default);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that completes after a specified time interval.
        /// </summary>
        /// <param name="delay">
        /// The time span to wait before completing the returned task, or TimeSpan.FromMilliseconds(-1)
        /// to wait indefinitely.
        /// </param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the delay.</param>
        /// <returns>Task that represents the time delay.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask Delay(TimeSpan delay, CancellationToken cancellationToken) =>
            MachineRuntime.Current.CreateControlledTaskDelay(delay, cancellationToken);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask WhenAll(params ControlledTask[] tasks) =>
            MachineRuntime.Current.WaitAllTasksAsync(tasks);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask WhenAll(params Task[] tasks) =>
            MachineRuntime.Current.WaitAllTasksAsync(tasks);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask WhenAll(IEnumerable<ControlledTask> tasks) =>
            MachineRuntime.Current.WaitAllTasksAsync(tasks);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask WhenAll(IEnumerable<Task> tasks) =>
            MachineRuntime.Current.WaitAllTasksAsync(tasks);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask<TResult[]> WhenAll<TResult>(params ControlledTask<TResult>[] tasks) =>
            MachineRuntime.Current.WaitAllTasksAsync(tasks);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask<TResult[]> WhenAll<TResult>(params Task<TResult>[] tasks) =>
            MachineRuntime.Current.WaitAllTasksAsync(tasks);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask<TResult[]> WhenAll<TResult>(IEnumerable<ControlledTask<TResult>> tasks) =>
            MachineRuntime.Current.WaitAllTasksAsync(tasks);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask<TResult[]> WhenAll<TResult>(IEnumerable<Task<TResult>> tasks) =>
            MachineRuntime.Current.WaitAllTasksAsync(tasks);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask<Task> WhenAny(params ControlledTask[] tasks) =>
            MachineRuntime.Current.WaitAnyTaskAsync(tasks);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask<Task> WhenAny(params Task[] tasks) =>
            MachineRuntime.Current.WaitAnyTaskAsync(tasks);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask<Task> WhenAny(IEnumerable<ControlledTask> tasks) =>
            MachineRuntime.Current.WaitAnyTaskAsync(tasks);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask<Task> WhenAny(IEnumerable<Task> tasks) =>
            MachineRuntime.Current.WaitAnyTaskAsync(tasks);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask<Task<TResult>> WhenAny<TResult>(params ControlledTask<TResult>[] tasks) =>
            MachineRuntime.Current.WaitAnyTaskAsync(tasks);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask<Task<TResult>> WhenAny<TResult>(params Task<TResult>[] tasks) =>
            MachineRuntime.Current.WaitAnyTaskAsync(tasks);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask<Task<TResult>> WhenAny<TResult>(IEnumerable<ControlledTask<TResult>> tasks) =>
            MachineRuntime.Current.WaitAnyTaskAsync(tasks);

        /// <summary>
        /// Creates a <see cref="ControlledTask"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ControlledTask<Task<TResult>> WhenAny<TResult>(IEnumerable<Task<TResult>> tasks) =>
            MachineRuntime.Current.WaitAnyTaskAsync(tasks);

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete execution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(params ControlledTask[] tasks) =>
            MachineRuntime.Current.WaitAnyTask(tasks);

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified number of milliseconds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(ControlledTask[] tasks, int millisecondsTimeout) =>
            MachineRuntime.Current.WaitAnyTask(tasks, millisecondsTimeout);

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified number of milliseconds or until a cancellation
        /// token is cancelled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(ControlledTask[] tasks, int millisecondsTimeout, CancellationToken cancellationToken) =>
            MachineRuntime.Current.WaitAnyTask(tasks, millisecondsTimeout, cancellationToken);

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete
        /// execution unless the wait is cancelled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(ControlledTask[] tasks, CancellationToken cancellationToken) =>
            MachineRuntime.Current.WaitAnyTask(tasks, cancellationToken);

        /// <summary>
        /// Waits for any of the provided <see cref="ControlledTask"/> objects to complete
        /// execution within a specified time interval.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(ControlledTask[] tasks, TimeSpan timeout) =>
            MachineRuntime.Current.WaitAnyTask(tasks, timeout);

        /// <summary>
        /// Creates an awaitable that asynchronously yields back to the current context when awaited.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static YieldAwaitable Yield() => default;

        /// <summary>
        /// Converts the specified <see cref="ControlledTask"/> into a <see cref="Task"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Task ToTask() => this.InternalTask;

        /// <summary>
        /// Gets an awaiter for this awaitable.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual ControlledTaskAwaiter GetAwaiter() => new ControlledTaskAwaiter(this, this.InternalTask);

        /// <summary>
        /// Ends the wait for the completion of the task.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void GetResult(TaskAwaiter awaiter) => awaiter.GetResult();

        /// <summary>
        /// Sets the action to perform when the task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void OnCompleted(Action continuation, TaskAwaiter awaiter) => awaiter.OnCompleted(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void UnsafeOnCompleted(Action continuation, TaskAwaiter awaiter) =>
            awaiter.UnsafeOnCompleted(continuation);

        /// <summary>
        /// Configures an awaiter used to await this task.
        /// </summary>
        /// <param name="continueOnCapturedContext">
        /// True to attempt to marshal the continuation back to the original context captured; otherwise, false.
        /// </param>
        public virtual ConfiguredControlledTaskAwaitable ConfigureAwait(bool continueOnCapturedContext) =>
            new ConfiguredControlledTaskAwaitable(this, this.InternalTask, continueOnCapturedContext);

        /// <summary>
        /// Injects a context switch point that can be systematically explored during testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExploreContextSwitch() => MachineRuntime.Current.ExploreContextSwitch();

        /// <summary>
        /// Ends the wait for the completion of the task.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void GetResult(ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter) => awaiter.GetResult();

        /// <summary>
        /// Sets the action to perform when the task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void OnCompleted(Action continuation, ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter) =>
            awaiter.OnCompleted(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void UnsafeOnCompleted(Action continuation, ConfiguredTaskAwaitable.ConfiguredTaskAwaiter awaiter) =>
            awaiter.UnsafeOnCompleted(continuation);

        /// <summary>
        /// Disposes the <see cref="ControlledTask"/>, releasing all of its unmanaged resources.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Disposes the <see cref="ControlledTask"/>, releasing all of its unmanaged resources.
        /// </summary>
        /// <remarks>
        /// Unlike most of the members of <see cref="ControlledTask"/>, this method is not thread-safe.
        /// </remarks>
        public void Dispose()
        {
            this.InternalTask.Dispose();
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// Represents an asynchronous operation that can return a value. Each <see cref="ControlledTask{TResult}"/>
    /// is a thin wrapper over <see cref="Task{TResult}"/> and each call simply invokes the wrapped task. During
    /// testing, a <see cref="ControlledTask"/> is controlled by the runtime and systematically interleaved with
    /// other asynchronous operations to find bugs.
    /// </summary>
    /// <typeparam name="TResult">The type of the produced result.</typeparam>
    [AsyncMethodBuilder(typeof(AsyncControlledTaskMethodBuilder<>))]
    public class ControlledTask<TResult> : ControlledTask
    {
        /// <summary>
        /// Task that provides access to the completed work.
        /// </summary>
        internal new Task<TResult> AwaiterTask => this.InternalTask as Task<TResult>;

        /// <summary>
        /// Gets the result value of this task.
        /// </summary>
        public TResult Result => this.AwaiterTask.Result;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledTask{TResult}"/> class.
        /// </summary>
        internal ControlledTask(Task<TResult> task)
            : base(task)
        {
        }

        /// <summary>
        /// Gets an awaiter for this awaitable.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public new virtual ControlledTaskAwaiter<TResult> GetAwaiter() =>
            new ControlledTaskAwaiter<TResult>(this, this.AwaiterTask);

        /// <summary>
        /// Ends the wait for the completion of the task.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual TResult GetResult(TaskAwaiter<TResult> awaiter) => awaiter.GetResult();

        /// <summary>
        /// Sets the action to perform when the task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void OnCompleted(Action continuation, TaskAwaiter<TResult> awaiter) =>
            awaiter.OnCompleted(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void UnsafeOnCompleted(Action continuation, TaskAwaiter<TResult> awaiter) =>
            awaiter.UnsafeOnCompleted(continuation);

        /// <summary>
        /// Configures an awaiter used to await this task.
        /// </summary>
        /// <param name="continueOnCapturedContext">
        /// True to attempt to marshal the continuation back to the original context captured; otherwise, false.
        /// </param>
        public new virtual ConfiguredControlledTaskAwaitable<TResult> ConfigureAwait(bool continueOnCapturedContext) =>
            new ConfiguredControlledTaskAwaitable<TResult>(this, this.AwaiterTask, continueOnCapturedContext);

        /// <summary>
        /// Ends the wait for the completion of the task.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual TResult GetResult(ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter awaiter) =>
            awaiter.GetResult();

        /// <summary>
        /// Sets the action to perform when the task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void OnCompleted(Action continuation, ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter awaiter) =>
            awaiter.OnCompleted(continuation);

        /// <summary>
        /// Schedules the continuation action that is invoked when the task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void UnsafeOnCompleted(Action continuation, ConfiguredTaskAwaitable<TResult>.ConfiguredTaskAwaiter awaiter) =>
            awaiter.UnsafeOnCompleted(continuation);
    }
}
