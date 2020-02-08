// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
        ControlledTask ScheduleAction(Action action, Task predecessor, CancellationToken cancellationToken);

        /// <summary>
        /// Schedules the specified function to be executed asynchronously.
        /// </summary>
        ControlledTask ScheduleFunction(Func<ControlledTask> function, Task predecessor, CancellationToken cancellationToken);

        /// <summary>
        /// Schedules the specified function to be executed asynchronously.
        /// </summary>
        ControlledTask<TResult> ScheduleFunction<TResult>(Func<ControlledTask<TResult>> function, Task predecessor,
            CancellationToken cancellationToken);

        /// <summary>
        /// Schedules the specified delegate to be executed asynchronously.
        /// </summary>
        ControlledTask<TResult> ScheduleDelegate<TResult>(Delegate work, Task predecessor, CancellationToken cancellationToken);

        /// <summary>
        /// Schedules the specified delay to be executed asynchronously.
        /// </summary>
        ControlledTask ScheduleDelay(TimeSpan delay, CancellationToken cancellationToken);

        /// <summary>
        /// Schedules the specified task awaiter continuation to be executed asynchronously.
        /// </summary>
        void ScheduleTaskAwaiterContinuation(Task task, Action continuation);

        /// <summary>
        /// Schedules the specified yield awaiter continuation to be executed asynchronously.
        /// </summary>
        void ScheduleYieldAwaiterContinuation(Action continuation);

        /// <summary>
        /// Creates a controlled task that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        ControlledTask WhenAllTasksCompleteAsync(IEnumerable<ControlledTask> tasks);

        /// <summary>
        /// Creates a controlled task that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        ControlledTask<TResult[]> WhenAllTasksCompleteAsync<TResult>(IEnumerable<ControlledTask<TResult>> tasks);

        /// <summary>
        /// Creates a controlled task that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        ControlledTask<ControlledTask> WhenAnyTaskCompletesAsync(IEnumerable<ControlledTask> tasks);

        /// <summary>
        /// Creates a controlled task that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        ControlledTask<ControlledTask<TResult>> WhenAnyTaskCompletesAsync<TResult>(IEnumerable<ControlledTask<TResult>> tasks);

        /// <summary>
        /// Waits for all of the provided controlled task objects to complete execution within
        /// a specified number of milliseconds or until a cancellation token is cancelled.
        /// </summary>
        bool WaitAllTasksComplete(ControlledTask[] tasks, int millisecondsTimeout, CancellationToken cancellationToken);

        /// <summary>
        /// Waits for any of the provided controlled task objects to complete execution within
        /// a specified number of milliseconds or until a cancellation token is cancelled.
        /// </summary>
        int WaitAnyTaskCompletes(ControlledTask[] tasks, int millisecondsTimeout, CancellationToken cancellationToken);

        /// <summary>
        /// Waits for the task to complete execution. The wait terminates if a timeout interval
        /// elapses or a cancellation token is canceled before the task completes.
        /// </summary>
        bool WaitTaskCompletes(ControlledTask task, int millisecondsTimeout, CancellationToken cancellationToken);

        /// <summary>
        /// Waits for the task to complete execution and returns the result.
        /// </summary>
        TResult WaitTaskCompletes<TResult>(ControlledTask<TResult> task);

        /// <summary>
        /// Callback invoked when the <see cref="AsyncControlledTaskMethodBuilder.Start"/> is called.
        /// </summary>
        void OnAsyncControlledTaskMethodBuilderStart(Type stateMachineType);

        /// <summary>
        /// Callback invoked when the <see cref="AsyncControlledTaskMethodBuilder.Task"/> is accessed.
        /// </summary>
        void OnAsyncControlledTaskMethodBuilderTask();

        /// <summary>
        /// Callback invoked when the <see cref="AsyncControlledTaskMethodBuilder.AwaitOnCompleted"/>
        /// or <see cref="AsyncControlledTaskMethodBuilder.AwaitUnsafeOnCompleted"/> is called.
        /// </summary>
        void OnAsyncControlledTaskMethodBuilderAwaitCompleted(Type awaiterType, Type stateMachineType);

        /// <summary>
        /// Callback invoked when the currently executing task operation gets a controlled awaiter.
        /// </summary>
        void OnGetControlledAwaiter();

        /// <summary>
        /// Callback invoked when the <see cref="ControlledYieldAwaitable.ControlledYieldAwaiter.GetResult"/> is called.
        /// </summary>
        void OnControlledYieldAwaiterGetResult();

        /// <summary>
        /// Callback invoked when the executing operation is waiting for the specified task to complete.
        /// </summary>
        void OnWaitTask(Task task);
    }
}
