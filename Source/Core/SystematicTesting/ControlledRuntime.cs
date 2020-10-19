// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Mocks;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Actors.Timers.Mocks;
using Microsoft.Coyote.Coverage;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting.Strategies;
using CoyoteTasks = Microsoft.Coyote.Tasks;
using EventInfo = Microsoft.Coyote.Actors.EventInfo;
using Monitor = Microsoft.Coyote.Specifications.Monitor;

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Runtime for controlling asynchronous operations.
    /// </summary>
    internal sealed class ControlledRuntime : ActorRuntime
    {
        /// <summary>
        /// The currently executing runtime.
        /// </summary>
        internal static new ControlledRuntime Current => CoyoteRuntime.Current as ControlledRuntime;

        /// <summary>
        /// The asynchronous operation scheduler.
        /// </summary>
        internal readonly OperationScheduler Scheduler;

        /// <summary>
        /// Map from controlled tasks to their corresponding operations,
        /// if such an operation exists.
        /// </summary>
        private readonly ConcurrentDictionary<Task, TaskOperation> TaskMap;

        /// <summary>
        /// Data structure containing information regarding testing coverage.
        /// </summary>
        internal CoverageInfo CoverageInfo;

        /// <summary>
        /// Map that stores all unique names and their corresponding actor ids.
        /// </summary>
        internal readonly ConcurrentDictionary<string, ActorId> NameValueToActorId;

        /// <summary>
        /// The root task id.
        /// </summary>
        internal readonly int? RootTaskId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ControlledRuntime"/> class.
        /// </summary>
        internal ControlledRuntime(Configuration configuration, ISchedulingStrategy strategy,
            IRandomValueGenerator valueGenerator)
            : base(configuration, valueGenerator)
        {
            IncrementExecutionControlledUseCount();
            Interception.ControlledThread.ClearCache();

            this.RootTaskId = Task.CurrentId;
            this.TaskMap = new ConcurrentDictionary<Task, TaskOperation>();
            this.NameValueToActorId = new ConcurrentDictionary<string, ActorId>();

            this.CoverageInfo = new CoverageInfo();

            var scheduleTrace = new ScheduleTrace();
            if (configuration.IsLivenessCheckingEnabled)
            {
                strategy = new TemperatureCheckingStrategy(configuration, this.Monitors, strategy);
            }

            this.Scheduler = new OperationScheduler(this, strategy, scheduleTrace, this.Configuration);
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

            TaskOperation op = this.CreateTaskOperation();
            OperationExecutionOptions options = OperationContext.CreateOperationExecutionOptions(failException, isYield);
            var context = new OperationContext<Action, object>(op, action, predecessor, options, cancellationToken);
            var task = new Task(this.ExecuteOperation, context, cancellationToken);
            return this.ScheduleTaskOperation(op, task, context.ResultSource);
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
            TaskOperation op = this.CreateTaskOperation();
            var context = new AsyncOperationContext<Func<Task>, Task, Task>(op, function, task, predecessor,
                OperationExecutionOptions.None, cancellationToken);
            task = new Task<Task>(this.ExecuteOperation<Func<Task>, Task, Task>, context, cancellationToken);
            return this.ScheduleTaskOperation(op, task, context.ExecutorSource);
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
            TaskOperation op = this.CreateTaskOperation();
            var context = new AsyncOperationContext<Func<Task<TResult>>, Task<TResult>, TResult>(op, function, task, predecessor,
                OperationExecutionOptions.None, cancellationToken);
            task = new Task<TResult>(this.ExecuteOperation<Func<Task<TResult>>, Task<TResult>, TResult>, context, cancellationToken);
            return this.ScheduleTaskOperation(op, task, context.ExecutorSource);
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
            TaskOperation op = this.CreateTaskOperation();
            var context = new AsyncOperationContext<Func<CoyoteTasks.Task>, Task, Task>(op, function, task, predecessor,
                OperationExecutionOptions.None, cancellationToken);
            task = new Task<Task>(this.ExecuteOperation<Func<CoyoteTasks.Task>, Task, Task>, context, cancellationToken);
            return new CoyoteTasks.Task(this, this.ScheduleTaskOperation(op, task, context.ResultSource));
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
            TaskOperation op = this.CreateTaskOperation();
            var context = new AsyncOperationContext<Func<CoyoteTasks.Task<TResult>>, Task<TResult>, TResult>(op, function, task, predecessor,
                OperationExecutionOptions.None, cancellationToken);
            task = new Task<TResult>(this.ExecuteOperation<Func<CoyoteTasks.Task<TResult>>, Task<TResult>, TResult>, context, cancellationToken);
            return new CoyoteTasks.Task<TResult>(this, this.ScheduleTaskOperation(op, task, context.ResultSource));
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

            TaskOperation op = this.CreateTaskOperation();
            var context = new OperationContext<Func<TResult>, TResult>(op, function, predecessor,
                OperationExecutionOptions.None, cancellationToken);
            var task = new Task<TResult>(this.ExecuteOperation<Func<TResult>, TResult, TResult>, context, cancellationToken);
            return this.ScheduleTaskOperation(op, task, context.ResultSource);
        }

        /// <summary>
        /// Schedules the specified task operation for execution.
        /// </summary>
        private Task<TResult> ScheduleTaskOperation<TResult>(TaskOperation op, Task task, TaskCompletionSource<TResult> tcs)
        {
            IO.Debug.WriteLine("<CreateLog> Operation '{0}' was created to execute task '{1}'.", op.Name, task.Id);
            task.Start();
            this.Scheduler.WaitOperationStart(op);
            this.Scheduler.ScheduleNextOperation();
            this.TaskMap.TryAdd(tcs.Task, op);
            return tcs.Task;
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
                AssignAsyncControlFlowRuntime(this);

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
                    exception = UnwrapException(ex);
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
                AssignAsyncControlFlowRuntime(this);

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
                exception = UnwrapException(ex);
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
        internal Task UnwrapTask(Task<Task> task)
        {
            var unwrappedTask = task.AsyncState is TaskCompletionSource<Task> tcs ? tcs.Task : task.Unwrap();
            this.TaskMap.TryGetValue(task, out TaskOperation op);
            this.TaskMap.TryAdd(unwrappedTask, op);
            return unwrappedTask;
        }

        /// <summary>
        /// Unwraps the specified task.
        /// </summary>
        internal Task<TResult> UnwrapTask<TResult>(Task<Task<TResult>> task)
        {
            var unwrappedTask = task.AsyncState is TaskCompletionSource<TResult> tcs ? tcs.Task : task.Unwrap();
            this.TaskMap.TryGetValue(task, out TaskOperation op);
            this.TaskMap.TryAdd(unwrappedTask, op);
            return unwrappedTask;
        }

        /// <summary>
        /// Callback invoked when the task of a task completion source is accessed.
        /// </summary>
        internal void OnTaskCompletionSourceGetTask(Task task)
        {
            this.TaskMap.TryAdd(task, null);
        }

        /// <summary>
        /// Callback invoked when the <see cref="AsyncTaskMethodBuilder.SetException"/> is accessed.
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

        /// <inheritdoc/>
        public override ActorId CreateActorIdFromName(Type type, string name)
        {
            // It is important that all actor ids use the monotonically incrementing
            // value as the id during testing, and not the unique name.
            var id = new ActorId(type, name, this);
            return this.NameValueToActorId.GetOrAdd(name, id);
        }

        /// <inheritdoc/>
        public override ActorId CreateActor(Type type, Event initialEvent = null, EventGroup group = null) =>
            this.CreateActor(null, type, null, initialEvent, group);

        /// <inheritdoc/>
        public override ActorId CreateActor(Type type, string name, Event initialEvent = null, EventGroup group = null) =>
            this.CreateActor(null, type, name, initialEvent, group);

        /// <inheritdoc/>
        public override ActorId CreateActor(ActorId id, Type type, Event initialEvent = null, EventGroup group = null)
        {
            this.Assert(id != null, "Cannot create an actor using a null actor id.");
            return this.CreateActor(id, type, null, initialEvent, group);
        }

        /// <inheritdoc/>
        public override Task<ActorId> CreateActorAndExecuteAsync(Type type, Event e = null, EventGroup group = null) =>
            this.CreateActorAndExecuteAsync(null, type, null, e, group);

        /// <inheritdoc/>
        public override Task<ActorId> CreateActorAndExecuteAsync(Type type, string name, Event e = null, EventGroup group = null) =>
            this.CreateActorAndExecuteAsync(null, type, name, e, group);

        /// <inheritdoc/>
        public override Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, Event e = null, EventGroup group = null)
        {
            this.Assert(id != null, "Cannot create an actor using a null actor id.");
            return this.CreateActorAndExecuteAsync(id, type, null, e, group);
        }

        /// <inheritdoc/>
        public override void SendEvent(ActorId targetId, Event e, EventGroup group = null, SendOptions options = null)
        {
            var senderOp = this.Scheduler.GetExecutingOperation<ActorOperation>();
            this.SendEvent(targetId, e, senderOp?.Actor, group, options);
        }

        /// <inheritdoc/>
        public override Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event e, EventGroup group = null,
            SendOptions options = null)
        {
            var senderOp = this.Scheduler.GetExecutingOperation<ActorOperation>();
            return this.SendEventAndExecuteAsync(targetId, e, senderOp?.Actor, group, options);
        }

        /// <inheritdoc/>
        public override EventGroup GetCurrentEventGroup(ActorId currentActorId)
        {
            var callerOp = this.Scheduler.GetExecutingOperation<ActorOperation>();
            this.Assert(callerOp != null && currentActorId == callerOp.Actor.Id,
                "Trying to access the event group id of {0}, which is not the currently executing actor.",
                currentActorId);
            return callerOp.Actor.CurrentEventGroup;
        }

        /// <summary>
        /// Runs the specified test method.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void RunTest(Delegate testMethod, string testName)
        {
            testName = string.IsNullOrEmpty(testName) ? string.Empty : $" '{testName}'";
            this.Logger.WriteLine($"<TestLog> Running test{testName}.");
            this.Assert(testMethod != null, "Unable to execute a null test method.");
            this.Assert(Task.CurrentId != null, "The test must execute inside a controlled task.");

            TaskOperation op = this.CreateTaskOperation();
            Task task = new Task(() =>
            {
                try
                {
                    // Update the current asynchronous control flow with the current runtime instance,
                    // allowing future retrieval in the same asynchronous call stack.
                    AssignAsyncControlFlowRuntime(this);

                    this.Scheduler.StartOperation(op);

                    Task testMethodTask = null;
                    if (testMethod is Action<IActorRuntime> actionWithRuntime)
                    {
                        actionWithRuntime(this);
                    }
                    else if (testMethod is Action action)
                    {
                        action();
                    }
                    else if (testMethod is Func<IActorRuntime, Task> functionWithRuntime)
                    {
                        testMethodTask = functionWithRuntime(this);
                    }
                    else if (testMethod is Func<Task> function)
                    {
                        testMethodTask = function();
                    }
                    else if (testMethod is Func<IActorRuntime, CoyoteTasks.Task> functionWithRuntime2)
                    {
                        testMethodTask = functionWithRuntime2(this).UncontrolledTask;
                    }
                    else if (testMethod is Func<CoyoteTasks.Task> function2)
                    {
                        testMethodTask = function2().UncontrolledTask;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unsupported test delegate of type '{testMethod.GetType()}'.");
                    }

                    if (testMethodTask != null)
                    {
                        // The test method is asynchronous, so wait on the task to complete.
                        op.OnWaitTask(testMethodTask);
                        if (testMethodTask.Exception != null)
                        {
                            // The test method failed with an unhandled exception.
                            ExceptionDispatchInfo.Capture(testMethodTask.Exception).Throw();
                        }
                        else if (testMethodTask.IsCanceled)
                        {
                            throw new TaskCanceledException(testMethodTask);
                        }
                    }

                    IO.Debug.WriteLine("<ScheduleDebug> Completed operation {0} on task '{1}'.", op.Name, Task.CurrentId);
                    op.OnCompleted();

                    // Task has completed, schedule the next enabled operation, which terminates exploration.
                    this.Scheduler.ScheduleNextOperation();
                }
                catch (Exception ex)
                {
                    this.ProcessUnhandledExceptionInOperation(op, ex);
                }
            });

            task.Start();
            this.Scheduler.WaitOperationStart(op);
        }

        /// <summary>
        /// Creates a new task operation.
        /// </summary>
        internal TaskOperation CreateTaskOperation()
        {
            ulong operationId = this.GetNextOperationId();
            var op = new TaskOperation(operationId, $"Task({operationId})", this.Scheduler);
            this.Scheduler.RegisterOperation(op);
            return op;
        }

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and name, using the specified
        /// unbound actor id, and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled.
        /// </summary>
        internal ActorId CreateActor(ActorId id, Type type, string name, Event initialEvent = null, EventGroup group = null)
        {
            var creatorOp = this.Scheduler.GetExecutingOperation<ActorOperation>();
            return this.CreateActor(id, type, name, initialEvent, creatorOp?.Actor, group);
        }

        /// <inheritdoc/>
        internal override ActorId CreateActor(ActorId id, Type type, string name, Event initialEvent, Actor creator, EventGroup group)
        {
            this.AssertExpectedCallerActor(creator, "CreateActor");

            Actor actor = this.CreateActor(id, type, name, creator, group);
            this.RunActorEventHandler(actor, initialEvent, true, null);
            return actor.Id;
        }

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/> and name, using the specified
        /// unbound actor id, and passes the specified optional <see cref="Event"/>. This event
        /// can only be used to access its payload, and cannot be handled. The method returns only
        /// when the actor is initialized and the <see cref="Event"/> (if any) is handled.
        /// </summary>
        internal Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, string name, Event initialEvent = null,
            EventGroup group = null)
        {
            var creatorOp = this.Scheduler.GetExecutingOperation<ActorOperation>();
            return this.CreateActorAndExecuteAsync(id, type, name, initialEvent, creatorOp?.Actor, group);
        }

        /// <inheritdoc/>
        internal override async Task<ActorId> CreateActorAndExecuteAsync(ActorId id, Type type, string name,
            Event initialEvent, Actor creator, EventGroup group = null)
        {
            this.AssertExpectedCallerActor(creator, "CreateActorAndExecuteAsync");
            this.Assert(creator != null, "Only an actor can call 'CreateActorAndExecuteAsync': avoid calling " +
                "it directly from the test method; instead call it through a test driver actor.");

            Actor actor = this.CreateActor(id, type, name, creator, group);
            this.RunActorEventHandler(actor, initialEvent, true, creator);

            // Wait until the actor reaches quiescence.
            await creator.ReceiveEventAsync(typeof(QuiescentEvent), rev => (rev as QuiescentEvent).ActorId == actor.Id);
            return await Task.FromResult(actor.Id);
        }

        /// <summary>
        /// Creates a new actor of the specified <see cref="Type"/>.
        /// </summary>
        private Actor CreateActor(ActorId id, Type type, string name, Actor creator, EventGroup group)
        {
            this.Assert(type.IsSubclassOf(typeof(Actor)), "Type '{0}' is not an actor.", type.FullName);

            // Using ulong.MaxValue because a Create operation cannot specify
            // the id of its target, because the id does not exist yet.
            this.Scheduler.ScheduleNextOperation();
            ResetProgramCounter(creator);

            if (id is null)
            {
                id = new ActorId(type, name, this);
            }
            else
            {
                this.Assert(id.Runtime is null || id.Runtime == this, "Unbound actor id '{0}' was created by another runtime.", id.Value);
                this.Assert(id.Type == type.FullName, "Cannot bind actor id '{0}' of type '{1}' to an actor of type '{2}'.",
                    id.Value, id.Type, type.FullName);
                id.Bind(this);
            }

            // If a group was not provided, inherit the current event group from the creator (if any).
            if (group == null && creator != null)
            {
                group = creator.Manager.CurrentEventGroup;
            }

            Actor actor = ActorFactory.Create(type);
            IActorManager actorManager;
            if (actor is StateMachine stateMachine)
            {
                actorManager = new MockStateMachineManager(this, stateMachine, group);
            }
            else
            {
                actorManager = new MockActorManager(this, actor, group);
            }

            IEventQueue eventQueue = new MockEventQueue(actorManager, actor);
            actor.Configure(this, id, actorManager, eventQueue);
            actor.SetupEventHandlers();

            if (this.Configuration.ReportActivityCoverage)
            {
                this.ReportActivityCoverageOfActor(actor);
            }

            bool result = this.Scheduler.RegisterOperation(new ActorOperation(actor, this.Scheduler));
            this.Assert(result, "Actor id '{0}' is used by an existing or previously halted actor.", id.Value);
            if (actor is StateMachine)
            {
                this.LogWriter.LogCreateStateMachine(id, creator?.Id.Name, creator?.Id.Type);
            }
            else
            {
                this.LogWriter.LogCreateActor(id, creator?.Id.Name, creator?.Id.Type);
            }

            return actor;
        }

        /// <inheritdoc/>
        internal override void SendEvent(ActorId targetId, Event e, Actor sender, EventGroup group, SendOptions options)
        {
            if (e is null)
            {
                string message = sender != null ?
                    string.Format("{0} is sending a null event.", sender.Id.ToString()) :
                    "Cannot send a null event.";
                this.Assert(false, message);
            }

            if (sender != null)
            {
                this.Assert(targetId != null, "{0} is sending event {1} to a null actor.", sender.Id, e);
            }
            else
            {
                this.Assert(targetId != null, "Cannot send event {1} to a null actor.", e);
            }

            this.AssertExpectedCallerActor(sender, "SendEvent");

            EnqueueStatus enqueueStatus = this.EnqueueEvent(targetId, e, sender, group, options, out Actor target);
            if (enqueueStatus is EnqueueStatus.EventHandlerNotRunning)
            {
                this.RunActorEventHandler(target, null, false, null);
            }
        }

         /// <inheritdoc/>
        internal override async Task<bool> SendEventAndExecuteAsync(ActorId targetId, Event e, Actor sender,
            EventGroup group, SendOptions options)
        {
            this.Assert(sender is StateMachine, "Only an actor can call 'SendEventAndExecuteAsync': avoid " +
                "calling it directly from the test method; instead call it through a test driver actor.");
            this.Assert(e != null, "{0} is sending a null event.", sender.Id);
            this.Assert(targetId != null, "{0} is sending event {1} to a null actor.", sender.Id, e);
            this.AssertExpectedCallerActor(sender, "SendEventAndExecuteAsync");
            EnqueueStatus enqueueStatus = this.EnqueueEvent(targetId, e, sender, group, options, out Actor target);
            if (enqueueStatus is EnqueueStatus.EventHandlerNotRunning)
            {
                this.RunActorEventHandler(target, null, false, sender as StateMachine);
                // Wait until the actor reaches quiescence.
                await (sender as StateMachine).ReceiveEventAsync(typeof(QuiescentEvent), rev => (rev as QuiescentEvent).ActorId == targetId);
                return true;
            }

            // EnqueueStatus.EventHandlerNotRunning is not returned by EnqueueEvent
            // (even when the actor was previously inactive) when the event e requires
            // no action by the actor (i.e., it implicitly handles the event).
            return enqueueStatus is EnqueueStatus.Dropped || enqueueStatus is EnqueueStatus.NextEventUnavailable;
        }

        /// <summary>
        /// Enqueues an event to the actor with the specified id.
        /// </summary>
        private EnqueueStatus EnqueueEvent(ActorId targetId, Event e, Actor sender, EventGroup group,
            SendOptions options, out Actor target)
        {
            target = this.Scheduler.GetOperationWithId<ActorOperation>(targetId.Value)?.Actor;
            this.Assert(target != null,
                "Cannot send event '{0}' to actor id '{1}' that is not bound to an actor instance.",
                e.GetType().FullName, targetId.Value);

            this.Scheduler.ScheduleNextOperation();
            ResetProgramCounter(sender as StateMachine);

            // If no group is provided we default to passing along the group from the sender.
            if (group == null && sender != null)
            {
                group = sender.Manager.CurrentEventGroup;
            }

            if (target.IsHalted)
            {
                Guid groupId = group == null ? Guid.Empty : group.Id;
                this.LogWriter.LogSendEvent(targetId, sender?.Id.Name, sender?.Id.Type,
                    (sender as StateMachine)?.CurrentStateName ?? string.Empty, e, groupId, isTargetHalted: true);
                this.Assert(options is null || !options.MustHandle,
                    "A must-handle event '{0}' was sent to {1} which has halted.", e.GetType().FullName, targetId);
                this.TryHandleDroppedEvent(e, targetId);
                return EnqueueStatus.Dropped;
            }

            EnqueueStatus enqueueStatus = this.EnqueueEvent(target, e, sender, group, options);
            if (enqueueStatus == EnqueueStatus.Dropped)
            {
                this.TryHandleDroppedEvent(e, targetId);
            }

            return enqueueStatus;
        }

        /// <summary>
        /// Enqueues an event to the actor with the specified id.
        /// </summary>
        private EnqueueStatus EnqueueEvent(Actor actor, Event e, Actor sender, EventGroup group, SendOptions options)
        {
            EventOriginInfo originInfo;

            string stateName = null;
            if (sender is StateMachine senderStateMachine)
            {
                originInfo = new EventOriginInfo(sender.Id, senderStateMachine.GetType().FullName,
                    NameResolver.GetStateNameForLogging(senderStateMachine.CurrentState));
                stateName = senderStateMachine.CurrentStateName;
            }
            else if (sender is Actor senderActor)
            {
                originInfo = new EventOriginInfo(sender.Id, senderActor.GetType().FullName, string.Empty);
            }
            else
            {
                // Message comes from the environment.
                originInfo = new EventOriginInfo(null, "Env", "Env");
            }

            EventInfo eventInfo = new EventInfo(e, originInfo)
            {
                MustHandle = options?.MustHandle ?? false,
                Assert = options?.Assert ?? -1
            };

            Guid opId = group == null ? Guid.Empty : group.Id;
            this.LogWriter.LogSendEvent(actor.Id, sender?.Id.Name, sender?.Id.Type, stateName,
                e, opId, isTargetHalted: false);
            return actor.Enqueue(e, group, eventInfo);
        }

        /// <summary>
        /// Runs a new asynchronous event handler for the specified actor.
        /// This is a fire and forget invocation.
        /// </summary>
        /// <param name="actor">The actor that executes this event handler.</param>
        /// <param name="initialEvent">Optional event for initializing the actor.</param>
        /// <param name="isFresh">If true, then this is a new actor.</param>
        /// <param name="syncCaller">Caller actor that is blocked for quiscence.</param>
        private void RunActorEventHandler(Actor actor, Event initialEvent, bool isFresh, Actor syncCaller)
        {
            var op = this.Scheduler.GetOperationWithId<ActorOperation>(actor.Id.Value);
            Task task = new Task(async () =>
            {
                try
                {
                    // Update the current asynchronous control flow with this runtime instance,
                    // allowing future retrieval in the same asynchronous call stack.
                    AssignAsyncControlFlowRuntime(this);

                    this.Scheduler.StartOperation(op);

                    if (isFresh)
                    {
                        await actor.InitializeAsync(initialEvent);
                    }

                    await actor.RunEventHandlerAsync();
                    if (syncCaller != null)
                    {
                        this.EnqueueEvent(syncCaller, new QuiescentEvent(actor.Id), actor, actor.CurrentEventGroup, null);
                    }

                    if (!actor.IsHalted)
                    {
                        ResetProgramCounter(actor);
                    }

                    IO.Debug.WriteLine("<ScheduleDebug> Completed operation {0} on task '{1}'.", actor.Id, Task.CurrentId);
                    op.OnCompleted();

                    // The actor is inactive or halted, schedule the next enabled operation.
                    this.Scheduler.ScheduleNextOperation();
                }
                catch (Exception ex)
                {
                    this.ProcessUnhandledExceptionInOperation(op, ex);
                }
            });

            task.Start();
            this.Scheduler.WaitOperationStart(op);
        }

        /// <summary>
        /// Processes an unhandled exception in the specified asynchronous operation.
        /// </summary>
        private void ProcessUnhandledExceptionInOperation(AsyncOperation op, Exception ex)
        {
            string message = null;
            Exception exception = UnwrapException(ex);
            if (exception is ExecutionCanceledException || exception is TaskSchedulerException)
            {
                IO.Debug.WriteLine("<Exception> {0} was thrown from operation '{1}'.",
                    exception.GetType().Name, op.Name);
            }
            else if (exception is ObjectDisposedException)
            {
                IO.Debug.WriteLine("<Exception> {0} was thrown from operation '{1}' with reason '{2}'.",
                    exception.GetType().Name, op.Name, ex.Message);
            }
            else if (op is ActorOperation actorOp)
            {
                message = string.Format(CultureInfo.InvariantCulture,
                    $"Unhandled exception. {exception.GetType()} was thrown in actor {actorOp.Name}, " +
                    $"'{exception.Source}':\n" +
                    $"   {exception.Message}\n" +
                    $"The stack trace is:\n{exception.StackTrace}");
            }
            else
            {
                message = string.Format(CultureInfo.InvariantCulture, $"Unhandled exception. {exception}");
            }

            if (message != null)
            {
                // Report the unhandled exception.
                this.Scheduler.NotifyUnhandledException(exception, message);
            }
        }

        /// <summary>
        /// Unwraps the specified exception.
        /// </summary>
        private static Exception UnwrapException(Exception ex)
        {
            Exception exception = ex;
            while (exception is TargetInvocationException)
            {
                exception = exception.InnerException;
            }

            if (exception is AggregateException)
            {
                exception = exception.InnerException;
            }

            return exception;
        }

        /// <inheritdoc/>
        internal override IActorTimer CreateActorTimer(TimerInfo info, Actor owner)
        {
            var id = this.CreateActorId(typeof(MockStateMachineTimer));
            this.CreateActor(id, typeof(MockStateMachineTimer), new TimerSetupEvent(info, owner, this.Configuration.TimeoutDelay));
            return this.Scheduler.GetOperationWithId<ActorOperation>(id.Value).Actor as MockStateMachineTimer;
        }

        /// <inheritdoc/>
        internal override void TryCreateMonitor(Type type)
        {
            if (this.Monitors.Any(m => m.GetType() == type))
            {
                // Idempotence: only one monitor per type can exist.
                return;
            }

            this.Assert(type.IsSubclassOf(typeof(Monitor)), "Type '{0}' is not a subclass of Monitor.", type.FullName);

            Monitor monitor = Activator.CreateInstance(type) as Monitor;
            monitor.Initialize(this);
            monitor.InitializeStateInformation();

            this.LogWriter.LogCreateMonitor(type.FullName);

            if (this.Configuration.ReportActivityCoverage)
            {
                this.ReportActivityCoverageOfMonitor(monitor);
            }

            this.Monitors.Add(monitor);

            monitor.GotoStartState();
        }

        /// <inheritdoc/>
        internal override void Monitor(Type type, Event e, string senderName, string senderType, string senderStateName)
        {
            foreach (var monitor in this.Monitors)
            {
                if (monitor.GetType() == type)
                {
                    monitor.MonitorEvent(e, senderName, senderType, senderStateName);
                    break;
                }
            }
        }

        /// <inheritdoc/>
