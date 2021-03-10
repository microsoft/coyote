// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting;
using CoyoteTasks = Microsoft.Coyote.Tasks;
using MethodImpl = System.Runtime.CompilerServices.MethodImplAttribute;
using MethodImplOptions = System.Runtime.CompilerServices.MethodImplOptions;

namespace Microsoft.Coyote.Interception
{
    /// <summary>
    /// Provides methods for creating tasks that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ControlledTask
    {
        /// <summary>
        /// Provides access to factory methods for creating controlled <see cref="Task"/>
        /// and <see cref="Task{TResult}"/> instances.
        /// </summary>
        public static TaskFactory Factory { get; } = new TaskFactory();

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a <see cref="Task"/>
        /// object that represents that work. A cancellation token allows the work to be cancelled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Run(Action action) => Run(action, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a <see cref="Task"/>
        /// object that represents that work.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Run(Action action, CancellationToken cancellationToken) => CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.ScheduleAction(action, null, OperationContext.CreateOperationExecutionOptions(),
                false, cancellationToken) :
            Task.Run(action, cancellationToken);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a proxy for
        /// the <see cref="Task"/> returned by the function.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Run(Func<Task> function) => Run(function, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a proxy for
        /// the <see cref="Task"/> returned by the function. A cancellation
        /// token allows the work to be cancelled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Run(Func<Task> function, CancellationToken cancellationToken)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var runtime = CoyoteRuntime.Current;
                var task = runtime.ScheduleFunction(function, null, cancellationToken);
                return runtime.UnwrapTask(task);
            }

            return Task.Run(function, cancellationToken);
        }

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a proxy for the
        /// <see cref="Task{TResult}"/> returned by the function.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TResult> Run<TResult>(Func<Task<TResult>> function) => Run(function, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a proxy for the
        /// <see cref="Task{TResult}"/> returned by the function. A cancellation
        /// token allows the work to be cancelled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TResult> Run<TResult>(Func<Task<TResult>> function, CancellationToken cancellationToken)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var runtime = CoyoteRuntime.Current;
                var task = runtime.ScheduleFunction(function, null, cancellationToken);
                return runtime.UnwrapTask(task);
            }

            return Task.Run(function, cancellationToken);
        }

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a <see cref="Task"/>
        /// object that represents that work.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TResult> Run<TResult>(Func<TResult> function) => Run(function, default);

        /// <summary>
        /// Queues the specified work to run on the thread pool and returns a <see cref="Task"/>
        /// object that represents that work. A cancellation token allows the work to be cancelled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TResult> Run<TResult>(Func<TResult> function, CancellationToken cancellationToken) =>
            CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.ScheduleFunction(function, null, cancellationToken) :
            Task.Run(function, cancellationToken);

        /// <summary>
        /// Creates a <see cref="Task"/> that completes after a time delay.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Delay(int millisecondsDelay) => CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.ScheduleDelay(TimeSpan.FromMilliseconds(millisecondsDelay), default) :
            Task.Delay(millisecondsDelay);

        /// <summary>
        /// Creates a <see cref="Task"/> that completes after a time delay.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Delay(int millisecondsDelay, CancellationToken cancellationToken) => CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.ScheduleDelay(TimeSpan.FromMilliseconds(millisecondsDelay), cancellationToken) :
            Task.Delay(millisecondsDelay, cancellationToken);

        /// <summary>
        /// Creates a <see cref="Task"/> that completes after a specified time interval.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Delay(TimeSpan delay) => CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.ScheduleDelay(delay, default) : Task.Delay(delay);

        /// <summary>
        /// Creates a <see cref="Task"/> that completes after a specified time interval.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task Delay(TimeSpan delay, CancellationToken cancellationToken) => CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.ScheduleDelay(delay, cancellationToken) : Task.Delay(delay, cancellationToken);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WhenAll(params Task[] tasks) => CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.WhenAllTasksCompleteAsync(tasks) : Task.WhenAll(tasks);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WhenAll(IEnumerable<Task> tasks) => CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.WhenAllTasksCompleteAsync(tasks.ToArray()) : Task.WhenAll(tasks);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when all tasks
        /// in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TResult[]> WhenAll<TResult>(params Task<TResult>[] tasks) => CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.WhenAllTasksCompleteAsync(tasks) : Task.WhenAll(tasks);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<TResult[]> WhenAll<TResult>(IEnumerable<Task<TResult>> tasks) => CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.WhenAllTasksCompleteAsync(tasks.ToArray()) : Task.WhenAll(tasks);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Task> WhenAny(params Task[] tasks) => CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.WhenAnyTaskCompletesAsync(tasks) : Task.WhenAny(tasks);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Task> WhenAny(IEnumerable<Task> tasks) => CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.WhenAnyTaskCompletesAsync(tasks.ToArray()) : Task.WhenAny(tasks);

