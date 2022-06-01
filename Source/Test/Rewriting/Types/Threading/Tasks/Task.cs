// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Runtime.CompilerServices;
using MethodImpl = System.Runtime.CompilerServices.MethodImplAttribute;
using MethodImplOptions = System.Runtime.CompilerServices.MethodImplOptions;
using SystemCancellationToken = System.Threading.CancellationToken;
using SystemTask = System.Threading.Tasks.Task;
using SystemTaskContinuationOptions = System.Threading.Tasks.TaskContinuationOptions;
using SystemTaskCreationOptions = System.Threading.Tasks.TaskCreationOptions;
using SystemTaskFactory = System.Threading.Tasks.TaskFactory;
using SystemTasks = System.Threading.Tasks;
using SystemTimeout = System.Threading.Timeout;

namespace Microsoft.Coyote.Rewriting.Types.Threading.Tasks
{
    /// <summary>
    /// Provides methods for creating tasks that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class Task
    {
        /// <summary>
        /// Gets a task that has already completed successfully.
        /// </summary>
        public static SystemTask CompletedTask { get; } = SystemTask.CompletedTask;

        /// <summary>
        /// The default task factory.
        /// </summary>
        private static SystemTaskFactory DefaultFactory = new SystemTaskFactory();

        /// <summary>
        /// Provides access to factory methods for creating controlled task and generic task instances.
        /// </summary>
        public static SystemTaskFactory Factory
        {
            get
            {
                var runtime = CoyoteRuntime.Current;
                if (runtime.SchedulingPolicy is SchedulingPolicy.None)
                {
                    return DefaultFactory;
                }

                return runtime.TaskFactory;
            }
        }

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a task object that
        /// represents that work. A cancellation token allows the work to be cancelled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTask Run(Action action) => Run(action, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a task
        /// object that represents that work.
        /// </summary>
        public static SystemTask Run(Action action, SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return SystemTask.Run(action, cancellationToken);
            }

            var taskFactory = runtime.TaskFactory;
            return taskFactory.StartNew(action, cancellationToken,
                taskFactory.CreationOptions | SystemTaskCreationOptions.DenyChildAttach,
                taskFactory.Scheduler);
        }

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a task
        /// object that represents that work.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<TResult> Run<TResult>(Func<TResult> function) => Run(function, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a task object that
        /// represents that work. A cancellation token allows the work to be cancelled.
        /// </summary>
        public static SystemTasks.Task<TResult> Run<TResult>(Func<TResult> function,
            SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return SystemTask.Run(function, cancellationToken);
            }

