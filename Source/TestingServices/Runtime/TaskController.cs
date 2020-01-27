// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.TestingServices.Scheduling;
using Microsoft.Coyote.Threading.Tasks;

namespace Microsoft.Coyote.TestingServices.Runtime
{
    /// <summary>
    /// Responsible for controlling the execution of tasks during systematic testing.
    /// </summary>
    internal sealed class TaskController : ITaskController
    {
        /// <summary>
        /// The executing runtime.
        /// </summary>
        private readonly SystematicTestingRuntime Runtime;

        /// <summary>
        /// The asynchronous operation scheduler.
        /// </summary>
        private readonly OperationScheduler Scheduler;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskController"/> class.
        /// </summary>
        internal TaskController(SystematicTestingRuntime runtime, OperationScheduler scheduler)
        {
            this.Runtime = runtime;
            this.Scheduler = scheduler;
        }

        /// <summary>
        /// Schedules the specified action to be executed asynchronously.
        /// </summary>
        [DebuggerStepThrough]
        public ControlledTask ScheduleAction(Action action, Task predecessor, CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            this.Assert(action != null, "The task cannot execute a null action.");

            ulong operationId = this.Runtime.GetNextOperationId();
            var op = new TaskOperation(operationId, "ControlledTask", this.Scheduler);
            this.Scheduler.RegisterOperation(op);
            op.OnEnabled();

            Task task = new Task(() =>
            {
                try
                {
                    // Update the current asynchronous control flow with the current runtime instance,
                    // allowing future retrieval in the same asynchronous call stack.
                    CoyoteRuntime.SetRuntimeToAsynchronousControlFlow(this.Runtime);

                    OperationScheduler.StartOperation(op);
                    if (predecessor != null)
                    {
                        op.OnWaitTask(predecessor);
                    }

                    action();
                }
                catch (Exception ex)
                {
                    // Report the unhandled exception and rethrow it.
                    this.ReportUnhandledExceptionInOperation(op, ex);
                    throw;
                }
                finally
                {
                    IO.Debug.WriteLine($"<ScheduleDebug> Completed operation '{op.Name}' on task '{Task.CurrentId}'.");
                    op.OnCompleted();
                }
            });

            // Schedule a task continuation that will schedule the next enabled operation upon completion.
            task.ContinueWith(t => this.Scheduler.ScheduleNextEnabledOperation(), TaskScheduler.Current);

            IO.Debug.WriteLine($"<CreateLog> Operation '{op.Name}' was created to execute task '{task.Id}'.");
            this.Scheduler.ScheduleOperation(op, task.Id);
            task.Start();
            this.Scheduler.WaitOperationStart(op);
            this.Scheduler.ScheduleNextEnabledOperation();

            return new ControlledTask(this, task);
        }

        /// <summary>
        /// Schedules the specified function to be executed asynchronously.
        /// </summary>
        [DebuggerStepThrough]
        public ControlledTask ScheduleFunction(Func<ControlledTask> function, Task predecessor, CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            this.Assert(function != null, "The task cannot execute a null function.");

            ulong operationId = this.Runtime.GetNextOperationId();
            var op = new TaskOperation(operationId, "ControlledTask", this.Scheduler);
            this.Scheduler.RegisterOperation(op);
            op.OnEnabled();

            Task<Task> task = new Task<Task>(() =>
            {
                try
                {
                    // Update the current asynchronous control flow with the current runtime instance,
                    // allowing future retrieval in the same asynchronous call stack.
                    CoyoteRuntime.SetRuntimeToAsynchronousControlFlow(this.Runtime);

                    OperationScheduler.StartOperation(op);
                    if (predecessor != null)
                    {
                        op.OnWaitTask(predecessor);
                    }

                    ControlledTask resultTask = function();
                    this.OnWaitTask(operationId, resultTask.AwaiterTask);
                    return resultTask.AwaiterTask;
                }
                catch (Exception ex)
                {
                    // Report the unhandled exception and rethrow it.
                    this.ReportUnhandledExceptionInOperation(op, ex);
                    throw;
                }
                finally
                {
                    IO.Debug.WriteLine($"<ScheduleDebug> Completed operation '{op.Name}' on task '{Task.CurrentId}'.");
                    op.OnCompleted();
                }
            });

            Task innerTask = task.Unwrap();

            // Schedule a task continuation that will schedule the next enabled operation upon completion.
            innerTask.ContinueWith(t => this.Scheduler.ScheduleNextEnabledOperation(), TaskScheduler.Current);

            IO.Debug.WriteLine($"<CreateLog> Operation '{op.Name}' was created to execute task '{task.Id}'.");
            this.Scheduler.ScheduleOperation(op, task.Id);
            task.Start();
            this.Scheduler.WaitOperationStart(op);
            this.Scheduler.ScheduleNextEnabledOperation();

            return new ControlledTask(this, innerTask);
        }

