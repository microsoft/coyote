// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Implements a scheduler that serializes and schedules controlled operations.
    /// </summary>
    /// <remarks>
    /// Invoking methods of the scheduler is thread-safe.
    /// </remarks>
#if !DEBUG
    [DebuggerStepThrough]
#endif
    internal sealed class OperationScheduler
    {
        /// <summary>
        /// The configuration used by the scheduler.
        /// </summary>
        private readonly Configuration Configuration;

        /// <summary>
        /// The controlled runtime.
        /// </summary>
        private readonly ControlledRuntime Runtime;

        /// <summary>
        /// The scheduling strategy used for program exploration.
        /// </summary>
        private readonly ISchedulingStrategy Strategy;

        /// <summary>
        /// Map from unique ids to asynchronous operations.
        /// </summary>
        private readonly Dictionary<ulong, IAsyncOperation> OperationMap;

        /// <summary>
        /// Map from ids of tasks that are controlled by the runtime to operations.
        /// </summary>
        private readonly Dictionary<int, AsyncOperation> ControlledTaskMap;

        /// <summary>
        /// The program schedule trace.
        /// </summary>
        internal ScheduleTrace ScheduleTrace;

        /// <summary>
        /// Object that is used to synchronize access to the scheduler.
        /// </summary>
        private readonly object SyncObject;

        /// <summary>
        /// The scheduler completion source.
        /// </summary>
        private readonly TaskCompletionSource<bool> CompletionSource;

        /// <summary>
        /// Checks if the scheduler is running.
        /// </summary>
        internal bool IsRunning { get; private set; }

        /// <summary>
        /// The currently scheduled asynchronous operation.
        /// </summary>
        private AsyncOperation ScheduledOperation;

        /// <summary>
        /// Number of scheduled steps.
        /// </summary>
        internal int ScheduledSteps => this.Strategy.GetScheduledSteps();

        /// <summary>
        /// Checks if the schedule has been fully explored.
        /// </summary>
        internal bool HasFullyExploredSchedule { get; private set; }

        /// <summary>
        /// True if a bug was found.
        /// </summary>
        internal bool BugFound { get; private set; }

        /// <summary>
        /// Bug report.
        /// </summary>
        internal string BugReport { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationScheduler"/> class.
        /// </summary>
        internal OperationScheduler(ControlledRuntime runtime, ISchedulingStrategy strategy,
            ScheduleTrace trace, Configuration configuration)
        {
            this.Configuration = configuration;
            this.Runtime = runtime;
            this.Strategy = strategy;
            this.OperationMap = new Dictionary<ulong, IAsyncOperation>();
            this.ControlledTaskMap = new Dictionary<int, AsyncOperation>();
            this.ScheduleTrace = trace;
            this.SyncObject = new object();
            this.CompletionSource = new TaskCompletionSource<bool>();
            this.IsRunning = true;
            this.BugFound = false;
            this.HasFullyExploredSchedule = false;
        }

        /// <summary>
        /// Schedules the next operation, which can include the currently executing operation.
        /// Only operations that are not blocked nor completed can be scheduled.
        /// </summary>
        internal void ScheduleNextOperation()
        {
            lock (this.SyncObject)
            {
                int? taskId = Task.CurrentId;

                // TODO: figure out if this check is still needed.
                // If the caller is the root task, then return.
                if (taskId != null && taskId == this.Runtime.RootTaskId)
                {
                    return;
                }

                AsyncOperation current = this.ScheduledOperation;
                if (!this.IsRunning)
                {
                    // TODO: check if this stop is needed.
                    this.Detach();

                    if (current.Status != AsyncOperationStatus.Completed)
                    {
                        // If scheduler is not running, throw exception to force terminate the current operation.
                        throw new ExecutionCanceledException();
                    }
                }

                if (current.Status != AsyncOperationStatus.Completed)
                {
                    // Checks if concurrency not controlled by the runtime was used.
                    this.CheckNoExternalConcurrencyUsed();
                }

                // Checks if the scheduling steps bound has been reached.
                this.CheckIfSchedulingStepsBoundIsReached();

                if (this.Configuration.IsProgramStateHashingEnabled)
                {
                    // Update the current operation with the hashed program state.
                    current.HashedProgramState = this.Runtime.GetProgramState();
                }

                // Get and order the operations by their id.
                var ops = this.OperationMap.Values.OrderBy(op => op.Id);

                // Try enable any operation that is currently waiting, but has its dependencies already satisfied.
                foreach (var op in ops)
                {
                    if (op is AsyncOperation machineOp)
                    {
                        machineOp.TryEnable();
                        IO.Debug.WriteLine("<ScheduleDebug> Operation '{0}' has status '{1}'.", op.Id, op.Status);
                    }
                }

                if (!this.Strategy.GetNextOperation(current, ops, out IAsyncOperation next))
                {
                    // Checks if the program has deadlocked.
                    this.CheckIfProgramHasDeadlocked(ops.Select(op => op as AsyncOperation));

                    IO.Debug.WriteLine("<ScheduleDebug> Schedule explored.");
                    this.HasFullyExploredSchedule = true;
                    this.Detach();

                    if (current.Status != AsyncOperationStatus.Completed)
                    {
                        // The schedule is explored so throw exception to force terminate the current operation.
                        throw new ExecutionCanceledException();
                    }
                }

                this.ScheduledOperation = next as AsyncOperation;
                this.ScheduleTrace.AddSchedulingChoice(next.Id);

                IO.Debug.WriteLine($"<ScheduleDebug> Scheduling the next operation of '{next.Name}'.");

                if (current != next)
                {
                    current.IsActive = false;
                    this.ScheduledOperation.IsActive = true;
                    Monitor.PulseAll(this.SyncObject);

                    if (!current.IsHandlerRunning)
                    {
                        return;
                    }

                    if (!this.ControlledTaskMap.ContainsKey(Task.CurrentId.Value))
                    {
                        this.ControlledTaskMap.Add(Task.CurrentId.Value, current);
                        IO.Debug.WriteLine("<ScheduleDebug> Operation '{0}' is associated with task '{1}'.", current.Id, Task.CurrentId);
                    }

                    while (!current.IsActive)
                    {
                        IO.Debug.WriteLine("<ScheduleDebug> Sleeping the operation of '{0}' on task '{1}'.", current.Name, Task.CurrentId);
                        Monitor.Wait(this.SyncObject);
                        IO.Debug.WriteLine("<ScheduleDebug> Waking up the operation of '{0}' on task '{1}'.", current.Name, Task.CurrentId);
                    }

                    if (current.Status != AsyncOperationStatus.Enabled)
                    {
                        throw new ExecutionCanceledException();
                    }
                }
            }
        }

        /// <summary>
        /// Returns the next nondeterministic boolean choice.
        /// </summary>
        internal bool GetNextNondeterministicBooleanChoice(int maxValue)
        {
            lock (this.SyncObject)
            {
                if (!this.IsRunning)
                {
                    // If scheduler is not running, throw exception to force terminate the caller.
                    throw new ExecutionCanceledException();
                }

                // Checks if concurrency not controlled by the runtime was used.
                this.CheckNoExternalConcurrencyUsed();

                // Checks if the scheduling steps bound has been reached.
                this.CheckIfSchedulingStepsBoundIsReached();

                if (this.Configuration.IsProgramStateHashingEnabled)
                {
                    // Update the current operation with the hashed program state.
                    this.ScheduledOperation.HashedProgramState = this.Runtime.GetProgramState();
                }

                if (!this.Strategy.GetNextBooleanChoice(this.ScheduledOperation, maxValue, out bool choice))
                {
                    IO.Debug.WriteLine("<ScheduleDebug> Schedule explored.");
                    this.Detach();
                    throw new ExecutionCanceledException();
                }

                this.ScheduleTrace.AddNondeterministicBooleanChoice(choice);
                return choice;
            }
        }

        /// <summary>
        /// Returns the next nondeterministic integer choice.
        /// </summary>
        internal int GetNextNondeterministicIntegerChoice(int maxValue)
        {
            lock (this.SyncObject)
            {
                if (!this.IsRunning)
                {
                    // If scheduler is not running, throw exception to force terminate the caller.
                    throw new ExecutionCanceledException();
                }

                // Checks if concurrency not controlled by the runtime was used.
                this.CheckNoExternalConcurrencyUsed();

                // Checks if the scheduling steps bound has been reached.
                this.CheckIfSchedulingStepsBoundIsReached();

                if (this.Configuration.IsProgramStateHashingEnabled)
                {
                    // Update the current operation with the hashed program state.
                    this.ScheduledOperation.HashedProgramState = this.Runtime.GetProgramState();
                }

                if (!this.Strategy.GetNextIntegerChoice(this.ScheduledOperation, maxValue, out int choice))
                {
                    IO.Debug.WriteLine("<ScheduleDebug> Schedule explored.");
                    this.Detach();
                    throw new ExecutionCanceledException();
                }

                this.ScheduleTrace.AddNondeterministicIntegerChoice(choice);
                return choice;
            }
        }

        /// <summary>
        /// Registers the specified asynchronous operation.
        /// </summary>
        /// <param name="op">The operation to register.</param>
        /// <returns>True if the operation was successfully registered, else false if it already exists.</returns>
        internal bool RegisterOperation(AsyncOperation op)
        {
            lock (this.SyncObject)
            {
                if (this.OperationMap.Count == 0)
                {
                    this.ScheduledOperation = op;
                }

#if NETSTANDARD2_0
                if (!this.OperationMap.ContainsKey(op.Id))
                {
                    this.OperationMap.Add(op.Id, op);
                    return true;
                }

                return false;
#else
                return this.OperationMap.TryAdd(op.Id, op);
#endif
            }
        }

        /// <summary>
        /// Schedules the specified asynchronous operation to execute on the task with the given id.
        /// </summary>
        /// <param name="op">The operation to schedule.</param>
        /// <param name="taskId">The id of the task to be used to execute the operation.</param>
        internal void ScheduleOperation(AsyncOperation op, int taskId)
        {
            lock (this.SyncObject)
            {
                IO.Debug.WriteLine($"<ScheduleDebug> Scheduling operation '{op.Name}' to execute on task '{taskId}'.");
#if NETSTANDARD2_0
                if (!this.ControlledTaskMap.ContainsKey(taskId))
                {
                    this.ControlledTaskMap.Add(taskId, op);
                }
#else
                this.ControlledTaskMap.TryAdd(taskId, op);
#endif
            }
        }

        /// <summary>
        /// Starts the execution of the specified asynchronous operation.
        /// </summary>
        /// <param name="op">The operation to start executing.</param>
        internal void StartOperation(AsyncOperation op)
        {
            lock (this.SyncObject)
            {
                IO.Debug.WriteLine($"<ScheduleDebug> Starting the operation of '{op.Name}' on task '{Task.CurrentId}'.");

                op.IsHandlerRunning = true;
                Monitor.PulseAll(this.SyncObject);
                while (!op.IsActive)
                {
                    IO.Debug.WriteLine($"<ScheduleDebug> Sleeping the operation of '{op.Name}' on task '{Task.CurrentId}'.");
                    Monitor.Wait(this.SyncObject);
                    IO.Debug.WriteLine($"<ScheduleDebug> Waking up the operation of '{op.Name}' on task '{Task.CurrentId}'.");
                }

                if (op.Status != AsyncOperationStatus.Enabled)
                {
                    throw new ExecutionCanceledException();
                }
            }
        }

        /// <summary>
        /// Waits for the specified asynchronous operation to start executing.
        /// </summary>
        /// <param name="op">The operation to wait.</param>
        internal void WaitOperationStart(AsyncOperation op)
        {
            lock (this.SyncObject)
            {
                if (this.OperationMap.Count == 1)
                {
                    op.IsActive = true;
                    Monitor.PulseAll(this.SyncObject);
                }
                else
                {
                    while (!op.IsHandlerRunning)
                    {
                        Monitor.Wait(this.SyncObject);
                    }
                }
            }
        }

        /// <summary>
        /// Gets the <see cref="IAsyncOperation"/> associated with the specified
        /// unique id, or null if no such operation exists.
        /// </summary>
        [DebuggerStepThrough]
        internal TAsyncOperation GetOperationWithId<TAsyncOperation>(ulong id)
            where TAsyncOperation : IAsyncOperation
        {
            lock (this.SyncObject)
            {
                if (this.OperationMap.TryGetValue(id, out IAsyncOperation op) &&
                    op is TAsyncOperation expected)
                {
                    return expected;
                }
            }

            return default;
        }

        /// <summary>
        /// Gets the <see cref="IAsyncOperation"/> that is executing on the current
        /// synchronization context, or null if no such operation is executing.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal TAsyncOperation GetExecutingOperation<TAsyncOperation>()
            where TAsyncOperation : IAsyncOperation
        {
            lock (this.SyncObject)
            {
                if (!this.IsRunning)
                {
                    // If scheduler is not running, throw exception to force terminate the caller.
                    throw new ExecutionCanceledException();
                }

                if (Task.CurrentId.HasValue &&
                    this.ControlledTaskMap.TryGetValue(Task.CurrentId.Value, out AsyncOperation op) &&
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
        internal IEnumerable<IAsyncOperation> GetRegisteredOperations()
        {
            lock (this.SyncObject)
            {
                return this.OperationMap.Values;
            }
        }

        /// <summary>
        /// Returns the enabled operation ids.
        /// </summary>
        internal HashSet<ulong> GetEnabledOperationIds()
        {
            lock (this.SyncObject)
            {
                var enabledSchedulableIds = new HashSet<ulong>();
                foreach (var machineInfo in this.OperationMap.Values)
                {
                    if (machineInfo.Status is AsyncOperationStatus.Enabled)
                    {
                        enabledSchedulableIds.Add(machineInfo.Id);
                    }
                }

                return enabledSchedulableIds;
            }
        }

        /// <summary>
        /// Returns a test report with the scheduling statistics.
        /// </summary>
        internal TestReport GetReport()
        {
            lock (this.SyncObject)
            {
                TestReport report = new TestReport(this.Configuration);

                if (this.BugFound)
                {
                    report.NumOfFoundBugs++;
                    report.BugReports.Add(this.BugReport);
                }

                if (this.Strategy.IsFair())
                {
                    report.NumOfExploredFairSchedules++;
                    report.TotalExploredFairSteps += this.ScheduledSteps;

                    if (report.MinExploredFairSteps < 0 ||
                        report.MinExploredFairSteps > this.ScheduledSteps)
                    {
                        report.MinExploredFairSteps = this.ScheduledSteps;
                    }

                    if (report.MaxExploredFairSteps < this.ScheduledSteps)
                    {
                        report.MaxExploredFairSteps = this.ScheduledSteps;
                    }

                    if (this.Strategy.HasReachedMaxSchedulingSteps())
                    {
                        report.MaxFairStepsHitInFairTests++;
                    }

                    if (this.ScheduledSteps >= report.Configuration.MaxUnfairSchedulingSteps)
                    {
                        report.MaxUnfairStepsHitInFairTests++;
                    }
                }
                else
                {
                    report.NumOfExploredUnfairSchedules++;

                    if (this.Strategy.HasReachedMaxSchedulingSteps())
                    {
                        report.MaxUnfairStepsHitInUnfairTests++;
                    }
                }

                return report;
            }
        }

        /// <summary>
        /// Checks that no task that is not controlled by the runtime is currently executing.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void CheckNoExternalConcurrencyUsed()
        {
            lock (this.SyncObject)
            {
                if (!Task.CurrentId.HasValue || !this.ControlledTaskMap.ContainsKey(Task.CurrentId.Value))
                {
                    this.NotifyAssertionFailure(string.Format(CultureInfo.InvariantCulture,
                        "Uncontrolled task '{0}' invoked a runtime method. Please make sure to avoid using concurrency APIs " +
                        "(e.g. 'Task.Run', 'Task.Delay' or 'Task.Yield' from the 'System.Threading.Tasks' namespace) inside " +
                        "actor handlers or controlled tasks. If you are using external libraries that are executing concurrently, " +
                        "you will need to mock them during testing.",
                        Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>"));
                }
            }
        }

        /// <summary>
        /// Checks for a deadlock. This happens when there are no more enabled operations,
        /// but there is one or more blocked operations that are waiting to receive an event
        /// or for a task to complete.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        private void CheckIfProgramHasDeadlocked(IEnumerable<AsyncOperation> ops)
        {
            var blockedOnReceiveOperations = ops.Where(op => op.Status is AsyncOperationStatus.BlockedOnReceive).ToList();
            var blockedOnWaitOperations = ops.Where(op => op.Status is AsyncOperationStatus.BlockedOnWaitAll ||
                op.Status is AsyncOperationStatus.BlockedOnWaitAny).ToList();
            var blockedOnResourceSynchronization = ops.Where(op => op.Status is AsyncOperationStatus.BlockedOnResource).ToList();
            if (blockedOnReceiveOperations.Count == 0 &&
                blockedOnWaitOperations.Count == 0 &&
                blockedOnResourceSynchronization.Count == 0)
            {
                return;
            }

            string message = "Deadlock detected.";
            if (blockedOnReceiveOperations.Count > 0)
            {
                for (int i = 0; i < blockedOnReceiveOperations.Count; i++)
                {
                    message += string.Format(CultureInfo.InvariantCulture, " {0}", blockedOnReceiveOperations[i].Name);
                    if (i == blockedOnReceiveOperations.Count - 2)
                    {
                        message += " and";
                    }
                    else if (i < blockedOnReceiveOperations.Count - 1)
                    {
                        message += ",";
                    }
                }

                message += blockedOnReceiveOperations.Count == 1 ? " is " : " are ";
                message += "waiting to receive an event, but no other controlled tasks are enabled.";
            }

            if (blockedOnWaitOperations.Count > 0)
            {
                for (int i = 0; i < blockedOnWaitOperations.Count; i++)
                {
                    message += string.Format(CultureInfo.InvariantCulture, " {0}", blockedOnWaitOperations[i].Name);
                    if (i == blockedOnWaitOperations.Count - 2)
                    {
                        message += " and";
                    }
                    else if (i < blockedOnWaitOperations.Count - 1)
                    {
                        message += ",";
                    }
                }

                message += blockedOnWaitOperations.Count == 1 ? " is " : " are ";
                message += "waiting for a task to complete, but no other controlled tasks are enabled.";
            }

            if (blockedOnResourceSynchronization.Count > 0)
            {
                for (int i = 0; i < blockedOnResourceSynchronization.Count; i++)
                {
                    message += string.Format(CultureInfo.InvariantCulture, " {0}", blockedOnResourceSynchronization[i].Name);
                    if (i == blockedOnResourceSynchronization.Count - 2)
                    {
                        message += " and";
                    }
                    else if (i < blockedOnResourceSynchronization.Count - 1)
                    {
                        message += ",";
                    }
                }

                message += blockedOnResourceSynchronization.Count == 1 ? " is " : " are ";
                message += "waiting to acquire a resource that is already acquired, ";
                message += "but no other controlled tasks are enabled.";
            }

            this.NotifyAssertionFailure(message);
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
            if (this.Strategy.HasReachedMaxSchedulingSteps())
            {
                int bound = this.Strategy.IsFair() ? this.Configuration.MaxFairSchedulingSteps :
                    this.Configuration.MaxUnfairSchedulingSteps;
                string message = $"Scheduling steps bound of {bound} reached.";

                if (this.Configuration.ConsiderDepthBoundHitAsBug)
                {
                    this.NotifyAssertionFailure(message);
                }
                else
                {
                    IO.Debug.WriteLine($"<ScheduleDebug> {message}");
                    this.Detach();

                    if (this.ScheduledOperation.Status != AsyncOperationStatus.Completed)
                    {
                        // The schedule is explored so throw exception to force terminate the current operation.
                        throw new ExecutionCanceledException();
                    }
                }
            }
        }

        /// <summary>
        /// Notify that an assertion has failed.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void NotifyAssertionFailure(string text, bool killTasks = true, bool cancelExecution = true)
        {
            lock (this.SyncObject)
            {
                if (!this.BugFound)
                {
                    this.BugReport = text;

                    this.Runtime.LogWriter.LogAssertionFailure($"<ErrorLog> {text}");
                    StackTrace trace = new StackTrace();
                    this.Runtime.LogWriter.LogAssertionFailure(string.Format("<StackTrace> {0}", trace.ToString()));
                    this.Runtime.RaiseOnFailureEvent(new AssertionFailureException(text));
                    this.Runtime.LogWriter.LogStrategyDescription(this.Configuration.SchedulingStrategy,
                        this.Strategy.GetDescription());

                    this.BugFound = true;

                    if (this.Configuration.AttachDebugger)
                    {
                        Debugger.Break();
                    }
                }

                if (killTasks)
                {
                    this.Detach();
                }

                if (cancelExecution)
                {
                    throw new ExecutionCanceledException();
                }
            }
        }

        /// <summary>
        /// Waits until the scheduler terminates.
        /// </summary>
        internal Task WaitAsync() => this.CompletionSource.Task;

        /// <summary>
        /// Detaches from the scheduler releasing all controlled operations.
        /// </summary>
        private void Detach()
        {
            this.IsRunning = false;

            // Cancel any remaining operations at the end of the schedule.
            foreach (var operation in this.OperationMap.Values)
            {
                // This casting is always safe.
                var op = operation as AsyncOperation;
                op.IsActive = true;
                op.Status = AsyncOperationStatus.Canceled;
            }

            Monitor.PulseAll(this.SyncObject);

            // Check if the completion source is completed, else set its result.
            if (!this.CompletionSource.Task.IsCompleted)
            {
                this.CompletionSource.SetResult(true);
            }
        }
    }
}
