// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Coyote.Rewriting.Types.Runtime.CompilerServices;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Runtime.CompilerServices;
using MethodImpl = System.Runtime.CompilerServices.MethodImplAttribute;
using MethodImplOptions = System.Runtime.CompilerServices.MethodImplOptions;
using SystemCancellationToken = System.Threading.CancellationToken;
using SystemCancellationTokenRegistration = System.Threading.CancellationTokenRegistration;
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

#if NET8_0_OR_GREATER
        /// <summary>
        /// Creates a task that completes after a specified time interval.
        /// </summary>
        public static SystemTask Delay(TimeSpan delay, TimeProvider timeProvider) =>
            Delay(delay, timeProvider, default);

        /// <summary>
        /// Creates a task that completes after a specified time interval.
        /// </summary>
        public static SystemTask Delay(TimeSpan delay, TimeProvider timeProvider,
            SystemCancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(timeProvider);
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return SystemTask.Delay(delay, timeProvider, cancellationToken);
            }

            if (!ReferenceEquals(timeProvider, TimeProvider.System))
            {
                const string message = "Custom time providers are not supported in systematic testing.";
                runtime.NotifyAssertionFailure(message);
                return FromException(new NotSupportedException(message));
            }

            return runtime.ScheduleDelay(delay, cancellationToken);
        }
#endif

#if NET
        /// <summary>
        /// Waits asynchronously for the task to complete or for cancellation to be requested.
        /// </summary>
        public static SystemTask WaitAsync(SystemTask task, SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.Interleaving)
            {
                SystemTask result = task.WaitAsync(cancellationToken);
                runtime.RegisterKnownControlledTask(result);
                return result;
            }

            if (task.IsCompleted || !cancellationToken.CanBeCanceled)
            {
                runtime.RegisterKnownControlledTask(task);
                return task;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return FromCanceled(cancellationToken);
            }

            var cancellation = new SystemTasks.TaskCompletionSource();
            runtime.RegisterKnownControlledTask(cancellation.Task);
            SystemCancellationTokenRegistration registration = RegisterWaitAsyncCancellation(task,
                () => cancellation.TrySetCanceled(cancellationToken), cancellationToken);
            return runtime.UnwrapTask(CreateWaitAsyncTask(task, cancellation.Task, runtime, registration));
        }

        /// <summary>
        /// Waits asynchronously for the task to complete within the specified timeout.
        /// </summary>
        public static SystemTask WaitAsync(SystemTask task, TimeSpan timeout) =>
            WaitAsync(task, timeout, default(SystemCancellationToken));

#if NET8_0_OR_GREATER
        /// <summary>
        /// Waits asynchronously for the task to complete within the specified timeout.
        /// </summary>
        public static SystemTask WaitAsync(SystemTask task, TimeSpan timeout, TimeProvider timeProvider) =>
            WaitAsync(task, timeout, timeProvider, default);
#endif

        /// <summary>
        /// Waits asynchronously for the task to complete within the specified timeout or for cancellation.
        /// </summary>
        public static SystemTask WaitAsync(SystemTask task, TimeSpan timeout,
            SystemCancellationToken cancellationToken)
        {
            ValidateTimeout(timeout);
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return task.WaitAsync(timeout, cancellationToken);
            }

            return WaitAsync(task, timeout, runtime, cancellationToken);
        }

#if NET8_0_OR_GREATER
        /// <summary>
        /// Waits asynchronously for the task to complete within the specified timeout or for cancellation.
        /// </summary>
        public static SystemTask WaitAsync(SystemTask task, TimeSpan timeout, TimeProvider timeProvider,
            SystemCancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(timeProvider);
            ValidateTimeout(timeout);
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return task.WaitAsync(timeout, timeProvider, cancellationToken);
            }

            if (!ReferenceEquals(timeProvider, TimeProvider.System))
            {
                const string message = "Custom time providers are not supported in systematic testing.";
                runtime.NotifyAssertionFailure(message);
                return FromException(new NotSupportedException(message));
            }

            return WaitAsync(task, timeout, runtime, cancellationToken);
        }