        /// <summary>
        /// Schedules the specified function to be executed asynchronously.
        /// </summary>
        [DebuggerStepThrough]
        public ControlledTask<TResult> ScheduleFunction<TResult>(Func<ControlledTask<TResult>> function, Task predecessor,
            CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            this.Assert(function != null, "The task cannot execute a null function.");

            ulong operationId = this.Runtime.GetNextOperationId();
            var op = new TaskOperation(operationId, "ControlledTask", this.Scheduler);
            this.Scheduler.RegisterOperation(op);
            op.OnEnabled();

            Task<Task<TResult>> task = new Task<Task<TResult>>(() =>
            {
                try
                {
                    // Update the current asynchronous control flow with the current runtime instance,
                    // allowing future retrieval in the same asynchronous call stack.
                    CoyoteRuntime.SetRuntimeToAsynchronousControlFlow(this.Runtime);

                    OperationScheduler.StartOperation(op);
                    if (predecessor != null)
                    {
                        op.OnWaitTask(predecessor);
                    }

                    ControlledTask<TResult> resultTask = function();
                    this.OnWaitTask(operationId, resultTask.AwaiterTask);
                    return resultTask.AwaiterTask;
                }
                catch (Exception ex)
                {
                    // Report the unhandled exception and rethrow it.
                    this.ReportUnhandledExceptionInOperation(op, ex);
                    throw;
                }
                finally
                {
                    IO.Debug.WriteLine($"<ScheduleDebug> Completed operation '{op.Name}' on task '{Task.CurrentId}'.");
                    op.OnCompleted();
                }
            });

            Task<TResult> innerTask = task.Unwrap();

            // Schedule a task continuation that will schedule the next enabled operation upon completion.
            innerTask.ContinueWith(t => this.Scheduler.ScheduleNextEnabledOperation(), TaskScheduler.Current);

            IO.Debug.WriteLine($"<CreateLog> Operation '{op.Name}' was created to execute task '{task.Id}'.");
            this.Scheduler.ScheduleOperation(op, task.Id);
            task.Start();
            this.Scheduler.WaitOperationStart(op);
            this.Scheduler.ScheduleNextEnabledOperation();

            return new ControlledTask<TResult>(this, innerTask);
        }

        /// <summary>
        /// Schedules the specified delegate to be executed asynchronously.
        /// </summary>
        [DebuggerStepThrough]
        public ControlledTask<TResult> ScheduleDelegate<TResult>(Delegate work, Task predecessor, CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            this.Assert(work != null, "The task cannot execute a null delegate.");

            ulong operationId = this.Runtime.GetNextOperationId();
            var op = new TaskOperation(operationId, "ControlledTask", this.Scheduler);
            this.Scheduler.RegisterOperation(op);
            op.OnEnabled();

            Task<TResult> task = new Task<TResult>(() =>
            {
                try
                {
                    // Update the current asynchronous control flow with the current runtime instance,
                    // allowing future retrieval in the same asynchronous call stack.
                    CoyoteRuntime.SetRuntimeToAsynchronousControlFlow(this.Runtime);

                    OperationScheduler.StartOperation(op);
                    if (predecessor != null)
                    {
                        op.OnWaitTask(predecessor);
                    }

                    if (work is Func<Task> funcWithTaskResult)
                    {
                        Task resultTask = funcWithTaskResult();
                        this.OnWaitTask(operationId, resultTask);
                        if (resultTask is TResult typedResultTask)
                        {
                            return typedResultTask;
                        }
                    }
                    else if (work is Func<Task<TResult>> funcWithGenericTaskResult)
                    {
                        Task<TResult> resultTask = funcWithGenericTaskResult();
                        this.OnWaitTask(operationId, resultTask);
                        return resultTask.Result;
                    }
                    else if (work is Func<TResult> funcWithGenericResult)
                    {
                        return funcWithGenericResult();
                    }

                    return default;
                }
                catch (Exception ex)
                {
                    // Report the unhandled exception and rethrow it.
                    this.ReportUnhandledExceptionInOperation(op, ex);
                    throw;
                }
                finally
                {
                    IO.Debug.WriteLine($"<ScheduleDebug> Completed operation '{op.Name}' on task '{Task.CurrentId}'.");
                    op.OnCompleted();
                }
            });

            // Schedule a task continuation that will schedule the next enabled operation upon completion.
            task.ContinueWith(t => this.Scheduler.ScheduleNextEnabledOperation(), TaskScheduler.Current);

            IO.Debug.WriteLine($"<CreateLog> Operation '{op.Name}' was created to execute task '{task.Id}'.");
            this.Scheduler.ScheduleOperation(op, task.Id);
            task.Start();
            this.Scheduler.WaitOperationStart(op);
            this.Scheduler.ScheduleNextEnabledOperation();

            return new ControlledTask<TResult>(this, task);
        }