#if !DEBUG
        [DebuggerHidden]
#endif
        public override void Assert(bool predicate)
        {
            if (!predicate)
            {
                this.Scheduler.NotifyAssertionFailure("Detected an assertion failure.");
            }
        }

        /// <inheritdoc/>
#if !DEBUG
        [DebuggerHidden]
#endif
        public override void Assert(bool predicate, string s, object arg0)
        {
            if (!predicate)
            {
                var msg = string.Format(CultureInfo.InvariantCulture, s, arg0?.ToString());
                this.Scheduler.NotifyAssertionFailure(msg);
            }
        }

        /// <inheritdoc/>
#if !DEBUG
        [DebuggerHidden]
#endif
        public override void Assert(bool predicate, string s, object arg0, object arg1)
        {
            if (!predicate)
            {
                var msg = string.Format(CultureInfo.InvariantCulture, s, arg0?.ToString(), arg1?.ToString());
                this.Scheduler.NotifyAssertionFailure(msg);
            }
        }

        /// <inheritdoc/>
#if !DEBUG
        [DebuggerHidden]
#endif
        public override void Assert(bool predicate, string s, object arg0, object arg1, object arg2)
        {
            if (!predicate)
            {
                var msg = string.Format(CultureInfo.InvariantCulture, s, arg0?.ToString(), arg1?.ToString(), arg2?.ToString());
                this.Scheduler.NotifyAssertionFailure(msg);
            }
        }

        /// <inheritdoc/>
