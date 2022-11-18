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
using Microsoft.Coyote.Coverage;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Runtime.CompilerServices;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Testing;
using SpecMonitor = Microsoft.Coyote.Specifications.Monitor;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Runtime for controlling, scheduling and executing asynchronous operations.
    /// </summary>
    /// <remarks>
    /// Invoking scheduling methods is thread-safe.
    /// </remarks>
    internal sealed class CoyoteRuntime : ICoyoteRuntime, IDisposable
    {
        /// <summary>
        /// Provides access to the runtime associated with each controlled thread, or null
        /// if the current thread is not controlled.
        /// </summary>
        /// <remarks>
        /// In testing mode, each testing iteration uses a unique runtime instance. To safely
        /// retrieve it from static methods, we store it in each controlled thread local state.
        /// </remarks>
        [ThreadStatic]
        private static CoyoteRuntime ThreadLocalRuntime;

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
        /// The runtime installed in the current execution context.
        /// </summary>
        internal static CoyoteRuntime Current =>
            ThreadLocalRuntime ?? AsyncLocalRuntime.Value ?? RuntimeProvider.Default;

        /// <summary>
        /// Provides access to the operation executing on each controlled thread
        /// during systematic testing.
        /// </summary>
        [ThreadStatic]
        private static ControlledOperation ExecutingOperation;

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
        internal readonly Configuration Configuration;

        /// <summary>
        /// Scheduler that controls the execution of operations during testing.
        /// </summary>
        private readonly OperationScheduler Scheduler;

        /// <summary>
        /// The operation scheduling policy used by the runtime.
        /// </summary>
        internal SchedulingPolicy SchedulingPolicy => this.Scheduler?.SchedulingPolicy ??
            SchedulingPolicy.None;

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
        /// Pool of threads that execute controlled operations.
        /// </summary>
        private readonly ConcurrentDictionary<ulong, Thread> ThreadPool;

        /// <summary>
        /// Map from unique operation ids to asynchronous operations.
        /// </summary>
        private readonly Dictionary<ulong, ControlledOperation> OperationMap;

        /// <summary>
        /// Map from newly created operations that have not started executing yet
        /// to an event handler that is set when the operation starts.
        /// </summary>
        private readonly Dictionary<ControlledOperation, ManualResetEventSlim> PendingStartOperationMap;

        /// <summary>
        /// Map from unique controlled thread names to their corresponding operations.
        /// </summary>
        private readonly ConcurrentDictionary<string, ControlledOperation> ControlledThreads;

        /// <summary>
        /// Map from controlled tasks to their corresponding operations.
        /// </summary>
        private readonly ConcurrentDictionary<Task, ControlledOperation> ControlledTasks;

        /// <summary>
        /// Map from known uncontrolled tasks to an optional string with debug information.
        /// </summary>
        private readonly ConcurrentDictionary<Task, string> UncontrolledTasks;

        /// <summary>
        /// Set of method calls with uncontrolled concurrency or other sources of nondeterminism.
        /// </summary>
        private readonly HashSet<string> UncontrolledInvocations;

        /// <summary>
        /// The currently scheduled operation during systematic testing.
        /// </summary>
        private ControlledOperation ScheduledOperation;

        /// <summary>
        /// The installed runtime extension, which by default is the <see cref="NullRuntimeExtension"/>.
        /// </summary>
        internal readonly IRuntimeExtension Extension;

        /// <summary>
        /// Data structure containing information regarding testing coverage.
        /// </summary>
        internal readonly CoverageInfo CoverageInfo;

        /// <summary>
        /// Responsible for generating random values.
        /// </summary>
        internal readonly IRandomValueGenerator ValueGenerator;

        /// <summary>
        /// Responsible for writing to the installed <see cref="ILogger"/>.
        /// </summary>
        internal readonly LogWriter LogWriter;

        /// <inheritdoc/>
        public ILogger Logger
        {
            get => this.LogWriter;
            set => this.LogWriter.SetLogger(value);
        }

        /// <summary>
        /// Manages all registered <see cref="IRuntimeLog"/> objects.
        /// </summary>
        internal readonly LogManager LogManager;

        /// <summary>
        /// List of all registered safety and liveness specification monitors.
        /// </summary>
        private readonly List<SpecMonitor> SpecificationMonitors;

        /// <summary>
        /// List of all registered task liveness monitors.
        /// </summary>
        private readonly List<TaskLivenessMonitor> TaskLivenessMonitors;

        /// <summary>
        /// The runtime completion source.
        /// </summary>
        private readonly TaskCompletionSource<bool> CompletionSource;

        /// <summary>
        /// Object that is used to synchronize access to the runtime.
        /// </summary>
        private readonly object RuntimeLock;

        /// <summary>
        /// Produces tokens for canceling asynchronous operations when the runtime detaches.
        /// </summary>
        private readonly CancellationTokenSource CancellationSource;

        /// <summary>
        /// Monotonically increasing operation id counter.
        /// </summary>
        private long OperationIdCounter;

        /// <summary>
        /// Records if the runtime is running.
        /// </summary>
        internal volatile bool IsRunning;

        /// <summary>
        /// The execution status of the runtime.
        /// </summary>
        internal ExecutionStatus ExecutionStatus { get; private set; }

        /// <summary>
        /// If this value is not null, then it represents the last scheduling point that
        /// was postponed, which the runtime will try to schedule in the next available
        /// thread that invokes a scheduling point.
        /// </summary>
        /// <remarks>
        /// A scheduling point can be postponed in two scenarios. The first scenario is
        /// when an uncontrolled thread creates a new controlled operation and tries to
        /// schedule it, but this is only allowed from a controlled thread. In this case,
        /// the runtime will resume scheduling from the next available controlled thread.
        /// The second scenario is when a controlled operation waits or completes, but a
        /// potential deadlock is found due to uncontrolled concurrency that has not been
        /// resolved yet. In this case, the runtime will resume scheduling from the next
        /// available uncontrolled thread, unless there is a genuine deadlock.
        /// </remarks>
        private SchedulingPointType? LastPostponedSchedulingPoint;

        /// <summary>
        /// Value that suppresses interleavings of enabled operations when it is non-zero.
        /// </summary>
        private uint ScheduleSuppressionCount;

        /// <summary>
        /// True if the runtime is currently executing inside a specification, else false.
        /// </summary>
        private bool IsSpecificationInvoked;

        /// <summary>
        /// True if uncontrolled concurrency was detected, else false.
        /// </summary>
        private bool IsUncontrolledConcurrencyDetected;

        /// <summary>
        /// Associated with the bug report is an optional unhandled exception.
        /// </summary>
        private Exception UnhandledException;

        /// <summary>
        /// The max number of operations that were enabled at the same time.
        /// </summary>
        private uint MaxConcurrencyDegree;

        /// <summary>
        /// Bug report.
        /// </summary>
        internal string BugReport { get; private set; }

        /// <inheritdoc/>
        public event OnFailureHandler OnFailure;

        /// <summary>
        /// Initializes a new instance of the <see cref="CoyoteRuntime"/> class.
        /// </summary>
        internal static CoyoteRuntime Create(Configuration configuration, IRandomValueGenerator valueGenerator,
            LogWriter logWriter, LogManager logManager, IRuntimeExtension extension) =>
            new CoyoteRuntime(configuration, null, valueGenerator, logWriter, logManager, extension);

        /// <summary>
        /// Initializes a new instance of the <see cref="CoyoteRuntime"/> class.
        /// </summary>
        internal static CoyoteRuntime Create(Configuration configuration, OperationScheduler scheduler,
            LogWriter logWriter, LogManager logManager, IRuntimeExtension extension) =>
            new CoyoteRuntime(configuration, scheduler, scheduler.ValueGenerator, logWriter, logManager, extension);

        /// <summary>
        /// Initializes a new instance of the <see cref="CoyoteRuntime"/> class.
        /// </summary>
        private CoyoteRuntime(Configuration configuration, OperationScheduler scheduler, IRandomValueGenerator valueGenerator,
            LogWriter logWriter, LogManager logManager, IRuntimeExtension extension)
        {
            // Registers the runtime with the provider which in return assigns a unique identifier.
            this.Id = RuntimeProvider.Register(this);

            this.Configuration = configuration;
            this.Scheduler = scheduler;
            this.RuntimeLock = new object();
            this.CancellationSource = new CancellationTokenSource();
            this.OperationIdCounter = 0;
            this.IsRunning = true;
            this.ExecutionStatus = ExecutionStatus.Running;
            this.ScheduleSuppressionCount = 0;
            this.IsSpecificationInvoked = false;
            this.IsUncontrolledConcurrencyDetected = false;
            this.LastPostponedSchedulingPoint = null;
            this.MaxConcurrencyDegree = 0;

            this.ThreadPool = new ConcurrentDictionary<ulong, Thread>();
            this.OperationMap = new Dictionary<ulong, ControlledOperation>();
            this.PendingStartOperationMap = new Dictionary<ControlledOperation, ManualResetEventSlim>();
            this.ControlledThreads = new ConcurrentDictionary<string, ControlledOperation>();
            this.ControlledTasks = new ConcurrentDictionary<Task, ControlledOperation>();
            this.UncontrolledTasks = new ConcurrentDictionary<Task, string>();
            this.UncontrolledInvocations = new HashSet<string>();
            this.CompletionSource = new TaskCompletionSource<bool>();

            if (this.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                Interlocked.Increment(ref ExecutionControlledUseCount);
            }

            this.Extension = extension ?? NullRuntimeExtension.Instance;
            this.CoverageInfo = this.Extension.GetCoverageInfo() ?? new CoverageInfo();
            this.ValueGenerator = valueGenerator;
            this.LogWriter = logWriter;
            this.LogManager = logManager;
            this.SpecificationMonitors = new List<SpecMonitor>();
            this.TaskLivenessMonitors = new List<TaskLivenessMonitor>();

            this.ControlledTaskScheduler = new ControlledTaskScheduler(this);
            this.SyncContext = new ControlledSynchronizationContext(this);
            this.TaskFactory = new TaskFactory(CancellationToken.None, TaskCreationOptions.HideScheduler,
                TaskContinuationOptions.HideScheduler, this.ControlledTaskScheduler);
        }

        /// <summary>
        /// Runs the specified test method.
        /// </summary>
        internal Task RunTestAsync(Delegate testMethod, string testName)
        {
            this.LogWriter.LogInfo("[coyote::test] Runtime '{0}' started {1} on thread '{2}' using the '{3}' strategy.",
                this.Id, string.IsNullOrEmpty(testName) ? "the test" : $"'{testName}'",
                Thread.CurrentThread.ManagedThreadId, this.Scheduler.GetStrategyName());
            this.Assert(testMethod != null, "Unable to execute a null test method.");

            ControlledOperation op = this.CreateControlledOperation();
            this.ScheduleOperation(op, () =>
            {
                Task task = Task.CompletedTask;
                if (this.Extension.RunTest(testMethod, out Task extensionTask))
                {
                    task = extensionTask;
                }
                else if (testMethod is Action action)
                {
                    action();
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
                this.RegisterKnownControlledTask(task);
                TaskServices.WaitUntilTaskCompletes(this, op, task);
                task.GetAwaiter().GetResult();

                // Wait for any operations managed by the runtime extension to reach quiescence and propagate any exceptions.
                // This is required in tests that use a runtime extension so that the test does not terminate early, because
                // the main thread can complete without waiting for the extended operations to reach quiescence.
                Task extensionQuiescenceTask = this.Extension.WaitUntilQuiescenceAsync();
                this.RegisterKnownControlledTask(extensionQuiescenceTask);
                TaskServices.WaitUntilTaskCompletes(this, op, extensionQuiescenceTask);
                extensionQuiescenceTask.GetAwaiter().GetResult();
            },
            postCondition: () =>
            {
                using (SynchronizedSection.Enter(this.RuntimeLock))
                {
                    // Checks for any liveness errors at test termination.
                    this.CheckLivenessErrors();
                    this.Detach(ExecutionStatus.PathExplored);
                }
            });

            // Start running a background monitor that checks for potential deadlocks. This
            // mechanism is defensive for cases where there is uncontrolled concurrency or
            // synchronization primitives, and happens in conjunction with the deterministic
            // deadlock detection mechanism when scheduling controlled operations.
            this.StartMonitoringDeadlocks();
            return this.CompletionSource.Task;
        }

        /// <summary>
        /// Schedules the specified task to execute on the controlled thread pool.
        /// </summary>
        internal void Schedule(Task task)
        {
            // Check if an existing controlled operation is stored in the state of the task.
            ControlledOperation op;
            if (task.AsyncState is ControlledOperation existingOp)
            {
                op = existingOp;
                this.TryResetOperation(op);
            }
            else
            {
                op = this.CreateControlledOperation();
            }

            // Register this task as a known controlled task.
            this.ControlledTasks.TryAdd(task, op);

            this.ScheduleOperation(op, () => this.ControlledTaskScheduler.ExecuteTask(task));
            this.ScheduleNextOperation(default, SchedulingPointType.Create);
        }

        /// <summary>
        /// Schedules the specified continuation to execute on the controlled thread pool.
        /// </summary>
        internal void Schedule(Action continuation)
        {
            ControlledOperation op = this.CreateControlledOperation(group: ExecutingOperation?.Group);
            this.ScheduleOperation(op, continuation);
            this.ScheduleNextOperation(default, SchedulingPointType.ContinueWith);
        }

        /// <summary>
        /// Schedules the specified operation to execute on the controlled thread pool. The operation
        /// executes the given action alongside an optional pre-condition and post-condition.
        /// </summary>
        private void ScheduleOperation(ControlledOperation op, Action action, Action preCondition = null, Action postCondition = null)
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                if (this.ExecutionStatus != ExecutionStatus.Running)
                {
                    return;
                }

                // Create a new thread that is instrumented to control and execute the operation.
                var thread = new Thread(() =>
                {
                    try
                    {
                        // Execute the optional pre-condition.
                        preCondition?.Invoke();

                        // Start the operation.
                        this.StartOperation(op);

                        // If fuzzing is enabled, and this is not the first started operation,
                        // then try to delay it to explore race conditions.
                        if (this.SchedulingPolicy is SchedulingPolicy.Fuzzing && op.Id > 0)
                        {
                            this.DelayOperation(op);
                        }

                        // Execute the controlled action.
                        action.Invoke();

                        // Complete the operation and schedule the next enabled operation.
                        this.CompleteOperation(op);

                        // Execute the optional post-condition.
                        postCondition?.Invoke();

                        // Schedule the next operation, if there is one enabled.
                        this.ScheduleNextOperation(op, SchedulingPointType.Complete);
                    }
                    catch (Exception ex)
                    {
                        this.ProcessUnhandledExceptionInOperation(op, ex);
                    }
                    finally
                    {
                        CleanCurrentExecutionContext();
                    }
                });

                thread.Name = Guid.NewGuid().ToString();
                thread.IsBackground = true;

                // TODO: optimize by reusing threads instead of creating a new thread each time?
                this.ThreadPool.AddOrUpdate(op.Id, thread, (id, oldThread) => thread);
                this.ControlledThreads.AddOrUpdate(thread.Name, op, (threadName, oldOp) => op);

                thread.Start();
            }
        }

        /// <summary>
        /// Schedules the specified delay to be executed asynchronously.
        /// </summary>
        internal Task ScheduleDelay(TimeSpan delay, CancellationToken cancellationToken)
        {
            if (delay.TotalMilliseconds is 0)
            {
                // If the delay is 0, then complete synchronously.
                return Task.CompletedTask;
            }

            // TODO: support cancellations during testing.
            if (this.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                uint timeout = (uint)this.GetNextNondeterministicIntegerChoice((int)this.Configuration.TimeoutDelay, null, null);
                if (timeout is 0)
                {
                    // If the delay is 0, then complete synchronously.
                    return Task.CompletedTask;
                }

                // TODO: cache the dummy delay action to optimize memory.
                // TODO: figure out a good strategy for grouping delays, especially if they
                // are shared in different contexts and not awaited immediately.
                ControlledOperation op = this.CreateControlledOperation(group: ExecutingOperation?.Group, delay: timeout);
                return this.TaskFactory.StartNew(state =>
                {
                    var delayedOp = state as ControlledOperation;
                    delayedOp.Status = OperationStatus.PausedOnDelay;
                    this.ScheduleNextOperation(delayedOp, SchedulingPointType.Yield);
                },
                op,
                cancellationToken,
                this.TaskFactory.CreationOptions | TaskCreationOptions.DenyChildAttach,
                this.TaskFactory.Scheduler);
            }

            if (!this.TryGetExecutingOperation(out ControlledOperation current))
            {
                // Cannot fuzz the delay of an uncontrolled operation.
                return Task.Delay(delay, cancellationToken);
            }

            // TODO: we need to come up with something better!
            // Fuzz the delay.
            return Task.Delay(TimeSpan.FromMilliseconds(
                this.GetNondeterministicDelay(current, (int)delay.TotalMilliseconds)));
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
        /// Registers the specified task as a known uncontrolled task.
        /// </summary>
        internal void RegisterKnownUncontrolledTask(Task task, string methodName)
        {
            if (this.SchedulingPolicy != SchedulingPolicy.None)
            {
                this.UncontrolledTasks.TryAdd(task, methodName);
            }
        }

        /// <summary>
        /// Creates a new controlled operation with the specified group and an optional delay.
        /// </summary>
        internal ControlledOperation CreateControlledOperation(OperationGroup group = null, uint delay = 0)
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                // Create a new controlled operation using the next available operation id.
                ulong operationId = this.GetNextOperationId();
                ControlledOperation op = delay > 0 ?
                    new DelayOperation(operationId, $"Delay({operationId})", delay, group, this) :
                    new ControlledOperation(operationId, $"Op({operationId})", group, this);
                if (operationId > 0 && !this.IsThreadControlled(Thread.CurrentThread))
                {
                    op.IsSourceUncontrolled = true;
                }

                return op;
            }
        }

        /// <summary>
        /// Creates a new user-defined controlled operation from the specified builder.
        /// </summary>
        internal ControlledOperation CreateUserDefinedOperation(IOperationBuilder builder)
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                // Create a new controlled operation using the next available operation id.
                ulong operationId = this.GetNextOperationId();
                var op = new UserDefinedOperation(this, builder, operationId);
                if (operationId > 0 && !this.IsThreadControlled(Thread.CurrentThread))
                {
                    op.IsSourceUncontrolled = true;
                }

                return op;
            }
        }

        /// <summary>
        /// Registers the specified newly created controlled operation.
        /// </summary>
        /// <param name="op">The newly created operation to register.</param>
        internal void RegisterNewOperation(ControlledOperation op)
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                if (this.ExecutionStatus != ExecutionStatus.Running)
                {
                    return;
                }