        /// <summary>
        /// Schedules the specified delay to be executed asynchronously.
        /// </summary>
        [DebuggerStepThrough]
        public ControlledTask ScheduleDelay(TimeSpan delay, CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            if (delay.TotalMilliseconds == 0)
            {
                // If the delay is 0, then complete synchronously.
                return ControlledTask.CompletedTask;
            }

            // TODO: cache the dummy delay action to optimize memory.
            return this.ScheduleAction(() => { }, null, cancellationToken);
        }

        /// <summary>
        /// Schedules the specified task awaiter continuation to be executed asynchronously.
        /// </summary>
        [DebuggerStepThrough]
        public void ScheduleTaskAwaiterContinuation(Task task, Action continuation)
        {
            try
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                this.Assert(callerOp != null,
                    "Task with id '{0}' that is not controlled by the runtime is executing controlled task '{1}'.",
                    Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>", task.Id);

                if (callerOp.IsExecutingInRootAsyncMethod())
                {
                    IO.Debug.WriteLine("<ControlledTask> '{0}' is executing continuation of task '{1}' on task '{2}'.",
                        callerOp.Name, task.Id, Task.CurrentId);
                    continuation();
                    IO.Debug.WriteLine("<ControlledTask> '{0}' resumed after continuation of task '{1}' on task '{2}'.",
                        callerOp.Name, task.Id, Task.CurrentId);
                }
                else
                {
                    IO.Debug.WriteLine("<ControlledTask> '{0}' is dispatching continuation of task '{1}'.", callerOp.Name, task.Id);
                    this.ScheduleAction(continuation, task, default);
                    IO.Debug.WriteLine("<ControlledTask> '{0}' dispatched continuation of task '{1}'.", callerOp.Name, task.Id);
                }
            }
            catch (ExecutionCanceledException)
            {
                IO.Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown from task '{Task.CurrentId}'.");
            }
        }

        /// <summary>
        /// Schedules the specified yield awaiter continuation to be executed asynchronously.
        /// </summary>
        [DebuggerStepThrough]
        public void ScheduleYieldAwaiterContinuation(Action continuation)
        {
            try
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                this.Assert(callerOp != null,
                    "Uncontrolled task with id '{0}' invoked a yield operation.",
                    Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");
                IO.Debug.WriteLine("<ControlledTask> '{0}' is executing a yield operation.", callerOp.Id);
                this.ScheduleAction(continuation, null, default);
            }
            catch (ExecutionCanceledException)
            {
                IO.Debug.WriteLine($"<Exception> ExecutionCanceledException was thrown from task '{Task.CurrentId}'.");
            }
        }

        /// <summary>
        /// Creates a controlled task that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        [DebuggerStepThrough]
        public ControlledTask WhenAllTasksCompleteAsync(IEnumerable<ControlledTask> tasks)
        {
            this.Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            this.Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.Assert(callerOp != null,
                "Uncontrolled task with id '{0}' invoked a when-all operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");
            callerOp.OnWaitTasks(tasks, waitAll: true);

            List<Exception> exceptions = null;
            foreach (var task in tasks)
            {
                if (task.IsFaulted)
                {
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

                    exceptions.Add(task.Exception);
                }
            }

            if (exceptions != null)
            {
                return ControlledTask.FromException(new AggregateException(exceptions));
            }
            else
            {
                return ControlledTask.CompletedTask;
            }
        }

