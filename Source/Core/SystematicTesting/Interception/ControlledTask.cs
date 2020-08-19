// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
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
            ControlledRuntime.Current.TaskController.ScheduleAction(action, null, false, false, cancellationToken) :
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
        public static Task Run(Func<Task> function, CancellationToken cancellationToken)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var controller = ControlledRuntime.Current.TaskController;
                var task = controller.ScheduleFunction(function, null, cancellationToken);
                return controller.UnwrapTask(task);
            }

            return Task.Run(function, cancellationToken);
        }

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
        public static Task<TResult> Run<TResult>(Func<Task<TResult>> function, CancellationToken cancellationToken)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var controller = ControlledRuntime.Current.TaskController;
                var task = controller.ScheduleFunction(function, null, cancellationToken);
                return controller.UnwrapTask(task);
            }

            return Task.Run(function, cancellationToken);
        }

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
            ControlledRuntime.Current.TaskController.ScheduleFunction(function, null, cancellationToken) :
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
        /// The time span to wait before completing the returned task, or Timeout.InfiniteTimeSpan
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
        /// The time span to wait before completing the returned task, or Timeout.InfiniteTimeSpan
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
            ControlledRuntime.Current.TaskController.WhenAllTasksCompleteAsync(tasks.ToArray()) : Task.WhenAll(tasks);

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
            ControlledRuntime.Current.TaskController.WhenAllTasksCompleteAsync(tasks.ToArray()) : Task.WhenAll(tasks);

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
            ControlledRuntime.Current.TaskController.WhenAnyTaskCompletesAsync(tasks.ToArray()) : Task.WhenAny(tasks);

#if NET5_0
        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when either of the
        /// two tasks have completed.
        /// </summary>
        /// <param name="t1">The first task to wait for completion.</param>
        /// <param name="t2">The second task to wait for completion.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Task> WhenAny(Task t1, Task t2) => CoyoteRuntime.IsExecutionControlled ?
            ControlledRuntime.Current.TaskController.WhenAnyTaskCompletesAsync(new Task[] { t1, t2 }) : Task.WhenAny(t1, t2);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when either of the
        /// two tasks have completed.
        /// </summary>
        /// <param name="t1">The first task to wait for completion.</param>
        /// <param name="t2">The second task to wait for completion.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task<Task<TResult>> WhenAny<TResult>(Task<TResult> t1, Task<TResult> t2) => CoyoteRuntime.IsExecutionControlled ?
            ControlledRuntime.Current.TaskController.WhenAnyTaskCompletesAsync(new Task<TResult>[] { t1, t2 }) : Task.WhenAny(t1, t2);
