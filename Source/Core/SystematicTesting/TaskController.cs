// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using CoyoteTasks = Microsoft.Coyote.Tasks;

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Responsible for controlling the execution of tasks during systematic testing.
    /// </summary>
    internal sealed class TaskController
    {
        /// <summary>
        /// The executing runtime.
        /// </summary>
        private readonly ControlledRuntime Runtime;

        /// <summary>
        /// The asynchronous operation scheduler.
        /// </summary>
        private readonly OperationScheduler Scheduler;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskController"/> class.
        /// </summary>
        internal TaskController(ControlledRuntime runtime, OperationScheduler scheduler)
        {
            this.Runtime = runtime;
            this.Scheduler = scheduler;
            Interception.ControlledThread.ClearCache();
        }

        /// <summary>
        /// Schedules the specified action to be executed asynchronously.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal Task ScheduleAction(Action action, Task predecessor, bool isYield, bool failException, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TaskOperation op = this.Runtime.CreateTaskOperation();
            OperationExecutionOptions options = OperationContext.CreateOperationExecutionOptions(failException, isYield);
            var context = new OperationContext<Action, object>(op, action, predecessor, options, cancellationToken);
            var task = new Task(this.ExecuteOperation, context, cancellationToken);
            this.ScheduleTaskOperation(op, task);
            return context.ResultSource.Task;
        }

        /// <summary>
        /// Schedules the specified function to be executed asynchronously.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal Task<Task> ScheduleFunction(Func<Task> function, Task predecessor, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Task<Task> task = null;
            TaskOperation op = this.Runtime.CreateTaskOperation();
            var context = new AsyncOperationContext<Func<Task>, Task, Task>(op, function, task, predecessor,
                OperationExecutionOptions.None, cancellationToken);
            task = new Task<Task>(this.ExecuteOperation<Func<Task>, Task, Task>, context, cancellationToken);
            this.ScheduleTaskOperation(op, task);
            return context.ExecutorSource.Task;
        }

        /// <summary>
        /// Schedules the specified function to be executed asynchronously.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal Task<Task<TResult>> ScheduleFunction<TResult>(Func<Task<TResult>> function, Task predecessor, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Task<TResult> task = null;
            TaskOperation op = this.Runtime.CreateTaskOperation();
            var context = new AsyncOperationContext<Func<Task<TResult>>, Task<TResult>, TResult>(op, function, task, predecessor,
                OperationExecutionOptions.None, cancellationToken);
            task = new Task<TResult>(this.ExecuteOperation<Func<Task<TResult>>, Task<TResult>, TResult>, context, cancellationToken);
            this.ScheduleTaskOperation(op, task);
            return context.ExecutorSource.Task;
        }

        /// <summary>
        /// Schedules the specified function to be executed asynchronously.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal CoyoteTasks.Task ScheduleFunction(Func<CoyoteTasks.Task> function, Task predecessor, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Task<Task> task = null;
            TaskOperation op = this.Runtime.CreateTaskOperation();
            var context = new AsyncOperationContext<Func<CoyoteTasks.Task>, Task, Task>(op, function, task, predecessor,
                OperationExecutionOptions.None, cancellationToken);
            task = new Task<Task>(this.ExecuteOperation<Func<CoyoteTasks.Task>, Task, Task>, context, cancellationToken);
            this.ScheduleTaskOperation(op, task);
            return new CoyoteTasks.Task(this, context.ResultSource.Task);
        }

        /// <summary>
        /// Schedules the specified function to be executed asynchronously.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal CoyoteTasks.Task<TResult> ScheduleFunction<TResult>(Func<CoyoteTasks.Task<TResult>> function, Task predecessor,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Task<TResult> task = null;
            TaskOperation op = this.Runtime.CreateTaskOperation();
            var context = new AsyncOperationContext<Func<CoyoteTasks.Task<TResult>>, Task<TResult>, TResult>(op, function, task, predecessor,
                OperationExecutionOptions.None, cancellationToken);
            task = new Task<TResult>(this.ExecuteOperation<Func<CoyoteTasks.Task<TResult>>, Task<TResult>, TResult>, context, cancellationToken);
            this.ScheduleTaskOperation(op, task);
            return new CoyoteTasks.Task<TResult>(this, context.ResultSource.Task);
        }

        /// <summary>
        /// Schedules the specified function to be executed asynchronously.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal Task<TResult> ScheduleFunction<TResult>(Func<TResult> function, Task predecessor, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            TaskOperation op = this.Runtime.CreateTaskOperation();
            var context = new OperationContext<Func<TResult>, TResult>(op, function, predecessor,
                OperationExecutionOptions.None, cancellationToken);
            var task = new Task<TResult>(this.ExecuteOperation<Func<TResult>, TResult, TResult>, context, cancellationToken);
            this.ScheduleTaskOperation(op, task);
            return context.ResultSource.Task;
        }

        /// <summary>
        /// Schedules the specified task operation for execution.
        /// </summary>
        internal void ScheduleTaskOperation(TaskOperation op, Task task)
        {
            IO.Debug.WriteLine("<CreateLog> Operation '{0}' was created to execute task '{1}'.", op.Name, task.Id);
            task.Start();
            this.Scheduler.WaitOperationStart(op);
            this.Scheduler.ScheduleNextOperation();
        }

        /// <summary>
        /// Execute the operation with the specified context.
        /// </summary>
        internal void ExecuteOperation(object state)
        {
            // Extract the expected operation context from the task state.
            var context = state as OperationContext<Action, object>;

            TaskOperation op = context.Operation;
            CancellationToken ct = context.CancellationToken;
            Exception exception = null;

            try
            {
                // Update the current asynchronous control flow with the current runtime instance,
                // allowing future retrieval in the same asynchronous call stack.
                CoyoteRuntime.AssignAsyncControlFlowRuntime(this.Runtime);

                // Notify the scheduler that the operation started. This will yield execution until
                // the operation is ready to get scheduled.
                this.Scheduler.StartOperation(op);
                if (context.Predecessor != null)
                {
                    // If there is a predecessor task, then wait until the predecessor completes.
                    ct.ThrowIfCancellationRequested();
                    op.OnWaitTask(context.Predecessor);
                }

                if (context.Options.HasFlag(OperationExecutionOptions.YieldAtStart))
                {
                    // Try yield execution to the next operation.
                    this.Scheduler.ScheduleNextOperation(true);
                }

                // Check if the operation must be canceled before starting the work.
                ct.ThrowIfCancellationRequested();

                // Start executing the work.
                context.Work();
            }
            catch (Exception ex)
            {
                if (context.Options.HasFlag(OperationExecutionOptions.FailOnException))
                {
                    this.Assert(false, "Unhandled exception. {0}", ex);
                }
                else
                {
                    // Unwrap and cache the exception to propagate it.
                    exception = ControlledRuntime.UnwrapException(ex);
                    this.ReportThrownException(exception);
                }
            }
            finally
            {
                IO.Debug.WriteLine("<ScheduleDebug> Completed operation '{0}' on task '{1}'.", op.Name, Task.CurrentId);
                op.OnCompleted();

                // Set the result task completion source to notify to the awaiters that the operation
                // has been completed, and schedule the next enabled operation.
                this.SetTaskCompletionSource(context.ResultSource, null, exception, default);
                this.Scheduler.ScheduleNextOperation();
            }
        }

        /// <summary>
        /// Execute the (asynchronous) operation with the specified context.
        /// </summary>
        private TResult ExecuteOperation<TWork, TExecutor, TResult>(object state)
        {
            // Extract the expected operation context from the task state.
            var context = state as AsyncOperationContext<TWork, TExecutor, TResult> ?? state as OperationContext<TWork, TResult>;

            TaskOperation op = context.Operation;
            CancellationToken ct = context.CancellationToken;
            TResult result = default;
            Exception exception = null;

            // The operation execution logic uses two task completion sources: (1) an executor TCS and (2) a result TCS.
            // We do this to model the execution of tasks in the .NET runtime. For example, the `Task.Factory.StartNew`
            // method has different semantics from `Task.Run`, e.g. the returned task from `Task.Factory.StartNew(Func<T>)`
            // completes at the start of an asynchronous operation, so someone cannot await on it for the completion of
            // the operation. Instead, someone needs to first use `task.Unwrap()`, and then await on the unwrapped task.
            // To model this, the executor TCS completes at the start of the operation, and contains in its `task.AsyncState`
            // a reference to the result TCS. This approach allows us to implement `task.Unwrap` in a way that gives access
            // to the result TCS, which someone can then await for the asynchronous completion of the operation.

            try
            {
                // Update the current asynchronous control flow with the current runtime instance,
                // allowing future retrieval in the same asynchronous call stack.
                CoyoteRuntime.AssignAsyncControlFlowRuntime(this.Runtime);

                // Notify the scheduler that the operation started. This will yield execution until
                // the operation is ready to get scheduled.
                this.Scheduler.StartOperation(op);
                if (context is AsyncOperationContext<TWork, TExecutor, TResult> asyncContext)
                {
                    // If the operation is asynchronous, then set the executor task completion source, which
                    // can be used by `UnwrapTask` to unwrap and return the task executing this operation.
                    this.SetTaskCompletionSource(asyncContext.ExecutorSource, asyncContext.Executor, null, ct);
                }

                if (context.Predecessor != null)
                {
                    // If there is a predecessor task, then wait until the predecessor completes.
                    ct.ThrowIfCancellationRequested();
                    op.OnWaitTask(context.Predecessor);
                }

                // Check if the operation must be canceled before starting the work.
                ct.ThrowIfCancellationRequested();

                // Start executing the (asynchronous) work.
                Task executor = null;
                if (context.Work is Func<Task<TResult>> funcWithTaskResult)
                {
                    executor = funcWithTaskResult();
                }
                else if (context.Work is Func<Task> funcWithTask)
                {
                    executor = funcWithTask();
                }
                else if (context.Work is Func<CoyoteTasks.Task<TResult>> funcWithCoyoteTaskResult)
                {
                    // TODO: temporary until we remove the custom task type.
                    executor = funcWithCoyoteTaskResult().UncontrolledTask;
                }
                else if (context.Work is Func<CoyoteTasks.Task> funcWithCoyoteTask)
                {
                    // TODO: temporary until we remove the custom task type.
                    executor = funcWithCoyoteTask().UncontrolledTask;
                }
                else if (context.Work is Func<TResult> func)
                {
                    result = func();
                }
                else
                {
                    throw new NotSupportedException($"Unable to execute work with unsupported type {context.Work.GetType()}.");
                }

                if (executor != null)
                {
                    // If the work is asynchronous, then wait until it completes.
                    this.OnWaitTask(op.Id, executor);
                    if (executor.IsFaulted)
                    {
                        // Propagate the failing exception by rethrowing it.
                        ExceptionDispatchInfo.Capture(executor.Exception).Throw();
                    }
                    else if (executor.IsCanceled)
                    {
                        if (op.Exception != null)
                        {
                            // An exception has been already captured, so propagate it.
                            ExceptionDispatchInfo.Capture(op.Exception).Throw();
                        }
                        else
                        {
                            // Wait the canceled executor (which is non-blocking as it has already completed)
                            // to throw the generated `OperationCanceledException`.
                            executor.Wait();
                        }
                    }

                    // Safely get the result without blocking as the work has completed.
                    result = executor is Task<TResult> resultTask ? resultTask.Result :
                        executor is TResult r ? r : default;
                }
            }
            catch (Exception ex)
            {
                // Unwrap and cache the exception to propagate it.
                exception = ControlledRuntime.UnwrapException(ex);
                this.ReportThrownException(exception);
            }
            finally
            {
                IO.Debug.WriteLine("<ScheduleDebug> Completed operation '{0}' on task '{1}'.", op.Name, Task.CurrentId);
                op.OnCompleted();

                // Set the result task completion source to notify to the awaiters that the operation
                // has been completed, and schedule the next enabled operation.
                this.SetTaskCompletionSource(context.ResultSource, result, exception, default);
                this.Scheduler.ScheduleNextOperation();
            }

            return result;
        }

        /// <summary>
        /// Sets the specified task completion source with a result, cancelation or exception.
        /// </summary>
        private void SetTaskCompletionSource<TResult>(TaskCompletionSource<TResult> tcs, TResult result,
            Exception ex, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                tcs.SetCanceled();
            }
            else if (ex != null)
            {
                tcs.SetException(ex);
            }
            else
            {
                tcs.SetResult(result);
            }
        }

        /// <summary>
        /// Schedules the specified delay to be executed asynchronously.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal Task ScheduleDelay(TimeSpan delay, CancellationToken cancellationToken)
        {
            // TODO: support cancellations during testing.
            if (delay.TotalMilliseconds == 0)
            {
                // If the delay is 0, then complete synchronously.
                return Task.CompletedTask;
            }

            // TODO: cache the dummy delay action to optimize memory.
            return this.ScheduleAction(() => { }, null, false, false, cancellationToken);
        }

        /// <summary>
        /// Schedules the specified task awaiter continuation to be executed asynchronously.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal void ScheduleTaskAwaiterContinuation(Task task, Action continuation)
        {
            try
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                this.Assert(callerOp != null,
                    "Task with id '{0}' that is not controlled by the runtime is executing controlled task '{1}'.",
                    Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>", task.Id);

                if (IsCurrentOperationExecutingAsynchronously())
                {
                    IO.Debug.WriteLine("<Task> '{0}' is dispatching continuation of task '{1}'.", callerOp.Name, task.Id);
                    this.ScheduleAction(continuation, task, false, false, default);
                    IO.Debug.WriteLine("<Task> '{0}' dispatched continuation of task '{1}'.", callerOp.Name, task.Id);
                }
                else
                {
                    IO.Debug.WriteLine("<Task> '{0}' is executing continuation of task '{1}' on task '{2}'.",
                        callerOp.Name, task.Id, Task.CurrentId);
                    continuation();
                    IO.Debug.WriteLine("<Task> '{0}' resumed after continuation of task '{1}' on task '{2}'.",
                        callerOp.Name, task.Id, Task.CurrentId);
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
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal void ScheduleYieldAwaiterContinuation(Action continuation)
        {
            try
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                this.AssertIsTaskControlled(callerOp, "Yield");
                IO.Debug.WriteLine("<Task> '{0}' is executing a yield operation.", callerOp.Id);
                this.ScheduleAction(continuation, null, true, false, default);
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
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal Task WhenAllTasksCompleteAsync(Task[] tasks)
        {
            if (tasks is null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }
            else if (tasks.Length is 0)
            {
                return Task.CompletedTask;
            }

            return this.ScheduleAction(() =>
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                this.AssertIsTaskControlled(callerOp, "WhenAll");
                callerOp.OnWaitTasks(tasks, waitAll: true);

                List<Exception> exceptions = null;
                foreach (var task in tasks)
                {
                    if (task.IsFaulted)
                    {
                        exceptions ??= new List<Exception>();
                        exceptions.Add(task.Exception is AggregateException aex ? aex.InnerException : task.Exception);
                    }
                }

                if (exceptions != null)
                {
                    throw new AggregateException(exceptions);
                }
            }, null, false, false, default);
        }

        /// <summary>
        /// Creates a controlled task that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal Task WhenAllTasksCompleteAsync(CoyoteTasks.Task[] tasks)
        {
            if (tasks is null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }
            else if (tasks.Length is 0)
            {
                return Task.CompletedTask;
            }

            return this.ScheduleAction(() =>
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                this.AssertIsTaskControlled(callerOp, "WhenAll");
                callerOp.OnWaitTasks(tasks, waitAll: true);

                List<Exception> exceptions = null;
                foreach (var task in tasks)
                {
                    if (task.IsFaulted)
                    {
                        exceptions ??= new List<Exception>();
                        exceptions.Add(task.Exception is AggregateException aex ? aex.InnerException : task.Exception);
                    }
                }

                if (exceptions != null)
                {
                    throw new AggregateException(exceptions);
                }
            }, null, false, false, default);
        }

        /// <summary>
        /// Creates a controlled task that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal Task<TResult[]> WhenAllTasksCompleteAsync<TResult>(Task<TResult>[] tasks)
        {
            if (tasks is null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }
            else if (tasks.Length is 0)
            {
                return Task.FromResult(Array.Empty<TResult>());
            }

            return this.ScheduleFunction(() =>
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                this.AssertIsTaskControlled(callerOp, "WhenAll");
                callerOp.OnWaitTasks(tasks, waitAll: true);

                List<Exception> exceptions = null;
                foreach (var task in tasks)
                {
                    if (task.IsFaulted)
                    {
                        exceptions ??= new List<Exception>();
                        exceptions.Add(task.Exception is AggregateException aex ? aex.InnerException : task.Exception);
                    }
                }

                if (exceptions != null)
                {
                    throw new AggregateException(exceptions);
                }

                int idx = 0;
                TResult[] result = new TResult[tasks.Length];
                foreach (var task in tasks)
                {
                    result[idx] = task.Result;
                    idx++;
                }

                return result;
            }, null, default);
        }

        /// <summary>
        /// Creates a controlled task that will complete when all tasks
        /// in the specified enumerable collection have completed.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal Task<TResult[]> WhenAllTasksCompleteAsync<TResult>(CoyoteTasks.Task<TResult>[] tasks)
        {
            if (tasks is null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }
            else if (tasks.Length is 0)
            {
                return Task.FromResult(Array.Empty<TResult>());
            }

            return this.ScheduleFunction(() =>
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                this.AssertIsTaskControlled(callerOp, "WhenAll");
                callerOp.OnWaitTasks(tasks, waitAll: true);

                List<Exception> exceptions = null;
                foreach (var task in tasks)
                {
                    if (task.IsFaulted)
                    {
                        exceptions ??= new List<Exception>();
                        exceptions.Add(task.Exception is AggregateException aex ? aex.InnerException : task.Exception);
                    }
                }

                if (exceptions != null)
                {
                    throw new AggregateException(exceptions);
                }

                int idx = 0;
                TResult[] result = new TResult[tasks.Length];
                foreach (var task in tasks)
                {
                    result[idx] = task.Result;
                    idx++;
                }

                return result;
            }, null, default);
        }

        /// <summary>
        /// Creates a controlled task that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal Task<Task> WhenAnyTaskCompletesAsync(Task[] tasks)
        {
            if (tasks is null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }
            else if (tasks.Length is 0)
            {
                throw new ArgumentException("The tasks argument contains no tasks.");
            }

            var task = this.ScheduleFunction(() =>
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                this.AssertIsTaskControlled(callerOp, "WhenAny");
                callerOp.OnWaitTasks(tasks, waitAll: false);

                Task result = null;
                foreach (var task in tasks)
                {
                    if (task.IsCompleted)
                    {
                        result = task;
                        break;
                    }
                }

                return Task.FromResult(result);
            }, null, default);

            return this.UnwrapTask(task);
        }

        /// <summary>
        /// Creates a controlled task that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal CoyoteTasks.Task<CoyoteTasks.Task> WhenAnyTaskCompletesAsync(CoyoteTasks.Task[] tasks)
        {
            if (tasks is null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }
            else if (tasks.Length is 0)
            {
                throw new ArgumentException("The tasks argument contains no tasks.");
            }

            return this.ScheduleFunction(() =>
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                this.AssertIsTaskControlled(callerOp, "WhenAny");
                callerOp.OnWaitTasks(tasks, waitAll: false);

                CoyoteTasks.Task result = null;
                foreach (var task in tasks)
                {
                    if (task.IsCompleted)
                    {
                        result = task;
                        break;
                    }
                }

                return CoyoteTasks.Task.FromResult(result);
            }, null, default);
        }

        /// <summary>
        /// Creates a controlled task that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal Task<Task<TResult>> WhenAnyTaskCompletesAsync<TResult>(Task<TResult>[] tasks)
        {
            if (tasks is null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }
            else if (tasks.Length is 0)
            {
                throw new ArgumentException("The tasks argument contains no tasks.");
            }

            var task = this.ScheduleFunction(() =>
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                this.AssertIsTaskControlled(callerOp, "WhenAny");
                callerOp.OnWaitTasks(tasks, waitAll: false);

                Task<TResult> result = null;
                foreach (var task in tasks)
                {
                    if (task.IsCompleted)
                    {
                        result = task;
                        break;
                    }
                }

                return Task.FromResult(result);
            }, null, default);

            return this.UnwrapTask(task);
        }

        /// <summary>
        /// Creates a controlled task that will complete when any task
        /// in the specified enumerable collection have completed.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal CoyoteTasks.Task<CoyoteTasks.Task<TResult>> WhenAnyTaskCompletesAsync<TResult>(CoyoteTasks.Task<TResult>[] tasks)
        {
            if (tasks is null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }
            else if (tasks.Length is 0)
            {
                throw new ArgumentException("The tasks argument contains no tasks.");
            }

            return this.ScheduleFunction(() =>
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                this.AssertIsTaskControlled(callerOp, "WhenAny");
                callerOp.OnWaitTasks(tasks, waitAll: false);

                CoyoteTasks.Task<TResult> result = null;
                foreach (var task in tasks)
                {
                    if (task.IsCompleted)
                    {
                        result = task;
                        break;
                    }
                }

                return CoyoteTasks.Task.FromResult(result);
            }, null, default);
        }

        /// <summary>
        /// Waits for all of the provided controlled task objects to complete execution within
        /// a specified number of milliseconds or until a cancellation token is cancelled.
        /// </summary>
        internal bool WaitAllTasksComplete(Task[] tasks)
        {
            // TODO: support cancellations during testing.
            if (tasks is null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }
            else if (tasks.Length is 0)
            {
                return true;
            }

            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.AssertIsTaskControlled(callerOp, "WaitAll");
            callerOp.OnWaitTasks(tasks, waitAll: true);

            // TODO: support timeouts during testing, this would become false if there is a timeout.
            return true;
        }

        /// <summary>
        /// Waits for all of the provided controlled task objects to complete execution within
        /// a specified number of milliseconds or until a cancellation token is cancelled.
        /// </summary>
        internal bool WaitAllTasksComplete(CoyoteTasks.Task[] tasks)
        {
            // TODO: support cancellations during testing.
            if (tasks is null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }
            else if (tasks.Length is 0)
            {
                return true;
            }

            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.AssertIsTaskControlled(callerOp, "WaitAll");
            callerOp.OnWaitTasks(tasks, waitAll: true);

            // TODO: support timeouts during testing, this would become false if there is a timeout.
            return true;
        }

        /// <summary>
        /// Waits for any of the provided controlled task objects to complete execution within
        /// a specified number of milliseconds or until a cancellation token is cancelled.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal int WaitAnyTaskCompletes(Task[] tasks)
        {
            // TODO: support cancellations during testing.
            if (tasks is null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }
            else if (tasks.Length is 0)
            {
                throw new ArgumentException("The tasks argument contains no tasks.");
            }

            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.AssertIsTaskControlled(callerOp, "WaitAny");

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
        /// Waits for any of the provided controlled task objects to complete execution within
        /// a specified number of milliseconds or until a cancellation token is cancelled.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal int WaitAnyTaskCompletes(CoyoteTasks.Task[] tasks)
        {
            // TODO: support cancellations during testing.
            if (tasks is null)
            {
                throw new ArgumentNullException(nameof(tasks));
            }
            else if (tasks.Length is 0)
            {
                throw new ArgumentException("The tasks argument contains no tasks.");
            }

            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            this.AssertIsTaskControlled(callerOp, "WaitAny");
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
        internal bool WaitTaskCompletes(Task task)
        {
            // TODO: support timeouts and cancellation tokens.
            // int millisecondsTimeout, CancellationToken cancellationToken
            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            callerOp.OnWaitTask(task);

            if (task.IsFaulted)
            {
                // Propagate the failing exception by rethrowing it.
                ExceptionDispatchInfo.Capture(task.Exception).Throw();
            }

            return true;
        }

        /// <summary>
        /// Waits for the task to complete execution and returns the result.
        /// </summary>
        internal TResult WaitTaskCompletes<TResult>(Task<TResult> task)
        {
            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            callerOp.OnWaitTask(task);

            if (task.IsFaulted)
            {
                // Propagate the failing exception by rethrowing it.
                ExceptionDispatchInfo.Capture(task.Exception).Throw();
            }

            return task.Result;
        }

        /// <summary>
        /// Unwraps the specified task.
        /// </summary>
        internal Task UnwrapTask(Task<Task> task) =>
            task.AsyncState is TaskCompletionSource<Task> tcs ? tcs.Task : task.Unwrap();

        /// <summary>
        /// Unwraps the specified task.
        /// </summary>
        internal Task<TResult> UnwrapTask<TResult>(Task<Task<TResult>> task) =>
            task.AsyncState is TaskCompletionSource<TResult> tcs ? tcs.Task : task.Unwrap();

        /// <summary>
        /// Callback invoked when the <see cref="AsyncTaskMethodBuilder.Task"/> is accessed.
        /// </summary>
        internal void OnAsyncTaskMethodBuilderSetException(Exception exception)
        {
            var op = this.Scheduler.GetExecutingOperation<TaskOperation>();
            op?.SetException(exception);
        }

        /// <summary>
        /// Checks if the currently executing operation is controlled by the runtime.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void CheckExecutingOperationIsControlled()
        {
            if (!this.Scheduler.IsAttached)
            {
                throw new ExecutionCanceledException();
            }

            this.Scheduler.CheckExecutingOperationIsControlled();
        }

        /// <summary>
        /// Callback invoked when the <see cref="CoyoteTasks.YieldAwaitable.YieldAwaiter.GetResult"/> is called.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal void OnYieldAwaiterGetResult() => this.Scheduler.ScheduleNextOperation();

        /// <summary>
        /// Callback invoked when the executing operation is waiting for the specified task to complete.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal void OnWaitTask(Task task)
        {
            var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
            callerOp.OnWaitTask(task);
        }

        /// <summary>
        /// Callback invoked when the executing task is waiting for the task with the specified operation id to complete.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal void OnWaitTask(ulong operationId, Task task)
        {
            this.Assert(task != null, "Task '{0}' is waiting for a null task to complete.", Task.CurrentId);
            if (!task.IsCompleted)
            {
                var op = this.Scheduler.GetOperationWithId<TaskOperation>(operationId);
                op.OnWaitTask(task);
            }
        }

        /// <summary>
        /// Returns true if the current operation is executing an asynchronous state machine, else false.
        /// </summary>
        private static bool IsCurrentOperationExecutingAsynchronously()
        {
            StackTrace st = new StackTrace(false);
            bool result = false;
            for (int i = 0; i < st.FrameCount; i++)
            {
                // Traverse the stack trace to find if the current operation is executing an asynchronous state machine.
                MethodBase method = st.GetFrame(i).GetMethod();
                if (method.DeclaringType == typeof(AsyncVoidMethodBuilder) &&
                    (method.Name is "AwaitOnCompleted" || method.Name is "AwaitUnsafeOnCompleted"))
                {
                    // The operation is executing the root of an async void method, so we need to inline.
                    break;
                }
                else if (method.Name is "MoveNext" &&
                    method.DeclaringType.Namespace != typeof(ControlledRuntime).Namespace &&
                    typeof(IAsyncStateMachine).IsAssignableFrom(method.DeclaringType))
                {
                    // The operation is executing the `MoveNext` of an asynchronous state machine.
                    result = true;
                    break;
                }
            }

            return result;
        }

        /// <summary>
        /// Checks that the executing task is controlled.
        /// </summary>
        private void AssertIsTaskControlled(TaskOperation callerOp, string opName)
        {
            if (callerOp == null)
            {
                this.Assert(false, "Uncontrolled task '{0}' invoked a {1} operation.",
                    Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>", opName);
            }
        }

        /// <summary>
        /// Reports the specified thrown exception.
        /// </summary>
        private void ReportThrownException(Exception exception)
        {
            if (!(exception is ExecutionCanceledException) && !(exception is TaskCanceledException) && !(exception is OperationCanceledException))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    $"Exception '{exception.GetType()}' was thrown in task '{Task.CurrentId}', " +
                    $"'{exception.Source}':\n" +
                    $"   {exception.Message}\n" +
                    $"The stack trace is:\n{exception.StackTrace}");
                this.Runtime.Logger.WriteLine(IO.LogSeverity.Warning, $"<ExceptionLog> {message}");
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, triggers a failure.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        private void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                this.Scheduler.NotifyAssertionFailure(string.Format(CultureInfo.InvariantCulture, s, args));
            }
        }
    }
}