        /// <summary>
        /// Creates a controlled task that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
        [DebuggerStepThrough]
        public ControlledTask<TResult[]> WhenAllTasksCompleteAsync<TResult>(IEnumerable<ControlledTask<TResult>> tasks)
        {
            this.Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            this.Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.Assert(callerOp != null,
                "Uncontrolled task with id '{0}' invoked a when-all operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");
            callerOp.OnWaitTasks(tasks, waitAll: true);

            int idx = 0;
            TResult[] result = new TResult[tasks.Count()];
            foreach (var task in tasks)
            {
                result[idx] = task.Result;
                idx++;
            }

            return ControlledTask.FromResult(result);
        }

        /// <summary>
        /// Creates a controlled task that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        [DebuggerStepThrough]
        public ControlledTask<ControlledTask> WhenAnyTaskCompletesAsync(IEnumerable<ControlledTask> tasks)
        {
            this.Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            this.Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.Assert(callerOp != null,
                "Uncontrolled task with id '{0}' invoked a when-any operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");
            callerOp.OnWaitTasks(tasks, waitAll: false);

            ControlledTask result = null;
            foreach (var task in tasks)
            {
                if (task.IsCompleted)
                {
                    result = task;
                    break;
                }
            }

            return ControlledTask.FromResult(result);
        }

        /// <summary>
        /// Creates a controlled task that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
        [DebuggerStepThrough]
        public ControlledTask<ControlledTask<TResult>> WhenAnyTaskCompletesAsync<TResult>(IEnumerable<ControlledTask<TResult>> tasks)
        {
            this.Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            this.Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.Assert(callerOp != null,
                "Uncontrolled task with id '{0}' invoked a when-any operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");
            callerOp.OnWaitTasks(tasks, waitAll: false);

            ControlledTask<TResult> result = null;
            foreach (var task in tasks)
            {
                if (task.IsCompleted)
                {
                    result = task;
                    break;
                }
            }

            return ControlledTask.FromResult(result);
        }

        /// <summary>
        /// Waits for all of the provided controlled task objects to complete execution within
        /// a specified number of milliseconds or until a cancellation token is cancelled.
        /// </summary>
        public bool WaitAllTasksComplete(ControlledTask[] tasks, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            this.Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            this.Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.Assert(callerOp != null,
                "Uncontrolled task with id '{0}' invoked a wait-all operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");
            callerOp.OnWaitTasks(tasks, waitAll: true);

            // TODO: support timeouts during testing, this would become false if there is a timeout.
            return true;
        }

        /// <summary>
        /// Waits for any of the provided controlled task objects to complete execution within
        /// a specified number of milliseconds or until a cancellation token is cancelled.
        /// </summary>
        [DebuggerStepThrough]
        public int WaitAnyTaskCompletes(ControlledTask[] tasks, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            this.Assert(tasks != null, "Cannot wait for a null array of tasks to complete.");
            this.Assert(tasks.Count() > 0, "Cannot wait for zero tasks to complete.");

            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.Assert(callerOp != null,
                "Uncontrolled task with id '{0}' invoked a wait-any operation.",
                Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>");
            callerOp.OnWaitTasks(tasks, waitAll: false);

            int result = -1;
            for (int i = 0; i < tasks.Length; i++)
            {
                if (tasks[i].IsCompleted)
                {
                    result = i;
                    break;
                }
            }

            // TODO: support timeouts during testing, this would become false if there is a timeout.
            return result;
        }

        /// <summary>
        /// Waits for the task to complete execution. The wait terminates if a timeout interval
        /// elapses or a cancellation token is canceled before the task completes.
        /// </summary>
        public bool WaitTaskCompletes(ControlledTask task, int millisecondsTimeout, CancellationToken cancellationToken)
        {
            // TODO: return immediately if completed without errors.
            // TODO: support timeouts and cancellation tokens.
            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            IO.Debug.WriteLine("<ControlledTask> '{0}' is waiting task '{1}' to complete from task '{2}'.",
                callerOp.Name, task.Id, Task.CurrentId);
            callerOp.OnWaitTask(task.AwaiterTask);
            return true;
        }

        /// <summary>
        /// Waits for the task to complete execution and returns the result.
        /// </summary>
        public TResult WaitTaskCompletes<TResult>(ControlledTask<TResult> task)
        {
            // TODO: return immediately if completed without errors.
            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            IO.Debug.WriteLine("<ControlledTask> '{0}' is waiting task '{1}' with result type '{2}' to complete from task '{3}'.",
                callerOp.Name, task.Id, typeof(TResult), Task.CurrentId);
            callerOp.OnWaitTask(task.AwaiterTask);
            return task.AwaiterTask.Result;
        }

        /// <summary>
        /// Callback invoked when the <see cref="AsyncControlledTaskMethodBuilder.Start"/> is called.
        /// </summary>
        [DebuggerHidden]
        public void OnAsyncControlledTaskMethodBuilderStart(Type stateMachineType)
        {
            try
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                callerOp.SetRootAsyncControlledTaskStateMachine(stateMachineType);
            }
            catch (RuntimeException ex)
            {
                this.Assert(false, ex.Message);
            }
        }