#endif

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
            ControlledRuntime.Current.TaskController.WhenAnyTaskCompletesAsync(tasks.ToArray()) : Task.WhenAny(tasks);

        /// <summary>
        /// Waits for all of the provided <see cref="Task"/> objects to complete execution.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WaitAll(params Task[] tasks) => WaitAll(tasks, Timeout.Infinite, default);

        /// <summary>
        /// Waits for all of the provided <see cref="Task"/> objects to complete
        /// execution within a specified time interval.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <param name="timeout">
        /// A time span that represents the number of milliseconds to wait, or
        /// Timeout.InfiniteTimeSpan to wait indefinitely.
        /// </param>
        /// <returns>True if all tasks completed execution within the allotted time; otherwise, false.</returns>
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
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or -1 to wait indefinitely.</param>
        /// <returns>True if all tasks completed execution within the allotted time; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool WaitAll(Task[] tasks, int millisecondsTimeout) => WaitAll(tasks, millisecondsTimeout, default);

        /// <summary>
        /// Waits for all of the provided <see cref="Task"/> objects to complete
        /// execution unless the wait is cancelled.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the wait.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WaitAll(Task[] tasks, CancellationToken cancellationToken) =>
            WaitAll(tasks, Timeout.Infinite, cancellationToken);

        /// <summary>
        /// Waits for any of the provided <see cref="Task"/> objects to complete
        /// execution within a specified number of milliseconds or until a cancellation
        /// token is cancelled.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or -1 to wait indefinitely.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the wait.</param>
        /// <returns>True if all tasks completed execution within the allotted time; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool WaitAll(Task[] tasks, int millisecondsTimeout, CancellationToken cancellationToken) =>
            CoyoteRuntime.IsExecutionControlled ?
            ControlledRuntime.Current.TaskController.WaitAllTasksComplete(tasks) :
            Task.WaitAll(tasks, millisecondsTimeout, cancellationToken);

        /// <summary>
        /// Waits for any of the provided <see cref="Task"/> objects to complete execution.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <returns>The index of the completed task in the tasks array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(params Task[] tasks) => WaitAny(tasks, Timeout.Infinite, default);

        /// <summary>
        /// Waits for any of the provided <see cref="Task"/> objects to complete
        /// execution within a specified time interval.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <param name="timeout">
        /// A time span that represents the number of milliseconds to wait, or
        /// Timeout.InfiniteTimeSpan to wait indefinitely.
        /// </param>
        /// <returns>The index of the completed task in the tasks array, or -1 if the timeout occurred.</returns>
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
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or -1 to wait indefinitely.</param>
        /// <returns>The index of the completed task in the tasks array, or -1 if the timeout occurred.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(Task[] tasks, int millisecondsTimeout) => WaitAny(tasks, millisecondsTimeout, default);

        /// <summary>
        /// Waits for any of the provided <see cref="Task"/> objects to complete
        /// execution unless the wait is cancelled.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the wait.</param>
        /// <returns>The index of the completed task in the tasks array.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(Task[] tasks, CancellationToken cancellationToken) => WaitAny(tasks, Timeout.Infinite, cancellationToken);

        /// <summary>
        /// Waits for any of the provided <see cref="Task"/> objects to complete
        /// execution within a specified number of milliseconds or until a cancellation
        /// token is cancelled.
        /// </summary>
        /// <param name="tasks">The tasks to wait for completion.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or -1 to wait indefinitely.</param>
        /// <param name="cancellationToken">Cancellation token that can be used to cancel the wait.</param>
        /// <returns>The index of the completed task in the tasks array, or -1 if the timeout occurred.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int WaitAny(Task[] tasks, int millisecondsTimeout, CancellationToken cancellationToken) =>
            CoyoteRuntime.IsExecutionControlled ?
            ControlledRuntime.Current.TaskController.WaitAnyTaskCompletes(tasks) :
            Task.WaitAny(tasks, millisecondsTimeout, cancellationToken);

        /// <summary>
        /// Waits for the specified <see cref="Task"/> to complete execution.
        /// </summary>
        /// <param name="task">The task performing the wait operation.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Wait(Task task) => Wait(task, Timeout.Infinite, default);

        /// <summary>
        /// Waits for the specified <see cref="Task"/> to complete execution within a specified time interval.
        /// </summary>
        /// <param name="task">The task performing the wait operation.</param>
        /// <param name="timeout">
        /// A time span that represents the number of milliseconds to wait, or
        /// Timeout.InfiniteTimeSpan to wait indefinitely.
        /// </param>
        /// <returns>True if the task completed execution within the allotted time; otherwise, false.</returns>
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
        /// <param name="task">The task performing the wait operation.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or -1 to wait indefinitely.</param>
        /// <returns>True if the task completed execution within the allotted time; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Wait(Task task, int millisecondsTimeout) => Wait(task, millisecondsTimeout, default);

        /// <summary>
        /// Waits for the specified <see cref="Task"/> to complete execution. The wait terminates if a cancellation
        /// token is canceled before the task completes.
        /// </summary>
        /// <param name="task">The task performing the wait operation.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Wait(Task task, CancellationToken cancellationToken) => Wait(task, Timeout.Infinite, cancellationToken);

        /// <summary>
        /// Waits for the specified <see cref="Task"/> to complete execution. The wait terminates if a timeout interval
        /// elapses or a cancellation token is canceled before the task completes.
        /// </summary>
        /// <param name="task">The task performing the wait operation.</param>
        /// <param name="millisecondsTimeout">The number of milliseconds to wait, or -1 to wait indefinitely.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>True if the task completed execution within the allotted time; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Wait(Task task, int millisecondsTimeout, CancellationToken cancellationToken) =>
            CoyoteRuntime.IsExecutionControlled ?
            ControlledRuntime.Current.TaskController.WaitTaskCompletes(task) :
            task.Wait(millisecondsTimeout, cancellationToken);

        /// <summary>
        /// Returns a <see cref="CoyoteTasks.TaskAwaiter"/> for the specified <see cref="Task"/>.
        /// </summary>
        /// <param name="task">The task associated with the task awaiter.</param>
        /// <returns>The task awaiter.</returns>
        public static CoyoteTasks.TaskAwaiter GetAwaiter(Task task) => new CoyoteTasks.TaskAwaiter(
            CoyoteRuntime.IsExecutionControlled ? ControlledRuntime.Current.TaskController : null, task);

        /// <summary>
        /// Creates an awaitable that asynchronously yields back to the current context when awaited.
        /// </summary>
        /// <returns>The yield awaitable.</returns>
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

    /// <summary>
    /// Provides methods for creating generic tasks that can be controlled during testing.
    /// </summary>
    /// <typeparam name="TResult">The type of the produced result.</typeparam>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    public static class ControlledTask<TResult>
    {
#pragma warning disable CA1000 // Do not declare static members on generic types
        /// <summary>
        /// Gets the result value of the specified <see cref="Task{TResult}"/>.
        /// </summary>
        /// <param name="task">The task producing the result value.</param>
        /// <returns>The result value.</returns>
#pragma warning disable CA1707 // Remove the underscores from member name
#pragma warning disable SA1300 // Element should begin with an uppercase letter
#pragma warning disable IDE1006 // Naming Styles
        public static TResult get_Result(Task<TResult> task) => CoyoteRuntime.IsExecutionControlled ?
            ControlledRuntime.Current.TaskController.WaitTaskCompletes(task) : task.Result;
#pragma warning restore CA1707 // Remove the underscores from member name
#pragma warning restore SA1300 // Element should begin with an uppercase letter
#pragma warning restore IDE1006 // Naming Styles

        /// <summary>
        /// Returns a <see cref="CoyoteTasks.TaskAwaiter{TResult}"/> for the specified <see cref="Task{TResult}"/>.
        /// </summary>
        /// <param name="task">The task associated with the task awaiter.</param>
        /// <returns>The task awaiter.</returns>
        public static CoyoteTasks.TaskAwaiter<TResult> GetAwaiter(Task<TResult> task) => new CoyoteTasks.TaskAwaiter<TResult>(
            CoyoteRuntime.IsExecutionControlled ? ControlledRuntime.Current.TaskController : null, task);
#pragma warning restore CA1000 // Do not declare static members on generic types
    }
}
