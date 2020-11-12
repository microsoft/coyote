// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.Coyote.SystematicTesting.Strategies;
using CoyoteTasks = Microsoft.Coyote.Tasks;
using Monitor = Microsoft.Coyote.Specifications.Monitor;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Runtime for executing and controlling asynchronous operations.
    /// </summary>
    internal sealed class CoyoteRuntime : IDisposable
    {
        /// <summary>
        /// Provides access to the runtime associated with each asynchronous control flow.
        /// </summary>
        /// <remarks>
        /// In testing mode, each testing iteration uses a unique runtime instance. To safely
        /// retrieve it from static methods, we store it in each asynchronous control flow.
        /// </remarks>
        private static readonly AsyncLocal<CoyoteRuntime> AsyncLocalInstance = new AsyncLocal<CoyoteRuntime>();

        /// <summary>
        /// The currently executing runtime.
        /// </summary>
        internal static CoyoteRuntime Current
        {
            get
            {
                CoyoteRuntime runtime = AsyncLocalInstance.Value;
                if (runtime is null)
                {
                    if (IsExecutionControlled)
                    {
                        OperationScheduler.ThrowUncontrolledTaskException();
                    }

                    runtime = RuntimeFactory.InstalledRuntime;
                }

                return runtime;
            }
        }

        /// <summary>
        /// If true, the program execution is controlled by the runtime to
        /// explore interleavings and sources of nondeterminism, else false.
        /// </summary>
        internal static bool IsExecutionControlled => ExecutionControlledUseCount > 0;

        /// <summary>
        /// Count of controlled execution runtimes that have been used in this process.
        /// </summary>
        private static int ExecutionControlledUseCount;

        /// <summary>
        /// The configuration used by the runtime.
        /// </summary>
        internal readonly Configuration Configuration;

        /// <summary>
        /// The asynchronous operation scheduler.
        /// </summary>
        internal readonly OperationScheduler Scheduler;

        /// <summary>
        /// Responsible for checking specifications.
        /// </summary>
        private readonly SpecificationEngine SpecificationEngine;

        /// <summary>
        /// The default actor execution context.
        /// </summary>
        internal readonly ActorExecutionContext DefaultActorExecutionContext;

        /// <summary>
        /// Responsible for generating random values.
        /// </summary>
        private readonly IRandomValueGenerator ValueGenerator;

        /// <summary>
        /// Map from controlled tasks to their corresponding operations,
        /// if such an operation exists.
        /// </summary>
        private readonly ConcurrentDictionary<Task, TaskOperation> TaskMap;

        /// <summary>
        /// Monotonically increasing operation id counter.
        /// </summary>
        private long OperationIdCounter;

        /// <summary>
        /// Records if the runtime is running.
        /// </summary>
        internal bool IsRunning => this.Scheduler.IsProgramExecuting;

        /// <summary>
        /// If true, the execution is controlled, else false.
        /// </summary>
        internal bool IsControlled { get; private set; }

        /// <summary>
        /// The root task id.
        /// </summary>
        internal readonly int? RootTaskId;

        /// <summary>
        /// Responsible for writing to all registered <see cref="IActorRuntimeLog"/> objects.
        /// </summary>
        internal LogWriter LogWriter { get; private set; }

        /// <summary>
        /// Used to log text messages. Use <see cref="ICoyoteRuntime.SetLogger"/>
        /// to replace the logger with a custom one.
        /// </summary>
        internal ILogger Logger
        {
            get { return this.LogWriter.Logger; }
            set { using var v = this.LogWriter.SetLogger(value); }
        }

        /// <summary>
        /// Callback that is fired when an exception is thrown that includes failed assertions.
        /// </summary>
        internal event OnFailureHandler OnFailure;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoyoteRuntime"/> class.
        /// </summary>
        internal CoyoteRuntime(Configuration configuration, IRandomValueGenerator valueGenerator)
            : this(configuration, null, valueGenerator)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoyoteRuntime"/> class.
        /// </summary>
        internal CoyoteRuntime(Configuration configuration, ISchedulingStrategy strategy,
            IRandomValueGenerator valueGenerator)
        {
            this.Configuration = configuration;

            this.IsControlled = strategy != null;
            if (this.IsControlled)
            {
                Interlocked.Increment(ref ExecutionControlledUseCount);
            }

            this.OperationIdCounter = 0;
            this.RootTaskId = Task.CurrentId;
            this.TaskMap = new ConcurrentDictionary<Task, TaskOperation>();

            this.LogWriter = new LogWriter(configuration);

            var monitors = new List<Monitor>();
            if (configuration.IsLivenessCheckingEnabled)
            {
                strategy = new TemperatureCheckingStrategy(configuration, monitors, strategy);
            }

            var scheduleTrace = new ScheduleTrace();
            this.Scheduler = new OperationScheduler(this, strategy, scheduleTrace, this.Configuration);
            this.SpecificationEngine = new SpecificationEngine(this.Configuration, this.Scheduler, monitors);
            this.ValueGenerator = valueGenerator;

            this.DefaultActorExecutionContext = this.IsControlled ?
                new ActorExecutionContext.Mock(this.Configuration, this, this.Scheduler,
                this.SpecificationEngine, this.ValueGenerator, this.LogWriter) :
                new ActorExecutionContext(this.Configuration, this, this.Scheduler,
                this.SpecificationEngine, this.ValueGenerator, this.LogWriter);

            SystematicTesting.Interception.ControlledThread.ClearCache();
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
                        actionWithRuntime(this.DefaultActorExecutionContext);
                    }
                    else if (testMethod is Action action)
                    {
                        action();
                    }
                    else if (testMethod is Func<IActorRuntime, Task> functionWithRuntime)
                    {
                        testMethodTask = functionWithRuntime(this.DefaultActorExecutionContext);
                    }
                    else if (testMethod is Func<Task> function)
                    {
                        testMethodTask = function();
                    }
                    else if (testMethod is Func<IActorRuntime, CoyoteTasks.Task> functionWithRuntime2)
                    {
                        testMethodTask = functionWithRuntime2(this.DefaultActorExecutionContext).UncontrolledTask;
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
                        // If the test method is asynchronous, then wait until it completes.
                        op.TryBlockUntilTaskCompletes(testMethodTask);
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
        internal CoyoteTasks.Task ScheduleAsyncFunction(Func<CoyoteTasks.Task> function, Task predecessor, CancellationToken cancellationToken)
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
        internal CoyoteTasks.Task<TResult> ScheduleAsyncFunction<TResult>(Func<CoyoteTasks.Task<TResult>> function, Task predecessor,
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
                    // If the predecessor task is asynchronous, then wait until it completes.
                    ct.ThrowIfCancellationRequested();
                    op.TryBlockUntilTaskCompletes(context.Predecessor);
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
                SetTaskCompletionSource(context.ResultSource, null, exception, default);
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
                    SetTaskCompletionSource(asyncContext.ExecutorSource, asyncContext.Executor, null, ct);
                }

                if (context.Predecessor != null)
                {
                    // If the predecessor task is asynchronous, then wait until it completes.
                    ct.ThrowIfCancellationRequested();
                    op.TryBlockUntilTaskCompletes(context.Predecessor);
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
                    // If the work task is asynchronous, then wait until it completes.
                    op.TryBlockUntilTaskCompletes(executor);
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
                SetTaskCompletionSource(context.ResultSource, result, exception, default);
                this.Scheduler.ScheduleNextOperation();
            }

            return result;
        }

        /// <summary>
        /// Sets the specified task completion source with a result, cancelation or exception.
        /// </summary>
        private static void SetTaskCompletionSource<TResult>(TaskCompletionSource<TResult> tcs, TResult result,
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
            if (delay.TotalMilliseconds is 0)
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
                var callerOp = this.Scheduler?.GetExecutingOperation<TaskOperation>();
                if (callerOp is null)
                {
                    OperationScheduler.ThrowUncontrolledTaskException();
                }

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
                callerOp.BlockUntilTasksComplete(tasks, waitAll: true);

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
                callerOp.BlockUntilTasksComplete(tasks, waitAll: true);

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
                callerOp.BlockUntilTasksComplete(tasks, waitAll: true);

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
                callerOp.BlockUntilTasksComplete(tasks, waitAll: true);

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
                callerOp.BlockUntilTasksComplete(tasks, waitAll: false);

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

            return this.ScheduleAsyncFunction(() =>
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                callerOp.BlockUntilTasksComplete(tasks, waitAll: false);

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
                callerOp.BlockUntilTasksComplete(tasks, waitAll: false);

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

            return this.ScheduleAsyncFunction(() =>
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                callerOp.BlockUntilTasksComplete(tasks, waitAll: false);

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
            callerOp.BlockUntilTasksComplete(tasks, waitAll: true);

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
            callerOp.BlockUntilTasksComplete(tasks, waitAll: true);

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
            callerOp.BlockUntilTasksComplete(tasks, waitAll: false);

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
            callerOp.BlockUntilTasksComplete(tasks, waitAll: false);

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
            this.AssertIsAwaitedTaskControlled(task);

            // TODO: support timeouts and cancellation tokens.
            if (!task.IsCompleted)
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                callerOp.BlockUntilTaskCompletes(task);
            }

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
            this.AssertIsAwaitedTaskControlled(task);

            // TODO: support timeouts and cancellation tokens.
            if (!task.IsCompleted)
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                callerOp.BlockUntilTaskCompletes(task);
            }

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
        internal void CheckExecutingOperationIsControlled() =>
            this.Scheduler.GetExecutingOperation<AsyncOperation>();

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
            if (!task.IsCompleted)
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                callerOp.BlockUntilTaskCompletes(task);
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
                    method.DeclaringType.Namespace != typeof(OperationScheduler).Namespace &&
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

        /// <summary>
        /// Registers a new specification monitor of the specified <see cref="Type"/>.
        /// </summary>
        internal void RegisterMonitor<T>()
            where T : Monitor => this.DefaultActorExecutionContext.RegisterMonitor<T>();

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        internal void Monitor<T>(Event e)
            where T : Monitor => this.DefaultActorExecutionContext.Monitor<T>(e);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        internal void Assert(bool predicate) => this.SpecificationEngine.Assert(predicate);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        internal void Assert(bool predicate, string s, object arg0) => this.SpecificationEngine.Assert(predicate, s, arg0);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        internal void Assert(bool predicate, string s, object arg0, object arg1) =>
            this.SpecificationEngine.Assert(predicate, s, arg0, arg1);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        internal void Assert(bool predicate, string s, object arg0, object arg1, object arg2) =>
            this.SpecificationEngine.Assert(predicate, s, arg0, arg1, arg2);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        internal void Assert(bool predicate, string s, params object[] args) => this.SpecificationEngine.Assert(predicate, s, args);

        /// <summary>
        /// Waits until the liveness property is satisfied.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal Task AssertIsLivenessPropertySatisfied(Func<Task<bool>> predicate, Func<int> getHashCode,
            TimeSpan delay, CancellationToken cancellationToken = default) =>
            this.SpecificationEngine.WaitUntilLivenessPropertyIsSatisfied(predicate, getHashCode, delay, cancellationToken);

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
        /// Asserts that no monitor is in a hot state at test termination.
        /// </summary>
        /// <remarks>
        /// If the test is still running, then this method returns without performing a check.
        /// </remarks>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void AssertNoMonitorInHotStateAtTermination() =>
            this.SpecificationEngine.AssertNoMonitorInHotStateAtTermination();

        /// <summary>
        /// Returns a controlled nondeterministic boolean choice.
        /// </summary>
        internal bool GetNondeterministicBooleanChoice(int maxValue, string callerName, string callerType) =>
            this.DefaultActorExecutionContext.GetNondeterministicBooleanChoice(maxValue, callerName, callerType);

        /// <summary>
        /// Returns a controlled nondeterministic integer choice.
        /// </summary>
        internal int GetNondeterministicIntegerChoice(int maxValue, string callerName, string callerType) =>
            this.DefaultActorExecutionContext.GetNondeterministicIntegerChoice(maxValue, callerName, callerType);

        /// <summary>
        /// Returns the next available unique operation id.
        /// </summary>
        /// <returns>Value representing the next available unique operation id.</returns>
        internal ulong GetNextOperationId() =>
            // Atomically increments and safely wraps the value into an unsigned long.
            (ulong)Interlocked.Increment(ref this.OperationIdCounter) - 1;

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

        /// <summary>
        /// Callback that is invoked on each scheduling step.
        /// </summary>
        internal void OnNextSchedulingStep() => this.SpecificationEngine.OnNextSchedulingStep();

        /// <summary>
        /// Returns the current hashed state of the execution.
        /// </summary>
        /// <remarks>
        /// The hash is updated in each execution step.
        /// </remarks>
        [DebuggerStepThrough]
        internal int GetHashedProgramState()
        {
            unchecked
            {
                int hash = 19;
                hash = (hash * 397) + this.DefaultActorExecutionContext.GetHashedActorState();
                hash = (hash * 397) + this.SpecificationEngine.GetHashedMonitorState();
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
                this.Logger.WriteLine(LogSeverity.Warning, $"<ExceptionLog> {message}");
            }
        }

        /// <summary>
        /// Waits until all actors have finished execution.
        /// </summary>
        [DebuggerStepThrough]
        internal Task WaitAsync() => this.Scheduler.WaitAsync();

        /// <summary>
        /// Raises the <see cref="OnFailure"/> event with the specified <see cref="Exception"/>.
        /// </summary>
        internal void RaiseOnFailureEvent(Exception exception)
        {
            if (this.IsControlled &&
                (exception is ExecutionCanceledException ||
                (exception is ActionExceptionFilterException ae && ae.InnerException is ExecutionCanceledException)))
            {
                // Internal exception used during testing.
                return;
            }

            if (this.Configuration.AttachDebugger)
            {
                Debugger.Break();
                this.Configuration.AttachDebugger = false;
            }

            this.OnFailure?.Invoke(exception);
        }

        /// <summary>
        /// Assigns the specified runtime as the default for the current asynchronous control flow.
        /// </summary>
        internal static void AssignAsyncControlFlowRuntime(CoyoteRuntime runtime) => AsyncLocalInstance.Value = runtime;

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.OperationIdCounter = 0;
                this.DefaultActorExecutionContext.Dispose();
                this.SpecificationEngine.Dispose();

                if (this.IsControlled)
                {
                    // Note: this makes it possible to run a Controlled unit test followed by a production
                    // unit test, whereas before that would throw "Uncontrolled Task" exceptions.
                    // This does not solve mixing unit test type in parallel.
                    Interlocked.Decrement(ref ExecutionControlledUseCount);
                }

                AssignAsyncControlFlowRuntime(null);
            }
        }

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