        /// <summary>
        /// Callback invoked when the <see cref="AsyncControlledTaskMethodBuilder.Task"/> is accessed.
        /// </summary>
        [DebuggerHidden]
        public void OnAsyncControlledTaskMethodBuilderTask()
        {
            if (!this.Scheduler.IsRunning)
            {
                throw new ExecutionCanceledException();
            }

            this.Scheduler.CheckNoExternalConcurrencyUsed();
        }

        /// <summary>
        /// Callback invoked when the <see cref="AsyncControlledTaskMethodBuilder.AwaitOnCompleted"/>
        /// or <see cref="AsyncControlledTaskMethodBuilder.AwaitUnsafeOnCompleted"/> is called.
        /// </summary>
        [DebuggerHidden]
        public void OnAsyncControlledTaskMethodBuilderAwaitCompleted(Type awaiterType, Type stateMachineType)
        {
            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.Assert(callerOp.IsAwaiterControlled, "Controlled task '{0}' is trying to wait for an uncontrolled " +
                "task or awaiter to complete. Please make sure to use Coyote APIs to express concurrency " +
                "(e.g. ControlledTask instead of Task).",
                Task.CurrentId);
            this.Assert(awaiterType.Namespace == typeof(ControlledTask).Namespace,
                "Controlled task '{0}' is trying to wait for an uncontrolled task or awaiter to complete. " +
                "Please make sure to use Coyote APIs to express concurrency (e.g. ControlledTask instead of Task).",
                Task.CurrentId);
            callerOp.SetExecutingAsyncControlledTaskStateMachineType(stateMachineType);
        }

        /// <summary>
        /// Callback invoked when the currently executing task operation gets a controlled awaiter.
        /// </summary>
        [DebuggerHidden]
        public void OnGetControlledAwaiter()
        {
            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            callerOp.OnGetControlledAwaiter();
        }

        /// <summary>
        /// Callback invoked when the <see cref="ControlledYieldAwaitable.ControlledYieldAwaiter.GetResult"/> is called.
        /// </summary>
        [DebuggerStepThrough]
        public void OnControlledYieldAwaiterGetResult()
        {
            this.Scheduler.ScheduleNextEnabledOperation();
        }

        /// <summary>
        /// Callback invoked when the executing operation is waiting for the specified task to complete.
        /// </summary>
        [DebuggerStepThrough]
        public void OnWaitTask(Task task)
        {
            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            callerOp.OnWaitTask(task);
        }

        /// <summary>
        /// Callback invoked when the executing task is waiting for the task with the specified operation id to complete.
        /// </summary>
        [DebuggerStepThrough]
        internal void OnWaitTask(ulong operationId, Task task)
        {
            this.Assert(task != null, "Controlled task '{0}' is waiting for a null task to complete.", Task.CurrentId);
            if (!task.IsCompleted)
            {
                var op = this.Scheduler.GetOperationWithId<TaskOperation>(operationId);
                op.OnWaitTask(task);
            }
        }

        /// <summary>
        /// Reports an unhandled exception in the specified asynchronous operation.
        /// </summary>
        private void ReportUnhandledExceptionInOperation(AsyncOperation op, Exception ex)
        {
            string message = string.Format(CultureInfo.InvariantCulture,
                $"Exception '{ex.GetType()}' was thrown in operation '{op.Name}', " +
                $"'{ex.Source}':\n" +
                $"   {ex.Message}\n" +
                $"The stack trace is:\n{ex.StackTrace}");
            IO.Debug.WriteLine($"<Exception> {message}");
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, triggers a failure.
        /// </summary>
        [DebuggerHidden]
        private void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                this.Scheduler.NotifyAssertionFailure(string.Format(CultureInfo.InvariantCulture, s, args));
            }
        }
    }
}
