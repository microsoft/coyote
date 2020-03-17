// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Tasks
{
    /// <summary>
    /// Interface exposing methods for controlling the execution of tasks during systematic testing.
    /// </summary>
    internal interface ITaskController
    {
        /// <summary>
        /// Schedules the specified action to be executed asynchronously.
        /// </summary>
        Task ScheduleAction(Action action, SystemTasks.Task predecessor, CancellationToken cancellationToken);

        /// <summary>
        /// Schedules the specified function to be executed asynchronously.
        /// </summary>
        Task ScheduleFunction(Func<Task> function, SystemTasks.Task predecessor, CancellationToken cancellationToken);

        /// <summary>
        /// Schedules the specified function to be executed asynchronously.
        /// </summary>
        Task<TResult> ScheduleFunction<TResult>(Func<Task<TResult>> function, SystemTasks.Task predecessor,
            CancellationToken cancellationToken);

        /// <summary>
        /// Schedules the specified delegate to be executed asynchronously.
        /// </summary>
        Task<TResult> ScheduleDelegate<TResult>(Delegate work, SystemTasks.Task predecessor, CancellationToken cancellationToken);

        /// <summary>
        /// Schedules the specified delay to be executed asynchronously.
        /// </summary>
        Task ScheduleDelay(TimeSpan delay, CancellationToken cancellationToken);

        /// <summary>
        /// Schedules the specified task awaiter continuation to be executed asynchronously.
        /// </summary>
        void ScheduleTaskAwaiterContinuation(SystemTasks.Task task, Action continuation);

        /// <summary>
        /// Schedules the specified yield awaiter continuation to be executed asynchronously.
        /// </summary>
        void ScheduleYieldAwaiterContinuation(Action continuation);

        /// <summary>
        /// Creates a controlled task that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        Task WhenAllTasksCompleteAsync(IEnumerable<Task> tasks);

        /// <summary>
        /// Creates a controlled task that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        Task<TResult[]> WhenAllTasksCompleteAsync<TResult>(IEnumerable<Task<TResult>> tasks);

        /// <summary>
        /// Creates a controlled task that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        Task<Task> WhenAnyTaskCompletesAsync(IEnumerable<Task> tasks);

        /// <summary>
        /// Creates a controlled task that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        Task<Task<TResult>> WhenAnyTaskCompletesAsync<TResult>(IEnumerable<Task<TResult>> tasks);

        /// <summary>
        /// Waits for all of the provided controlled task objects to complete execution within
        /// a specified number of milliseconds or until a cancellation token is cancelled.
        /// </summary>
        bool WaitAllTasksComplete(Task[] tasks, int millisecondsTimeout, CancellationToken cancellationToken);

        /// <summary>
        /// Waits for any of the provided controlled task objects to complete execution within
        /// a specified number of milliseconds or until a cancellation token is cancelled.
        /// </summary>
        int WaitAnyTaskCompletes(Task[] tasks, int millisecondsTimeout, CancellationToken cancellationToken);

        /// <summary>
        /// Waits for the task to complete execution. The wait terminates if a timeout interval
        /// elapses or a cancellation token is canceled before the task completes.
        /// </summary>
        bool WaitTaskCompletes(Task task, int millisecondsTimeout, CancellationToken cancellationToken);

        /// <summary>
        /// Waits for the task to complete execution and returns the result.
        /// </summary>
        TResult WaitTaskCompletes<TResult>(Task<TResult> task);

        /// <summary>
        /// Callback invoked when the <see cref="AsyncTaskMethodBuilder.Start"/> is called.
        /// </summary>
        void OnAsyncTaskMethodBuilderStart(Type stateMachineType);

        /// <summary>
        /// Callback invoked when the <see cref="AsyncTaskMethodBuilder.Task"/> is accessed.
        /// </summary>
        void OnAsyncTaskMethodBuilderTask();

        /// <summary>
        /// Callback invoked when the <see cref="AsyncTaskMethodBuilder.AwaitOnCompleted"/>
        /// or <see cref="AsyncTaskMethodBuilder.AwaitUnsafeOnCompleted"/> is called.
        /// </summary>
        void OnAsyncTaskMethodBuilderAwaitCompleted(Type awaiterType, Type stateMachineType);

        /// <summary>
        /// Callback invoked when the currently executing task operation gets a controlled awaiter.
        /// </summary>
        void OnGetAwaiter();

        /// <summary>
        /// Callback invoked when the <see cref="YieldAwaitable.YieldAwaiter.GetResult"/> is called.
        /// </summary>
        void OnYieldAwaiterGetResult();

        /// <summary>
        /// Callback invoked when the executing operation is waiting for the specified task to complete.
        /// </summary>
        void OnWaitTask(SystemTasks.Task task);
    }
}