            var taskFactory = runtime.TaskFactory;
            return taskFactory.StartNew(function, cancellationToken,
                taskFactory.CreationOptions | SystemTaskCreationOptions.DenyChildAttach,
                taskFactory.Scheduler);
        }

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a proxy for
        /// the task returned by the function.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTask Run(Func<SystemTask> function) => Run(function, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a proxy for the task
        /// returned by the function. A cancellation token allows the work to be cancelled.
        /// </summary>
        public static SystemTask Run(Func<SystemTask> function,
            SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return SystemTask.Run(function, cancellationToken);
            }

            var taskFactory = runtime.TaskFactory;
            return taskFactory.StartNew(function, cancellationToken,
                taskFactory.CreationOptions | SystemTaskCreationOptions.DenyChildAttach,
                taskFactory.Scheduler).Unwrap();
        }

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a proxy for the
        /// generic task returned by the function.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<TResult> Run<TResult>(Func<SystemTasks.Task<TResult>> function) =>
            Run(function, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a proxy for the generic
        /// task returned by the function. A cancellation token allows the work to be cancelled.
        /// </summary>
        public static SystemTasks.Task<TResult> Run<TResult>(Func<SystemTasks.Task<TResult>> function,
            SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return SystemTask.Run(function, cancellationToken);
            }

            var taskFactory = runtime.TaskFactory;
            return taskFactory.StartNew(function, cancellationToken,
                taskFactory.CreationOptions | SystemTaskCreationOptions.DenyChildAttach,
                taskFactory.Scheduler).Unwrap();
        }

        /// <summary>
        /// Creates a task that completes after a time delay.
        /// </summary>
        public static SystemTask Delay(int millisecondsDelay)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return SystemTask.Delay(millisecondsDelay);
            }

            return runtime.ScheduleDelay(TimeSpan.FromMilliseconds(millisecondsDelay), default);
        }

        /// <summary>
        /// Creates a task that completes after a time delay.
        /// </summary>
        public static SystemTask Delay(int millisecondsDelay, SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return SystemTask.Delay(millisecondsDelay, cancellationToken);
            }

            return runtime.ScheduleDelay(TimeSpan.FromMilliseconds(millisecondsDelay), cancellationToken);
        }

        /// <summary>
        /// Creates a task that completes after a specified time interval.
        /// </summary>
        public static SystemTask Delay(TimeSpan delay)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return SystemTask.Delay(delay);
            }

            return runtime.ScheduleDelay(delay, default);
        }

        /// <summary>
        /// Creates a task that completes after a specified time interval.
        /// </summary>
        public static SystemTask Delay(TimeSpan delay, SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return SystemTask.Delay(delay, cancellationToken);
            }

            return runtime.ScheduleDelay(delay, cancellationToken);
        }

        /// <summary>
        /// Creates a task that will complete when all tasks in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTask WhenAll(params SystemTask[] tasks) => SystemTask.WhenAll(tasks);

        /// <summary>
        /// Creates a task that will complete when all tasks in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTask WhenAll(IEnumerable<SystemTask> tasks) => SystemTask.WhenAll(tasks);

        /// <summary>
        /// Creates a task that will complete when all tasks in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<TResult[]> WhenAll<TResult>(params SystemTasks.Task<TResult>[] tasks) =>
            SystemTask.WhenAll(tasks);

        /// <summary>
        /// Creates a task that will complete when all tasks in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<TResult[]> WhenAll<TResult>(IEnumerable<SystemTasks.Task<TResult>> tasks) =>
            SystemTask.WhenAll(tasks);

        /// <summary>
        /// Creates a task that will complete when any task in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<SystemTask> WhenAny(params SystemTask[] tasks) =>
            SystemTask.WhenAny(tasks);

        /// <summary>
        /// Creates a task that will complete when any task in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<SystemTask> WhenAny(IEnumerable<SystemTask> tasks) =>
            SystemTask.WhenAny(tasks);

#if NET
        /// <summary>
        /// Creates a task that will complete when either of the two tasks have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<SystemTask> WhenAny(SystemTask task1, SystemTask task2) =>
            SystemTask.WhenAny(task1, task2);

        /// <summary>
        /// Creates a task that will complete when either of the two tasks have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<SystemTasks.Task<TResult>> WhenAny<TResult>(
            SystemTasks.Task<TResult> task1, SystemTasks.Task<TResult> task2) =>
            SystemTask.WhenAny(task1, task2);