#endif

        private static SystemTask WaitAsync(SystemTask task, TimeSpan timeout,
            CoyoteRuntime runtime, SystemCancellationToken cancellationToken)
        {
            if (task.IsCompleted)
            {
                // An already completed task takes precedence over both cancellation and the
                // timeout, which matches the uncontrolled semantics of this API.
                runtime.RegisterKnownControlledTask(task);
                return task;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                // An already canceled token deterministically takes precedence over the timeout,
                // which matches the uncontrolled semantics of this API.
                SystemTask canceled = SystemTask.FromCanceled(cancellationToken);
                runtime.RegisterKnownControlledTask(canceled);
                return canceled;
            }

            if (timeout == System.Threading.Timeout.InfiniteTimeSpan)
            {
                return WaitAsync(task, cancellationToken);
            }

            if ((long)timeout.TotalMilliseconds is 0)
            {
                // A zero timeout expires before the task is given any chance to complete, so it
                // deterministically wins, which matches the uncontrolled semantics of this API.
                return FromException(new TimeoutException());
            }

            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                // Systematic testing does not model the passage of wall-clock time, so a finite
                // timeout must not be explored as an operation racing the task to complete the
                // wait, else the wait times out spuriously in some schedules, no matter how large
                // the timeout is. Instead, the wait is explored as if the timeout was infinite,
                // which is how the runtime models the timeout of the other controlled wait APIs,
                // such as 'Task.Wait', 'Task.WaitAll', 'Monitor.Wait' and 'SemaphoreSlim.Wait'.
                // A wait that no operation can complete is then reported as a deadlock.
                return WaitAsync(task, cancellationToken);
            }

            // Systematic fuzzing executes the program in real time, so the timeout keeps its
            // wall-clock meaning and is delegated to the uncontrolled runtime.
            SystemTask result = task.WaitAsync(timeout, cancellationToken);
            runtime.RegisterKnownControlledTask(result);
            return result;
        }

        /// <summary>
        /// Registers the wait cancellation so that it cannot complete a wait to which the source
        /// task has already delivered its outcome.
        /// </summary>
        /// <remarks>
        /// The uncontrolled wait observes the source task through a completion action that runs
        /// synchronously when the source completes, unless the source runs its continuations
        /// asynchronously. In the synchronous case, the first of the two events wins, so the source
        /// state is sampled when the token is canceled, which records the winner at the event boundary
        /// instead of when the controlled operation observing the wait runs, by which time both events
        /// may have happened. In the asynchronous case, the uncontrolled delivery of the source outcome
        /// races a later cancellation, which the controlled operation observing the wait models.
        /// </remarks>
        internal static SystemCancellationTokenRegistration RegisterWaitAsyncCancellation(SystemTask task,
            Func<bool> trySetCanceled, SystemCancellationToken cancellationToken)
        {
            bool isOutcomeDeliveredSynchronously =
                (task.CreationOptions & SystemTaskCreationOptions.RunContinuationsAsynchronously) is 0;
            return cancellationToken.Register(() =>
            {
                if (!isOutcomeDeliveredSynchronously || !task.IsCompleted)
                {
                    trySetCanceled();
                }
            });
        }

        /// <summary>
        /// Observes completion or cancellation using a controlled operation.
        /// </summary>
        /// <remarks>
        /// The cancellation task is canceled only if the wait cancellation can still complete the
        /// wait, see <see cref="RegisterWaitAsyncCancellation"/>.
        /// </remarks>
        internal static SystemTasks.Task<TTask> CreateWaitAsyncTask<TTask>(TTask task, TTask cancellationTask,
            CoyoteRuntime runtime, SystemCancellationTokenRegistration registration)
            where TTask : SystemTask
        {
            return Run<TTask>(() =>
            {
                try
                {
                    if (!cancellationTask.IsCompleted)
                    {
                        TaskServices.WaitUntilAnyTaskCompletes(runtime, new SystemTask[] { task, cancellationTask });
                    }

                    // Only unwrap completed tasks, so asynchronous source continuations cannot
                    // move completion of the wait onto the uncontrolled thread pool.
                    return cancellationTask.IsCanceled ? cancellationTask : task;
                }
                finally
                {
                    // Do not block a controlled operation waiting for an in-flight cancellation callback.
                    registration.Unregister();
                }
            });
        }

        private static void ValidateTimeout(TimeSpan timeout)
        {
            // Match Timer.MaxSupportedTimeout in .NET 8 and .NET 10. The runtime reserves
            // uint.MaxValue for the -1 millisecond infinite-timeout sentinel.
            const long MaxSupportedTimeoutMilliseconds = 0xfffffffe;
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > MaxSupportedTimeoutMilliseconds)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }
        }
