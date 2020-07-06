// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using CoyoteTasks = Microsoft.Coyote.Tasks;
using MethodImpl = System.Runtime.CompilerServices.MethodImplAttribute;
using MethodImplOptions = System.Runtime.CompilerServices.MethodImplOptions;

namespace Microsoft.Coyote.SystematicTesting.Interception
{
    /// <summary>
    /// Provides methods for creating tasks that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    public static class ControlledTask
    {
        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a <see cref="Task"/>
        /// object that represents that work. A cancellation token allows the work to be cancelled.
        /// </summary>
        /// <param name="action">The work to execute asynchronously.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Run(Action action) => Run(action, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a <see cref="Task"/>
        /// object that represents that work.
        /// </summary>
        /// <param name="action">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Run(Action action, CancellationToken cancellationToken) => CoyoteRuntime.IsExecutionControlled ?
            ControlledRuntime.Current.TaskController.ScheduleAction(action, null, false, cancellationToken) :
            Task.Run(action, cancellationToken);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a proxy for
        /// the <see cref="Task"/> returned by the function.
        /// </summary>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Run(Func<Task> function) => Run(function, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a proxy for
        /// the <see cref="Task"/> returned by the function. A cancellation
        /// token allows the work to be cancelled.
        /// </summary>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Run(Func<Task> function, CancellationToken cancellationToken) => CoyoteRuntime.IsExecutionControlled ?
            ControlledRuntime.Current.TaskController.ScheduleFunction(function, null, cancellationToken) :
            Task.Run(function, cancellationToken);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a proxy for the
        /// <see cref="Task{TResult}"/> returned by the function.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TResult> Run<TResult>(Func<Task<TResult>> function) => Run(function, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a proxy for the
        /// <see cref="Task{TResult}"/> returned by the function. A cancellation
        /// token allows the work to be cancelled.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TResult> Run<TResult>(Func<Task<TResult>> function, CancellationToken cancellationToken) =>
            CoyoteRuntime.IsExecutionControlled ?
            ControlledRuntime.Current.TaskController.ScheduleFunction(function, null, cancellationToken) :
            Task.Run(function, cancellationToken);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a <see cref="Task"/>
        /// object that represents that work.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TResult> Run<TResult>(Func<TResult> function) => Run(function, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a <see cref="Task"/>
        /// object that represents that work. A cancellation token allows the work to be cancelled.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="function">The work to execute asynchronously.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the work.</param>
        /// <returns>Task that represents the work to run asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TResult> Run<TResult>(Func<TResult> function, CancellationToken cancellationToken) =>
            CoyoteRuntime.IsExecutionControlled ?
            ControlledRuntime.Current.TaskController.ScheduleDelegate<TResult>(function, null, cancellationToken) :
            Task.Run(function, cancellationToken);

        /// <summary>
        /// Creates a <see cref="Task"/> that completes after a time delay.
        /// </summary>
        /// <param name="millisecondsDelay">
        /// The number of milliseconds to wait before completing the returned task, or -1 to wait indefinitely.
        /// </param>
        /// <returns>Task that represents the time delay.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Delay(int millisecondsDelay) => CoyoteRuntime.IsExecutionControlled ?
            ControlledRuntime.Current.TaskController.ScheduleDelay(TimeSpan.FromMilliseconds(millisecondsDelay), default) :
            Task.Delay(millisecondsDelay);

        /// <summary>
        /// Creates a <see cref="Task"/> that completes after a time delay.
        /// </summary>
        /// <param name="millisecondsDelay">
        /// The number of milliseconds to wait before completing the returned task, or -1 to wait indefinitely.
        /// </param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the delay.</param>
        /// <returns>Task that represents the time delay.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Delay(int millisecondsDelay, CancellationToken cancellationToken) => CoyoteRuntime.IsExecutionControlled ?
            ControlledRuntime.Current.TaskController.ScheduleDelay(TimeSpan.FromMilliseconds(millisecondsDelay), cancellationToken) :
            Task.Delay(millisecondsDelay, cancellationToken);

        /// <summary>
        /// Creates a <see cref="Task"/> that completes after a specified time interval.
        /// </summary>
        /// <param name="delay">
        /// The time span to wait before completing the returned task, or TimeSpan.FromMilliseconds(-1)
        /// to wait indefinitely.
        /// </param>
        /// <returns>Task that represents the time delay.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Delay(TimeSpan delay) => CoyoteRuntime.IsExecutionControlled ?
            ControlledRuntime.Current.TaskController.ScheduleDelay(delay, default) : Task.Delay(delay);

        /// <summary>
        /// Creates a <see cref="Task"/> that completes after a specified time interval.
        /// </summary>
        /// <param name="delay">
        /// The time span to wait before completing the returned task, or TimeSpan.FromMilliseconds(-1)
        /// to wait indefinitely.
        /// </param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the delay.</param>
        /// <returns>Task that represents the time delay.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Delay(TimeSpan delay, CancellationToken cancellationToken) => CoyoteRuntime.IsExecutionControlled ?
            ControlledRuntime.Current.TaskController.ScheduleDelay(delay, cancellationToken) : Task.Delay(delay, cancellationToken);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WhenAll(params Task[] tasks) => CoyoteRuntime.IsExecutionControlled ?
            ControlledRuntime.Current.TaskController.WhenAllTasksCompleteAsync(tasks) : Task.WhenAll(tasks);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WhenAll(IEnumerable<Task> tasks) => CoyoteRuntime.IsExecutionControlled ?
            ControlledRuntime.Current.TaskController.WhenAllTasksCompleteAsync(tasks) : Task.WhenAll(tasks);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TResult[]> WhenAll<TResult>(params Task<TResult>[] tasks) => CoyoteRuntime.IsExecutionControlled ?
            ControlledRuntime.Current.TaskController.WhenAllTasksCompleteAsync(tasks) : Task.WhenAll(tasks);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <typeparam name="TResult">The result type of the task.</typeparam>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>Task that represents the completion of all of the specified tasks.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TResult[]> WhenAll<TResult>(IEnumerable<Task<TResult>> tasks) => CoyoteRuntime.IsExecutionControlled ?
            ControlledRuntime.Current.TaskController.WhenAllTasksCompleteAsync(tasks) : Task.WhenAll(tasks);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Task> WhenAny(params Task[] tasks) => CoyoteRuntime.IsExecutionControlled ?
            ControlledRuntime.Current.TaskController.WhenAnyTaskCompletesAsync(tasks) : Task.WhenAny(tasks);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Task> WhenAny(IEnumerable<Task> tasks) => CoyoteRuntime.IsExecutionControlled ?
            ControlledRuntime.Current.TaskController.WhenAnyTaskCompletesAsync(tasks) : Task.WhenAny(tasks);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Task<TResult>> WhenAny<TResult>(params Task<TResult>[] tasks) => CoyoteRuntime.IsExecutionControlled ?
            ControlledRuntime.Current.TaskController.WhenAnyTaskCompletesAsync(tasks) : Task.WhenAny(tasks);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Task<TResult>> WhenAny<TResult>(IEnumerable<Task<TResult>> tasks) => CoyoteRuntime.IsExecutionControlled ?
            ControlledRuntime.Current.TaskController.WhenAnyTaskCompletesAsync(tasks) : Task.WhenAny(tasks);

        /// <summary>
        /// Returns a <see cref="CoyoteTasks.TaskAwaiter"/> for the specified <see cref="Task"/>.
        /// </summary>
        public static CoyoteTasks.TaskAwaiter GetAwaiter(Task task) => new CoyoteTasks.TaskAwaiter(
            CoyoteRuntime.IsExecutionControlled ? ControlledRuntime.Current.TaskController : null, task);

        /// <summary>
        /// Returns a <see cref="CoyoteTasks.TaskAwaiter{TResult}"/> for the specified <see cref="Task{TResult}"/>.
        /// </summary>
        public static CoyoteTasks.TaskAwaiter<TResult> GetAwaiter<TResult>(Task<TResult> task) => new CoyoteTasks.TaskAwaiter<TResult>(
            CoyoteRuntime.IsExecutionControlled ? ControlledRuntime.Current.TaskController : null, task);

        /// <summary>
        /// Creates an awaitable that asynchronously yields back to the current context when awaited.
        /// </summary>
        /// <remarks>
        /// You can use `await Task.Yield()` in an asynchronous method to force the method to complete
        /// asynchronously. During systematic testing, the underlying scheduling strategy can use this
        /// as a hint on how to better prioritize this work relative to other work that may be pending.
        /// </remarks>
        public static CoyoteTasks.YieldAwaitable Yield() =>
            new CoyoteTasks.YieldAwaitable(CoyoteRuntime.IsExecutionControlled ? ControlledRuntime.Current.TaskController : null);

        /// <summary>
        /// Injects a context switch point that can be systematically explored during testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExploreContextSwitch() => ControlledRuntime.Current.ScheduleNextOperation();
    }
}