#endif

        /// <summary>
        /// Creates a task that will complete when any task in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<SystemTasks.Task<TResult>> WhenAny<TResult>(
            params SystemTasks.Task<TResult>[] tasks) =>
            SystemTask.WhenAny(tasks);

        /// <summary>
        /// Creates a task that will complete when any task in the specified
        /// enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<SystemTasks.Task<TResult>> WhenAny<TResult>(
            IEnumerable<SystemTasks.Task<TResult>> tasks) =>
            SystemTask.WhenAny(tasks);

        /// <summary>
        /// Waits for all of the provided task objects to complete execution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WaitAll(params SystemTask[] tasks) =>
            WaitAll(tasks, SystemTimeout.Infinite, default);

        /// <summary>
        /// Waits for all of the provided task objects to complete execution
        /// within a specified time interval.
        /// </summary>
        public static bool WaitAll(SystemTask[] tasks, TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return WaitAll(tasks, (int)totalMilliseconds, default);
        }

        /// <summary>
        /// Waits for all of the provided task objects to complete execution within
        /// a specified number of milliseconds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool WaitAll(SystemTask[] tasks, int millisecondsTimeout) =>
            WaitAll(tasks, millisecondsTimeout, default);

        /// <summary>
        /// Waits for all of the provided task objects to complete execution unless the wait is cancelled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WaitAll(SystemTask[] tasks, SystemCancellationToken cancellationToken) =>
            WaitAll(tasks, SystemTimeout.Infinite, cancellationToken);

        /// <summary>
        /// Waits for any of the provided task objects to complete execution within a specified
        /// number of milliseconds or until a cancellation token is cancelled.
        /// </summary>
        public static bool WaitAll(SystemTask[] tasks, int millisecondsTimeout,
            SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                // TODO: support timeouts during testing, this would become false if there is a timeout.
                runtime.WaitAllTasksComplete(tasks);
            }

            return SystemTask.WaitAll(tasks, millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Waits for any of the provided task objects to complete execution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(params SystemTask[] tasks) =>
            WaitAny(tasks, SystemTimeout.Infinite, default);

        /// <summary>
        /// Waits for any of the provided task objects to complete execution within a specified time interval.
        /// </summary>
        public static int WaitAny(SystemTask[] tasks, TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return WaitAny(tasks, (int)totalMilliseconds, default);
        }

        /// <summary>
        /// Waits for any of the provided task objects to complete execution within
        /// a specified number of milliseconds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(SystemTask[] tasks, int millisecondsTimeout) =>
            WaitAny(tasks, millisecondsTimeout, default);

        /// <summary>
        /// Waits for any of the provided task objects to complete execution unless the wait is cancelled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(SystemTask[] tasks, SystemCancellationToken cancellationToken) =>
            WaitAny(tasks, SystemTimeout.Infinite, cancellationToken);

        /// <summary>
        /// Waits for any of the provided task objects to complete execution within a specified
        /// number of milliseconds or until a cancellation token is cancelled.
        /// </summary>
        public static int WaitAny(SystemTask[] tasks, int millisecondsTimeout,
            SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                // TODO: support timeouts during testing, this would become -1 if there is a timeout.
                return runtime.WaitAnyTaskCompletes(tasks);
            }

            return SystemTask.WaitAny(tasks, millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Waits for the specified task to complete execution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Wait(SystemTask task) => Wait(task, SystemTimeout.Infinite, default);

        /// <summary>
        /// Waits for the specified task to complete execution within a specified time interval.
        /// </summary>
        public static bool Wait(SystemTask task, TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return Wait(task, (int)totalMilliseconds, default);
        }

        /// <summary>
        /// Waits for the specified task to complete execution within a specified number of milliseconds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Wait(SystemTask task, int millisecondsTimeout) =>
            Wait(task, millisecondsTimeout, default);

        /// <summary>
        /// Waits for the specified task to complete execution. The wait terminates if a cancellation
        /// token is canceled before the task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Wait(SystemTask task, SystemCancellationToken cancellationToken) =>
            Wait(task, SystemTimeout.Infinite, cancellationToken);

        /// <summary>
        /// Waits for the specified task to complete execution. The wait terminates if a timeout interval
        /// elapses or a cancellation token is canceled before the task completes.
        /// </summary>
        public static bool Wait(SystemTask task, int millisecondsTimeout,
            SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None)
            {
                runtime.WaitUntilTaskCompletes(task);
            }

            return task.Wait(millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Creates a task that has completed successfully with the specified result.
        /// </summary>
        public static SystemTasks.Task<TResult> FromResult<TResult>(TResult result) =>
            SystemTask.FromResult(result);

        /// <summary>
        /// Creates a task that has completed due to cancellation with the specified cancellation token.
        /// </summary>
        public static SystemTask FromCanceled(SystemCancellationToken cancellationToken) =>
            SystemTask.FromCanceled(cancellationToken);

        /// <summary>
        /// Creates a task that has completed due to cancellation with the specified cancellation token.
        /// </summary>
        public static SystemTasks.Task<TResult> FromCanceled<TResult>(SystemCancellationToken cancellationToken) =>
            SystemTask.FromCanceled<TResult>(cancellationToken);

        /// <summary>
        /// Creates a task that has completed with the specified exception.
        /// </summary>
        public static SystemTask FromException(Exception exception) => SystemTask.FromException(exception);

        /// <summary>
        /// Creates a task that has completed with the specified exception.
        /// </summary>
        public static SystemTasks.Task<TResult> FromException<TResult>(Exception exception) =>
            SystemTask.FromException<TResult>(exception);

        /// <summary>
        /// Returns a task awaiter for the specified task.
        /// </summary>
        public static TaskAwaiter GetAwaiter(SystemTask task) => new TaskAwaiter(task);

        /// <summary>
        /// Configures an awaiter used to await this task.
        /// </summary>
        public static ConfiguredTaskAwaitable ConfigureAwait(SystemTask task,
            bool continueOnCapturedContext) =>
            new ConfiguredTaskAwaitable(task, continueOnCapturedContext);

        /// <summary>
        /// Creates an awaitable that asynchronously yields back to the current context when awaited.
        /// </summary>
        public static YieldAwaitable Yield() => new YieldAwaitable(default);
    }

    /// <summary>
    /// Provides methods for creating generic tasks that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class Task<TResult>
    {
#pragma warning disable CA1000 // Do not declare static members on generic types
        /// <summary>
        /// The default generic task factory.
        /// </summary>
        private static SystemTasks.TaskFactory<TResult> DefaultFactory = new SystemTasks.TaskFactory<TResult>();

        /// <summary>
        /// Provides access to factory methods for creating controlled generic task instances.
        /// </summary>
        public static SystemTasks.TaskFactory<TResult> Factory
        {
            get
            {
                var runtime = CoyoteRuntime.Current;
                if (runtime.SchedulingPolicy is SchedulingPolicy.None)
                {
                    return DefaultFactory;
                }

                // TODO: cache this per runtime.
                return new SystemTasks.TaskFactory<TResult>(SystemCancellationToken.None,
                    SystemTaskCreationOptions.HideScheduler, SystemTaskContinuationOptions.HideScheduler,
                    runtime.ControlledTaskScheduler);
            }
        }

        /// <summary>
        /// Gets the result value of the specified generic task.
        /// </summary>
#pragma warning disable CA1707 // Remove the underscores from member name
#pragma warning disable SA1300 // Element should begin with an uppercase letter
#pragma warning disable IDE1006 // Naming Styles
        public static TResult get_Result(SystemTasks.Task<TResult> task)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None)
            {
                runtime.WaitUntilTaskCompletes(task);
            }

            return task.Result;
        }
#pragma warning restore CA1707 // Remove the underscores from member name
#pragma warning restore SA1300 // Element should begin with an uppercase letter
#pragma warning restore IDE1006 // Naming Styles

        /// <summary>
        /// Returns a generic task awaiter for the specified generic task.
        /// </summary>
        public static TaskAwaiter<TResult> GetAwaiter(SystemTasks.Task<TResult> task) =>
            new TaskAwaiter<TResult>(task);

        /// <summary>
        /// Configures an awaiter used to await this task.
        /// </summary>
        public static ConfiguredTaskAwaitable<TResult> ConfigureAwait(
            SystemTasks.Task<TResult> task, bool continueOnCapturedContext) =>
            new ConfiguredTaskAwaitable<TResult>(task, continueOnCapturedContext);
#pragma warning restore CA1000 // Do not declare static members on generic types
    }
}