#endif

        /// <summary>
        /// Creates a task that will complete when all tasks in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTask WhenAll(params SystemTask[] tasks)
        {
            SystemTask task = SystemTask.WhenAll(tasks);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

#if NET10_0_OR_GREATER
        /// <summary>
        /// Creates an asynchronous enumerable that yields tasks as they complete.
        /// </summary>
        public static IAsyncEnumerable<SystemTask> WhenEach(params SystemTask[] tasks)
        {
            ArgumentNullException.ThrowIfNull(tasks);
            return WhenEach((ReadOnlySpan<SystemTask>)tasks);
        }

        /// <summary>
        /// Creates an asynchronous enumerable that yields tasks as they complete.
        /// </summary>
        public static IAsyncEnumerable<SystemTask> WhenEach(params ReadOnlySpan<SystemTask> tasks)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.Interleaving)
            {
                return SystemTask.WhenEach(tasks);
            }

            return WhenEachState.Iterate<SystemTask>(WhenEachState.Create(runtime, tasks));
        }

        /// <summary>
        /// Creates an asynchronous enumerable that yields tasks as they complete.
        /// </summary>
        public static IAsyncEnumerable<SystemTask> WhenEach(IEnumerable<SystemTask> tasks)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.Interleaving)
            {
                return SystemTask.WhenEach(tasks);
            }

            return WhenEachState.Iterate<SystemTask>(WhenEachState.Create(runtime, tasks));
        }

        /// <summary>
        /// Creates an asynchronous enumerable that yields tasks as they complete.
        /// </summary>
        public static IAsyncEnumerable<SystemTasks.Task<TResult>> WhenEach<TResult>(
            params SystemTasks.Task<TResult>[] tasks)
        {
            ArgumentNullException.ThrowIfNull(tasks);
            return WhenEach((ReadOnlySpan<SystemTasks.Task<TResult>>)tasks);
        }

        /// <summary>
        /// Creates an asynchronous enumerable that yields tasks as they complete.
        /// </summary>
        public static IAsyncEnumerable<SystemTasks.Task<TResult>> WhenEach<TResult>(
            params ReadOnlySpan<SystemTasks.Task<TResult>> tasks)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.Interleaving)
            {
                return SystemTask.WhenEach(tasks);
            }

            return WhenEachState.Iterate<SystemTasks.Task<TResult>>(WhenEachState.Create(runtime, tasks));
        }

        /// <summary>
        /// Creates an asynchronous enumerable that yields tasks as they complete.
        /// </summary>
        public static IAsyncEnumerable<SystemTasks.Task<TResult>> WhenEach<TResult>(
            IEnumerable<SystemTasks.Task<TResult>> tasks)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.Interleaving)
            {
                return SystemTask.WhenEach(tasks);
            }

            return WhenEachState.Iterate<SystemTasks.Task<TResult>>(WhenEachState.Create(runtime, tasks));
        }

        /// <summary>
        /// Creates a task that will complete when all tasks in the specified span have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTask WhenAll(params ReadOnlySpan<SystemTask> tasks)
        {
            SystemTask task = SystemTask.WhenAll(tasks);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }
