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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Testing;
using SyncMonitor = System.Threading.Monitor;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Runtime for controlling, scheduling and executing asynchronous operations.
    /// </summary>
    /// <remarks>
    /// Invoking scheduling methods is thread-safe.
    /// </remarks>
    internal sealed class CoyoteRuntime : IDisposable
    {
        /// <summary>
        /// Provides access to the runtime associated with each controlled thread.
        /// </summary>
        /// <remarks>
        /// In testing mode, each testing iteration uses a unique runtime instance. To safely
        /// retrieve it from static methods, we store it in each controlled thread local state.
        /// </remarks>
        private static readonly ThreadLocal<CoyoteRuntime> ThreadLocalRuntime =
            new ThreadLocal<CoyoteRuntime>(false);

        /// <summary>
        /// Provides access to the operation executing on each controlled thread
        /// during systematic testing.
        /// </summary>
        private static readonly ThreadLocal<AsyncOperation> ExecutingOperation =
            new ThreadLocal<AsyncOperation>(false);

        /// <summary>
        /// The runtime executing the current operation.
        /// </summary>
        internal static CoyoteRuntime Current
        {
            get => ThreadLocalRuntime.Value ?? RuntimeFactory.InstalledRuntime;
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
        private readonly Configuration Configuration;

        /// <summary>
        /// Scheduler that controls the execution of operations during testing.
        /// </summary>
        private readonly OperationScheduler Scheduler;

        /// <summary>
        /// Responsible for scheduling controlled tasks.
        /// </summary>
        internal readonly ControlledTaskScheduler ControlledTaskScheduler;

        /// <summary>
        /// The synchronization context where controlled operations are executed.
        /// </summary>
        private readonly ControlledSynchronizationContext SyncContext;

        /// <summary>
        /// Creates tasks that are controlled and scheduled by the runtime.
        /// </summary>
        internal readonly TaskFactory TaskFactory;

        /// <summary>
        /// The default actor execution context.
        /// </summary>
        internal readonly ActorExecutionContext DefaultActorExecutionContext;

        /// <summary>
        /// Responsible for checking specifications.
        /// </summary>
        private readonly SpecificationEngine SpecificationEngine;

        /// <summary>
        /// Pool of threads that execute controlled operations.
        /// </summary>
        private readonly ConcurrentDictionary<ulong, Thread> ThreadPool;

        /// <summary>
        /// Map from unique ids to asynchronous operations.
        /// </summary>
        private readonly Dictionary<ulong, AsyncOperation> OperationMap;

        /// <summary>
        /// Map from controlled tasks to their corresponding operations,
        /// if such an operation exists.
        /// </summary>
        private readonly ConcurrentDictionary<Task, TaskOperation> TaskMap;

        /// <summary>
        /// The program schedule trace.
        /// </summary>
        internal ScheduleTrace ScheduleTrace;

        /// <summary>
        /// The currently scheduled asynchronous operation during systematic testing.
        /// </summary>
        private AsyncOperation ScheduledOperation;

        /// <summary>
        /// The scheduler completion source.
        /// </summary>
        private readonly TaskCompletionSource<bool> CompletionSource;

        /// <summary>
        /// Object that is used to synchronize access to the scheduler.
        /// </summary>
        private readonly object SyncObject;

        /// <summary>
        /// Monotonically increasing operation id counter.
        /// </summary>
        private long OperationIdCounter;

        /// <summary>
        /// Records if the runtime is running.
        /// </summary>
        internal volatile bool IsRunning;

        /// <summary>
        /// True if the scheduler is attached to the executing program, else false.
        /// </summary>
        private bool IsAttached;

        /// <summary>
        /// True if interleavings of enabled operations are suppressed, else false.
        /// </summary>
        private bool IsSchedulingSuppressed;

        /// <summary>
        /// True if uncontrolled concurrency was detected, else false.
        /// </summary>
        internal bool IsUncontrolledConcurrencyDetected { get; private set; }

        /// <summary>
        /// Associated with the bug report is an optional unhandled exception.
        /// </summary>
        private Exception UnhandledException;

        /// <summary>
        /// The operation scheduling policy used by the runtime.
        /// </summary>
        internal SchedulingPolicy SchedulingPolicy => this.Scheduler?.SchedulingPolicy ??
            SchedulingPolicy.None;

        /// <summary>
        /// True if a bug was found, else false.
        /// </summary>
        internal bool IsBugFound { get; private set; }

        /// <summary>
        /// Bug report.
        /// </summary>
        internal string BugReport { get; private set; }

        /// <summary>
        /// Set of method calls with uncontrolled concurrency or other sources of nondeterminism.
        /// </summary>
        private HashSet<string> UncontrolledInvocations;

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
        /// Timer implementing a deadlock monitor.
        /// </summary>
        private readonly Timer DeadlockMonitor;

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
        internal CoyoteRuntime(Configuration configuration, OperationScheduler scheduler)
            : this(configuration, scheduler, scheduler.ValueGenerator)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CoyoteRuntime"/> class.
        /// </summary>
        private CoyoteRuntime(Configuration configuration, OperationScheduler scheduler, IRandomValueGenerator valueGenerator)
        {
            this.Configuration = configuration;
            this.Scheduler = scheduler;
            this.IsRunning = true;
            this.IsAttached = true;
            this.IsSchedulingSuppressed = false;
            this.IsUncontrolledConcurrencyDetected = false;
            this.IsBugFound = false;
            this.SyncObject = new object();
            this.OperationIdCounter = 0;

            this.ThreadPool = new ConcurrentDictionary<ulong, Thread>();
            this.OperationMap = new Dictionary<ulong, AsyncOperation>();
            this.TaskMap = new ConcurrentDictionary<Task, TaskOperation>();
            this.UncontrolledInvocations = new HashSet<string>();
            this.CompletionSource = new TaskCompletionSource<bool>();
            this.ScheduleTrace = new ScheduleTrace();

            if (this.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                Interlocked.Increment(ref ExecutionControlledUseCount);
            }
            else if (this.SchedulingPolicy is SchedulingPolicy.Fuzzing)
            {
                this.DeadlockMonitor = new Timer(this.CheckIfExecutionHasDeadlocked, new SchedulingActivityInfo(),
                    Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
            }

            this.SpecificationEngine = new SpecificationEngine(configuration, this);
            this.Scheduler?.SetSpecificationEngine(this.SpecificationEngine);

            this.LogWriter = new LogWriter(configuration);

            this.ControlledTaskScheduler = new ControlledTaskScheduler(this);
            this.SyncContext = new ControlledSynchronizationContext(this);
            this.TaskFactory = new TaskFactory(CancellationToken.None, TaskCreationOptions.HideScheduler,
                TaskContinuationOptions.HideScheduler, this.ControlledTaskScheduler);

            this.DefaultActorExecutionContext = this.SchedulingPolicy is SchedulingPolicy.Systematic ?
                new ActorExecutionContext.Mock(configuration, this, this.SpecificationEngine, valueGenerator, this.LogWriter) :
                new ActorExecutionContext(configuration, this, this.SpecificationEngine, valueGenerator, this.LogWriter);

            Interception.ControlledThread.ClearCache();
        }

        /// <summary>
        /// Runs the specified test method.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal Task RunTestAsync(Delegate testMethod, string testName)
        {
            testName = string.IsNullOrEmpty(testName) ? string.Empty : $" '{testName}'";
            this.Logger.WriteLine($"<TestLog> Running test{testName}.");
            this.Assert(testMethod != null, "Unable to execute a null test method.");

            TaskOperation op = this.CreateTaskOperation();
            var thread = new Thread(() =>
            {
                try
                {
                    // Update the current controlled thread with this runtime instance and executing
                    // operation, allowing future retrieval in the same controlled thread.
                    ThreadLocalRuntime.Value = this;
                    ExecutingOperation.Value = op;

                    // Set the synchronization context to the controlled synchronization context.
                    SynchronizationContext.SetSynchronizationContext(this.SyncContext);

                    this.StartOperation(op);

                    Task task = Task.CompletedTask;
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
                        task = functionWithRuntime(this.DefaultActorExecutionContext);
                    }
                    else if (testMethod is Func<Task> function)
                    {
                        task = function();
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unsupported test delegate of type '{testMethod.GetType()}'.");
                    }

                    // Wait for the task to complete and propagate any exceptions.
                    this.WaitUntilTaskCompletes(op, task);
                    task.GetAwaiter().GetResult();

                    this.CompleteOperation(op);

                    lock (this.SyncObject)
                    {
                        this.CheckLivenessErrorsAtTermination();
                        this.Detach(SchedulerDetachmentReason.PathExplored);
                    }
                }
                catch (Exception ex)
                {
                    this.ProcessUnhandledExceptionInOperation(op, ex);
                }
                finally
                {
                    // Clean the thread local state.
                    ExecutingOperation.Value = null;
                    ThreadLocalRuntime.Value = null;
                }
            });

            if (this.SchedulingPolicy is SchedulingPolicy.Fuzzing)
            {
                this.DeadlockMonitor.Change(TimeSpan.FromMilliseconds(this.Configuration.DeadlockTimeout),
                    Timeout.InfiniteTimeSpan);
            }
            else
            {
                // TODO: optimize by using a real threadpool instead of creating a new thread each time.
                this.ThreadPool.AddOrUpdate(op.Id, thread, (id, oldThread) => thread);
            }

            thread.IsBackground = true;
            thread.Start();

            return this.CompletionSource.Task;
        }

        /// <summary>
        /// Creates a new task operation.
        /// </summary>
        private TaskOperation CreateTaskOperation(uint delay = 0)
        {
            ulong operationId = this.GetNextOperationId();
            TaskOperation op = delay > 0 ?
                new TaskOperation(operationId, $"TaskDelay({operationId})", delay) :
                new TaskOperation(operationId, $"Task({operationId})", 0);
            this.RegisterOperation(op);
            return op;
        }

        /// <summary>
        /// Schedules the specified callback to be executed asynchronously.
        /// </summary>
        internal void Schedule(Action callback)
        {
            lock (this.SyncObject)
            {
                if (!this.IsAttached)
                {
                    return;
                }
            }

            TaskOperation op = this.CreateTaskOperation();
            var thread = new Thread(() =>
            {
                try
                {
                    // Update the current controlled thread with this runtime instance and executing
                    // operation, allowing future retrieval in the same controlled thread.
                    ThreadLocalRuntime.Value = this;
                    ExecutingOperation.Value = op;

                    // Set the synchronization context to the controlled synchronization context.
                    SynchronizationContext.SetSynchronizationContext(this.SyncContext);

                    this.StartOperation(op);

                    if (this.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                    {
                        this.DelayOperation();
                    }

                    callback();
                    this.CompleteOperation(op);
                    this.ScheduleNextOperation(AsyncOperationType.Stop);
                }
                catch (Exception ex)
                {
                    this.ProcessUnhandledExceptionInOperation(op, ex);
                }
                finally
                {
                    // Clean the thread local state.
                    ExecutingOperation.Value = null;
                    ThreadLocalRuntime.Value = null;
                }
            });

            // TODO: optimize by using a real threadpool instead of creating a new thread each time.
            this.ThreadPool.AddOrUpdate(op.Id, thread, (id, oldThread) => thread);

            thread.IsBackground = true;
            thread.Start();

            this.WaitOperationStart(op);
            this.ScheduleNextOperation(AsyncOperationType.Create);
        }

        /// <summary>
        /// Schedules the specified task to be executed asynchronously.
        /// </summary>
        internal void ScheduleTask(Task task)
        {
            lock (this.SyncObject)
            {
                if (!this.IsAttached)
                {
                    return;
                }
            }

            TaskOperation op = task.AsyncState as TaskOperation ?? this.CreateTaskOperation();
            var thread = new Thread(() =>
            {
                try
                {
                    // Update the current controlled thread with this runtime instance and executing
                    // operation, allowing future retrieval in the same controlled thread.
                    ThreadLocalRuntime.Value = this;
                    ExecutingOperation.Value = op;

                    // Set the synchronization context to the controlled synchronization context.
                    SynchronizationContext.SetSynchronizationContext(this.SyncContext);

                    this.StartOperation(op);

                    if (this.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                    {
                        this.DelayOperation();
                    }

                    this.ControlledTaskScheduler.ExecuteTask(task);

                    this.CompleteOperation(op);
                    this.ScheduleNextOperation(AsyncOperationType.Stop);
                }
                catch (Exception ex)
                {
                    this.ProcessUnhandledExceptionInOperation(op, ex);
                }
                finally
                {
                    // Clean the thread local state.
                    ExecutingOperation.Value = null;
                    ThreadLocalRuntime.Value = null;
                }
            });

            // TODO: optimize by using a real threadpool instead of creating a new thread each time.
            this.ThreadPool.AddOrUpdate(op.Id, thread, (id, oldThread) => thread);
            this.TaskMap.TryAdd(task, op);

            thread.IsBackground = true;
            thread.Start();

            this.WaitOperationStart(op);
            this.ScheduleNextOperation(AsyncOperationType.Create);
        }

        /// <summary>
        /// Schedules the specified delay to be executed asynchronously.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal Task ScheduleDelay(TimeSpan delay, CancellationToken cancellationToken)
        {
            if (delay.TotalMilliseconds is 0)
            {
                // If the delay is 0, then complete synchronously.
                return Task.CompletedTask;
            }

            // TODO: support cancellations during testing.
            if (this.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                uint timeout = (uint)this.GetNextNondeterministicIntegerChoice((int)this.Configuration.TimeoutDelay);
                if (timeout is 0)
                {
                    // If the delay is 0, then complete synchronously.
                    return Task.CompletedTask;
                }

                // TODO: cache the dummy delay action to optimize memory.
                TaskOperation op = this.CreateTaskOperation(timeout);
                return this.TaskFactory.StartNew(
                    state =>
                    {
                        var delayedOp = state as TaskOperation;
                        delayedOp.Status = AsyncOperationStatus.Delayed;
                        this.ScheduleNextOperation(AsyncOperationType.Yield);
                    },
                    op,
                    cancellationToken,
                    this.TaskFactory.CreationOptions | TaskCreationOptions.DenyChildAttach,
                    this.TaskFactory.Scheduler);
            }

            return Task.Delay(TimeSpan.FromMilliseconds(
                this.GetNondeterministicDelay((int)this.Configuration.TimeoutDelay)));
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

            var callerOp = this.GetExecutingOperation<TaskOperation>();
            this.WaitUntilTasksComplete(callerOp, tasks, waitAll: true);

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

            var callerOp = this.GetExecutingOperation<TaskOperation>();
            this.WaitUntilTasksComplete(callerOp, tasks, waitAll: false);

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
        /// Blocks the specified operation until all or any of the tasks complete.
        /// </summary>
        private void WaitUntilTasksComplete(TaskOperation op, Task[] tasks, bool waitAll)
        {
            if (this.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                // In the case where `waitAll` is false (e.g. for `Task.WhenAny` or `Task.WaitAny`), we check if all
                // tasks are not completed. If that is the case, then we add all tasks to `JoinDependencies` and wait
                // at least one to complete. If, however, even one task is completed, then we should not wait, as it
                // can cause potential deadlocks.
                if (waitAll || tasks.All(task => !task.IsCompleted))
                {
                    foreach (var task in tasks)
                    {
                        if (!task.IsCompleted)
                        {
                            IO.Debug.WriteLine("<ScheduleDebug> Operation '{0}' is waiting for task '{1}'.", op.Id, task.Id);
                            op.JoinDependencies.Add(task);
                        }
                    }

                    if (op.JoinDependencies.Count > 0)
                    {
                        op.Status = waitAll ? AsyncOperationStatus.BlockedOnWaitAll : AsyncOperationStatus.BlockedOnWaitAny;
                        this.ScheduleNextOperation(AsyncOperationType.Join);
                    }
                }
            }
        }

        /// <summary>
        /// Waits for the task to complete execution. The wait terminates if a timeout interval
        /// elapses or a cancellation token is canceled before the task completes.
        /// </summary>
        internal bool WaitTaskCompletes(Task task)
        {
            // TODO: support timeouts and cancellation tokens.
            if (!task.IsCompleted)
            {
                var callerOp = this.GetExecutingOperation<TaskOperation>();
                this.WaitUntilTaskCompletes(callerOp, task);
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
            // TODO: support timeouts and cancellation tokens.
            if (!task.IsCompleted)
            {
                var callerOp = this.GetExecutingOperation<TaskOperation>();
                this.WaitUntilTaskCompletes(callerOp, task);
            }

            if (task.IsFaulted)
            {
                // Propagate the failing exception by rethrowing it.
                ExceptionDispatchInfo.Capture(task.Exception).Throw();
            }

            return task.Result;
        }

        /// <summary>
        /// Blocks the specified operation until the task completes.
        /// </summary>
        internal void WaitUntilTaskCompletes(TaskOperation op, Task task)
        {
            if (this.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                IO.Debug.WriteLine("<ScheduleDebug> Operation '{0}' is waiting for task '{1}'.", op.Id, task.Id);
                op.JoinDependencies.Add(task);
                op.Status = AsyncOperationStatus.BlockedOnWaitAll;
                this.ScheduleNextOperation(AsyncOperationType.Join);
            }
        }

        /// <summary>
        /// Unwraps the specified task.
        /// </summary>
        internal Task UnwrapTask(Task<Task> task)
        {
            var unwrappedTask = task.Unwrap();
            if (this.TaskMap.TryGetValue(task, out TaskOperation op))
            {
                this.TaskMap.TryAdd(unwrappedTask, op);
            }

            return unwrappedTask;
        }

        /// <summary>
        /// Unwraps the specified task.
        /// </summary>
        internal Task<TResult> UnwrapTask<TResult>(Task<Task<TResult>> task)
        {
            var unwrappedTask = task.Unwrap();
            if (this.TaskMap.TryGetValue(task, out TaskOperation op))
            {
                this.TaskMap.TryAdd(unwrappedTask, op);
            }

            return unwrappedTask;
        }

        /// <summary>
        /// Callback invoked when the task of a task completion source is accessed.
        /// </summary>
        internal void OnTaskCompletionSourceGetTask(Task task)
        {
            if (this.SchedulingPolicy != SchedulingPolicy.None)
            {
                this.TaskMap.TryAdd(task, null);
            }
        }

        /// <summary>
        /// Callback invoked when the <see cref="TaskAwaiter.GetResult"/> is called.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal void OnWaitTask(Task task)
        {
            if (!task.IsCompleted)
            {
                if (this.SchedulingPolicy is SchedulingPolicy.Systematic)
                {
                    var callerOp = this.GetExecutingOperation<TaskOperation>();
                    this.WaitUntilTaskCompletes(callerOp, task);
                }
                else if (this.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                {
                    this.DelayOperation();
                }
            }
        }

        /// <summary>
        /// Schedules the next enabled operation, which can include the currently executing operation.
        /// </summary>
        /// <param name="type">Type of the operation.</param>
        /// <param name="isYielding">True if the current operation is yielding, else false.</param>
        /// <param name="checkCaller">If true, schedule only if the caller is an operation.</param>
        /// <remarks>
        /// An enabled operation is one that is not blocked nor completed.
        /// </remarks>
        internal void ScheduleNextOperation(AsyncOperationType type, bool isYielding = false, bool checkCaller = false)
        {
            lock (this.SyncObject)
            {
                if (!this.IsAttached || this.SchedulingPolicy != SchedulingPolicy.Systematic)
                {
                    // Cannot schedule the next operation if the scheduler is not attached,
                    // or if the scheduling policy is not systematic.
                    return;
                }

                if (checkCaller)
                {
                    var callerOp = this.GetExecutingOperation<AsyncOperation>();
                    if (callerOp is null)
                    {
                        return;
                    }
                }

                AsyncOperation current = this.ScheduledOperation;
                if (current.Status != AsyncOperationStatus.Completed)
                {
                    // Checks if the current operation is controlled by the runtime.
                    if (ExecutingOperation.Value is null)
                    {
                        this.NotifyUncontrolledConcurrencyDetected();
                    }
                }

                if (this.IsSchedulingSuppressed && current.Status is AsyncOperationStatus.Enabled)
                {
                    // Suppress the interleaving.
                    return;
                }

                // Checks if the scheduling steps bound has been reached.
                this.CheckIfSchedulingStepsBoundIsReached();

                // Update the operation type.
                current.Type = type;

                if (this.Configuration.IsProgramStateHashingEnabled)
                {
                    // Update the current operation with the hashed program state.
                    current.HashedProgramState = this.GetHashedProgramState();
                }

                // Choose the next operation to schedule, if there is one enabled.
                if (!this.TryGetNextEnabledOperation(current, isYielding, out AsyncOperation next))
                {
                    this.Detach(SchedulerDetachmentReason.BoundReached);
                }

                IO.Debug.WriteLine("<ScheduleDebug> Scheduling the next operation of '{0}'.", next.Name);
                this.ScheduleTrace.AddSchedulingChoice(next.Id);
                if (current != next)
                {
                    // Pause the currently scheduled operation, and enable the next one.
                    this.ScheduledOperation = next;
                    this.PauseOperation(current);
                }
            }
        }

        internal void SuppressScheduling()
        {
            lock (this.SyncObject)
            {
                IO.Debug.WriteLine("<ScheduleDebug> Suppressing scheduling of enabled operations.");
                this.IsSchedulingSuppressed = true;
            }
        }

        internal void ResumeScheduling()
        {
            lock (this.SyncObject)
            {
                IO.Debug.WriteLine("<ScheduleDebug> Resuming scheduling of enabled operations.");
                this.IsSchedulingSuppressed = false;
            }
        }

        /// <summary>
        /// Tries to get the next enabled operation to schedule.
        /// </summary>
        private bool TryGetNextEnabledOperation(AsyncOperation current, bool isYielding, out AsyncOperation next)
        {
            // Get and order the operations by their id.
            var ops = this.OperationMap.Values.OrderBy(op => op.Id);

            // The scheduler might need to retry choosing a next operation in the presence of uncontrolled
            // concurrency, as explained below. In this case, we implement a simple retry logic.
            int retries = 0;
            do
            {
                // Enable any blocked operation that has its dependencies already satisfied.
                IO.Debug.WriteLine("<ScheduleDebug> Enabling any blocked operation with satisfied dependencies.");
                foreach (var op in ops)
                {
                    this.TryEnableOperation(op);
                    IO.Debug.WriteLine("<ScheduleDebug> Operation '{0}' has status '{1}'.", op.Id, op.Status);
                }

                // Choose the next operation to schedule, if there is one enabled.
                if (!this.Scheduler.GetNextOperation(ops, current, isYielding, out next) &&
                    this.Configuration.IsRelaxedControlledTestingEnabled)
                {
                    // At least one operation is blocked, potentially on an uncontrolled operation,
                    // so retry after an asynchronous delay.
                    Task.Run(async () =>
                    {
                        await Task.Delay(10);
                        lock (this.SyncObject)
                        {
                            SyncMonitor.PulseAll(this.SyncObject);
                        }
                    });

                    // Pause the current operation until the scheduler retries.
                    SyncMonitor.Wait(this.SyncObject);
                    retries++;
                    continue;
                }

                break;
            }
            while (retries < 5);

            if (next is null)
            {
                // Check if the execution has deadlocked.
                this.CheckIfExecutionHasDeadlocked(ops);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Delays the currently executing operation for a nondeterministically chosen amount of time.
        /// </summary>
        /// <remarks>
        /// The delay is chosen nondeterministically by an underlying fuzzing strategy.
        /// If a delay of 0 is chosen, then the operation is not delayed.
        /// </remarks>
        internal void DelayOperation()
        {
            int delay;
            lock (this.SyncObject)
            {
                // Choose the next delay to inject. The value is in milliseconds.
                delay = this.GetNondeterministicDelay((int)this.Configuration.TimeoutDelay);
                IO.Debug.WriteLine("<ScheduleDebug> Delaying the operation that executes on thread '{0}' by {1}ms.",
                    Thread.CurrentThread.ManagedThreadId, delay);
            }

            if (delay > 0)
            {
                // Only sleep if a non-zero delay was chosen.
                Thread.Sleep(delay);
            }
        }

        /// <summary>
        /// Returns a controlled nondeterministic boolean choice.
        /// </summary>
        internal bool GetNondeterministicBooleanChoice(int maxValue, string callerName, string callerType) =>
            this.DefaultActorExecutionContext.GetNondeterministicBooleanChoice(maxValue, callerName, callerType);

        /// <summary>
        /// Returns the next nondeterministic boolean choice.
        /// </summary>
        internal bool GetNextNondeterministicBooleanChoice(int maxValue)
        {
            lock (this.SyncObject)
            {
                // Checks if the current operation is controlled by the runtime.
                if (ExecutingOperation.Value is null)
                {
                    this.NotifyUncontrolledConcurrencyDetected();
                }

                // Checks if the scheduling steps bound has been reached.
                this.CheckIfSchedulingStepsBoundIsReached();

                if (this.Configuration.IsProgramStateHashingEnabled)
                {
                    // Update the current operation with the hashed program state.
                    this.ScheduledOperation.HashedProgramState = this.GetHashedProgramState();
                }

                if (!this.Scheduler.GetNextBooleanChoice(this.ScheduledOperation, maxValue, out bool choice))
                {
                    this.Detach(SchedulerDetachmentReason.BoundReached);
                }

                this.ScheduleTrace.AddNondeterministicBooleanChoice(choice);
                return choice;
            }
        }

        /// <summary>
        /// Returns a controlled nondeterministic integer choice.
        /// </summary>
        internal int GetNondeterministicIntegerChoice(int maxValue, string callerName, string callerType) =>
            this.DefaultActorExecutionContext.GetNondeterministicIntegerChoice(maxValue, callerName, callerType);

        /// <summary>
        /// Returns the next nondeterministic integer choice.
        /// </summary>
        internal int GetNextNondeterministicIntegerChoice(int maxValue)
        {
            lock (this.SyncObject)
            {
                // Checks if the current operation is controlled by the runtime.
                if (ExecutingOperation.Value is null)
                {
                    this.NotifyUncontrolledConcurrencyDetected();
                }

                // Checks if the scheduling steps bound has been reached.
                this.CheckIfSchedulingStepsBoundIsReached();

                if (this.Configuration.IsProgramStateHashingEnabled)
                {
                    // Update the current operation with the hashed program state.
                    this.ScheduledOperation.HashedProgramState = this.GetHashedProgramState();
                }

                if (!this.Scheduler.GetNextIntegerChoice(this.ScheduledOperation, maxValue, out int choice))
                {
                    this.Detach(SchedulerDetachmentReason.BoundReached);
                }

                this.ScheduleTrace.AddNondeterministicIntegerChoice(choice);
                return choice;
            }
        }

        /// <summary>
        /// Returns a controlled nondeterministic delay.
        /// </summary>
        internal int GetNondeterministicDelay(int maxValue)
        {
            lock (this.SyncObject)
            {
                // Checks if the scheduling steps bound has been reached.
                this.CheckIfSchedulingStepsBoundIsReached();

                // Choose the next delay to inject.
                int maxDelay = maxValue > 0 ? (int)this.Configuration.TimeoutDelay : 1;
                if (!this.Scheduler.GetNextDelay(maxDelay, out int next))
                {
                    this.Detach(SchedulerDetachmentReason.BoundReached);
                }

                return next;
            }
        }

        /// <summary>
        /// Registers the specified asynchronous operation.
        /// </summary>
        /// <param name="op">The operation to register.</param>
        internal void RegisterOperation(AsyncOperation op)
        {
            lock (this.SyncObject)
            {
                if (this.OperationMap.Count is 0)
                {
                    this.ScheduledOperation = op;
                }

#if NETSTANDARD2_0 || NETFRAMEWORK
                if (!this.OperationMap.ContainsKey(op.Id))
                {
                    this.OperationMap.Add(op.Id, op);
                }
#else
                this.OperationMap.TryAdd(op.Id, op);
#endif
            }
        }

        /// <summary>
        /// Starts the execution of the specified asynchronous operation.
        /// </summary>
        /// <param name="op">The operation to start executing.</param>
        /// <remarks>
        /// This method performs a handshake with <see cref="WaitOperationStart"/>.
        /// </remarks>
        private void StartOperation(AsyncOperation op)
        {
            lock (this.SyncObject)
            {
                IO.Debug.WriteLine("<ScheduleDebug> Starting the operation of '{0}' on thread '{1}'.",
                    op.Name, Thread.CurrentThread.ManagedThreadId);

                // Enable the operation and store it in the async local context.
                op.Status = AsyncOperationStatus.Enabled;
                if (this.SchedulingPolicy is SchedulingPolicy.Systematic)
                {
                    this.PauseOperation(op);
                }
            }
        }

        /// <summary>
        /// Waits for the specified asynchronous operation to start executing.
        /// </summary>
        /// <param name="op">The operation to wait.</param>
        /// <remarks>
        /// This method performs a handshake with <see cref="StartOperation"/>.
        /// </remarks>
        private void WaitOperationStart(AsyncOperation op)
        {
            lock (this.SyncObject)
            {
                if (this.SchedulingPolicy is SchedulingPolicy.Systematic && this.OperationMap.Count > 1)
                {
                    while (op.Status != AsyncOperationStatus.Enabled && this.IsAttached)
                    {
                        SyncMonitor.Wait(this.SyncObject);
                    }
                }
            }
        }

        /// <summary>
        /// Pauses the specified operation.
        /// </summary>
        /// <remarks>
        /// It is assumed that this method runs in the scope of a 'lock (this.SyncObject)' statement.
        /// </remarks>
        private void PauseOperation(AsyncOperation op)
        {
            SyncMonitor.PulseAll(this.SyncObject);
            if (op.Status is AsyncOperationStatus.Completed)
            {
                // The operation is completed, so no need to wait.
                return;
            }

            while (op != this.ScheduledOperation && this.IsAttached)
            {
                IO.Debug.WriteLine("<ScheduleDebug> Sleeping the operation of '{0}' on thread '{1}'.",
                    op.Name, Thread.CurrentThread.ManagedThreadId);
                SyncMonitor.Wait(this.SyncObject);
                IO.Debug.WriteLine("<ScheduleDebug> Waking up the operation of '{0}' on thread '{1}'.",
                    op.Name, Thread.CurrentThread.ManagedThreadId);
            }
        }

        /// <summary>
        /// Completes the specified operation.
        /// </summary>
        private void CompleteOperation(AsyncOperation op)
        {
            lock (this.SyncObject)
            {
                IO.Debug.WriteLine("<ScheduleDebug> Completed the operation of '{0}' on thread '{1}'.",
                    op.Name, Thread.CurrentThread.ManagedThreadId);
                op.Status = AsyncOperationStatus.Completed;
            }
        }

        /// <summary>
        /// Tries to enable the specified operation.
        /// </summary>
        /// <remarks>
        /// It is assumed that this method runs in the scope of a 'lock (this.SyncObject)' statement.
        /// </remarks>
        private bool TryEnableOperation(AsyncOperation op)
        {
            if (op.Status != AsyncOperationStatus.Enabled)
            {
                if (op.Status is AsyncOperationStatus.Delayed && op is TaskOperation delayedOp)
                {
                    if (delayedOp.Timeout > 0)
                    {
                        delayedOp.Timeout--;
                    }

                    // The task operation is delayed, so it is enabled either if the delay completes
                    // or if no other operation is enabled.

                    if (delayedOp.Timeout is 0 ||
                        !this.OperationMap.Any(kvp => kvp.Value.Status is AsyncOperationStatus.Enabled))
                    {
                        delayedOp.Timeout = 0;
                        delayedOp.Status = AsyncOperationStatus.Enabled;
                        return true;
                    }

                    return false;
                }

                // If this is the root operation, then only try enable it if all actor operations (if any) are
                // completed. This is required because in tests that include actors, actors can execute without
                // the main task explicitly waiting for them to terminate or reach quiescence. Otherwise, if the
                // root operation was enabled, the test can terminate early.
                if (op.Id is 0 && this.OperationMap.Any(
                    kvp => kvp.Value is ActorOperation && kvp.Value.Status != AsyncOperationStatus.Completed))
                {
                    return false;
                }

                // If this is a task operation blocked on other tasks, then check if the
                // necessary tasks are completed.
                if (op is TaskOperation taskOp &&
                    ((taskOp.Status is AsyncOperationStatus.BlockedOnWaitAll &&
                    taskOp.JoinDependencies.All(task => task.IsCompleted)) ||
                    (taskOp.Status is AsyncOperationStatus.BlockedOnWaitAny &&
                    taskOp.JoinDependencies.Any(task => task.IsCompleted))))
                {
                    taskOp.JoinDependencies.Clear();
                    taskOp.Status = AsyncOperationStatus.Enabled;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the <see cref="AsyncOperation"/> that is currently executing,
        /// or null if no such operation is executing.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal TAsyncOperation GetExecutingOperation<TAsyncOperation>()
            where TAsyncOperation : AsyncOperation
        {
            lock (this.SyncObject)
            {
                var op = ExecutingOperation.Value;
                if (op is null)
                {
                    this.NotifyUncontrolledConcurrencyDetected();
                }

                return op.Equals(this.ScheduledOperation) && op is TAsyncOperation expected ? expected : default;
            }
        }

        /// <summary>
        /// Gets the <see cref="AsyncOperation"/> associated with the specified
        /// unique id, or null if no such operation exists.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal TAsyncOperation GetOperationWithId<TAsyncOperation>(ulong id)
            where TAsyncOperation : AsyncOperation
        {
            lock (this.SyncObject)
            {
                if (this.OperationMap.TryGetValue(id, out AsyncOperation op) &&
                    op is TAsyncOperation expected)
                {
                    return expected;
                }
            }

            return default;
        }

        /// <summary>
        /// Returns all registered operations.
        /// </summary>
        /// <remarks>
        /// This operation is thread safe because the systematic testing
        /// runtime serializes the execution.
        /// </remarks>
        internal IEnumerable<AsyncOperation> GetRegisteredOperations()
        {
            lock (this.SyncObject)
            {
                return this.OperationMap.Values;
            }
        }

        /// <summary>
        /// Returns the next available unique operation id.
        /// </summary>
        /// <returns>Value representing the next available unique operation id.</returns>
        internal ulong GetNextOperationId() =>
            // Atomically increments and safely wraps the value into an unsigned long.
            (ulong)Interlocked.Increment(ref this.OperationIdCounter) - 1;

        /// <summary>
        /// Returns the current hashed state of the execution.
        /// </summary>
        /// <remarks>
        /// The hash is updated in each execution step.
        /// </remarks>
        [DebuggerStepThrough]
        private int GetHashedProgramState()
        {
            unchecked
            {
                int hash = 19;

                foreach (var operation in this.GetRegisteredOperations().OrderBy(op => op.Id))
                {
                    if (operation is ActorOperation actorOperation)
                    {
                        int operationHash = 31 + actorOperation.Actor.GetHashedState();
                        operationHash = (operationHash * 31) + actorOperation.Type.GetHashCode();
                        hash *= operationHash;
                    }
                    else if (operation is TaskOperation taskOperation)
                    {
                        hash *= 31 + taskOperation.Type.GetHashCode();
                    }
                }

                hash = (hash * 31) + this.SpecificationEngine.GetHashedMonitorState();
                return hash;
            }
        }

        /// <summary>
        /// Checks if the scheduling steps bound has been reached. If yes,
        /// it stops the scheduler and kills all enabled machines.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        private void CheckIfSchedulingStepsBoundIsReached()
        {
            if (this.Scheduler.IsMaxStepsReached)
            {
                string message = $"Scheduling steps bound of {this.Scheduler.StepCount} reached.";
                if (this.Configuration.ConsiderDepthBoundHitAsBug)
                {
                    this.NotifyAssertionFailure(message);
                }
                else
                {
                    IO.Debug.WriteLine($"<ScheduleDebug> {message}");
                    this.Detach(SchedulerDetachmentReason.BoundReached);
                }
            }
        }

        /// <summary>
        /// Registers a new specification monitor of the specified <see cref="Type"/>.
        /// </summary>
        internal void RegisterMonitor<T>()
            where T : Specifications.Monitor => this.DefaultActorExecutionContext.RegisterMonitor<T>();

        /// <summary>
        /// Invokes the specified monitor with the specified <see cref="Event"/>.
        /// </summary>
        internal void Monitor<T>(Event e)
            where T : Specifications.Monitor => this.DefaultActorExecutionContext.Monitor<T>(e);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an exception.
        /// </summary>
        internal void Assert(bool predicate) => this.SpecificationEngine.Assert(predicate);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an exception.
        /// </summary>
        internal void Assert(bool predicate, string s, object arg0) => this.SpecificationEngine.Assert(predicate, s, arg0);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an exception.
        /// </summary>
        internal void Assert(bool predicate, string s, object arg0, object arg1) =>
            this.SpecificationEngine.Assert(predicate, s, arg0, arg1);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an exception.
        /// </summary>
        internal void Assert(bool predicate, string s, object arg0, object arg1, object arg2) =>
            this.SpecificationEngine.Assert(predicate, s, arg0, arg1, arg2);

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an exception.
        /// </summary>
        internal void Assert(bool predicate, string s, params object[] args) => this.SpecificationEngine.Assert(predicate, s, args);

        /// <summary>
        /// Creates a liveness monitor that checks if the specified task eventually completes execution successfully.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal void MonitorTaskCompletion(Task task) => this.SpecificationEngine.MonitorTaskCompletion(task);

        /// <summary>
        /// Checks if the task returned from the specified method is uncontrolled.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void CheckIfReturnedTaskIsUncontrolled(Task task, string methodName)
        {
            if (!task.IsCompleted && !this.TaskMap.ContainsKey(task) &&
                !this.Configuration.IsRelaxedControlledTestingEnabled)
            {
                this.NotifyUncontrolledTaskReturned(task, methodName);
            }
        }

        /// <summary>
        /// Checks if the execution has deadlocked. This happens when there are no more enabled operations,
        /// but there is one or more blocked operations that are waiting some resource to complete.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        private void CheckIfExecutionHasDeadlocked(IEnumerable<AsyncOperation> ops)
        {
            var blockedOnReceiveOperations = ops.Where(op => op.Status is AsyncOperationStatus.BlockedOnReceive).ToList();
            var blockedOnWaitOperations = ops.Where(op => op.Status is AsyncOperationStatus.BlockedOnWaitAll ||
                op.Status is AsyncOperationStatus.BlockedOnWaitAny).ToList();
            var blockedOnResources = ops.Where(op => op.Status is AsyncOperationStatus.BlockedOnResource).ToList();

            var totalCount = blockedOnReceiveOperations.Count + blockedOnWaitOperations.Count + blockedOnResources.Count;
            if (totalCount is 0)
            {
                return;
            }

            // To simplify the error message, remove the root operation, unless it is the only one that is blocked.
            if (totalCount > 1)
            {
                blockedOnReceiveOperations.RemoveAll(op => op.Id is 0);
                blockedOnWaitOperations.RemoveAll(op => op.Id is 0);
                blockedOnResources.RemoveAll(op => op.Id is 0);
            }

            var msg = new StringBuilder("Deadlock detected.");
            if (blockedOnReceiveOperations.Count > 0)
            {
                for (int idx = 0; idx < blockedOnReceiveOperations.Count; idx++)
                {
                    msg.Append(string.Format(CultureInfo.InvariantCulture, " {0}", blockedOnReceiveOperations[idx].Name));
                    if (idx == blockedOnReceiveOperations.Count - 2)
                    {
                        msg.Append(" and");
                    }
                    else if (idx < blockedOnReceiveOperations.Count - 1)
                    {
                        msg.Append(',');
                    }
                }

                msg.Append(blockedOnReceiveOperations.Count is 1 ? " is " : " are ");
                msg.Append("waiting to receive an event, but no other controlled operations are enabled.");
            }

            if (blockedOnWaitOperations.Count > 0)
            {
                for (int idx = 0; idx < blockedOnWaitOperations.Count; idx++)
                {
                    msg.Append(string.Format(CultureInfo.InvariantCulture, " {0}", blockedOnWaitOperations[idx].Name));
                    if (idx == blockedOnWaitOperations.Count - 2)
                    {
                        msg.Append(" and");
                    }
                    else if (idx < blockedOnWaitOperations.Count - 1)
                    {
                        msg.Append(',');
                    }
                }

                msg.Append(blockedOnWaitOperations.Count is 1 ? " is " : " are ");
                msg.Append("waiting for a task to complete, but no other controlled operations are enabled.");
            }

            if (blockedOnResources.Count > 0)
            {
                for (int idx = 0; idx < blockedOnResources.Count; idx++)
                {
                    msg.Append(string.Format(CultureInfo.InvariantCulture, " {0}", blockedOnResources[idx].Name));
                    if (idx == blockedOnResources.Count - 2)
                    {
                        msg.Append(" and");
                    }
                    else if (idx < blockedOnResources.Count - 1)
                    {
                        msg.Append(',');
                    }
                }

                msg.Append(blockedOnResources.Count is 1 ? " is " : " are ");
                msg.Append("waiting to acquire a resource that is already acquired, ");
                msg.Append("but no other controlled operations are enabled.");
            }

            this.NotifyAssertionFailure(msg.ToString());
        }

        /// <summary>
        /// Checks if the execution has deadlocked. This checks occurs periodically.
        /// </summary>
        private void CheckIfExecutionHasDeadlocked(object state)
        {
            lock (this.SyncObject)
            {
                if (!this.IsAttached)
                {
                    return;
                }

                SchedulingActivityInfo info = state as SchedulingActivityInfo;
                if (info.StepCount == this.Scheduler.StepCount)
                {
                    string msg = "Potential deadlock detected. If you think this is not a deadlock, you can try " +
                        "increase the dealock detection timeout (--deadlock-timeout).";
                    this.NotifyAssertionFailure(msg);
                }
                else
                {
                    info.StepCount = this.Scheduler.StepCount;

                    try
                    {
                        // Start the next timeout period.
                        this.DeadlockMonitor.Change(TimeSpan.FromMilliseconds(this.Configuration.DeadlockTimeout),
                            Timeout.InfiniteTimeSpan);
                    }
                    catch (ObjectDisposedException)
                    {
                        // Benign race condition while disposing the timer.
                    }
                }
            }
        }

        /// <summary>
        /// Checks for liveness errors at test termination.
        /// </summary>
        /// <remarks>
        /// The liveness check only happens if no safety errors have been found, and all controlled
        /// operations have completed to ensure that any found liveness bug is not a false positive.
        /// </remarks>
        private void CheckLivenessErrorsAtTermination()
        {
            if (!this.IsBugFound && this.OperationMap.All(kvp => kvp.Value.Status is AsyncOperationStatus.Completed))
            {
                this.SpecificationEngine.CheckLivenessErrors();
            }
        }

        /// <summary>
        /// Notify that an exception was not handled.
        /// </summary>
        internal void NotifyUnhandledException(Exception ex, string message)
        {
            lock (this.SyncObject)
            {
                if (!this.IsAttached)
                {
                    return;
                }

                if (this.UnhandledException is null)
                {
                    this.UnhandledException = ex;
                }

                this.NotifyAssertionFailure(message);
            }
        }

        /// <summary>
        /// Notify that an assertion has failed.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void NotifyAssertionFailure(string text)
        {
            lock (this.SyncObject)
            {
                if (!this.IsAttached)
                {
                    return;
                }

                if (!this.IsBugFound)
                {
                    this.BugReport = text;
                    this.LogWriter.LogAssertionFailure($"<ErrorLog> {text}");
                    this.RaiseOnFailureEvent(new AssertionFailureException(text));

                    this.IsBugFound = true;
                    if (this.Configuration.AttachDebugger)
                    {
                        Debugger.Break();
                    }
                }

                this.Detach(SchedulerDetachmentReason.BugFound);
            }
        }

        /// <summary>
        /// Checks if the currently executing operation is controlled by the runtime.
        /// </summary>
        private void NotifyUncontrolledConcurrencyDetected()
        {
            if (this.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                // TODO: figure out if there is a way to get more information about the creator of the
                // uncontrolled task to ease the user debugging experience.
                // Report the invalid operation and then throw it to fail the uncontrolled task. This will
                // most likely crash the program, but we try to fail as cleanly and fast as possible.
                string taskId = Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>";
                string message = $"Detected task '{taskId}' that is not intercepted and controlled during " +
                    "testing, so it can interfere with the ability to reproduce bug traces.";
                if (this.Configuration.IsConcurrencyFuzzingFallbackEnabled)
                {
                    this.Logger.WriteLine($"<TestLog> {message}");
                    this.IsUncontrolledConcurrencyDetected = true;
                    this.Detach(SchedulerDetachmentReason.UncontrolledConcurrencyDetected);
                }
                else
                {
                    throw new NotSupportedException(FormatUncontrolledConcurrencyExceptionMessage(message));
                }
            }
        }

        /// <summary>
        /// Notify that an uncontrolled task was returned.
        /// </summary>
        private void NotifyUncontrolledTaskReturned(Task task, string methodName)
        {
            lock (this.SyncObject)
            {
                if (this.SchedulingPolicy != SchedulingPolicy.None)
                {
                    this.UncontrolledInvocations.Add(methodName);
                }

                if (this.SchedulingPolicy is SchedulingPolicy.Systematic)
                {
                    string message = $"Invoking '{methodName}' returned task '{task.Id}' that is not intercepted and " +
                        "controlled during testing, so it can interfere with the ability to reproduce bug traces.";
                    if (this.Configuration.IsConcurrencyFuzzingFallbackEnabled)
                    {
                        this.Logger.WriteLine($"<TestLog> {message}");
                        this.IsUncontrolledConcurrencyDetected = true;
                        this.Detach(SchedulerDetachmentReason.UncontrolledConcurrencyDetected);
                    }
                    else
                    {
                        throw new NotSupportedException(FormatUncontrolledConcurrencyExceptionMessage(message, methodName));
                    }
                }
            }
        }

        /// <summary>
        /// Notify that an uncontrolled invocation was detected.
        /// </summary>
        internal void NotifyUncontrolledInvocation(string methodName)
        {
            lock (this.SyncObject)
            {
                if (this.SchedulingPolicy != SchedulingPolicy.None)
                {
                    this.UncontrolledInvocations.Add(methodName);
                }

                if (this.SchedulingPolicy is SchedulingPolicy.Systematic)
                {
                    string message = $"Invoking '{methodName}' is not intercepted and controlled during " +
                        "testing, so it can interfere with the ability to reproduce bug traces.";
                    if (this.Configuration.IsConcurrencyFuzzingFallbackEnabled)
                    {
                        this.Logger.WriteLine($"<TestLog> {message}");
                        this.IsUncontrolledConcurrencyDetected = true;
                        this.Detach(SchedulerDetachmentReason.UncontrolledConcurrencyDetected);
                    }
                    else
                    {
                        throw new NotSupportedException(FormatUncontrolledConcurrencyExceptionMessage(message, methodName));
                    }
                }
            }
        }

        /// <summary>
        /// Formats the message of the uncontrolled concurrency exception.
        /// </summary>
        private static string FormatUncontrolledConcurrencyExceptionMessage(string message, string methodName = default)
        {
            var mockMessage = methodName is null ? string.Empty : $" either mock '{methodName}' or";
            return $"{message} As a workaround, you can{mockMessage} use the '--no-repro' command line option " +
                "to ignore this error by disabling bug trace repro. Learn more at http://aka.ms/coyote-no-repro.";
        }

        /// <summary>
        /// Processes an unhandled exception in the specified asynchronous operation.
        /// </summary>
        internal void ProcessUnhandledExceptionInOperation(AsyncOperation op, Exception ex)
        {
            string message = null;
            Exception exception = UnwrapException(ex);
            if (exception is ThreadInterruptedException)
            {
                // Ignore this exception, its thrown by the runtime.
                IO.Debug.WriteLine("<ScheduleDebug> Controlled thread '{0}' executing operation '{1}' was interrupted.",
                    Thread.CurrentThread.ManagedThreadId, op.Name);
            }
            else if (op is ActorOperation actorOp)
            {
                message = string.Format(CultureInfo.InvariantCulture,
                    $"Unhandled exception '{exception.GetType()}' was thrown in actor '{actorOp.Name}', " +
                    $"'{exception.Source}':\n" +
                    $"   {exception.Message}\n" +
                    $"The stack trace is:\n{exception.StackTrace}");
            }
            else
            {
                message = $"Unhandled exception. {ex.GetType()}: {ex.Message}\n" +
                    $"The stack trace is:\n{ex.StackTrace}";
            }

            // Complete the failed operation. This is required so that the operation
            // does not throw if it detaches.
            op.Status = AsyncOperationStatus.Completed;
            if (message != null)
            {
                // Report the unhandled exception.
                this.NotifyUnhandledException(exception, message);
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

            if (exception is AggregateException aex)
            {
                exception = aex.InnerException;
            }

            return exception;
        }

        /// <summary>
        /// Raises the <see cref="OnFailure"/> event with the specified <see cref="Exception"/>.
        /// </summary>
        internal void RaiseOnFailureEvent(Exception exception)
        {
            if (this.Configuration.AttachDebugger)
            {
                Debugger.Break();
                this.Configuration.AttachDebugger = false;
            }

            this.OnFailure?.Invoke(exception);
        }

        /// <summary>
        /// Populates the specified test report.
        /// </summary>
        internal void PopulateTestReport(ITestReport report)
        {
            lock (this.SyncObject)
            {
                report.SetSchedulingStatistics(this.IsBugFound, this.BugReport, this.Scheduler.StepCount,
                    this.Scheduler.IsMaxStepsReached, this.Scheduler.IsScheduleFair);
                if (this.IsBugFound)
                {
                    report.SetUnhandledException(this.UnhandledException);
                }

                report.SetUncontrolledInvocations(this.UncontrolledInvocations);
            }
        }

        /// <summary>
        /// Forces the scheduler to terminate.
        /// </summary>
        public void ForceStop() => this.IsRunning = false;

        /// <summary>
        /// Detaches the scheduler and interrupts all controlled operations.
        /// </summary>
        private void Detach(SchedulerDetachmentReason reason)
        {
            if (!this.IsAttached)
            {
                return;
            }

            try
            {
                if (reason is SchedulerDetachmentReason.PathExplored)
                {
                    this.Logger.WriteLine("<TestLog> Exploration finished [reached the end of the test method].");
                }
                else if (reason is SchedulerDetachmentReason.BoundReached)
                {
                    this.Logger.WriteLine("<TestLog> Exploration finished [reached the given bound].");
                }
                else if (reason is SchedulerDetachmentReason.UncontrolledConcurrencyDetected)
                {
                    this.Logger.WriteLine("<TestLog> Exploration finished [detected uncontrolled concurrency].");
                }
                else if (reason is SchedulerDetachmentReason.BugFound)
                {
                    this.Logger.WriteLine("<TestLog> Exploration finished [found a bug using the '{0}' strategy].",
                        this.Configuration.SchedulingStrategy);
                }

                this.IsAttached = false;

                // Complete any remaining operations at the end of the schedule.
                foreach (var op in this.OperationMap.Values)
                {
                    if (op.Status != AsyncOperationStatus.Completed)
                    {
                        op.Status = AsyncOperationStatus.Completed;

                        // Interrupt the thread executing this operation.
                        if (op == ExecutingOperation.Value)
                        {
                            throw new ThreadInterruptedException();
                        }
                        else if (this.ThreadPool.TryGetValue(op.Id, out Thread thread))
                        {
                            thread.Interrupt();
                        }
                    }
                }
            }
            finally
            {
                // Check if the completion source is completed, else set its result.
                if (!this.CompletionSource.Task.IsCompleted)
                {
                    this.IsRunning = false;
                    this.CompletionSource.SetResult(true);
                }
            }
        }

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.OperationIdCounter = 0;
                this.ThreadPool.Clear();
                this.OperationMap.Clear();
                this.TaskMap.Clear();
                this.UncontrolledInvocations.Clear();

                this.DefaultActorExecutionContext.Dispose();
                this.ControlledTaskScheduler.Dispose();
                this.SyncContext.Dispose();
                this.SpecificationEngine.Dispose();
                this.ScheduleTrace.Dispose();

                if (this.SchedulingPolicy is SchedulingPolicy.Systematic)
                {
                    // Note: this makes it possible to run a Controlled unit test followed by a production
                    // unit test, whereas before that would throw "Uncontrolled Task" exceptions.
                    // This does not solve mixing unit test type in parallel.
                    Interlocked.Decrement(ref ExecutionControlledUseCount);
                }
                else if (this.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                {
                    this.DeadlockMonitor.Dispose();
                }
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
