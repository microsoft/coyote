// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
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
    /// RuntimeStats.
    /// </summary>
    public static class RuntimeStats
    {
        /// <summary>
        /// NumOnlyDisabledOperations.
        /// </summary>
        public static int NumOnlyDisabledOperations { get; internal set; } = 0;

        /// <summary>
        /// VisitedSchedules.
        /// </summary>
        public static int NumVisitedSchedules { get; internal set; } = 0;
    }

    /// <summary>
    /// Runtime for controlling, scheduling and executing asynchronous operations.
    /// </summary>
    /// <remarks>
    /// Invoking scheduling methods is thread-safe.
    /// </remarks>
    internal sealed class CoyoteRuntime : IDisposable
    {
        /// <summary>
        /// Provides access to the runtime associated with each controlled thread, or null
        /// if the current thread is not controlled.
        /// </summary>
        /// <remarks>
        /// In testing mode, each testing iteration uses a unique runtime instance. To safely
        /// retrieve it from static methods, we store it in each controlled thread local state.
        /// </remarks>
        private static readonly ThreadLocal<CoyoteRuntime> ThreadLocalRuntime =
            new ThreadLocal<CoyoteRuntime>(false);

        /// <summary>
        /// For debugging purposes.
        /// </summary>
        internal static readonly AsyncLocal<string> AsyncLocalDebugInfo =
            new AsyncLocal<string>();

        /// <summary>
        /// Provides access to the runtime associated with each async local context, or null
        /// if the current async local context has no associated runtime.
        /// </summary>
        /// <remarks>
        /// In testing mode, each testing iteration uses a unique runtime instance. To safely
        /// retrieve it from static methods, we store it in each controlled async local state.
        /// </remarks>
        private static readonly AsyncLocal<CoyoteRuntime> AsyncLocalRuntime =
            new AsyncLocal<CoyoteRuntime>();

        /// <summary>
        /// Provides access to the operation executing on each controlled thread
        /// during systematic testing.
        /// </summary>
        private static readonly ThreadLocal<ControlledOperation> ExecutingOperation =
            new ThreadLocal<ControlledOperation>(false);

        /// <summary>
        /// The runtime executing the current operation.
        /// </summary>
        internal static CoyoteRuntime Current
        {
            get => ThreadLocalRuntime.Value ?? AsyncLocalRuntime.Value ?? RuntimeProvider.DefaultRuntime;
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
        /// The unique id of this runtime.
        /// </summary>
        internal readonly Guid Id;

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
        private readonly Dictionary<ulong, ControlledOperation> OperationMap;

        /// <summary>
        /// Map from unique controlled thread names to their corresponding operations.
        /// </summary>
        private readonly ConcurrentDictionary<string, ControlledOperation> ControlledThreads;

        /// <summary>
        /// Map from controlled tasks to their corresponding operations.
        /// </summary>
        private readonly ConcurrentDictionary<Task, ControlledOperation> ControlledTasks;

        /// <summary>
        /// Set of known uncontrolled tasks.
        /// </summary>
        private readonly HashSet<Task> UncontrolledTasks;

        /// <summary>
        /// Set of method calls with uncontrolled concurrency or other sources of nondeterminism.
        /// </summary>
        private readonly HashSet<string> UncontrolledInvocations;

        /// <summary>
        /// The program schedule trace.
        /// </summary>
        internal readonly ScheduleTrace ScheduleTrace;

        /// <summary>
        /// The currently scheduled operation during systematic testing.
        /// </summary>
        private ControlledOperation ScheduledOperation;

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
        private bool IsSchedulerSuppressed;

        /// <summary>
        /// True if the last scheduling point was postponed. This happens if an uncontrolled
        /// thread created a new controlled operation and tried to schedule it, but this can
        /// only happen from a controlled thread. If this value is set, the runtime will try
        /// to invoke the scheduler from a controlled thread before resuming executing the
        /// currently scheduled operation, which can potentially increase coverage.
        /// </summary>
        private bool IsLastSchedulingPointPostponed;

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
        /// The max number of operations that were enabled at the same time.
        /// </summary>
        private uint MaxConcurrencyDegree;

        /// <summary>
        /// True if a bug was found, else false.
        /// </summary>
        internal bool IsBugFound { get; private set; }

        /// <summary>
        /// Bug report.
        /// </summary>
        internal string BugReport { get; private set; }

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
            // Registers the runtime with the provider which in return assigns a unique identifier.
            this.Id = RuntimeProvider.Register(this);
            IO.Debug.WriteLine($">>>> NEW RUNTIME {this.Id}");

            this.Configuration = configuration;
            this.Scheduler = scheduler;
            this.SyncObject = new object();
            this.OperationIdCounter = 0;
            this.IsRunning = true;
            this.IsAttached = true;
            this.IsSchedulerSuppressed = false;
            this.IsUncontrolledConcurrencyDetected = false;
            this.IsLastSchedulingPointPostponed = false;
            this.MaxConcurrencyDegree = 0;
            this.IsBugFound = false;

            this.ThreadPool = new ConcurrentDictionary<ulong, Thread>();
            this.OperationMap = new Dictionary<ulong, ControlledOperation>();
            this.ControlledThreads = new ConcurrentDictionary<string, ControlledOperation>();
            this.ControlledTasks = new ConcurrentDictionary<Task, ControlledOperation>();
            this.UncontrolledTasks = new HashSet<Task>();
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
        }

        /// <summary>
        /// Runs the specified test method.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal Task RunTestAsync(Delegate testMethod, string testName)
        {
            this.Logger.WriteLine("<TestLog> Running test{0} on thread '{1}'.",
                string.IsNullOrEmpty(testName) ? string.Empty : $" '{testName}'",
                Thread.CurrentThread.ManagedThreadId);
            this.Assert(testMethod != null, "Unable to execute a null test method.");

            ControlledOperation op = this.CreateControlledOperation();
            var thread = new Thread(() =>
            {
                try
                {
                    // Install the runtime to the execution context of the current thread.
                    this.SetThreadExecutionContext();

                    // Update the controlled thread with the currently executing operation,
                    // allowing future retrieval in the same controlled thread.
                    ExecutingOperation.Value = op;

                    this.StartOperation(op);

                    Task task = Task.CompletedTask;
                    Task actorQuiescenceTask = Task.CompletedTask;
                    if (testMethod is Action<IActorRuntime> actionWithRuntime)
                    {
                        actionWithRuntime(this.DefaultActorExecutionContext);
                        actorQuiescenceTask = this.DefaultActorExecutionContext.WaitUntilQuiescenceAsync();
                    }
                    else if (testMethod is Action action)
                    {
                        action();
                    }
                    else if (testMethod is Func<IActorRuntime, Task> functionWithRuntime)
                    {
                        task = functionWithRuntime(this.DefaultActorExecutionContext);
                        actorQuiescenceTask = this.DefaultActorExecutionContext.WaitUntilQuiescenceAsync();
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

                    if (this.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                    {
                        // Wait for any actors to reach quiescence and propagate any exceptions.
                        this.WaitUntilTaskCompletes(op, actorQuiescenceTask);
                        actorQuiescenceTask.GetAwaiter().GetResult();
                    }

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
                    AsyncLocalRuntime.Value = null;
                    ThreadLocalRuntime.Value = null;
                }
            });

            if (this.SchedulingPolicy is SchedulingPolicy.Fuzzing)
            {
                this.DeadlockMonitor.Change(TimeSpan.FromMilliseconds(this.Configuration.DeadlockTimeout),
                    Timeout.InfiniteTimeSpan);
            }

            thread.Name = Guid.NewGuid().ToString();
            thread.IsBackground = true;

            // TODO: optimize by using a real threadpool instead of creating a new thread each time.
            this.ThreadPool.AddOrUpdate(op.Id, thread, (id, oldThread) => thread);
            this.ControlledThreads.AddOrUpdate(thread.Name, op, (threadName, oldOp) => op);

            thread.Start();

            return this.CompletionSource.Task;
        }

        /// <summary>
        /// Creates a new controlled operation with an optional delay.
        /// </summary>
        private ControlledOperation CreateControlledOperation(uint delay = 0)
        {
            ulong operationId = this.GetNextOperationId();
            ControlledOperation op = delay > 0 ?
                new DelayOperation(operationId, $"Delay({operationId})", delay) :
                new ControlledOperation(operationId, $"Op({operationId})");
            this.RegisterOperation(op);
            if (operationId > 0 && this.IsThreadUncontrolled(Thread.CurrentThread))
            {
                op.IsSourceUncontrolled = true;
            }

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

            ControlledOperation op = this.CreateControlledOperation();
            var thread = new Thread(() =>
            {
                try
                {
                    // Install the runtime to the execution context of the current thread.
                    this.SetThreadExecutionContext();

                    // Update the controlled thread with the currently executing operation,
                    // allowing future retrieval in the same controlled thread.
                    ExecutingOperation.Value = op;

                    this.StartOperation(op);

                    if (this.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                    {
                        this.DelayOperation();
                    }

                    callback();
                    this.CompleteOperation(op);
                    this.ScheduleNextOperation(SchedulingPointType.Stop);
                }
                catch (Exception ex)
                {
                    this.ProcessUnhandledExceptionInOperation(op, ex);
                }
                finally
                {
                    // Clean the thread local state.
                    ExecutingOperation.Value = null;
                    AsyncLocalRuntime.Value = null;
                    ThreadLocalRuntime.Value = null;
                }
            });

            thread.Name = Guid.NewGuid().ToString();
            thread.IsBackground = true;

            // TODO: optimize by using a real threadpool instead of creating a new thread each time.
            this.ThreadPool.AddOrUpdate(op.Id, thread, (id, oldThread) => thread);
            this.ControlledThreads.AddOrUpdate(thread.Name, op, (threadName, oldOp) => op);

            thread.Start();

            this.WaitOperationStart(op);
            this.ScheduleNextOperation(SchedulingPointType.Create);
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

            // Check if an existing controlled operation is stored in the state of the task.
            ControlledOperation op = task.AsyncState as ControlledOperation ?? this.CreateControlledOperation();
            var thread = new Thread(() =>
            {
                try
                {
                    // Install the runtime to the execution context of the current thread.
                    this.SetThreadExecutionContext();

                    // Update the controlled thread with the currently executing operation,
                    // allowing future retrieval in the same controlled thread.
                    ExecutingOperation.Value = op;

                    this.StartOperation(op);

                    if (this.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                    {
                        this.DelayOperation();
                    }

                    this.ControlledTaskScheduler.ExecuteTask(task);

                    this.CompleteOperation(op);
                    this.ScheduleNextOperation(SchedulingPointType.Stop);
                }
                catch (Exception ex)
                {
                    this.ProcessUnhandledExceptionInOperation(op, ex);
                }
                finally
                {
                    // Clean the thread local state.
                    ExecutingOperation.Value = null;
                    AsyncLocalRuntime.Value = null;
                    ThreadLocalRuntime.Value = null;
                }
            });

            thread.Name = Guid.NewGuid().ToString();
            thread.IsBackground = true;

            // TODO: optimize by using a real threadpool instead of creating a new thread each time.
            this.ThreadPool.AddOrUpdate(op.Id, thread, (id, oldThread) => thread);
            this.ControlledThreads.AddOrUpdate(thread.Name, op, (threadName, oldOp) => op);
            this.ControlledTasks.TryAdd(task, op);

            thread.Start();

            this.WaitOperationStart(op);
            this.ScheduleNextOperation(SchedulingPointType.Create);
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
                ControlledOperation op = this.CreateControlledOperation(timeout);
                return this.TaskFactory.StartNew(state =>
                {
                    var delayedOp = state as ControlledOperation;
                    delayedOp.Status = OperationStatus.Delayed;
                    this.ScheduleNextOperation(SchedulingPointType.Yield);
                },
                op,
                cancellationToken,
                this.TaskFactory.CreationOptions | TaskCreationOptions.DenyChildAttach,
                this.TaskFactory.Scheduler);
            }

            var current = this.GetExecutingOperation<ControlledOperation>();
            if (current is null)
            {
                // Cannot fuzz the delay of an uncontrolled operation.
                return Task.Delay(delay, cancellationToken);
            }

            return Task.Delay(TimeSpan.FromMilliseconds(
                this.GetNondeterministicDelay(current, (int)this.Configuration.TimeoutDelay)));
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

            var callerOp = this.GetExecutingOperation<ControlledOperation>();
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

            var callerOp = this.GetExecutingOperation<ControlledOperation>();
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
        private void WaitUntilTasksComplete(ControlledOperation op, Task[] tasks, bool waitAll)
        {
            if (this.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                // In the case where `waitAll` is false (e.g. for `Task.WhenAny` or `Task.WaitAny`), we check if all
                // tasks are not completed. If that is the case, then we add all tasks to `Dependencies` and wait
                // at least one to complete. If, however, even one task is completed, then we should not wait, as it
                // can cause potential deadlocks.
                if (waitAll || tasks.All(task => !task.IsCompleted))
                {
                    foreach (var task in tasks)
                    {
                        if (!task.IsCompleted)
                        {
                            IO.Debug.WriteLine("<CoyoteDebug> Operation '{0}' is waiting for task '{1}'.", op.Id, task.Id);
                            op.SetDependency(task, this.ControlledTasks.ContainsKey(task));
                        }
                    }

                    if (op.Dependencies.Count > 0)
                    {
                        op.Status = waitAll ? OperationStatus.BlockedOnWaitAll : OperationStatus.BlockedOnWaitAny;
                        this.ScheduleNextOperation(SchedulingPointType.Join);
                    }
                }
            }
        }

        /// <summary>
        /// Blocks the currently executing operation until the task completes.
        /// </summary>
        internal void WaitUntilTaskCompletes(Task task)
        {
            if (!task.IsCompleted)
            {
                if (this.SchedulingPolicy is SchedulingPolicy.Systematic)
                {
                    // TODO: support timeouts and cancellation tokens.
                    if (!this.ControlledTasks.ContainsKey(task))
                    {
                        this.NotifyUncontrolledTaskWait(task);
                    }

                    var op = this.GetExecutingOperation<ControlledOperation>();
                    this.WaitUntilTaskCompletes(op, task);
                }
                else if (this.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                {
                    this.DelayOperation();
                }
            }
        }

        /// <summary>
        /// Blocks the specified operation until the task completes.
        /// </summary>
        internal void WaitUntilTaskCompletes(ControlledOperation op, Task task)
        {
            if (this.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                IO.Debug.WriteLine("<CoyoteDebug> Operation '{0}' is waiting for task '{1}'.", op.Id, task.Id);
                op.SetDependency(task, this.ControlledTasks.ContainsKey(task));
                op.Status = OperationStatus.BlockedOnWaitAll;
                this.ScheduleNextOperation(SchedulingPointType.Join);
            }
        }

        /// <summary>
        /// Unwraps the specified task.
        /// </summary>
        internal Task UnwrapTask(Task<Task> task)
        {
            var unwrappedTask = task.Unwrap();
            if (this.ControlledTasks.TryGetValue(task, out ControlledOperation op))
            {
                this.ControlledTasks.TryAdd(unwrappedTask, op);
            }

            return unwrappedTask;
        }

        /// <summary>
        /// Unwraps the specified task.
        /// </summary>
        internal Task<TResult> UnwrapTask<TResult>(Task<Task<TResult>> task)
        {
            var unwrappedTask = task.Unwrap();
            if (this.ControlledTasks.TryGetValue(task, out ControlledOperation op))
            {
                this.ControlledTasks.TryAdd(unwrappedTask, op);
            }

            return unwrappedTask;
        }

        /// <summary>
        /// Callback invoked when the continuation of an asynchronous state machine is scheduled.
        /// </summary>
        internal void OnAsyncStateMachineScheduleMoveNext(Task builderTask)
        {
            if (this.SchedulingPolicy != SchedulingPolicy.None)
            {
                this.ControlledTasks.TryAdd(builderTask, null);
            }
        }

        /// <summary>
        /// Registers the specified task as a known controlled task.
        /// </summary>
        internal void RegisterKnownControlledTask(Task task)
        {
            if (this.SchedulingPolicy != SchedulingPolicy.None)
            {
                this.ControlledTasks.TryAdd(task, null);
            }
        }

        /// <summary>
        /// Schedules the next enabled operation, which can include the currently executing operation.
        /// </summary>
        /// <param name="type">The type of the scheduling point.</param>
        /// <param name="isSuppressible">True if the interleaving can be suppressed, else false.</param>
        /// <param name="isYielding">True if the current operation is yielding, else false.</param>
        /// <remarks>
        /// An enabled operation is one that is not blocked nor completed.
        /// </remarks>
        internal void ScheduleNextOperation(SchedulingPointType type, bool isSuppressible = true, bool isYielding = false)
        {
            lock (this.SyncObject)
            {
                if (!this.IsAttached || this.SchedulingPolicy != SchedulingPolicy.Systematic)
                {
                    // Cannot schedule the next operation if the scheduler is not attached,
                    // or if the scheduling policy is not systematic.
                    return;
                }

                if (type is SchedulingPointType.Create)
                {
                    // If this scheduling point was triggered because a new operation was created by
                    // an uncontrolled thread, then the scheduling point must be postponed.
                    if (this.IsThreadUncontrolled(Thread.CurrentThread))
                    {
                        IO.Debug.WriteLine("<CoyoteDebug> Postponing scheduling point in uncontrolled thread '{0}'.",
                            Thread.CurrentThread.ManagedThreadId);
                        this.IsLastSchedulingPointPostponed = true;
                        return;
                    }
                }

                var current = this.GetExecutingOperation<ControlledOperation>();
                if (current is null)
                {
                    // Cannot schedule the next operation if there is no controlled operation
                    // executing on the current thread.
                    return;
                }

                if (current != this.ScheduledOperation)
                {
                    // The currently executing operation is not scheduled, so send it to sleep.
                    this.PauseOperation(current);
                    return;
                }

                if (this.IsSchedulerSuppressed && !this.IsLastSchedulingPointPostponed &&
                    isSuppressible && current.Status is OperationStatus.Enabled)
                {
                    // Suppress the scheduling point.
                    IO.Debug.WriteLine("<CoyoteDebug> Supressing scheduling point in operation '{0}'.", current.Name);
                    return;
                }

                // Checks if the scheduling steps bound has been reached.
                this.CheckIfSchedulingStepsBoundIsReached();

                // Update metadata related to this scheduling point.
                current.SchedulingPoint = type;
                this.IsLastSchedulingPointPostponed = false;

                if (this.Configuration.IsProgramStateHashingEnabled)
                {
                    // Update the current operation with the hashed program state.
                    current.HashedProgramState = this.GetHashedProgramState();
                }

                // Try to enable any operations with satisfied dependencies before asking the
                // scheduler to choose the next one to schedule.
                IEnumerable<ControlledOperation> ops = this.OperationMap.Values;
                if (this.TryEnableOperationsWithSatisfiedDependencies(current))
                {
                    // Order the operations by their id.
                    ops = ops.OrderBy(op => op.Id);
                }

                if (!this.Scheduler.GetNextOperation(ops, current, isYielding, out ControlledOperation next))
                {
                    // Check if the execution has deadlocked.
                    this.CheckIfExecutionHasDeadlocked(ops);
                    this.Detach(SchedulerDetachmentReason.BoundReached);
                }

                IO.Debug.WriteLine("<CoyoteDebug> Scheduling operation '{0}' (tid: {1}, msg: {2}).", next.Name, next.Thread, next.Msg);
                this.WriteDebugInfo(false);
                this.ScheduleTrace.AddSchedulingChoice(next.Id);
                if (current != next)
                {
                    // Pause the currently scheduled operation, and enable the next one.
                    this.ScheduledOperation = next;
                    SyncMonitor.PulseAll(this.SyncObject);
                    this.PauseOperation(current);
                }
            }
        }

        /// <summary>
        /// Suppresses scheduling points until <see cref="ResumeScheduling"/> is invoked,
        /// unless a scheduling point must occur naturally.
        /// </summary>
        internal void SuppressScheduling()
        {
            lock (this.SyncObject)
            {
                IO.Debug.WriteLine("<CoyoteDebug> Suppressing scheduling of enabled operations.");
                this.IsSchedulerSuppressed = true;
            }
        }

        /// <summary>
        /// Resumes scheduling points that were suppressed by invoking <see cref="SuppressScheduling"/>.
        /// </summary>
        internal void ResumeScheduling()
        {
            lock (this.SyncObject)
            {
                IO.Debug.WriteLine("<CoyoteDebug> Resuming scheduling of enabled operations.");
                this.IsSchedulerSuppressed = false;
            }
        }

        internal void EnableOps(string msg)
        {
            lock (this.SyncObject)
            {
                foreach (var op in this.OperationMap)
                {
                    if (op.Value.Status is OperationStatus.Disabled &&
                        (string.IsNullOrEmpty(msg) || op.Value.Msg.Contains(msg)))
                    {
                        // IO.Debug.WriteLine($"--- Enabling Op: {op.Value.Name} (tid: {op.Value.Thread})");
                        // IO.Debug.WriteLine($" |_ Msg: {op.Value.Msg}");
                        // IO.Debug.WriteLine($" |_ Stack Trace: {op.Value.StackTrace}");
                        op.Value.Status = OperationStatus.Enabled;
                    }
                }
            }
        }

        internal void DisableCurrentOp()
        {
            lock (this.SyncObject)
            {
                var currentOp = ExecutingOperation.Value;
                if (currentOp != null && currentOp.Status is OperationStatus.Enabled)
                {
                    // IO.Debug.WriteLine($"--- Disabling Op: {currentOp.Name} (tid: {currentOp.Thread})");
                    // IO.Debug.WriteLine($" |_ Msg: {currentOp.Msg}");
                    // IO.Debug.WriteLine($" |_ Stack Trace: {currentOp.StackTrace}");
                    currentOp.Status = OperationStatus.Disabled;
                }
            }
        }

        internal void SetDebugInfo(string msg)
        {
            lock (this.SyncObject)
            {
                var currentOp = ExecutingOperation.Value;
                if (currentOp != null)
                {
                    // IO.Debug.WriteLine("--- SET DEBUG INFO ---");
                    // IO.Debug.WriteLine($"--- Current Op: {currentOp.Name} (tid: {currentOp.Thread})");
                    var oldMsg = currentOp.Msg;
                    currentOp.Msg = $"{msg} ({oldMsg})";
                    // IO.Debug.WriteLine($" |_ Old msg: {oldMsg}");
                    // IO.Debug.WriteLine($" |_ New msg: {currentOp.Msg}");
                    // IO.Debug.WriteLine($" |_ Stack Trace: {currentOp.StackTrace}");
                }
            }
        }

        internal void WriteDebugInfo(bool fail)
        {
            lock (this.SyncObject)
            {
                // IO.Debug.WriteLine("--- DEBUG INFO ---");
                var currentOp = ExecutingOperation.Value;
                var enabledOps = this.OperationMap.Where(op => op.Value.Status is OperationStatus.Enabled);
                var disabledOps = this.OperationMap.Where(op => op.Value.Status is OperationStatus.Disabled);
                var blockedOps = this.OperationMap.Where(op => op.Value.Status is OperationStatus.BlockedOnWaitAll || op.Value.Status is OperationStatus.BlockedOnWaitAny);
                // IO.Debug.WriteLine($"--- Current Op: {currentOp?.Name} (tid: {currentOp?.Thread}, msg: {currentOp?.Msg})");
                // IO.Debug.WriteLine($"--- Enabled Ops: {enabledOps.Count()}");
                // IO.Debug.WriteLine($"--- Disabled Ops: {disabledOps.Count()}");
                // IO.Debug.WriteLine($"--- Blocked Ops: {blockedOps.Count()}");
                // IO.Debug.WriteLine($"--- Enabled PUT: {enabledOps.Where(op => op.Value.Msg.Contains("PUT /")).Count()}");
                // IO.Debug.WriteLine($"--- Enabled GET: {enabledOps.Where(op => op.Value.Msg.Contains("GET /")).Count()}");
                // IO.Debug.WriteLine($"--- Enabled DELETE: {enabledOps.Where(op => op.Value.Msg.Contains("DELETE /")).Count()}");
                // IO.Debug.WriteLine($"--- Enabled PATCH: {enabledOps.Where(op => op.Value.Msg.Contains("PATCH /")).Count()}");

                // foreach (var op in enabledOps)
                // {
                //     IO.Debug.WriteLine("   |_ Operation '{0}' (tid: {1}, msg: {2}) has status '{3}': {4}",
                //         op.Key, op.Value.Thread, op.Value.Msg, op.Value.Status, op.Value.StackTrace);
                // }
                // foreach (var op in disabledOps)
                // {
                //     IO.Debug.WriteLine("   |_ Operation '{0}' (tid: {1}, msg: {2}) has status '{3}': {4}",
                //         op.Key, op.Value.Thread, op.Value.Msg, op.Value.Status, op.Value.StackTrace);
                // }
                // foreach (var op in this.OperationMap.Where(
                //     op => op.Value.Status != OperationStatus.Enabled &&
                //     op.Value.Status != OperationStatus.Disabled &&
                //     op.Value.Status != OperationStatus.Completed))
                // {
                //     IO.Debug.WriteLine("   |_ Operation '{0}' (tid: {1}, msg: {2}) has status '{3}': {4}",
                //         op.Key, op.Value.Thread, op.Value.Msg, op.Value.Status, op.Value.StackTrace);
                // }

                if (fail)
                {
                    this.Detach(SchedulerDetachmentReason.BoundReached);
                }
            }
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
            int delay = 0;
            ControlledOperation current = null;
            lock (this.SyncObject)
            {
                if (!this.IsAttached)
                {
                    throw new ThreadInterruptedException();
                }

                current = this.GetExecutingOperation<ControlledOperation>();
                if (current != null)
                {
                    // Choose the next delay to inject. The value is in milliseconds.
                    delay = this.GetNondeterministicDelay(current, (int)this.Configuration.TimeoutDelay);
                    IO.Debug.WriteLine("<CoyoteDebug> Delaying operation '{0}' on thread '{1}' by {2}ms.",
                        current.Name, Thread.CurrentThread.ManagedThreadId, delay);
                }
            }

            // Only sleep the executing operation if a non-zero delay was chosen.
            if (delay > 0 && current != null)
            {
                var previousStatus = current.Status;
                current.Status = OperationStatus.Delayed;
                Thread.Sleep(delay);
                current.Status = previousStatus;
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
                this.GetExecutingOperation<ControlledOperation>();

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
                Console.WriteLine($"??? GetNextNondeterministicIntegerChoice: {new StackTrace()}");

                // Checks if the current operation is controlled by the runtime.
                this.GetExecutingOperation<ControlledOperation>();

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
        /// Returns a controlled nondeterministic delay for the specified operation.
        /// </summary>
        private int GetNondeterministicDelay(ControlledOperation op, int maxValue)
        {
            lock (this.SyncObject)
            {
                // Checks if the scheduling steps bound has been reached.
                this.CheckIfSchedulingStepsBoundIsReached();

                // Choose the next delay to inject.
                int maxDelay = maxValue > 0 ? (int)this.Configuration.TimeoutDelay : 1;
                if (!this.Scheduler.GetNextDelay(this.OperationMap.Values, op, maxDelay, out int next))
                {
                    this.Detach(SchedulerDetachmentReason.BoundReached);
                }

                return next;
            }
        }

        /// <summary>
        /// Registers the specified controlled operation.
        /// </summary>
        /// <param name="op">The operation to register.</param>
        internal void RegisterOperation(ControlledOperation op)
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
        /// Starts the execution of the specified controlled operation.
        /// </summary>
        /// <param name="op">The operation to start executing.</param>
        /// <remarks>
        /// This method performs a handshake with <see cref="WaitOperationStart"/>.
        /// </remarks>
        private void StartOperation(ControlledOperation op)
        {
            lock (this.SyncObject)
            {
                IO.Debug.WriteLine("<CoyoteDebug> Starting operation '{0}' on thread '{1}'.",
                    op.Name, Thread.CurrentThread.ManagedThreadId);

                // Enable the operation and store it in the async local context.
                op.Status = OperationStatus.Enabled;
                op.Thread = Thread.CurrentThread.ManagedThreadId;
                if (this.SchedulingPolicy is SchedulingPolicy.Systematic)
                {
                    SyncMonitor.PulseAll(this.SyncObject);
                    this.PauseOperation(op);
                }
            }
        }

        /// <summary>
        /// Waits for the specified controlled operation to start executing.
        /// </summary>
        /// <param name="op">The operation to wait.</param>
        /// <remarks>
        /// This method performs a handshake with <see cref="StartOperation"/>.
        /// </remarks>
        private void WaitOperationStart(ControlledOperation op)
        {
            lock (this.SyncObject)
            {
                if (this.SchedulingPolicy is SchedulingPolicy.Systematic && this.OperationMap.Count > 1)
                {
                    while (op.Status != OperationStatus.Enabled && this.IsAttached)
                    {
                        SyncMonitor.Wait(this.SyncObject);
                    }
                }
            }
        }

        /// <summary>
        /// Pauses the execution of the specified operation.
        /// </summary>
        /// <remarks>
        /// It is assumed that this method runs in the scope of a 'lock(this.SyncObject)' statement.
        /// </remarks>
        private void PauseOperation(ControlledOperation op)
        {
            if (op.Status is OperationStatus.Completed)
            {
                // The operation is completed, so no need to wait.
                return;
            }

            while (op != this.ScheduledOperation && this.IsAttached)
            {
                IO.Debug.WriteLine("<CoyoteDebug> Sleeping operation '{0}' on thread '{1}'.",
                    op.Name, Thread.CurrentThread.ManagedThreadId);
                SyncMonitor.Wait(this.SyncObject);
                IO.Debug.WriteLine("<CoyoteDebug> Waking up operation '{0}' on thread '{1}'.",
                    op.Name, Thread.CurrentThread.ManagedThreadId);
            }
        }

        /// <summary>
        /// Completes the specified operation.
        /// </summary>
        private void CompleteOperation(ControlledOperation op)
        {
            lock (this.SyncObject)
            {
                IO.Debug.WriteLine("<CoyoteDebug> Completed operation '{0}' on thread '{1}'.",
                    op.Name, Thread.CurrentThread.ManagedThreadId);
                op.Status = OperationStatus.Completed;
            }
        }

        /// <summary>
        /// Tries to enable any operations that have their dependencies satisfied. It returns
        /// true if there is at least one operation enabled, else false.
        /// </summary>
        /// <remarks>
        /// It is assumed that this method runs in the scope of a 'lock(this.SyncObject)' statement.
        /// </remarks>
        private bool TryEnableOperationsWithSatisfiedDependencies(ControlledOperation current)
        {
            IO.Debug.WriteLine("<CoyoteDebug> ===============================");
            IO.Debug.WriteLine("<CoyoteDebug> Trying to enable any operation with satisfied dependencies.");

            if (current.Status is OperationStatus.Completed && current.IsSourceUncontrolled)
            {
                IO.Debug.WriteLine("<CoyoteDebug> Operation '{0}' is completed with uncontrolled source.", current.Id);
            }

            int retries = 0;
            uint enabledOpsCount = 0;
            uint accumulatedDelay = 0;
            while (true)
            {
                // Cache the count of enabled operations from the previous attempt.
                uint previousEnabledOpsCount = enabledOpsCount;
                enabledOpsCount = 0;

                uint statusChanges = 0;
                int disabledOpsCount = 0;
                bool isRootDependencyUnresolved = false;
                bool isAnyDependencyUnresolved = false;
                foreach (var op in this.OperationMap.Values)
                {
                    var previousStatus = op.Status;
                    if (previousStatus != OperationStatus.None &&
                        previousStatus != OperationStatus.Enabled &&
                        previousStatus != OperationStatus.Completed)
                    {
                        this.TryEnableOperation(op);
                        if (previousStatus == op.Status)
                        {
                            IO.Debug.WriteLine("<CoyoteDebug> Operation '{0}' has status '{1}'.", op.Id, op.Status);
                            if (op.IsBlocked())
                            {
                                foreach (var dep in op.Dependencies)
                                {
                                    IO.Debug.WriteLine($"          |_ Dep: task '{(dep as Task)?.Id}'");
                                }
                            }

                            if (op.IsBlocked() && op.IsAnyDependencyUncontrolled)
                            {
                                IO.Debug.WriteLine("          |_ Op '{0}' is blocked with uncontrolled dependency.", op.Name);
                                if (op.IsRoot())
                                {
                                    isRootDependencyUnresolved = true;
                                }
                                else
                                {
                                    isAnyDependencyUnresolved = true;
                                }
                            }

                            if (previousStatus is OperationStatus.Disabled)
                            {
                                // op.Status = OperationStatus.Enabled;
                                disabledOpsCount++;
                            }
                        }
                        else
                        {
                            IO.Debug.WriteLine("<CoyoteDebug> Operation '{0}' changed status from '{1}' to '{2}'.",
                                op.Id, previousStatus, op.Status);
                            statusChanges++;
                        }
                    }

                    if (op.Status is OperationStatus.Enabled || op.Status is OperationStatus.Disabled)
                    {
                        enabledOpsCount++;
                    }
                }

                // Heuristics for handling a partially controlled execution.
                if (this.Configuration.IsPartiallyControlledConcurrencyEnabled)
                {
                    // Compute the delta of enabled operations from the previous attempt.
                    uint enabledOpsDelta = retries is 0 ? 0 : enabledOpsCount - previousEnabledOpsCount;

                    // This value is true if the current operation just completed and has uncontrolled source.
                    bool isSourceUnresolved = current.Status is OperationStatus.Completed && current.IsSourceUncontrolled;

                    // We consider the concurrency to be unresolved if there were no new enabled operations
                    // or status changes in this attempt, and one of the following cases holds:
                    // - If there are no enabled operations, then the concurrency is unresolved if
                    //   the current operation was just completed and has uncontrolled source, or
                    //   if there are any unresolved dependencies.
                    // - If there are enabled operations, then the concurrency is unresolved if
                    //   there are any (non-root) unresolved dependencies.
                    bool isNoEnabledOpsCaseResolved = enabledOpsCount is 0 &&
                        (isSourceUnresolved || isAnyDependencyUnresolved || isRootDependencyUnresolved);
                    bool isSomeEnabledOpsCaseResolved = enabledOpsCount > 0 && isAnyDependencyUnresolved;
                    bool isConcurrencyUnresolved = enabledOpsDelta is 0 && statusChanges is 0 &&
                        (isNoEnabledOpsCaseResolved || isSomeEnabledOpsCaseResolved);

                    // Retry if there is unresolved concurrency and any retries left, or if there are no enabled
                    // operations and the accumulated delay is less than the specified deadlock timeout limit.
                    if ((++retries < 5 && isConcurrencyUnresolved) ||
                        (enabledOpsCount is 0 && accumulatedDelay < this.Configuration.DeadlockTimeout))
                    {
                        // Implement a simple retry logic to try resolve uncontrolled concurrency.
                        IO.Debug.WriteLine(
                            "<CoyoteDebug> Pausing operation '{0}' on thread '{1}' to try resolve uncontrolled concurrency.",
                            this.ScheduledOperation.Name, Thread.CurrentThread.ManagedThreadId);
                        uint delay = this.Configuration.UncontrolledConcurrencyTimeout;
                        SyncMonitor.Wait(this.SyncObject, (int)delay);
                        accumulatedDelay += delay;
                        continue;
                    }
                }

                break;
            }

            uint y = 0;
            foreach (var x in this.OperationMap.Values)
            {
                if (x.Status is OperationStatus.Disabled)
                {
                    // x.Status = OperationStatus.Enabled;
                    y++;
                }
            }

            if (enabledOpsCount == y)
            {
                RuntimeStats.NumOnlyDisabledOperations++;
            }

            IO.Debug.WriteLine("<CoyoteDebug> There are {0} enabled operations.", enabledOpsCount);
            this.MaxConcurrencyDegree = Math.Max(this.MaxConcurrencyDegree, enabledOpsCount);
            return enabledOpsCount > 0;
        }

        /// <summary>
        /// Tries to enable the specified operation, if its dependencies have been satisfied.
        /// </summary>
        /// <remarks>
        /// It is assumed that this method runs in the scope of a 'lock (this.SyncObject)' statement.
        /// </remarks>
        private bool TryEnableOperation(ControlledOperation op)
        {
            if (op.Status is OperationStatus.Delayed && op is DelayOperation delayedOp)
            {
                if (delayedOp.Delay > 0)
                {
                    delayedOp.Delay--;
                }

                // The operation is delayed, so it is enabled either if the delay completes
                // or if no other operation is enabled.
                if (delayedOp.Delay is 0 ||
                    !this.OperationMap.Any(kvp => kvp.Value.Status is OperationStatus.Enabled))
                {
                    delayedOp.Delay = 0;
                    delayedOp.Status = OperationStatus.Enabled;
                    return true;
                }

                return false;
            }

            // If this is the root operation, then only try enable it if all actor operations (if any) are
            // completed. This is required because in tests that include actors, actors can execute without
            // the main task explicitly waiting for them to terminate or reach quiescence. Otherwise, if the
            // root operation was enabled, the test can terminate early.
            if (op.IsRoot() && this.OperationMap.Any(
                kvp => kvp.Value is ActorOperation && kvp.Value.Status != OperationStatus.Completed))
            {
                return false;
            }

            // If the operation is blocked on one or more tasks, then check if the tasks have completed.
            if ((op.Status is OperationStatus.BlockedOnWaitAll &&
                op.Dependencies.All(dependency => dependency is Task task && task.IsCompleted)) ||
                (op.Status is OperationStatus.BlockedOnWaitAny &&
                op.Dependencies.Any(dependency => dependency is Task task && task.IsCompleted)))
            {
                op.Unblock();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Pauses the scheduled controlled operation until either the uncontrolled task completes,
        /// it tries to invoke an uncontrolled scheduling point, or the timeout expires.
        /// </summary>
        /// <remarks>
        /// It is assumed that this method runs in the scope of a 'lock(this.SyncObject)' statement.
        /// </remarks>
        private void TryPauseAndResolveUncontrolledTask(Task task)
        {
            ControlledOperation op = ExecutingOperation.Value;
            if (op != null)
            {
                IO.Debug.WriteLine("<CoyoteDebug> ===============================");
                // A scheduling point from an uncontrolled thread has not been postponed yet, so pause the execution
                // of the current operation to try give time to the uncontrolled concurrency to be resolved.
                if (!this.IsLastSchedulingPointPostponed)
                {
                    IO.Debug.WriteLine(
                        "<CoyoteDebug> Pausing operation '{0}' on thread '{1}' to try resolve uncontrolled concurrency.",
                        op.Name, Thread.CurrentThread.ManagedThreadId);
                    int retries = 0;
                    int delay = (int)this.Configuration.UncontrolledConcurrencyTimeout;
                    while (retries++ < 10 && !task.IsCompleted)
                    {
                        SyncMonitor.Wait(this.SyncObject, delay);
                        if (this.IsLastSchedulingPointPostponed)
                        {
                            // A scheduling point from an uncontrolled thread has been postponed,
                            // so stop trying to resolve the uncontrolled concurrency.
                            break;
                        }
                    }
                }

                if (this.IsLastSchedulingPointPostponed)
                {
                    IO.Debug.WriteLine(
                        "<CoyoteDebug> Resuming operation '{0}' on thread '{1}' with uncontrolled concurrency resolved.",
                        op.Name, Thread.CurrentThread.ManagedThreadId);
                    this.ScheduleNextOperation(SchedulingPointType.Default, isSuppressible: false);
                }

                IO.Debug.WriteLine("<CoyoteDebug> ===============================");
            }
        }

        /// <summary>
        /// Gets the <see cref="ControlledOperation"/> that is currently executing,
        /// or null if no such operation is executing.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal TControlledOperation GetExecutingOperation<TControlledOperation>()
            where TControlledOperation : ControlledOperation
        {
            lock (this.SyncObject)
            {
                var op = ExecutingOperation.Value;
                if (op is null)
                {
                    this.NotifyUncontrolledCurrentThread();
                }

                return op is TControlledOperation expected ? expected : default;
            }
        }

        /// <summary>
        /// Gets the <see cref="ControlledOperation"/> associated with the specified
        /// unique id, or null if no such operation exists.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal TControlledOperation GetOperationWithId<TControlledOperation>(ulong id)
            where TControlledOperation : ControlledOperation
        {
            lock (this.SyncObject)
            {
                if (this.OperationMap.TryGetValue(id, out ControlledOperation op) &&
                    op is TControlledOperation expected)
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
        private IEnumerable<ControlledOperation> GetRegisteredOperations()
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
                        int operationHash = 31 + actorOperation.Actor.GetHashedState(this.SchedulingPolicy);
                        operationHash = (operationHash * 31) + actorOperation.SchedulingPoint.GetHashCode();
                        hash *= operationHash;
                    }
                    else
                    {
                        hash *= 31 + operation.SchedulingPoint.GetHashCode();
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
                    IO.Debug.WriteLine($"<CoyoteDebug> {message}");
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
        /// Returns true if the specified thread is uncontrolled, else false.
        /// </summary>
        internal bool IsThreadUncontrolled(Thread thread)
        {
            string name = thread?.Name;
            return name is null || !this.ControlledThreads.ContainsKey(name);
        }

        /// <summary>
        /// Returns true if the specified task is uncontrolled, else false.
        /// </summary>
        internal bool IsTaskUncontrolled(Task task) =>
            task != null && !task.IsCompleted && !this.ControlledTasks.ContainsKey(task);

        /// <summary>
        /// Checks if the task returned from the specified method is uncontrolled.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void CheckIfReturnedTaskIsUncontrolled(Task task, string methodName)
        {
            if (this.IsTaskUncontrolled(task))
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
        private void CheckIfExecutionHasDeadlocked(IEnumerable<ControlledOperation> ops)
        {
            var blockedOnReceiveOperations = ops.Where(op => op.Status is OperationStatus.BlockedOnReceive).ToList();
            var blockedOnWaitOperations = ops.Where(op => op.Status is OperationStatus.BlockedOnWaitAll ||
                op.Status is OperationStatus.BlockedOnWaitAny).ToList();
            var blockedOnResources = ops.Where(op => op.Status is OperationStatus.BlockedOnResource).ToList();

            var totalCount = blockedOnReceiveOperations.Count + blockedOnWaitOperations.Count + blockedOnResources.Count;
            if (totalCount is 0)
            {
                return;
            }

            // To simplify the error message, remove the root operation, unless it is the only one that is blocked.
            if (totalCount > 1)
            {
                blockedOnReceiveOperations.RemoveAll(op => op.IsRoot());
                blockedOnWaitOperations.RemoveAll(op => op.IsRoot());
                blockedOnResources.RemoveAll(op => op.IsRoot());
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

            foreach (var op in ops)
            {
                if (op.Status != OperationStatus.None &&
                    op.Status != OperationStatus.Enabled &&
                    op.Status != OperationStatus.Completed)
                {
                    msg.AppendFormat("<CoyoteDebug> Operation '{0}' has status '{1}'.\n", op.Name, op.Status);
                    if (op.IsBlocked())
                    {
                        foreach (var dep in op.Dependencies)
                        {
                            msg.AppendLine($"          |_ Dep: task '{(dep as Task)?.Id}'");
                        }
                    }

                    if (op.IsBlocked() && op.IsAnyDependencyUncontrolled)
                    {
                        msg.AppendFormat("          |_ Op '{0}' is blocked with uncontrolled dependency.\n", op.Name);
                    }

                    if (op.IsSourceUncontrolled)
                    {
                        msg.AppendFormat("          |_ Op '{0}' has uncontrolled source.\n", op.Name);
                    }
                }
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
            if (!this.IsBugFound && this.OperationMap.All(kvp => kvp.Value.Status is OperationStatus.Completed))
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
        /// Notify that the currently executing thread is uncontrolled.
        /// </summary>
        private void NotifyUncontrolledCurrentThread()
        {
            if (this.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                // TODO: figure out if there is a way to get more information about the creator of the
                // uncontrolled thread to ease the user debugging experience.
                // Report the invalid operation and then throw it to fail the current thread. This will
                // most likely crash the program, but we try to fail as cleanly and fast as possible.
                string message = $"Executing thread '{Thread.CurrentThread.ManagedThreadId}' is not intercepted and " +
                    "controlled during testing, so it can interfere with the ability to reproduce bug traces.";
                if (this.Configuration.IsPartiallyControlledConcurrencyEnabled)
                {
                    this.Logger.WriteLine($"<TestLog> {message}");
                }
                else if (this.Configuration.IsConcurrencyFuzzingFallbackEnabled)
                {
                    this.Logger.WriteLine($"<TestLog> {message}");
                    this.IsUncontrolledConcurrencyDetected = true;
                    this.Detach(SchedulerDetachmentReason.UncontrolledConcurrencyDetected);
                }
                else
                {
                    this.NotifyAssertionFailure(FormatUncontrolledConcurrencyExceptionMessage(message, 3));
                }
            }
        }

        /// <summary>
        /// Notify that an uncontrolled task is being waited.
        /// </summary>
        private void NotifyUncontrolledTaskWait(Task task)
        {
            lock (this.SyncObject)
            {
                if (this.SchedulingPolicy is SchedulingPolicy.Systematic)
                {
                    // TODO: figure out if there is a way to get more information about the creator of the
                    // uncontrolled task to ease the user debugging experience.
                    // Report the invalid operation and then throw it to fail the current thread. This will
                    // most likely crash the program, but we try to fail as cleanly and fast as possible.
                    string message = $"Waiting task '{task.Id}' that is not intercepted and controlled during " +
                        "testing, so it can interfere with the ability to reproduce bug traces.";
                    if (this.Configuration.IsPartiallyControlledConcurrencyEnabled)
                    {
                        this.Logger.WriteLine($"<TestLog> {message}");
                        if (this.UncontrolledTasks.Add(task))
                        {
                            this.TryPauseAndResolveUncontrolledTask(task);
                        }
                    }
                    else if (this.Configuration.IsConcurrencyFuzzingFallbackEnabled)
                    {
                        this.Logger.WriteLine($"<TestLog> {message}");
                        this.IsUncontrolledConcurrencyDetected = true;
                        this.Detach(SchedulerDetachmentReason.UncontrolledConcurrencyDetected);
                    }
                    else
                    {
                        this.NotifyAssertionFailure(FormatUncontrolledConcurrencyExceptionMessage(message, 3));
                    }
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
                    if (this.Configuration.IsPartiallyControlledConcurrencyEnabled)
                    {
                        this.Logger.WriteLine($"<TestLog> {message}");
                        if (this.UncontrolledTasks.Add(task))
                        {
                            this.TryPauseAndResolveUncontrolledTask(task);
                        }
                    }
                    else if (this.Configuration.IsConcurrencyFuzzingFallbackEnabled)
                    {
                        this.Logger.WriteLine($"<TestLog> {message}");
                        this.IsUncontrolledConcurrencyDetected = true;
                        this.Detach(SchedulerDetachmentReason.UncontrolledConcurrencyDetected);
                    }
                    else
                    {
                        this.NotifyAssertionFailure(FormatUncontrolledConcurrencyExceptionMessage(message, 4, methodName));
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
                    if (this.Configuration.IsPartiallyControlledConcurrencyEnabled)
                    {
                        this.Logger.WriteLine($"<TestLog> {message}");
                    }
                    else if (this.Configuration.IsConcurrencyFuzzingFallbackEnabled)
                    {
                        this.Logger.WriteLine($"<TestLog> {message}");
                        this.IsUncontrolledConcurrencyDetected = true;
                        this.Detach(SchedulerDetachmentReason.UncontrolledConcurrencyDetected);
                    }
                    else
                    {
                        this.NotifyAssertionFailure(FormatUncontrolledConcurrencyExceptionMessage(message, 3, methodName));
                    }
                }
            }
        }

        /// <summary>
        /// Formats the message of the uncontrolled concurrency exception.
        /// </summary>
        private static string FormatUncontrolledConcurrencyExceptionMessage(string message,
            int skipStackFrames, string methodName = default)
        {
            var mockMessage = methodName is null ? string.Empty : $" either replace or mock '{methodName}', or";
            return $"{message} As a workaround, you can{mockMessage} use the '--no-repro' command line option " +
                "(or the 'Configuration.WithNoBugTraceRepro()' method) to ignore this error by disabling bug " +
                $"trace repro. Learn more at http://aka.ms/coyote-no-repro.\n{new StackTrace(skipStackFrames)}";
        }

        /// <summary>
        /// Processes an unhandled exception in the specified controlled operation.
        /// </summary>
        internal void ProcessUnhandledExceptionInOperation(ControlledOperation op, Exception exception)
        {
            // Complete the failed operation. This is required so that the operation
            // does not throw if it detaches.
            op.Status = OperationStatus.Completed;
            if (exception.GetBaseException() is ThreadInterruptedException)
            {
                // Ignore this exception, its thrown by the runtime.
                IO.Debug.WriteLine("<CoyoteDebug> Controlled thread '{0}' executing operation '{1}' was interrupted.",
                    Thread.CurrentThread.ManagedThreadId, op.Name);
            }
            else
            {
                string message;
                string trace = FormatExceptionStackTrace(exception);
                if (op is ActorOperation actorOp)
                {
                    message = string.Format(CultureInfo.InvariantCulture,
                        $"Unhandled exception in actor '{actorOp.Name}'. {trace}");
                }
                else
                {
                    message = $"Unhandled exception. {trace}";
                }

                // Report the unhandled exception.
                this.NotifyUnhandledException(exception, message);
            }
        }

        /// <summary>
        /// Formats the stack trace of the specified exception.
        /// </summary>
        private static string FormatExceptionStackTrace(Exception exception)
        {
            string[] lines = exception.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].StartsWith("   at Microsoft.Coyote.Rewriting", StringComparison.Ordinal))
                {
                    lines[i] = string.Empty;
                }
            }

            return string.Join(Environment.NewLine, lines.Where(line => !string.IsNullOrEmpty(line)));
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
                report.SetSchedulingStatistics(this.IsBugFound, this.BugReport, this.OperationMap.Count,
                    (int)this.MaxConcurrencyDegree, this.Scheduler.StepCount, this.Scheduler.IsMaxStepsReached,
                    this.Scheduler.IsScheduleFair);
                if (this.IsBugFound)
                {
                    report.SetUnhandledException(this.UnhandledException);
                }

                report.SetUncontrolledInvocations(this.UncontrolledInvocations);
            }
        }

        /// <summary>
        /// Sets this runtime instance to the execution context of the current thread,
        /// allowing future retrieval in the same thread, as well as across threads in
        /// the same asynchronous control flow.
        /// </summary>
        private void SetThreadExecutionContext()
        {
            ThreadLocalRuntime.Value = this;
            AsyncLocalRuntime.Value = this;
            this.SetControlledSynchronizationContext();
        }

        /// <summary>
        /// Sets the synchronization context to the controlled synchronization context.
        /// </summary>
        private void SetControlledSynchronizationContext() =>
            SynchronizationContext.SetSynchronizationContext(this.SyncContext);

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
                    if (op.Status != OperationStatus.Completed)
                    {
                        op.Status = OperationStatus.Completed;

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
                RuntimeProvider.Deregister(this.Id);

                this.ThreadPool.Clear();
                this.OperationMap.Clear();
                this.ControlledThreads.Clear();
                this.ControlledTasks.Clear();
                this.UncontrolledTasks.Clear();
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