#if NETSTANDARD2_0 || NETFRAMEWORK
                if (!this.OperationMap.ContainsKey(op.Id))
                {
                    this.OperationMap.Add(op.Id, op);
                }
#else
                this.OperationMap.TryAdd(op.Id, op);
#endif

                // Assign the operation as a member of its group.
                op.Group.RegisterMember(op);
                if (this.OperationMap.Count is 1)
                {
                    // This is the first operation registered, so schedule it.
                    this.ScheduledOperation = op;
                }
                else
                {
                    // As this is not the first operation getting created, assign an event
                    // handler so that the next scheduling decision cannot be made until
                    // this operation starts executing to avoid race conditions.
                    this.PendingStartOperationMap.Add(op, new ManualResetEventSlim(false));
                }

                this.LogWriter.LogDebug("[coyote::debug] Created operation '{0}' of group '{1}' on thread '{2}'.",
                    op.Name, op.Group, Thread.CurrentThread.ManagedThreadId);
            }
        }

        /// <summary>
        /// Starts the execution of the specified controlled operation.
        /// </summary>
        /// <param name="op">The operation to start executing.</param>
        /// <remarks>
        /// This method performs a handshake with <see cref="WaitOperationsStart"/>.
        /// </remarks>
        internal void StartOperation(ControlledOperation op)
        {
            // Configures the execution context of the current thread with data
            // related to the runtime and the operation executed by this thread.
            this.SetCurrentExecutionContext(op);
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                this.LogWriter.LogDebug("[coyote::debug] Started operation '{0}' of group '{1}' on thread '{2}'.",
                    op.Name, op.Group, Thread.CurrentThread.ManagedThreadId);
                op.Status = OperationStatus.Enabled;
                if (this.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    // If this operation has an associated handler that notifies another awaiting
                    // operation about this operation starting its execution, then set the handler.
                    if (this.PendingStartOperationMap.TryGetValue(op, out ManualResetEventSlim handler))
                    {
                        handler.Set();
                    }

                    // Pause the operation as soon as it starts executing to allow the runtime
                    // to explore a potential interleaving with another executing operation.
                    this.PauseOperation(op);
                }
            }
        }

        /// <summary>
        /// Waits for all recently created operations to start executing.
        /// </summary>
        /// <remarks>
        /// This method performs a handshake with <see cref="StartOperation"/>. It is assumed that this
        /// method is invoked by the same thread executing the operation and that it runs in the scope
        /// of a <see cref="SynchronizedSection"/>.
        /// </remarks>
        private void WaitOperationsStart()
        {
            if (this.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                while (this.PendingStartOperationMap.Count > 0)
                {
                    var pendingOp = this.PendingStartOperationMap.First();
                    while (pendingOp.Key.Status is OperationStatus.None)
                    {
                        this.LogWriter.LogDebug("[coyote::debug] Sleeping thread '{0}' until operation '{1}' of group '{2}' starts.",
                            Thread.CurrentThread.ManagedThreadId, pendingOp.Key.Name, pendingOp.Key.Group);
                        using (SynchronizedSection.Exit(this.RuntimeLock))
                        {
                            try
                            {
                                pendingOp.Value.Wait();
                            }
                            catch (ObjectDisposedException)
                            {
                                // The handler was disposed, so we can ignore this exception.
                            }
                        }

                        this.LogWriter.LogDebug("[coyote::debug] Waking up thread '{0}'.", Thread.CurrentThread.ManagedThreadId);
                    }

                    pendingOp.Value.Dispose();
                    this.PendingStartOperationMap.Remove(pendingOp.Key);
                }
            }
        }

        /// <summary>
        /// Pauses the execution of the specified operation.
        /// </summary>
        /// <remarks>
        /// It is assumed that this method is invoked by the same thread executing the operation
        /// and that it runs in the scope of a <see cref="SynchronizedSection"/>.
        /// </remarks>
        private void PauseOperation(ControlledOperation op)
        {
            // Only pause the operation if it is not already completed and it is currently executing on this thread.
            if (op.Status != OperationStatus.Completed && op == ExecutingOperation)
            {
                // Do not allow the operation to wake up, unless its currently scheduled and enabled or the runtime stopped running.
                while (!(op == this.ScheduledOperation && op.Status is OperationStatus.Enabled) && this.ExecutionStatus is ExecutionStatus.Running)
                {
                    this.LogWriter.LogDebug("[coyote::debug] Sleeping operation '{0}' of group '{1}' on thread '{2}'.",
                        op.Name, op.Group, Thread.CurrentThread.ManagedThreadId);
                    using (SynchronizedSection.Exit(this.RuntimeLock))
                    {
                        op.WaitSignal();
                    }

                    this.LogWriter.LogDebug("[coyote::debug] Waking up operation '{0}' of group '{1}' on thread '{2}'.",
                        op.Name, op.Group, Thread.CurrentThread.ManagedThreadId);
                }
            }
        }

        /// <summary>
        /// Pauses the currently executing operation until the specified condition gets resolved.
        /// </summary>
        internal void PauseOperationUntil(ControlledOperation current, Func<bool> condition, bool isConditionControlled = true, string debugMsg = null)
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                if (this.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    // Only proceed if there is an operation executing on the current thread and
                    // the condition is not already resolved.
                    current ??= this.GetExecutingOperation();
                    while (current != null && !condition() && this.ExecutionStatus is ExecutionStatus.Running)
                    {
                        this.LogWriter.LogDebug("[coyote::debug] Operation '{0}' of group '{1}' is waiting for {2} on thread '{3}'.",
                            current.Name, current.Group, debugMsg ?? "condition to get resolved", Thread.CurrentThread.ManagedThreadId);
                        // TODO: can we identify when the dependency is uncontrolled?
                        current.PauseWithDependency(condition, isConditionControlled);
                        this.ScheduleNextOperation(current, SchedulingPointType.Pause);
                    }
                }
            }
        }

        /// <summary>
        /// Asynchronously pauses the currently executing operation until the specified condition gets resolved.
        /// </summary>
        internal PausedOperationAwaitable PauseOperationUntilAsync(Func<bool> condition, bool resumeAsynchronously)
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                if (this.SchedulingPolicy is SchedulingPolicy.Interleaving &&
                    this.TryGetExecutingOperation(out ControlledOperation current))
                {
                    return new PausedOperationAwaitable(this, current, condition, resumeAsynchronously);
                }
            }

            return new PausedOperationAwaitable(this, null, condition, resumeAsynchronously);
        }

        /// <summary>
        /// Schedules the next enabled operation, which can include the currently executing operation.
        /// </summary>
        /// <param name="current">The currently executing operation, if there is one.</param>
        /// <param name="type">The type of the scheduling point.</param>
        /// <param name="isSuppressible">True if the interleaving can be suppressed, else false.</param>
        /// <param name="isYielding">True if the current operation is yielding, else false.</param>
        /// <returns>True if an operation other than the current was scheduled, else false.</returns>
        /// <remarks>
        /// An enabled operation is one that is not paused nor completed.
        /// </remarks>
        internal bool ScheduleNextOperation(ControlledOperation current, SchedulingPointType type,
            bool isSuppressible = true, bool isYielding = false)
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                // Wait for all recently created operations to start executing.
                this.WaitOperationsStart();
                if (this.ExecutionStatus != ExecutionStatus.Running ||
                    this.SchedulingPolicy != SchedulingPolicy.Interleaving)
                {
                    // Cannot schedule the next operation if the scheduler is not attached,
                    // or if the scheduling policy is not systematic.
                    return false;
                }

                // Check if the currently executing thread is uncontrolled.
                bool isThreadUncontrolled = false;
                if (current is null && !this.IsThreadControlled(Thread.CurrentThread))
                {
                    if (this.LastPostponedSchedulingPoint is SchedulingPointType.Pause ||
                        this.LastPostponedSchedulingPoint is SchedulingPointType.Complete)
                    {
                        // A scheduling point was postponed due to a potential deadlock, which has
                        // now been resolved, so resume it on this uncontrolled thread.
                        current = this.ScheduledOperation;
                        type = this.LastPostponedSchedulingPoint.Value;
                        this.LogWriter.LogDebug("[coyote::debug] Resuming scheduling point '{0}' of operation '{1}' in uncontrolled thread '{2}'.",
                            type, current, Thread.CurrentThread.ManagedThreadId);
                    }
                    else if (type is SchedulingPointType.Create || type is SchedulingPointType.ContinueWith)
                    {
                        // This is a scheduling point that was invoked because a new operation was
                        // created by an uncontrolled thread, so postpone the scheduling point and
                        // resume it on the next available controlled thread.
                        this.LogWriter.LogDebug("[coyote::debug] Postponing scheduling point '{0}' in uncontrolled thread '{1}'.",
                            type, Thread.CurrentThread.ManagedThreadId);
                        this.LastPostponedSchedulingPoint = type;
                        return false;
                    }

                    isThreadUncontrolled = true;
                }

                // If the current operation was provided as argument to this method, or it is null, then this
                // is a controlled thread, so get the currently executing operation to proceed with scheduling.
                current ??= this.GetExecutingOperation();
                if (current is null)
                {
                    // Cannot proceed without having access to the currently executing operation.
                    return false;
                }

                if (current != this.ScheduledOperation)
                {
                    // The currently executing operation is not scheduled, so send it to sleep.
                    this.PauseOperation(current);
                    return false;
                }

                if (this.ScheduleSuppressionCount > 0 && this.LastPostponedSchedulingPoint is null &&
                    isSuppressible && current.Status is OperationStatus.Enabled)
                {
                    // Suppress the scheduling point.
                    this.LogWriter.LogDebug("[coyote::debug] Suppressing scheduling point in operation '{0}'.", current.Name);
                    return false;
                }

                this.LogWriter.LogDebug("[coyote::debug] Invoking scheduling point '{0}' at execution step '{1}'.", type, this.Scheduler.StepCount);
                this.Assert(!this.IsSpecificationInvoked, "Executing a specification monitor must be atomic.");

                // Checks if the scheduling steps bound has been reached.
                this.CheckIfSchedulingStepsBoundIsReached();

                // Update metadata related to this scheduling point.
                current.LastSchedulingPoint = type;
                this.LastPostponedSchedulingPoint = null;

                if (this.Configuration.IsProgramStateHashingEnabled)
                {
                    // Update the current operation with the hashed program state.
                    current.LastHashedProgramState = this.GetHashedProgramState();
                }

                // Try to enable any operations with resolved dependencies before asking the
                // scheduler to choose the next one to schedule.
                IEnumerable<ControlledOperation> ops = this.OperationMap.Values;
                if (!this.TryEnableOperationsWithResolvedDependencies(current))
                {
                    if (this.IsUncontrolledConcurrencyDetected &&
                        this.Configuration.IsPartiallyControlledConcurrencyAllowed)
                    {
                        // TODO: optimize and make this more fine-grained.
                        // If uncontrolled concurrency is detected, then do not check for deadlocks directly,
                        // but instead leave it to the background deadlock detection timer and postpone the
                        // scheduling point, which might get resolved from an uncontrolled thread.
                        this.LogWriter.LogDebug("[coyote::debug] Postponing scheduling point '{0}' of operation '{1}' due to potential deadlock.",
                            type, current);
                        this.LastPostponedSchedulingPoint = type;
                        this.PauseOperation(current);
                        return false;
                    }

                    // Check if the execution has deadlocked.
                    this.CheckIfExecutionHasDeadlocked(ops);
                }

                if (this.Configuration.IsLivenessCheckingEnabled && this.Scheduler.IsIterationFair)
                {
                    // Check if the liveness threshold has been reached if scheduling is fair.
                    this.CheckLivenessThresholdExceeded();
                }

                if (!this.Scheduler.GetNextOperation(ops, current, isYielding, out ControlledOperation next))
                {
                    // The scheduler hit the scheduling steps bound.
                    this.Detach(ExecutionStatus.BoundReached);
                    return false;
                }

                this.LogWriter.LogDebug("[coyote::debug] Scheduling operation '{0}' of group '{1}'.", next.Name, next.Group);
                bool isNextOperationScheduled = current != next;
                if (isNextOperationScheduled)
                {
                    // Pause the currently scheduled operation, and enable the next one.
                    this.ScheduledOperation = next;
                    next.Signal();
                    this.PauseOperation(current);
                }
                else if (isThreadUncontrolled)
                {
                    // If the current operation is the next operation to schedule, and the current thread
                    // is uncontrolled, then we need to signal the current operation to resume execution.
                    next.Signal();
                }

                return isNextOperationScheduled;
            }
        }

        /// <summary>
        /// Delays the currently executing operation for a non-deterministically chosen amount of time.
        /// </summary>
        /// <remarks>
        /// The delay is chosen non-deterministically by an underlying fuzzing strategy.
        /// If a delay of 0 is chosen, then the operation is not delayed.
        /// </remarks>
        internal void DelayOperation(ControlledOperation current)
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                if (this.ExecutionStatus != ExecutionStatus.Running)
                {
                    throw new ThreadInterruptedException();
                }

                if (current != null || this.TryGetExecutingOperation(out current))
                {
                    // Choose the next delay to inject. The value is in milliseconds.
                    int delay = this.GetNondeterministicDelay(current, (int)this.Configuration.MaxFuzzingDelay);
                    this.LogWriter.LogDebug("[coyote::debug] Delaying operation '{0}' on thread '{1}' by {2}ms.",
                        current.Name, Thread.CurrentThread.ManagedThreadId, delay);

                    // Only sleep the executing operation if a non-zero delay was chosen.
                    if (delay > 0)
                    {
                        var previousStatus = current.Status;
                        current.Status = OperationStatus.PausedOnDelay;
                        using (SynchronizedSection.Exit(this.RuntimeLock))
                        {
                            Thread.SpinWait(delay);
                        }

                        current.Status = previousStatus;
                    }
                }
            }
        }

        /// <summary>
        /// Completes the specified operation.
        /// </summary>
        internal void CompleteOperation(ControlledOperation op)
        {
            op.ExecuteContinuations();
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                this.LogWriter.LogDebug("[coyote::debug] Completed operation '{0}' of group '{1}' on thread '{2}'.",
                    op.Name, op.Group, Thread.CurrentThread.ManagedThreadId);
                op.Status = OperationStatus.Completed;
            }
        }

        /// <summary>
        /// Tries to reset the specified controlled operation so that it can start executing again.
        /// This is only allowed if the operation is already completed.
        /// </summary>
        /// <param name="op">The operation to reset.</param>
        internal bool TryResetOperation(ControlledOperation op)
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                if (op.Status is OperationStatus.Completed)
                {
                    this.LogWriter.LogDebug("[coyote::debug] Resetting operation '{0}' of group '{1}' from thread '{2}'.",
                        op.Name, op.Group, Thread.CurrentThread.ManagedThreadId);
                    op.Status = OperationStatus.None;
                    if (this.SchedulingPolicy is SchedulingPolicy.Interleaving)
                    {
                        // Assign an event handler so that the next scheduling decision cannot be
                        // made until this operation starts executing to avoid race conditions.
                        this.PendingStartOperationMap.Add(op, new ManualResetEventSlim(false));
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Suppresses scheduling points until <see cref="ResumeScheduling"/> is invoked,
        /// unless a scheduling point must occur naturally.
        /// </summary>
        internal void SuppressScheduling()
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                this.LogWriter.LogDebug("[coyote::debug] Suppressing scheduling of enabled operations in runtime '{0}'.", this.Id);
                this.ScheduleSuppressionCount++;
            }
        }

        /// <summary>
        /// Resumes scheduling points that were suppressed by invoking <see cref="SuppressScheduling"/>.
        /// </summary>
        internal void ResumeScheduling()
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                this.LogWriter.LogDebug("[coyote::debug] Resuming scheduling of enabled operations in runtime '{0}'.", this.Id);
                if (this.ScheduleSuppressionCount > 0)
                {
                    this.ScheduleSuppressionCount--;
                }
            }
        }

        /// <summary>
        /// Sets a checkpoint in the currently explored execution trace, that allows replaying all
        /// scheduling decisions until the checkpoint in subsequent iterations.
        /// </summary>
        internal void CheckpointExecutionTrace()
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                ExecutionTrace trace = this.Scheduler.CheckpointExecutionTrace();
                this.LogWriter.LogDebug("[coyote::debug] Set checkpoint in current execution path with length '{0}' in runtime '{1}'.",
                    trace.Length, this.Id);
            }
        }

        /// <inheritdoc/>
        public bool RandomBoolean() => this.GetNextNondeterministicBooleanChoice(null, null);

        /// <summary>
        /// Returns the next nondeterministic boolean choice.
        /// </summary>
        internal bool GetNextNondeterministicBooleanChoice(string callerName, string callerType)
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                bool result;
                if (this.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    // Checks if the current operation is controlled by the runtime.
                    this.GetExecutingOperation();

                    // Checks if the scheduling steps bound has been reached.
                    this.CheckIfSchedulingStepsBoundIsReached();

                    if (this.SchedulingPolicy is SchedulingPolicy.Interleaving &&
                        this.Configuration.IsLivenessCheckingEnabled && this.Scheduler.IsIterationFair)
                    {
                        // Check if the liveness threshold has been reached if scheduling is fair.
                        this.CheckLivenessThresholdExceeded();
                    }

                    if (this.Configuration.IsProgramStateHashingEnabled)
                    {
                        // Update the current operation with the hashed program state.
                        this.ScheduledOperation.LastHashedProgramState = this.GetHashedProgramState();
                    }

                    if (!this.Scheduler.GetNextBoolean(this.ScheduledOperation, out result))
                    {
                        this.Detach(ExecutionStatus.BoundReached);
                    }
                }
                else
                {
                    result = this.ValueGenerator.Next(2) is 0 ? true : false;
                }

                this.LogManager.LogRandom(result, callerName, callerType);
                return result;
            }
        }

        /// <inheritdoc/>
        public int RandomInteger(int maxValue) => this.GetNextNondeterministicIntegerChoice(maxValue, null, null);

        /// <summary>
        /// Returns the next nondeterministic integer choice.
        /// </summary>
        internal int GetNextNondeterministicIntegerChoice(int maxValue, string callerName, string callerType)
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                int result;
                if (this.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    // Checks if the current operation is controlled by the runtime.
                    this.GetExecutingOperation();

                    // Checks if the scheduling steps bound has been reached.
                    this.CheckIfSchedulingStepsBoundIsReached();

                    if (this.SchedulingPolicy is SchedulingPolicy.Interleaving &&
                        this.Configuration.IsLivenessCheckingEnabled && this.Scheduler.IsIterationFair)
                    {
                        // Check if the liveness threshold has been reached if scheduling is fair.
                        this.CheckLivenessThresholdExceeded();
                    }

                    if (this.Configuration.IsProgramStateHashingEnabled)
                    {
                        // Update the current operation with the hashed program state.
                        this.ScheduledOperation.LastHashedProgramState = this.GetHashedProgramState();
                    }

                    if (!this.Scheduler.GetNextInteger(this.ScheduledOperation, maxValue, out result))
                    {
                        this.Detach(ExecutionStatus.BoundReached);
                    }
                }
                else
                {
                    result = this.ValueGenerator.Next(maxValue);
                }

                this.LogManager.LogRandom(result, callerName, callerType);
                return result;
            }
        }

        /// <summary>
        /// Returns a controlled nondeterministic delay for the specified operation.
        /// </summary>
        private int GetNondeterministicDelay(ControlledOperation op, int maxDelay)
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                // Checks if the scheduling steps bound has been reached.
                this.CheckIfSchedulingStepsBoundIsReached();

                // Choose the next delay to inject.
                if (!this.Scheduler.GetNextDelay(this.OperationMap.Values, op, maxDelay, out int next))
                {
                    this.Detach(ExecutionStatus.BoundReached);
                }

                return next;
            }
        }

        /// <summary>
        /// Tries to enable any operations that have their dependencies resolved. It returns
        /// true if there is at least one operation enabled, else false.
        /// </summary>
        /// <remarks>
        /// It is assumed that this method runs in the scope of a <see cref="SynchronizedSection"/>.
        /// </remarks>
        private bool TryEnableOperationsWithResolvedDependencies(ControlledOperation current)
        {
            this.LogWriter.LogDebug("[coyote::debug] Trying to enable any operation with resolved dependencies in runtime '{0}'.", this.Id);

            int attempt = 0;
            int delay = (int)this.Configuration.UncontrolledConcurrencyResolutionDelay;
            uint maxAttempts = this.Configuration.UncontrolledConcurrencyResolutionAttempts;
            uint enabledOpsCount = 0;
            while (true)
            {
                // Cache the count of enabled operations from the previous attempt.
                uint previousEnabledOpsCount = enabledOpsCount;
                enabledOpsCount = 0;

                uint statusChanges = 0;
                bool isRootDependencyUnresolved = false;
                bool isAnyDependencyUnresolved = false;
                foreach (var op in this.OperationMap.Values)
                {
                    var previousStatus = op.Status;
                    if (op.IsPaused)
                    {
                        this.TryEnableOperation(op);
                        if (previousStatus == op.Status)
                        {
                            this.LogWriter.LogDebug("[coyote::debug] Operation '{0}' of group '{1}' has status '{2}'.",
                                op.Name, op.Group, op.Status);
                            if (op.IsPaused && op.IsDependencyUncontrolled)
                            {
                                if (op.IsRoot)
                                {
                                    isRootDependencyUnresolved = true;
                                }
                                else
                                {
                                    isAnyDependencyUnresolved = true;
                                }
                            }
                        }
                        else
                        {
                            this.LogWriter.LogDebug("[coyote::debug] Operation '{0}' of group '{1}' changed status from '{2}' to '{3}'.",
                                op.Name, op.Group, previousStatus, op.Status);
                            statusChanges++;
                        }
                    }

                    if (op.Status is OperationStatus.Enabled)
                    {
                        enabledOpsCount++;
                    }
                }

                // Heuristics for handling a partially controlled execution.
                if (this.IsUncontrolledConcurrencyDetected &&
                    this.Configuration.IsPartiallyControlledConcurrencyAllowed)
                {
                    // Compute the delta of enabled operations from the previous attempt.
                    uint enabledOpsDelta = attempt is 0 ? 0 : enabledOpsCount - previousEnabledOpsCount;

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

                    // Retry if there is unresolved concurrency and attempts left.
                    if (++attempt < maxAttempts && isConcurrencyUnresolved)
                    {
                        // Implement a simple retry logic to try resolve uncontrolled concurrency.
                        this.LogWriter.LogDebug("[coyote::debug] Pausing controlled thread '{0}' to try resolve uncontrolled concurrency.",
                            Thread.CurrentThread.ManagedThreadId);
                        using (SynchronizedSection.Exit(this.RuntimeLock))
                        {
                            Thread.SpinWait(delay);
                        }

                        continue;
                    }
                }

                break;
            }

            this.LogWriter.LogDebug("[coyote::debug] There are {0} enabled operations in runtime '{1}'.",
                enabledOpsCount, this.Id);
            this.MaxConcurrencyDegree = Math.Max(this.MaxConcurrencyDegree, enabledOpsCount);
            return enabledOpsCount > 0;
        }

        /// <summary>
        /// Tries to enable the specified operation, if its dependencies have been resolved.
        /// </summary>
        /// <remarks>
        /// It is assumed that this method runs in the scope of a <see cref="SynchronizedSection"/>.
        /// </remarks>
        private bool TryEnableOperation(ControlledOperation op)
        {
            if (op.Status is OperationStatus.PausedOnDelay && op is DelayOperation delayedOp)
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

            // If the operation is paused, then check if its dependency has been resolved.
            return op.TryEnable();
        }

        /// <summary>
        /// Pauses the scheduled controlled operation until either the uncontrolled task completes,
        /// it tries to invoke an uncontrolled scheduling point, or the timeout expires.
        /// </summary>
        /// <remarks>
        /// It is assumed that this method runs in the scope of a <see cref="SynchronizedSection"/>.
        /// </remarks>
        private void TryPauseAndResolveUncontrolledTask(Task task)
        {
            if (this.IsThreadControlled(Thread.CurrentThread))
            {
                // A scheduling point from an uncontrolled thread has not been postponed yet, so pause the execution
                // of the current operation to try give time to the uncontrolled concurrency to be resolved.
                if (this.LastPostponedSchedulingPoint is null)
                {
                    int attempt = 0;
                    int delay = (int)this.Configuration.UncontrolledConcurrencyResolutionDelay;
                    uint maxAttempts = this.Configuration.UncontrolledConcurrencyResolutionAttempts;
                    while (attempt++ < maxAttempts && !task.IsCompleted)
                    {
                        this.LogWriter.LogDebug("[coyote::debug] Pausing controlled thread '{0}' to try resolve uncontrolled concurrency.",
                            Thread.CurrentThread.ManagedThreadId);
                        using (SynchronizedSection.Exit(this.RuntimeLock))
                        {
                            Thread.SpinWait(delay);
                        }

                        if (this.LastPostponedSchedulingPoint.HasValue)
                        {
                            // A scheduling point from an uncontrolled thread has been postponed,
                            // so stop trying to resolve the uncontrolled concurrency.
                            break;
                        }
                    }
                }

                if (this.LastPostponedSchedulingPoint.HasValue)
                {
                    this.LogWriter.LogDebug("[coyote::debug] Resuming controlled thread '{0}' with uncontrolled concurrency resolved.",
                        Thread.CurrentThread.ManagedThreadId);
                    this.ScheduleNextOperation(default, this.LastPostponedSchedulingPoint.Value, isSuppressible: false);
                }
            }
        }

        /// <summary>
        /// Returns the currently executing <see cref="ControlledOperation"/>,
        /// or null if no such operation is executing.
        /// </summary>
        internal ControlledOperation GetExecutingOperation()
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                var op = ExecutingOperation;
                if (op is null)
                {
                    this.NotifyUncontrolledCurrentThread();
                }

                return op;
            }
        }

        /// <summary>
        /// Returns the currently executing <see cref="ControlledOperation"/> of the
        /// specified type, or null if no such operation is executing.
        /// </summary>
        internal TControlledOperation GetExecutingOperation<TControlledOperation>()
            where TControlledOperation : ControlledOperation
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                var op = ExecutingOperation;
                if (op is null)
                {
                    this.NotifyUncontrolledCurrentThread();
                }

                return op is TControlledOperation expected ? expected : default;
            }
        }

        /// <summary>
        /// Tries to return the currently executing <see cref="ControlledOperation"/>,
        /// or false if no such operation is executing.
        /// </summary>
        internal bool TryGetExecutingOperation(out ControlledOperation op)
        {
            op = this.GetExecutingOperation();
            return op != null;
        }

        /// <summary>
        /// Returns the <see cref="ControlledOperation"/> associated with the specified
        /// operation id, or null if no such operation exists.
        /// </summary>
        internal ControlledOperation GetOperationWithId(ulong operationId)
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                this.OperationMap.TryGetValue(operationId, out ControlledOperation op);
                return op;
            }
        }

        /// <summary>
        /// Returns the <see cref="ControlledOperation"/> of the specified type that is associated
        /// with the specified operation id, or null if no such operation exists.
        /// </summary>
        internal TControlledOperation GetOperationWithId<TControlledOperation>(ulong operationId)
            where TControlledOperation : ControlledOperation
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                if (this.OperationMap.TryGetValue(operationId, out ControlledOperation op) &&
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
            using (SynchronizedSection.Enter(this.RuntimeLock))
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
        private int GetHashedProgramState()
        {
            unchecked
            {
                int hash = 19;
                foreach (var operation in this.GetRegisteredOperations())
                {
                    hash *= 31 + operation.GetHashedState(this.SchedulingPolicy);
                }

                foreach (var monitor in this.SpecificationMonitors)
                {
                    hash *= 31 + monitor.GetHashedState();
                }

                return hash;
            }
        }

        /// <inheritdoc/>
        public void RegisterMonitor<T>()
            where T : SpecMonitor =>
            this.TryCreateMonitor(typeof(T));

        /// <summary>
        /// Tries to create a new <see cref="SpecMonitor"/> of the specified <see cref="Type"/>.
        /// </summary>
        private bool TryCreateMonitor(Type type)
        {
            if (this.SchedulingPolicy != SchedulingPolicy.None ||
                this.Configuration.IsMonitoringEnabledOutsideTesting)
            {
                using (SynchronizedSection.Enter(this.RuntimeLock))
                {
                    // Only one monitor per type is allowed.
                    if (!this.SpecificationMonitors.Any(m => m.GetType() == type))
                    {
                        var monitor = (SpecMonitor)Activator.CreateInstance(type);
                        monitor.Initialize(this.Configuration, this);
                        monitor.InitializeStateInformation();
                        this.SpecificationMonitors.Add(monitor);
                        if (this.SchedulingPolicy is SchedulingPolicy.Interleaving)
                        {
                            this.SuppressScheduling();
                            this.IsSpecificationInvoked = true;
                            monitor.GotoStartState();
                            this.IsSpecificationInvoked = false;
                            this.ResumeScheduling();
                        }
                        else
                        {
                            monitor.GotoStartState();
                        }

                        return true;
                    }
                }
            }

            return false;
        }

        /// <inheritdoc/>
        public void Monitor<T>(SpecMonitor.Event e)
            where T : SpecMonitor =>
            this.InvokeMonitor(typeof(T), e, null, null, null);

        /// <summary>
        /// Invokes the specified <see cref="SpecMonitor"/> with the specified <see cref="SpecMonitor.Event"/>.
        /// </summary>
        internal void InvokeMonitor(Type type, SpecMonitor.Event e, string senderName, string senderType, string senderStateName)
        {
            if (this.SchedulingPolicy != SchedulingPolicy.None ||
                this.Configuration.IsMonitoringEnabledOutsideTesting)
            {
                using (SynchronizedSection.Enter(this.RuntimeLock))
                {
                    SpecMonitor monitor = null;
                    foreach (var m in this.SpecificationMonitors)
                    {
                        if (m.GetType() == type)
                        {
                            monitor = m;
                            break;
                        }
                    }

                    if (monitor != null)
                    {
                        this.Assert(e != null, "Cannot invoke monitor '{0}' with a null event.", type.FullName);
                        if (this.SchedulingPolicy is SchedulingPolicy.Interleaving)
                        {
                            this.SuppressScheduling();
                            this.IsSpecificationInvoked = true;
                            monitor.MonitorEvent(e, senderName, senderType, senderStateName);
                            this.IsSpecificationInvoked = false;
                            this.ResumeScheduling();
                        }
                        else
                        {
                            monitor.MonitorEvent(e, senderName, senderType, senderStateName);
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public void Assert(bool predicate)
        {
            if (!predicate)
            {
                string msg = "Detected an assertion failure.";
                if (this.SchedulingPolicy is SchedulingPolicy.None)
                {
                    throw new AssertionFailureException(msg);
                }

                this.NotifyAssertionFailure(msg);
            }
        }

        /// <inheritdoc/>
        public void Assert(bool predicate, string s, object arg0)
        {
            if (!predicate)
            {
                var msg = string.Format(CultureInfo.InvariantCulture, s, arg0?.ToString());
                if (this.SchedulingPolicy is SchedulingPolicy.None)
                {
                    throw new AssertionFailureException(msg);
                }

                this.NotifyAssertionFailure(msg);
            }
        }

        /// <inheritdoc/>
        public void Assert(bool predicate, string s, object arg0, object arg1)
        {
            if (!predicate)
            {
                var msg = string.Format(CultureInfo.InvariantCulture, s, arg0?.ToString(), arg1?.ToString());
                if (this.SchedulingPolicy is SchedulingPolicy.None)
                {
                    throw new AssertionFailureException(msg);
                }

                this.NotifyAssertionFailure(msg);
            }
        }

        /// <inheritdoc/>
        public void Assert(bool predicate, string s, object arg0, object arg1, object arg2)
        {
            if (!predicate)
            {
                var msg = string.Format(CultureInfo.InvariantCulture, s, arg0?.ToString(), arg1?.ToString(), arg2?.ToString());
                if (this.SchedulingPolicy is SchedulingPolicy.None)
                {
                    throw new AssertionFailureException(msg);
                }

                this.NotifyAssertionFailure(msg);
            }
        }

        /// <inheritdoc/>
        public void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                var msg = string.Format(CultureInfo.InvariantCulture, s, args);
                if (this.SchedulingPolicy is SchedulingPolicy.None)
                {
                    throw new AssertionFailureException(msg);
                }

                this.NotifyAssertionFailure(msg);
            }
        }

        /// <summary>
        /// Creates a liveness monitor that checks if the specified task eventually completes execution successfully.
        /// </summary>
        internal void MonitorTaskCompletion(Task task)
        {
            if (this.SchedulingPolicy is SchedulingPolicy.Interleaving &&
                task.Status != TaskStatus.RanToCompletion)
            {
                var monitor = new TaskLivenessMonitor(task);
                this.TaskLivenessMonitors.Add(monitor);
            }
        }

        /// <summary>
        /// Starts running a background monitor that checks for potential deadlocks.
        /// </summary>
        private void StartMonitoringDeadlocks() => Task.Factory.StartNew(this.CheckIfExecutionHasDeadlockedAsync,
            this.CancellationSource.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        /// <summary>
        /// Returns true if the specified thread is controlled, else false.
        /// </summary>
        private bool IsThreadControlled(Thread thread)
        {
            string name = thread?.Name;
            return name != null && this.ControlledThreads.ContainsKey(name);
        }

        /// <summary>
        /// Returns true if the specified task is uncontrolled, else false.
        /// </summary>
        internal bool IsTaskUncontrolled(Task task) => this.IsTaskUncontrolled(task, out _);

        /// <summary>
        /// Returns true if the specified task is uncontrolled, else false.
        /// </summary>
        internal bool IsTaskUncontrolled(Task task, out string methodName)
        {
            if (task is null || task.IsCompleted)
            {
                methodName = null;
                return false;
            }

            return this.UncontrolledTasks.TryGetValue(task, out methodName) ||
                !this.ControlledTasks.ContainsKey(task);
        }

        /// <summary>
        /// Checks if the awaited task is uncontrolled.
        /// </summary>
        internal bool CheckIfAwaitedTaskIsUncontrolled(Task task)
        {
            if (this.IsTaskUncontrolled(task, out string methodName))
            {
                if (string.IsNullOrEmpty(methodName))
                {
                    this.NotifyUncontrolledTaskWait(task);
                }
                else
                {
                    this.NotifyUncontrolledTaskWait(task, methodName);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the task returned from the specified method is uncontrolled.
        /// </summary>
        internal bool CheckIfReturnedTaskIsUncontrolled(Task task, string methodName)
        {
            if (this.IsTaskUncontrolled(task))
            {
                this.NotifyUncontrolledTaskReturned(task, methodName);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if the execution has deadlocked. This happens when there are no more enabled operations,
        /// but there is one or more paused operations that are waiting some resource to complete.
        /// </summary>
        private void CheckIfExecutionHasDeadlocked(IEnumerable<ControlledOperation> ops)
        {
            if (this.ExecutionStatus != ExecutionStatus.Running ||
                ops.Any(op => op.Status is OperationStatus.Enabled))
            {
                // Either the runtime has stopped executing, or there are still enabled operations, so do not check for a deadlock.
                return;
            }

            var pausedOperations = ops.Where(op => op.Status is OperationStatus.Paused).ToList();
            var pausedOnResources = ops.Where(op => op.Status is OperationStatus.PausedOnResource).ToList();
            var pausedOnReceiveOperations = ops.Where(op => op.Status is OperationStatus.PausedOnReceive).ToList();

            var totalCount = pausedOperations.Count + pausedOnResources.Count + pausedOnReceiveOperations.Count;
            if (totalCount is 0)
            {
                // There are no paused operations, so the execution is not deadlocked.
                return;
            }

            // To simplify the error message, remove the root operation, unless it is the only one that is paused.
            if (totalCount > 1)
            {
                pausedOperations.RemoveAll(op => op.IsRoot);
                pausedOnResources.RemoveAll(op => op.IsRoot);
                pausedOnReceiveOperations.RemoveAll(op => op.IsRoot);
            }

            StringBuilder msg;
            if (this.IsUncontrolledConcurrencyDetected)
            {
                msg = new StringBuilder("Potential deadlock detected.");
            }
            else
            {
                msg = new StringBuilder("Deadlock detected.");
            }

            if (pausedOperations.Count > 0)
            {
                for (int idx = 0; idx < pausedOperations.Count; idx++)
                {
                    msg.Append(string.Format(CultureInfo.InvariantCulture, " {0}", pausedOperations[idx].Name));
                    if (idx == pausedOperations.Count - 2)
                    {
                        msg.Append(" and");
                    }
                    else if (idx < pausedOperations.Count - 1)
                    {
                        msg.Append(',');
                    }
                }

                msg.Append(pausedOperations.Count is 1 ? " is " : " are ");
                msg.Append("paused on a dependency, but no other controlled operations are enabled.");
            }

            if (pausedOnResources.Count > 0)
            {
                for (int idx = 0; idx < pausedOnResources.Count; idx++)
                {
                    msg.Append(string.Format(CultureInfo.InvariantCulture, " {0}", pausedOnResources[idx].Name));
                    if (idx == pausedOnResources.Count - 2)
                    {
                        msg.Append(" and");
                    }
                    else if (idx < pausedOnResources.Count - 1)
                    {
                        msg.Append(',');
                    }
                }

                msg.Append(pausedOnResources.Count is 1 ? " is " : " are ");
                msg.Append("waiting to acquire a resource that is already acquired, ");
                msg.Append("but no other controlled operations are enabled.");
            }

            if (pausedOnReceiveOperations.Count > 0)
            {
                for (int idx = 0; idx < pausedOnReceiveOperations.Count; idx++)
                {
                    msg.Append(string.Format(CultureInfo.InvariantCulture, " {0}", pausedOnReceiveOperations[idx].Name));
                    if (idx == pausedOnReceiveOperations.Count - 2)
                    {
                        msg.Append(" and");
                    }
                    else if (idx < pausedOnReceiveOperations.Count - 1)
                    {
                        msg.Append(',');
                    }
                }

                msg.Append(pausedOnReceiveOperations.Count is 1 ? " is " : " are ");
                msg.Append("waiting to receive an event, but no other controlled operations are enabled.");
            }

            if (this.IsUncontrolledConcurrencyDetected)
            {
                msg.Append(" Due to the presence of uncontrolled concurrency in the test, ");
                msg.Append("Coyote cannot accurately determine if this is a real deadlock or not.");
                if (!this.Configuration.ReportPotentialDeadlocksAsBugs)
                {
                    this.LogWriter.LogInfo("[coyote::test] {0}", msg);
                    this.Detach(ExecutionStatus.Deadlocked);
                }

                msg.Append(" If you believe that this is not a real deadlock, you can disable reporting ");
                msg.Append("potential deadlocks as bugs by setting '--skip-potential-deadlocks' or ");
                msg.Append("'Configuration.WithPotentialDeadlocksReportedAsBugs(false)'.");
            }

            this.NotifyAssertionFailure(msg.ToString());
        }

        /// <summary>
        /// Periodically checks if the execution has deadlocked.
        /// </summary>
        private async Task CheckIfExecutionHasDeadlockedAsync()
        {
            var info = new SchedulingActivityInfo();
            this.LogWriter.LogDebug("[coyote::debug] Started periodic monitoring for potential deadlocks in runtime '{0}'.", this.Id);
            while (true)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(this.Configuration.DeadlockTimeout), this.CancellationSource.Token);
                    using (SynchronizedSection.Enter(this.RuntimeLock))
                    {
                        if (this.ExecutionStatus != ExecutionStatus.Running)
                        {
                            break;
                        }

                        if (info.OperationCount == this.OperationMap.Count &&
                            info.StepCount == this.Scheduler.StepCount)
                        {
                            string msg = "Potential deadlock or hang detected. The periodic deadlock detection monitor was used, so " +
                                "Coyote cannot accurately determine if this is a deadlock, hang or false positive. If you believe " +
                                "that this is a false positive, you can try increase the deadlock detection timeout by setting " +
                                "'--deadlock-timeout N' or 'Configuration.WithDeadlockTimeout(N)'.";
                            if (Debugger.IsAttached)
                            {
                                msg += " The deadlock or hang was detected with a debugger attached, so Coyote is only inserting " +
                                    "a breakpoint, instead of failing this execution.";
                                this.LogWriter.LogError("[coyote::error] {0}", msg);
                                Debugger.Break();
                            }
                            else if (this.Configuration.ReportPotentialDeadlocksAsBugs)
                            {
                                msg += " Alternatively, you can disable reporting potential deadlocks or hangs as bugs by setting " +
                                    "'--skip-potential-deadlocks' or 'Configuration.WithPotentialDeadlocksReportedAsBugs(false)'.";
                                this.NotifyAssertionFailure(msg);
                            }
                            else
                            {
                                this.LogWriter.LogError("[coyote::error] {0}", msg);
                                this.Detach(ExecutionStatus.Deadlocked);
                            }
                        }
                        else
                        {
                            // Passed check, so continue with the next timeout period.
                            this.LogWriter.LogDebug("[coyote::debug] Passed periodic check for potential deadlocks and hangs in runtime '{0}'.",
                                this.Id);
                            info.OperationCount = this.OperationMap.Count;
                            info.StepCount = this.Scheduler.StepCount;
                            if (this.LastPostponedSchedulingPoint is SchedulingPointType.Pause ||
                                this.LastPostponedSchedulingPoint is SchedulingPointType.Complete)
                            {
                                // A scheduling point was postponed due to a potential deadlock, so try to check if it has been resolved.
                                this.ScheduleNextOperation(default, this.LastPostponedSchedulingPoint.Value, isSuppressible: false);
                            }
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Checks for liveness errors.
        /// </summary>
        internal void CheckLivenessErrors()
        {
            foreach (var monitor in this.TaskLivenessMonitors)
            {
                if (!monitor.IsSatisfied)
                {
                    string msg = string.Format(CultureInfo.InvariantCulture,
                        "Found liveness bug at the end of program execution.\nThe stack trace is:\n{0}",
                        FormatSpecificationMonitorStackTrace(monitor.StackTrace));
                    this.NotifyAssertionFailure(msg);
                }
            }

            // Checks if there is a specification monitor stuck in a hot state.
            foreach (var monitor in this.SpecificationMonitors)
            {
                if (monitor.IsInHotState(out string stateName))
                {
                    string msg = string.Format(CultureInfo.InvariantCulture,
                        "{0} detected liveness bug in hot state '{1}' at the end of program execution.",
                        monitor.GetType().FullName, stateName);
                    this.NotifyAssertionFailure(msg);
                }
            }
        }

        /// <summary>
        /// Checks if a liveness monitor exceeded its threshold and, if yes, it reports an error.
        /// </summary>
        internal void CheckLivenessThresholdExceeded()
        {
            foreach (var monitor in this.TaskLivenessMonitors)
            {
                if (monitor.IsLivenessThresholdExceeded(this.Configuration.LivenessTemperatureThreshold))
                {
                    string msg = string.Format(CultureInfo.InvariantCulture,
                        "Found potential liveness bug at the end of program execution.\nThe stack trace is:\n{0}",
                        FormatSpecificationMonitorStackTrace(monitor.StackTrace));
                    this.NotifyAssertionFailure(msg);
                }
            }

            foreach (var monitor in this.SpecificationMonitors)
            {
                if (monitor.IsLivenessThresholdExceeded(this.Configuration.LivenessTemperatureThreshold))
                {
                    string msg = $"{monitor.Name} detected potential liveness bug in hot state '{monitor.CurrentStateName}'.";
                    this.NotifyAssertionFailure(msg);
                }
            }
        }

        /// <summary>
        /// Checks if the scheduling steps bound has been reached. If yes,
        /// it stops the scheduler and kills all enabled machines.
        /// </summary>
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
                    this.LogWriter.LogDebug("[coyote::debug] {0}", message);
                    this.Detach(ExecutionStatus.BoundReached);
                }
            }
        }

        /// <summary>
        /// Notify that an exception was not handled.
        /// </summary>
        internal void NotifyUnhandledException(Exception ex, string message)
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                if (this.ExecutionStatus != ExecutionStatus.Running)
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
        internal void NotifyAssertionFailure(string text)
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                if (this.ExecutionStatus is ExecutionStatus.Running)
                {
                    this.BugReport = text;
                    this.LogManager.LogAssertionFailure($"[coyote::error] {text}");
                    this.RaiseOnFailureEvent(new AssertionFailureException(text));
                    if (Debugger.IsAttached)
                    {
                        Debugger.Break();
                    }

                    this.Detach(ExecutionStatus.BugFound);
                }
            }
        }

        /// <summary>
        /// Notify that an uncontrolled method invocation was detected.
        /// </summary>
        internal void NotifyUncontrolledInvocation(string methodName)
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                if (this.SchedulingPolicy != SchedulingPolicy.None)
                {
                    this.UncontrolledInvocations.Add(methodName);
                }

                if (this.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    string message = $"Invoking '{methodName}' is not intercepted and controlled during " +
                        "testing, so it can interfere with the ability to reproduce bug traces.";
                    this.TryHandleUncontrolledConcurrency(message, methodName);
                }
            }
        }

        /// <summary>
        /// Notify that an uncontrolled synchronization method invocation was detected.
        /// </summary>
        internal void NotifyUncontrolledSynchronizationInvocation(string methodName)
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                if (this.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    string message = $"Executing thread '{Thread.CurrentThread.ManagedThreadId}' is not controlled and " +
                        $"is invoking the {methodName} synchronization method, which can cause deadlocks during testing.";
                    if (this.Configuration.IsSystematicFuzzingFallbackEnabled)
                    {
                        this.LogWriter.LogDebug("[coyote::debug] {0}", message);
                        this.IsUncontrolledConcurrencyDetected = true;
                        this.Detach(ExecutionStatus.ConcurrencyUncontrolled);
                    }
                    else
                    {
                        this.NotifyAssertionFailure(message);
                    }
                }
            }
        }

        /// <summary>
        /// Notify that the currently executing thread is uncontrolled.
        /// </summary>
        private void NotifyUncontrolledCurrentThread()
        {
            if (this.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                // TODO: figure out if there is a way to get more information about the creator of the
                // uncontrolled thread to ease the user debugging experience.
                string message = $"Executing thread '{Thread.CurrentThread.ManagedThreadId}' is not intercepted and " +
                    "controlled during testing, so it can interfere with the ability to reproduce bug traces.";
                this.TryHandleUncontrolledConcurrency(message);
            }
        }

        /// <summary>
        /// Notify that an uncontrolled task is being waited.
        /// </summary>
        private void NotifyUncontrolledTaskWait(Task task)
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                if (this.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    string message = $"Waiting task '{task.Id}' that is not intercepted and controlled during " +
                        "testing, so it can interfere with the ability to reproduce bug traces.";
                    if (this.TryHandleUncontrolledConcurrency(message))
                    {
                        this.UncontrolledTasks.TryAdd(task, null);
                        this.TryPauseAndResolveUncontrolledTask(task);
                    }
                }
            }
        }

        /// <summary>
        /// Notify that an uncontrolled task with a known source is being waited.
        /// </summary>
        private void NotifyUncontrolledTaskWait(Task task, string methodName)
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                if (this.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    string message = $"Waiting task '{task.Id}' from '{methodName}' that is not intercepted and controlled " +
                        "during testing, so it can interfere with the ability to reproduce bug traces.";
                    if (this.TryHandleUncontrolledConcurrency(message, methodName))
                    {
                        this.UncontrolledTasks.TryAdd(task, methodName);
                        this.TryPauseAndResolveUncontrolledTask(task);
                    }
                }
            }
        }

        /// <summary>
        /// Notify that an uncontrolled task was returned.
        /// </summary>
        private void NotifyUncontrolledTaskReturned(Task task, string methodName)
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                if (this.SchedulingPolicy != SchedulingPolicy.None)
                {
                    this.UncontrolledInvocations.Add(methodName);
                }

                if (this.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    string message = $"Invoking '{methodName}' returned task '{task.Id}' that is not intercepted and " +
                        "controlled during testing, so it can interfere with the ability to reproduce bug traces.";
                    if (this.TryHandleUncontrolledConcurrency(message, methodName))
                    {
                        this.UncontrolledTasks.TryAdd(task, methodName);
                        this.TryPauseAndResolveUncontrolledTask(task);
                    }
                }
            }
        }

        /// <summary>
        /// Invoked when uncontrolled concurrency is detected. Based on the test configuration, it can try
        /// handle the uncontrolled concurrency, else it terminates the current test iteration.
        /// </summary>
        private bool TryHandleUncontrolledConcurrency(string message, string methodName = default)
        {
            if (this.Configuration.IsPartiallyControlledConcurrencyAllowed)
            {
                this.LogWriter.LogDebug("[coyote::debug] {0}", message);
                this.IsUncontrolledConcurrencyDetected = true;
                return true;
            }
            else if (this.Configuration.IsSystematicFuzzingFallbackEnabled)
            {
                this.LogWriter.LogDebug("[coyote::debug] {0}", message);
                this.IsUncontrolledConcurrencyDetected = true;
                this.Detach(ExecutionStatus.ConcurrencyUncontrolled);
            }
            else
            {
                this.NotifyAssertionFailure(FormatUncontrolledConcurrencyExceptionMessage(message, methodName));
            }

            return false;
        }

        /// <summary>
        /// Throws an <see cref="AssertionFailureException"/> exception containing the specified exception.
        /// </summary>
        internal void WrapAndThrowException(Exception exception, string s, params object[] args)
        {
            string msg = string.Format(CultureInfo.InvariantCulture, s, args);
            string message = string.Format(CultureInfo.InvariantCulture,
                "Exception '{0}' was thrown in {1}: {2}\n" +
                "from location '{3}':\n" +
                "The stack trace is:\n{4}",
                exception.GetType(), msg, exception.Message, exception.Source, exception.StackTrace);

            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                throw new AssertionFailureException(message, exception);
            }

            this.NotifyUnhandledException(exception, message);
        }

        /// <summary>
        /// Formats the message of the uncontrolled concurrency exception.
        /// </summary>
        private static string FormatUncontrolledConcurrencyExceptionMessage(string message, string methodName = default)
        {
            var mockMessage = methodName is null ? string.Empty : $" either replace or mock '{methodName}', or";
            return $"{message} As a workaround, you can{mockMessage} use the '--no-repro' command line option " +
                "(or the 'Configuration.WithNoBugTraceRepro()' method) to ignore this error by disabling bug " +
                $"trace repro. Learn more at http://aka.ms/coyote-no-repro.\n{new StackTrace()}";
        }

        /// <summary>
        /// Processes an unhandled exception in the specified controlled operation.
        /// </summary>
        internal void ProcessUnhandledExceptionInOperation(ControlledOperation op, Exception exception)
        {
            // Complete the failed operation. This is required so that the operation does not throw if it detaches.
            op.Status = OperationStatus.Completed;

            if (exception is AggregateException aex)
            {
                exception = aex.Flatten().InnerExceptions.OfType<ThreadInterruptedException>().FirstOrDefault() ?? exception;
            }

            // Ignore this exception, its thrown by the runtime to terminate controlled threads.
            if (!(exception is ThreadInterruptedException || exception.GetBaseException() is ThreadInterruptedException))
            {
                // Report the unhandled exception.
                string trace = FormatExceptionStackTrace(exception);
                string message = $"Unhandled exception. {trace}";
                this.NotifyUnhandledException(exception, message);
            }
        }

        /// <summary>
        /// Formats the stack trace of the specified exception.
        /// </summary>
        private static string FormatExceptionStackTrace(Exception exception)
        {
#if NET || NETCOREAPP3_1
            string[] lines = exception.ToString().Split(Environment.NewLine, StringSplitOptions.None);
#else
            string[] lines = exception.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
#endif
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
        /// Formats the specified stack trace of a specification monitor.
        /// </summary>
        private static string FormatSpecificationMonitorStackTrace(StackTrace trace)
        {
            StringBuilder sb = new StringBuilder();
#if NET || NETCOREAPP3_1
            string[] lines = trace.ToString().Split(Environment.NewLine, StringSplitOptions.None);
#else
            string[] lines = trace.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
#endif
            foreach (var line in lines)
            {
                if ((line.Contains("at Microsoft.Coyote.Specifications") ||
                    line.Contains("at Microsoft.Coyote.Runtime")) &&
                    !line.Contains($"at {typeof(Specification).FullName}.{nameof(Specification.Monitor)}"))
                {
                    continue;
                }

                sb.AppendLine(line);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Raises the <see cref="OnFailure"/> event with the specified <see cref="Exception"/>.
        /// </summary>
        internal void RaiseOnFailureEvent(Exception exception)
        {
            if (Debugger.IsAttached)
            {
                Debugger.Break();
            }

            this.OnFailure?.Invoke(exception);
        }

        /// <summary>
        /// Populates the specified test report.
        /// </summary>
        internal void PopulateTestReport(ITestReport report)
        {
            using (SynchronizedSection.Enter(this.RuntimeLock))
            {
                bool isBugFound = this.ExecutionStatus is ExecutionStatus.BugFound;
                int groupingDegree = this.OperationMap.Values.Select(op => op.Group).Distinct().Count();
                report.SetSchedulingStatistics(isBugFound, this.BugReport, this.OperationMap.Count, (int)this.MaxConcurrencyDegree,
                    groupingDegree, this.Scheduler.StepCount, this.Scheduler.IsMaxStepsReached, this.Scheduler.IsIterationFair);
                if (isBugFound)
                {
                    report.SetUnhandledException(this.UnhandledException);
                }

                report.SetUncontrolledInvocations(this.UncontrolledInvocations);
            }
        }

        /// <summary>
        /// Builds the <see cref="CoverageInfo"/>.
        /// </summary>
        internal CoverageInfo BuildCoverageInfo() => this.Extension.BuildCoverageInfo() ?? this.CoverageInfo;

        /// <summary>
        /// Returns the <see cref="CoverageGraph"/> of the current execution.
        /// </summary>
        internal CoverageGraph GetCoverageGraph() => this.Extension.GetCoverageGraph();

        /// <summary>
        /// Enters the synchronized section of the runtime. When the synchronized section
        /// gets disposed, the thread will automatically exit it.
        /// </summary>
        internal SynchronizedSection EnterSynchronizedSection() => SynchronizedSection.Enter(this.RuntimeLock);

        /// <summary>
        /// Sets up the context of the executing controlled thread, allowing future retrieval
        /// of runtime related data from the same thread, as well as across threads that share
        /// the same asynchronous control flow.
        /// </summary>
        private void SetCurrentExecutionContext(ControlledOperation op)
        {
            AsyncLocalRuntime.Value = this;
            ThreadLocalRuntime = this;
            ExecutingOperation = op;
            SynchronizationContext.SetSynchronizationContext(this.SyncContext);
        }

        /// <summary>
        /// Removes any runtime related data from the context of the executing controlled thread.
        /// </summary>
        private static void CleanCurrentExecutionContext()
        {
            ExecutingOperation = null;
            ThreadLocalRuntime = null;
            AsyncLocalRuntime.Value = null;
        }

        /// <inheritdoc/>
        public void RegisterLog(IRuntimeLog log) => this.LogManager.RegisterLog(log, this.LogWriter);

        /// <inheritdoc/>
        public void RemoveLog(IRuntimeLog log) => this.LogManager.RemoveLog(log);

        /// <inheritdoc/>
        public void Stop() => this.IsRunning = false;

        /// <summary>
        /// Detaches the scheduler and interrupts all controlled operations.
        /// </summary>
        /// <remarks>
        /// It is assumed that this method runs in the scope of a <see cref="SynchronizedSection"/>.
        /// </remarks>
        private void Detach(ExecutionStatus status)
        {
            if (this.ExecutionStatus != ExecutionStatus.Running)
            {
                return;
            }

            try
            {
                if (status is ExecutionStatus.PathExplored)
                {
                    this.LogWriter.LogInfo("[coyote::test] Exploration finished in runtime '{0}' [reached the end of the test method].", this.Id);
                }
                else if (status is ExecutionStatus.BoundReached)
                {
                    this.LogWriter.LogInfo("[coyote::test] Exploration finished in runtime '{0}' [reached the given bound].", this.Id);
                }
                else if (status is ExecutionStatus.Deadlocked)
                {
                    this.LogWriter.LogInfo("[coyote::test] Exploration finished in runtime '{0}' [detected a potential deadlock].", this.Id);
                }
                else if (status is ExecutionStatus.ConcurrencyUncontrolled)
                {
                    this.LogWriter.LogInfo("[coyote::test] Exploration finished in runtime '{0}' [detected uncontrolled concurrency].", this.Id);
                }
                else if (status is ExecutionStatus.BugFound)
                {
                    this.LogWriter.LogInfo("[coyote::test] Exploration finished in runtime '{0}' [found a bug using the '{1}' strategy].",
                        this.Id, this.Scheduler.GetStrategyName());
                }

                this.ExecutionStatus = status;
                this.CancellationSource.Cancel();

                // Complete any remaining operations at the end of the schedule.
                ControlledOperation current = ExecutingOperation;
                foreach (var op in this.OperationMap.Values)
                {
                    if (op.Status != OperationStatus.Completed && op != current)
                    {
                        // Force the operation to complete and interrupt its thread.
                        op.Status = OperationStatus.Completed;
                        if (this.ThreadPool.TryGetValue(op.Id, out Thread thread))
                        {
                            thread.Interrupt();
                        }
                    }
                }

                if (current.Status != OperationStatus.Completed)
                {
                    // Force the current operation to complete and interrupt the current thread.
                    current.Status = OperationStatus.Completed;
                    throw new ThreadInterruptedException();
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
                using (SynchronizedSection.Enter(this.RuntimeLock))
                {
                    foreach (var op in this.OperationMap.Values)
                    {
                        op.Dispose();
                    }

                    foreach (var handler in this.PendingStartOperationMap.Values)
                    {
                        handler.Dispose();
                    }

                    this.ThreadPool.Clear();
                    this.OperationMap.Clear();
                    this.PendingStartOperationMap.Clear();
                    this.ControlledThreads.Clear();
                    this.ControlledTasks.Clear();
                    this.UncontrolledTasks.Clear();
                    this.UncontrolledInvocations.Clear();
                    this.SpecificationMonitors.Clear();
                    this.TaskLivenessMonitors.Clear();

                    if (!(this.Extension is NullRuntimeExtension))
                    {
                        this.Extension.Dispose();
                    }

                    this.ControlledTaskScheduler.Dispose();
                    this.SyncContext.Dispose();
                    this.CancellationSource.Dispose();
                    this.LogWriter.Dispose();
                }

                if (this.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    // Note: this makes it possible to run a Controlled unit test followed by a production
                    // unit test, whereas before that would throw "Uncontrolled Task" exceptions.
                    // This does not solve mixing unit test type in parallel.
                    Interlocked.Decrement(ref ExecutionControlledUseCount);
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