#if !DEBUG
        [DebuggerHidden]
#endif
        public override void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                var msg = string.Format(CultureInfo.InvariantCulture, s, args);
                this.Scheduler.NotifyAssertionFailure(msg);
            }
        }

#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal void AssertIsAwaitedTaskControlled(Task task)
        {
            if (!task.IsCompleted && !this.TaskMap.ContainsKey(task) &&
                !this.Configuration.IsPartiallyControlledTestingEnabled)
            {
                this.Assert(false, $"Awaiting uncontrolled task with id '{task.Id}' is not allowed: " +
                    "either mock the method that created the task, or rewrite the method's assembly.");
            }
        }

#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal void AssertIsReturnedTaskControlled(Task task, string methodName)
        {
            if (!task.IsCompleted && !this.TaskMap.ContainsKey(task) &&
                !this.Configuration.IsPartiallyControlledTestingEnabled)
            {
                this.Assert(false, $"Method '{methodName}' returned an uncontrolled task with id '{task.Id}', " +
                    "which is not allowed: either mock the method, or rewrite the method's assembly.");
            }
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
        /// Asserts that the actor calling an actor method is also
        /// the actor that is currently executing.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        private void AssertExpectedCallerActor(Actor caller, string calledAPI)
        {
            if (caller is null)
            {
                return;
            }

            var op = this.Scheduler.GetExecutingOperation<ActorOperation>();
            if (op is null)
            {
                return;
            }

            this.Assert(op.Actor.Equals(caller), "{0} invoked {1} on behalf of {2}.",
                op.Actor.Id, calledAPI, caller.Id);
        }

        /// <summary>
        /// Checks that no monitor is in a hot state upon program termination.
        /// If the program is still running, then this method returns without
        /// performing a check.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void CheckNoMonitorInHotStateAtTermination()
        {
            if (!this.Scheduler.HasFullyExploredSchedule)
            {
                return;
            }

            foreach (var monitor in this.Monitors)
            {
                if (monitor.IsInHotState(out string stateName))
                {
                    string message = string.Format(CultureInfo.InvariantCulture,
                        "{0} detected liveness bug in hot state '{1}' at the end of program execution.",
                        monitor.GetType().FullName, stateName);
                    this.Scheduler.NotifyAssertionFailure(message, killTasks: false, cancelExecution: false);
                }
            }
        }

        /// <inheritdoc/>
        internal override bool GetNondeterministicBooleanChoice(int maxValue, string callerName, string callerType)
        {
            var caller = this.Scheduler.GetExecutingOperation<ActorOperation>()?.Actor;
            if (caller is StateMachine callerStateMachine)
            {
                (callerStateMachine.Manager as MockStateMachineManager).ProgramCounter++;
            }
            else if (caller is Actor callerActor)
            {
                (callerActor.Manager as MockActorManager).ProgramCounter++;
            }

            var choice = this.Scheduler.GetNextNondeterministicBooleanChoice(maxValue);
            this.LogWriter.LogRandom(choice, callerName ?? caller?.Id.Name, callerType ?? caller?.Id.Type);
            return choice;
        }

        /// <inheritdoc/>
        internal override int GetNondeterministicIntegerChoice(int maxValue, string callerName, string callerType)
        {
            var caller = this.Scheduler.GetExecutingOperation<ActorOperation>()?.Actor;
            if (caller is StateMachine callerStateMachine)
            {
                (callerStateMachine.Manager as MockStateMachineManager).ProgramCounter++;
            }
            else if (caller is Actor callerActor)
            {
                (callerActor.Manager as MockActorManager).ProgramCounter++;
            }

            var choice = this.Scheduler.GetNextNondeterministicIntegerChoice(maxValue);
            this.LogWriter.LogRandom(choice, callerName ?? caller?.Id.Name, callerType ?? caller?.Id.Type);
            return choice;
        }

        /// <summary>
        /// Gets the <see cref="AsyncOperation"/> that is executing on the current
        /// synchronization context, or null if no such operation is executing.
        /// </summary>
        internal TAsyncOperation GetExecutingOperation<TAsyncOperation>()
            where TAsyncOperation : AsyncOperation =>
            this.Scheduler.GetExecutingOperation<TAsyncOperation>();

        /// <summary>
        /// Schedules the next controlled asynchronous operation. This method
        /// is only used during testing.
        /// </summary>
        internal void ScheduleNextOperation()
        {
            var callerOp = this.Scheduler.GetExecutingOperation<AsyncOperation>();
            if (callerOp != null)
            {
                this.Scheduler.ScheduleNextOperation();
            }
        }

        /// <inheritdoc/>
        internal override void NotifyInvokedAction(Actor actor, MethodInfo action, string handlingStateName,
            string currentStateName, Event receivedEvent)
        {
            this.LogWriter.LogExecuteAction(actor.Id, handlingStateName, currentStateName, action.Name);
        }

        /// <inheritdoc/>
        internal override void NotifyDequeuedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            var op = this.Scheduler.GetOperationWithId<ActorOperation>(actor.Id.Value);

            // Skip `ReceiveEventAsync` if the last operation exited the previous event handler,
            // to avoid scheduling duplicate `ReceiveEventAsync` operations.
            if (op.SkipNextReceiveSchedulingPoint)
            {
                op.SkipNextReceiveSchedulingPoint = false;
            }
            else
            {
                this.Scheduler.ScheduleNextOperation();
                ResetProgramCounter(actor);
            }

            string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
            this.LogWriter.LogDequeueEvent(actor.Id, stateName, e);
        }

        /// <inheritdoc/>
        internal override void NotifyDefaultEventDequeued(Actor actor)
        {
            this.Scheduler.ScheduleNextOperation();
            ResetProgramCounter(actor);
        }

        /// <inheritdoc/>
        internal override void NotifyDefaultEventHandlerCheck(Actor actor)
        {
            this.Scheduler.ScheduleNextOperation();
        }

        /// <inheritdoc/>
        internal override void NotifyRaisedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
            this.LogWriter.LogRaiseEvent(actor.Id, stateName, e);
        }

        /// <inheritdoc/>
        internal override void NotifyHandleRaisedEvent(Actor actor, Event e)
        {
            string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
            this.LogWriter.LogHandleRaisedEvent(actor.Id, stateName, e);
        }

        /// <inheritdoc/>
        internal override void NotifyReceiveCalled(Actor actor)
        {
            this.AssertExpectedCallerActor(actor, "ReceiveEventAsync");
        }

        /// <inheritdoc/>
        internal override void NotifyReceivedEvent(Actor actor, Event e, EventInfo eventInfo)
        {
            string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
            this.LogWriter.LogReceiveEvent(actor.Id, stateName, e, wasBlocked: true);
            var op = this.Scheduler.GetOperationWithId<ActorOperation>(actor.Id.Value);
            op.OnReceivedEvent();
        }

        /// <inheritdoc/>
        internal override void NotifyReceivedEventWithoutWaiting(Actor actor, Event e, EventInfo eventInfo)
        {
            string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
            this.LogWriter.LogReceiveEvent(actor.Id, stateName, e, wasBlocked: false);
            this.Scheduler.ScheduleNextOperation();
            ResetProgramCounter(actor);
        }

        /// <inheritdoc/>
        internal override void NotifyWaitTask(Actor actor, Task task)
        {
            this.Assert(task != null, "{0} is waiting for a null task to complete.", actor.Id);

            bool finished = task.IsCompleted || task.IsCanceled || task.IsFaulted;
            if (!finished)
            {
                this.Assert(finished,
                    "Controlled task '{0}' is trying to wait for an uncontrolled task or awaiter to complete. Please " +
                    "make sure to avoid using concurrency APIs (e.g. 'Task.Run', 'Task.Delay' or 'Task.Yield' from " +
                    "the 'System.Threading.Tasks' namespace) inside actor handlers. If you are using external libraries " +
                    "that are executing concurrently, you will need to mock them during testing.",
                    Task.CurrentId);
            }
        }

        /// <inheritdoc/>
        internal override void NotifyWaitEvent(Actor actor, IEnumerable<Type> eventTypes)
        {
            string stateName = actor is StateMachine stateMachine ? stateMachine.CurrentStateName : null;
            var op = this.Scheduler.GetOperationWithId<ActorOperation>(actor.Id.Value);
            op.OnWaitEvent(eventTypes);

            var eventWaitTypesArray = eventTypes.ToArray();
            if (eventWaitTypesArray.Length == 1)
            {
                this.LogWriter.LogWaitEvent(actor.Id, stateName, eventWaitTypesArray[0]);
            }
            else
            {
                this.LogWriter.LogWaitEvent(actor.Id, stateName, eventWaitTypesArray);
            }

            this.Scheduler.ScheduleNextOperation();
            ResetProgramCounter(actor);
        }

        /// <inheritdoc/>
        internal override void NotifyEnteredState(StateMachine stateMachine)
        {
            string stateName = stateMachine.CurrentStateName;
            this.LogWriter.LogStateTransition(stateMachine.Id, stateName, isEntry: true);
        }

        /// <inheritdoc/>
        internal override void NotifyExitedState(StateMachine stateMachine)
        {
            this.LogWriter.LogStateTransition(stateMachine.Id, stateMachine.CurrentStateName, isEntry: false);
        }

        /// <inheritdoc/>
        internal override void NotifyPopState(StateMachine stateMachine)
        {
            this.AssertExpectedCallerActor(stateMachine, "Pop");
            this.LogWriter.LogPopState(stateMachine.Id, string.Empty, stateMachine.CurrentStateName);
        }

        /// <inheritdoc/>
        internal override void NotifyInvokedOnEntryAction(StateMachine stateMachine, MethodInfo action, Event receivedEvent)
        {
            string stateName = stateMachine.CurrentStateName;
            this.LogWriter.LogExecuteAction(stateMachine.Id, stateName, stateName, action.Name);
        }

        /// <inheritdoc/>
        internal override void NotifyInvokedOnExitAction(StateMachine stateMachine, MethodInfo action, Event receivedEvent)
        {
            string stateName = stateMachine.CurrentStateName;
            this.LogWriter.LogExecuteAction(stateMachine.Id, stateName, stateName, action.Name);
        }

        /// <inheritdoc/>
        internal override void NotifyEnteredState(Monitor monitor)
        {
            string monitorState = monitor.CurrentStateName;
            this.LogWriter.LogMonitorStateTransition(monitor.GetType().FullName, monitorState, true, monitor.GetHotState());
        }

        /// <inheritdoc/>
        internal override void NotifyExitedState(Monitor monitor)
        {
            this.LogWriter.LogMonitorStateTransition(monitor.GetType().FullName,
                monitor.CurrentStateName, false, monitor.GetHotState());
        }

        /// <inheritdoc/>
        internal override void NotifyInvokedAction(Monitor monitor, MethodInfo action, string stateName, Event receivedEvent)
        {
            this.LogWriter.LogMonitorExecuteAction(monitor.GetType().FullName, stateName, action.Name);
        }

        /// <inheritdoc/>
        internal override void NotifyRaisedEvent(Monitor monitor, Event e)
        {
            string monitorState = monitor.CurrentStateName;
            this.LogWriter.LogMonitorRaiseEvent(monitor.GetType().FullName, monitorState, e);
        }

        /// <summary>
        /// Get the coverage graph information (if any). This information is only available
        /// when <see cref="Configuration.ReportActivityCoverage"/> is enabled.
        /// </summary>
        /// <returns>A new CoverageInfo object.</returns>
        public CoverageInfo GetCoverageInfo()
        {
            var result = this.CoverageInfo;
            if (result != null)
            {
                var builder = this.LogWriter.GetLogsOfType<ActorRuntimeLogGraphBuilder>().FirstOrDefault();
                if (builder != null)
                {
                    result.CoverageGraph = builder.SnapshotGraph(this.Configuration.IsDgmlBugGraph);
                }

                var eventCoverage = this.LogWriter.GetLogsOfType<ActorRuntimeLogEventCoverage>().FirstOrDefault();
                if (eventCoverage != null)
                {
                    result.EventInfo = eventCoverage.EventCoverage;
                }
            }

            return result;
        }

        /// <summary>
        /// Reports actors that are to be covered in coverage report.
        /// </summary>
        private void ReportActivityCoverageOfActor(Actor actor)
        {
            var name = actor.GetType().FullName;
            if (this.CoverageInfo.IsMachineDeclared(name))
            {
                return;
            }

            if (actor is StateMachine stateMachine)
            {
                // Fetch states.
                var states = stateMachine.GetAllStates();
                foreach (var state in states)
                {
                    this.CoverageInfo.DeclareMachineState(name, state);
                }

                // Fetch registered events.
                var pairs = stateMachine.GetAllStateEventPairs();
                foreach (var tup in pairs)
                {
                    this.CoverageInfo.DeclareStateEvent(name, tup.Item1, tup.Item2);
                }
            }
            else
            {
                var fakeStateName = actor.GetType().Name;
                this.CoverageInfo.DeclareMachineState(name, fakeStateName);

                foreach (var eventId in actor.GetAllRegisteredEvents())
                {
                    this.CoverageInfo.DeclareStateEvent(name, fakeStateName, eventId);
                }
            }
        }

        /// <summary>
        /// Reports coverage for the specified monitor.
        /// </summary>
        private void ReportActivityCoverageOfMonitor(Monitor monitor)
        {
            var monitorName = monitor.GetType().FullName;
            if (this.CoverageInfo.IsMachineDeclared(monitorName))
            {
                return;
            }

            // Fetch states.
            var states = monitor.GetAllStates();

            foreach (var state in states)
            {
                this.CoverageInfo.DeclareMachineState(monitorName, state);
            }

            // Fetch registered events.
            var pairs = monitor.GetAllStateEventPairs();

            foreach (var tup in pairs)
            {
                this.CoverageInfo.DeclareStateEvent(monitorName, tup.Item1, tup.Item2);
            }
        }

        /// <summary>
        /// Resets the program counter of the specified actor.
        /// </summary>
        private static void ResetProgramCounter(Actor actor)
        {
            if (actor is StateMachine stateMachine)
            {
                (stateMachine.Manager as MockStateMachineManager).ProgramCounter = 0;
            }
            else if (actor != null)
            {
                (actor.Manager as MockActorManager).ProgramCounter = 0;
            }
        }

        /// <summary>
        /// Returns the current hashed state of the execution using the specified
        /// level of abstraction. The hash is updated in each execution step.
        /// </summary>
        [DebuggerStepThrough]
        internal int GetProgramState()
        {
            unchecked
            {
                int hash = 19;

                foreach (var operation in this.Scheduler.GetRegisteredOperations().OrderBy(op => op.Id))
                {
                    if (operation is ActorOperation actorOperation)
                    {
                        hash *= 31 + actorOperation.Actor.GetHashedState();
                    }
                }

                foreach (var monitor in this.Monitors)
                {
                    hash = (hash * 397) + monitor.GetHashedState();
                }

                return hash;
            }
        }

        /// <summary>
        /// Reports the specified thrown exception.
        /// </summary>
        private void ReportThrownException(Exception exception)
        {
            if (!(exception is ExecutionCanceledException || exception is TaskCanceledException ||
                exception is OperationCanceledException))
            {
                string message = string.Format(CultureInfo.InvariantCulture,
                    $"Exception '{exception.GetType()}' was thrown in task '{Task.CurrentId}', " +
                    $"'{exception.Source}':\n" +
                    $"   {exception.Message}\n" +
                    $"The stack trace is:\n{exception.StackTrace}");
                this.Logger.WriteLine(IO.LogSeverity.Warning, $"<ExceptionLog> {message}");
            }
        }

        /// <inheritdoc/>
        [DebuggerStepThrough]
        internal override void WrapAndThrowException(Exception exception, string s, params object[] args)
        {
            string msg = string.Format(CultureInfo.InvariantCulture, s, args);
            string message = string.Format(CultureInfo.InvariantCulture,
                "Exception '{0}' was thrown in {1}: {2}\n" +
                "from location '{3}':\n" +
                "The stack trace is:\n{4}",
                exception.GetType(), msg, exception.Message, exception.Source, exception.StackTrace);

            this.Scheduler.NotifyAssertionFailure(message);
        }

        /// <summary>
        /// Waits until all actors have finished execution.
        /// </summary>
        [DebuggerStepThrough]
        internal async Task WaitAsync()
        {
            await this.Scheduler.WaitAsync();
            this.IsRunning = false;
        }

        /// <inheritdoc/>
        protected internal override void RaiseOnFailureEvent(Exception exception)
        {
            if (exception is ExecutionCanceledException ||
                (exception is ActionExceptionFilterException ae && ae.InnerException is ExecutionCanceledException))
            {
                // Internal exception used during testing.
                return;
            }

            base.RaiseOnFailureEvent(exception);
        }

        /// <inheritdoc/>
        [DebuggerStepThrough]
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Monitors.Clear();

                // Note: this makes it possible to run a Controlled unit test followed by a production
                // unit test, whereas before that would throw "Uncontrolled Task" exceptions.
                // This does not solve mixing unit test type in parallel.
                DecrementExecutionControlledUseCount();
            }

            base.Dispose(disposing);
        }
    }
}