#if NET5_0
        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when either of the
        /// two tasks have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Task> WhenAny(Task t1, Task t2) => CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.WhenAnyTaskCompletesAsync(new Task[] { t1, t2 }) : Task.WhenAny(t1, t2);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when either of the
        /// two tasks have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Task<TResult>> WhenAny<TResult>(Task<TResult> t1, Task<TResult> t2) => CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.WhenAnyTaskCompletesAsync(new Task<TResult>[] { t1, t2 }) : Task.WhenAny(t1, t2);
#endif

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when any task
        /// in the specified array have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Task<TResult>> WhenAny<TResult>(params Task<TResult>[] tasks) => CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.WhenAnyTaskCompletesAsync(tasks) : Task.WhenAny(tasks);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Task<TResult>> WhenAny<TResult>(IEnumerable<Task<TResult>> tasks) => CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.WhenAnyTaskCompletesAsync(tasks.ToArray()) : Task.WhenAny(tasks);

        /// <summary>
        /// Waits for all of the provided <see cref="Task"/> objects to complete execution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WaitAll(params Task[] tasks) => WaitAll(tasks, Timeout.Infinite, default);

        /// <summary>
        /// Waits for all of the provided <see cref="Task"/> objects to complete
        /// execution within a specified time interval.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool WaitAll(Task[] tasks, TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return WaitAll(tasks, (int)totalMilliseconds, default);
        }

        /// <summary>
        /// Waits for all of the provided <see cref="Task"/> objects to complete
        /// execution within a specified number of milliseconds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool WaitAll(Task[] tasks, int millisecondsTimeout) => WaitAll(tasks, millisecondsTimeout, default);

        /// <summary>
        /// Waits for all of the provided <see cref="Task"/> objects to complete
        /// execution unless the wait is cancelled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WaitAll(Task[] tasks, CancellationToken cancellationToken) =>
            WaitAll(tasks, Timeout.Infinite, cancellationToken);

        /// <summary>
        /// Waits for any of the provided <see cref="Task"/> objects to complete
        /// execution within a specified number of milliseconds or until a cancellation
        /// token is cancelled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool WaitAll(Task[] tasks, int millisecondsTimeout, CancellationToken cancellationToken) =>
            CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.WaitAllTasksComplete(tasks) :
            Task.WaitAll(tasks, millisecondsTimeout, cancellationToken);

        /// <summary>
        /// Waits for any of the provided <see cref="Task"/> objects to complete execution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(params Task[] tasks) => WaitAny(tasks, Timeout.Infinite, default);

        /// <summary>
        /// Waits for any of the provided <see cref="Task"/> objects to complete
        /// execution within a specified time interval.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(Task[] tasks, TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return WaitAny(tasks, (int)totalMilliseconds, default);
        }

        /// <summary>
        /// Waits for any of the provided <see cref="Task"/> objects to complete
        /// execution within a specified number of milliseconds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(Task[] tasks, int millisecondsTimeout) => WaitAny(tasks, millisecondsTimeout, default);

        /// <summary>
        /// Waits for any of the provided <see cref="Task"/> objects to complete
        /// execution unless the wait is cancelled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(Task[] tasks, CancellationToken cancellationToken) => WaitAny(tasks, Timeout.Infinite, cancellationToken);

        /// <summary>
        /// Waits for any of the provided <see cref="Task"/> objects to complete
        /// execution within a specified number of milliseconds or until a cancellation
        /// token is cancelled.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(Task[] tasks, int millisecondsTimeout, CancellationToken cancellationToken) =>
            CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.WaitAnyTaskCompletes(tasks) :
            Task.WaitAny(tasks, millisecondsTimeout, cancellationToken);

        /// <summary>
        /// Waits for the specified <see cref="Task"/> to complete execution.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Wait(Task task) => Wait(task, Timeout.Infinite, default);

        /// <summary>
        /// Waits for the specified <see cref="Task"/> to complete execution within a specified time interval.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Wait(Task task, TimeSpan timeout)
        {
            long totalMilliseconds = (long)timeout.TotalMilliseconds;
            if (totalMilliseconds < -1 || totalMilliseconds > int.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout));
            }

            return Wait(task, (int)totalMilliseconds, default);
        }

        /// <summary>
        /// Waits for the specified <see cref="Task"/> to complete execution within a specified number of milliseconds.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Wait(Task task, int millisecondsTimeout) => Wait(task, millisecondsTimeout, default);

        /// <summary>
        /// Waits for the specified <see cref="Task"/> to complete execution. The wait terminates if a cancellation
        /// token is canceled before the task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Wait(Task task, CancellationToken cancellationToken) => Wait(task, Timeout.Infinite, cancellationToken);

        /// <summary>
        /// Waits for the specified <see cref="Task"/> to complete execution. The wait terminates if a timeout interval
        /// elapses or a cancellation token is canceled before the task completes.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Wait(Task task, int millisecondsTimeout, CancellationToken cancellationToken) =>
            CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.WaitTaskCompletes(task) :
            task.Wait(millisecondsTimeout, cancellationToken);

        /// <summary>
        /// Returns a <see cref="CoyoteTasks.TaskAwaiter"/> for the specified <see cref="Task"/>.
        /// </summary>
        public static CoyoteTasks.TaskAwaiter GetAwaiter(Task task)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var runtime = CoyoteRuntime.Current;
                runtime.AssertIsAwaitedTaskControlled(task);
                return new CoyoteTasks.TaskAwaiter(runtime, task);
            }

            return new CoyoteTasks.TaskAwaiter(null, task);
        }

        /// <summary>
        /// Configures an awaiter used to await this task.
        /// </summary>
        public static CoyoteTasks.ConfiguredTaskAwaitable ConfigureAwait(Task task, bool continueOnCapturedContext)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var runtime = CoyoteRuntime.Current;
                runtime.AssertIsAwaitedTaskControlled(task);
                return new CoyoteTasks.ConfiguredTaskAwaitable(runtime, task, continueOnCapturedContext);
            }

            return new CoyoteTasks.ConfiguredTaskAwaitable(null, task, continueOnCapturedContext);
        }

        /// <summary>
        /// Creates an awaitable that asynchronously yields back to the current context when awaited.
        /// </summary>
        /// <remarks>
        /// You can use `await Task.Yield()` in an asynchronous method to force the method to complete
        /// asynchronously. During systematic testing, the underlying scheduling strategy can use this
        /// as a hint on how to better prioritize this work relative to other work that may be pending.
        /// </remarks>
        public static CoyoteTasks.YieldAwaitable Yield() =>
            new CoyoteTasks.YieldAwaitable(CoyoteRuntime.IsExecutionControlled ? CoyoteRuntime.Current : null);

        /// <summary>
        /// Injects a context switch point that can be systematically explored during testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExploreContextSwitch() => CoyoteRuntime.Current.ScheduleNextOperation();
    }

    /// <summary>
    /// Provides methods for creating generic tasks that can be controlled during testing.
    /// </summary>
    /// <typeparam name="TResult">The type of the produced result.</typeparam>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ControlledTask<TResult>
    {
#pragma warning disable CA1000 // Do not declare static members on generic types
        /// <summary>
        /// Provides access to factory methods for creating controlled <see cref="Task"/>
        /// and <see cref="Task{TResult}"/> instances.
        /// </summary>
        public static TaskFactory<TResult> Factory { get; } = new TaskFactory<TResult>();

        /// <summary>
        /// Gets the result value of the specified <see cref="Task{TResult}"/>.
        /// </summary>
#pragma warning disable CA1707 // Remove the underscores from member name
#pragma warning disable SA1300 // Element should begin with an uppercase letter
#pragma warning disable IDE1006 // Naming Styles
        public static TResult get_Result(Task<TResult> task) => CoyoteRuntime.IsExecutionControlled ?
            CoyoteRuntime.Current.WaitTaskCompletes(task) : task.Result;
#pragma warning restore CA1707 // Remove the underscores from member name
#pragma warning restore SA1300 // Element should begin with an uppercase letter
#pragma warning restore IDE1006 // Naming Styles

        /// <summary>
        /// Returns a <see cref="CoyoteTasks.TaskAwaiter{TResult}"/> for the specified <see cref="Task{TResult}"/>.
        /// </summary>
        public static CoyoteTasks.TaskAwaiter<TResult> GetAwaiter(Task<TResult> task)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var runtime = CoyoteRuntime.Current;
                runtime.AssertIsAwaitedTaskControlled(task);
                return new CoyoteTasks.TaskAwaiter<TResult>(runtime, task);
            }

            return new CoyoteTasks.TaskAwaiter<TResult>(null, task);
        }

        /// <summary>
        /// Configures an awaiter used to await this task.
        /// </summary>
        public static CoyoteTasks.ConfiguredTaskAwaitable<TResult> ConfigureAwait(Task<TResult> task, bool continueOnCapturedContext)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var runtime = CoyoteRuntime.Current;
                runtime.AssertIsAwaitedTaskControlled(task);
                return new CoyoteTasks.ConfiguredTaskAwaitable<TResult>(runtime, task, continueOnCapturedContext);
            }

            return new CoyoteTasks.ConfiguredTaskAwaitable<TResult>(null, task, continueOnCapturedContext);
        }
#pragma warning restore CA1000 // Do not declare static members on generic types
    }
}