#endif

        /// <summary>
        /// Creates a task that will complete when all tasks in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTask WhenAll(IEnumerable<SystemTask> tasks)
        {
            SystemTask task = SystemTask.WhenAll(tasks);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

        /// <summary>
        /// Creates a task that will complete when all tasks in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<TResult[]> WhenAll<TResult>(params SystemTasks.Task<TResult>[] tasks)
        {
            SystemTasks.Task<TResult[]> task = SystemTask.WhenAll(tasks);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

#if NET10_0_OR_GREATER
        /// <summary>
        /// Creates a task that will complete when all tasks in the specified span have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<TResult[]> WhenAll<TResult>(
            params ReadOnlySpan<SystemTasks.Task<TResult>> tasks)
        {
            SystemTasks.Task<TResult[]> task = SystemTask.WhenAll(tasks);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }
#endif

        /// <summary>
        /// Creates a task that will complete when all tasks in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<TResult[]> WhenAll<TResult>(IEnumerable<SystemTasks.Task<TResult>> tasks)
        {
            SystemTasks.Task<TResult[]> task = SystemTask.WhenAll(tasks);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

        /// <summary>
        /// Creates a task that will complete when any task in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<SystemTask> WhenAny(params SystemTask[] tasks)
        {
            SystemTasks.Task<SystemTask> task = SystemTask.WhenAny(tasks);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

#if NET10_0_OR_GREATER
        /// <summary>
        /// Creates a task that will complete when any task in the specified span has completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<SystemTask> WhenAny(params ReadOnlySpan<SystemTask> tasks)
        {
            SystemTasks.Task<SystemTask> task = SystemTask.WhenAny(tasks);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }
#endif

        /// <summary>
        /// Creates a task that will complete when any task in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<SystemTask> WhenAny(IEnumerable<SystemTask> tasks)
        {
            SystemTasks.Task<SystemTask> task = SystemTask.WhenAny(tasks);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

#if NET
        /// <summary>
        /// Creates a task that will complete when either of the two tasks have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<SystemTask> WhenAny(SystemTask task1, SystemTask task2)
        {
            SystemTasks.Task<SystemTask> task = SystemTask.WhenAny(task1, task2);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

        /// <summary>
        /// Creates a task that will complete when either of the two tasks have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<SystemTasks.Task<TResult>> WhenAny<TResult>(
            SystemTasks.Task<TResult> task1, SystemTasks.Task<TResult> task2)
        {
            SystemTasks.Task<SystemTasks.Task<TResult>> task = SystemTask.WhenAny(task1, task2);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }
#endif

        /// <summary>
        /// Creates a task that will complete when any task in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<SystemTasks.Task<TResult>> WhenAny<TResult>(
            params SystemTasks.Task<TResult>[] tasks)
        {
            SystemTasks.Task<SystemTasks.Task<TResult>> task = SystemTask.WhenAny(tasks);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

#if NET10_0_OR_GREATER
        /// <summary>
        /// Creates a task that will complete when any task in the specified span has completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<SystemTasks.Task<TResult>> WhenAny<TResult>(
            params ReadOnlySpan<SystemTasks.Task<TResult>> tasks)
        {
            SystemTasks.Task<SystemTasks.Task<TResult>> task = SystemTask.WhenAny(tasks);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }
#endif

        /// <summary>
        /// Creates a task that will complete when any task in the specified
        /// enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemTasks.Task<SystemTasks.Task<TResult>> WhenAny<TResult>(
            IEnumerable<SystemTasks.Task<TResult>> tasks)
        {
            SystemTasks.Task<SystemTasks.Task<TResult>> task = SystemTask.WhenAny(tasks);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

        /// <summary>
        /// Waits for all of the provided task objects to complete execution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WaitAll(params SystemTask[] tasks) =>
            WaitAll(tasks, SystemTimeout.Infinite, default);

#if NET10_0_OR_GREATER
        /// <summary>
        /// Waits for all of the provided task objects to complete execution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WaitAll(params ReadOnlySpan<SystemTask> tasks) =>
            WaitAll(tasks.ToArray(), SystemTimeout.Infinite, default);
#endif

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

#if NET10_0_OR_GREATER
        /// <summary>
        /// Waits for all tasks in the enumerable collection to complete unless the wait is canceled.
        /// </summary>
        public static void WaitAll(IEnumerable<SystemTask> tasks,
            SystemCancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(tasks);
            WaitAll(new List<SystemTask>(tasks).ToArray(), cancellationToken);
        }
#endif

        /// <summary>
        /// Waits for any of the provided task objects to complete execution within a specified
        /// number of milliseconds or until a cancellation token is cancelled.
        /// </summary>
        public static bool WaitAll(SystemTask[] tasks, int millisecondsTimeout,
            SystemCancellationToken cancellationToken)
        {
            if (tasks is null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }

            for (int idx = 0; idx < tasks.Length; idx++)
            {
                if (tasks[idx] is null)
                {
                    throw new ArgumentException("The tasks collection included a null task.", nameof(tasks));
                }
            }

            cancellationToken.ThrowIfCancellationRequested();

            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.None)
            {
                // TODO: support timeouts during testing, this would become false if there is a timeout.
                TaskServices.WaitUntilAllTasksComplete(runtime, tasks);
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
            if (runtime.SchedulingPolicy != SchedulingPolicy.None && tasks != null)
            {
                // TODO: support timeouts during testing, this would become -1 if there is a timeout.
                TaskServices.WaitUntilAnyTaskCompletes(runtime, tasks);
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
                TaskServices.WaitUntilTaskCompletes(runtime, task);
            }

            return task.Wait(millisecondsTimeout, cancellationToken);
        }

        /// <summary>
        /// Creates a task that has completed successfully with the specified result.
        /// </summary>
        public static SystemTasks.Task<TResult> FromResult<TResult>(TResult result)
        {
            SystemTasks.Task<TResult> task = SystemTask.FromResult(result);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

        /// <summary>
        /// Creates a task that has completed due to cancellation with the specified cancellation token.
        /// </summary>
        public static SystemTask FromCanceled(SystemCancellationToken cancellationToken)
        {
            SystemTask task = SystemTask.FromCanceled(cancellationToken);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

        /// <summary>
        /// Creates a task that has completed due to cancellation with the specified cancellation token.
        /// </summary>
        public static SystemTasks.Task<TResult> FromCanceled<TResult>(SystemCancellationToken cancellationToken)
        {
            SystemTasks.Task<TResult> task = SystemTask.FromCanceled<TResult>(cancellationToken);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

        /// <summary>
        /// Creates a task that has completed with the specified exception.
        /// </summary>
        public static SystemTask FromException(Exception exception)
        {
            SystemTask task = SystemTask.FromException(exception);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

        /// <summary>
        /// Creates a task that has completed with the specified exception.
        /// </summary>
        public static SystemTasks.Task<TResult> FromException<TResult>(Exception exception)
        {
            SystemTasks.Task<TResult> task = SystemTask.FromException<TResult>(exception);
            CoyoteRuntime.Current.RegisterKnownControlledTask(task);
            return task;
        }

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
                TaskServices.WaitUntilTaskCompletes(runtime, task);
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

#if NET
        /// <summary>
        /// Waits asynchronously for the task to complete or for cancellation to be requested.
        /// </summary>
        public static SystemTasks.Task<TResult> WaitAsync(SystemTasks.Task<TResult> task,
            SystemCancellationToken cancellationToken)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy != SchedulingPolicy.Interleaving)
            {
                SystemTasks.Task<TResult> result = task.WaitAsync(cancellationToken);
                runtime.RegisterKnownControlledTask(result);
                return result;
            }

            if (task.IsCompleted || !cancellationToken.CanBeCanceled)
            {
                runtime.RegisterKnownControlledTask(task);
                return task;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromCanceled<TResult>(cancellationToken);
            }

            var cancellation = new SystemTasks.TaskCompletionSource<TResult>();
            runtime.RegisterKnownControlledTask(cancellation.Task);
            SystemCancellationTokenRegistration registration = Task.RegisterWaitAsyncCancellation(task,
                () => cancellation.TrySetCanceled(cancellationToken), cancellationToken);
            return runtime.UnwrapTask(Task.CreateWaitAsyncTask(task, cancellation.Task, runtime, registration));
        }

        /// <summary>
        /// Waits asynchronously for the task to complete within the specified timeout.
        /// </summary>
        public static SystemTasks.Task<TResult> WaitAsync(SystemTasks.Task<TResult> task, TimeSpan timeout) =>
            WaitAsync(task, timeout, default(SystemCancellationToken));

#if NET8_0_OR_GREATER
        /// <summary>
        /// Waits asynchronously for the task to complete within the specified timeout.
        /// </summary>
        public static SystemTasks.Task<TResult> WaitAsync(SystemTasks.Task<TResult> task, TimeSpan timeout,
            TimeProvider timeProvider) =>
            WaitAsync(task, timeout, timeProvider, default);
#endif

        /// <summary>
        /// Waits asynchronously for the task to complete within the specified timeout or for cancellation.
        /// </summary>
        public static SystemTasks.Task<TResult> WaitAsync(SystemTasks.Task<TResult> task, TimeSpan timeout,
            SystemCancellationToken cancellationToken)
        {
            const long MaxSupportedTimeoutMilliseconds = 0xfffffffe;
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > MaxSupportedTimeoutMilliseconds)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return task.WaitAsync(timeout, cancellationToken);
            }

            return WaitAsync(task, timeout, runtime, cancellationToken);
        }

#if NET8_0_OR_GREATER
        /// <summary>
        /// Waits asynchronously for the task to complete within the specified timeout or for cancellation.
        /// </summary>
        public static SystemTasks.Task<TResult> WaitAsync(SystemTasks.Task<TResult> task, TimeSpan timeout,
            TimeProvider timeProvider, SystemCancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(timeProvider);
            const long MaxSupportedTimeoutMilliseconds = 0xfffffffe;
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > MaxSupportedTimeoutMilliseconds)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                return task.WaitAsync(timeout, timeProvider, cancellationToken);
            }

            if (!ReferenceEquals(timeProvider, TimeProvider.System))
            {
                const string message = "Custom time providers are not supported in systematic testing.";
                runtime.NotifyAssertionFailure(message);
                SystemTasks.Task<TResult> unsupported = SystemTask.FromException<TResult>(
                    new NotSupportedException(message));
                runtime.RegisterKnownControlledTask(unsupported);
                return unsupported;
            }

            return WaitAsync(task, timeout, runtime, cancellationToken);
        }
#endif

        private static SystemTasks.Task<TResult> WaitAsync(SystemTasks.Task<TResult> task, TimeSpan timeout,
            CoyoteRuntime runtime, SystemCancellationToken cancellationToken)
        {
            if (task.IsCompleted)
            {
                // An already completed task takes precedence over both cancellation and the
                // timeout, which matches the uncontrolled semantics of this API.
                runtime.RegisterKnownControlledTask(task);
                return task;
            }

            if (cancellationToken.IsCancellationRequested)
            {
                // An already canceled token deterministically takes precedence over the timeout,
                // which matches the uncontrolled semantics of this API.
                SystemTasks.Task<TResult> canceled = SystemTask.FromCanceled<TResult>(cancellationToken);
                runtime.RegisterKnownControlledTask(canceled);
                return canceled;
            }

            if (timeout == System.Threading.Timeout.InfiniteTimeSpan)
            {
                return WaitAsync(task, cancellationToken);
            }

            if ((long)timeout.TotalMilliseconds is 0)
            {
                // A zero timeout expires before the task is given any chance to complete, so it
                // deterministically wins, which matches the uncontrolled semantics of this API.
                SystemTasks.Task<TResult> timedOut = SystemTask.FromException<TResult>(new TimeoutException());
                runtime.RegisterKnownControlledTask(timedOut);
                return timedOut;
            }

            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                // Systematic testing does not model the passage of wall-clock time, so a finite
                // timeout must not be explored as an operation racing the task to complete the
                // wait, else the wait times out spuriously in some schedules, no matter how large
                // the timeout is. Instead, the wait is explored as if the timeout was infinite,
                // which is how the runtime models the timeout of the other controlled wait APIs,
                // such as 'Task.Wait', 'Task.WaitAll', 'Monitor.Wait' and 'SemaphoreSlim.Wait'.
                // A wait that no operation can complete is then reported as a deadlock.
                return WaitAsync(task, cancellationToken);
            }

            // Systematic fuzzing executes the program in real time, so the timeout keeps its
            // wall-clock meaning and is delegated to the uncontrolled runtime.
            SystemTasks.Task<TResult> result = task.WaitAsync(timeout, cancellationToken);
            runtime.RegisterKnownControlledTask(result);
            return result;
        }
#endif
#pragma warning restore CA1000 // Do not declare static members on generic types
    }
}
