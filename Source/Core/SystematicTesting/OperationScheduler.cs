// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Coyote.SystematicTesting
{
    /// <summary>
    /// Scheduler that controls the execution of asynchronous operations during systematic testing.
    /// </summary>
    /// <remarks>
    /// Invoking the scheduler is thread-safe.
    /// </remarks>
#if !DEBUG
    [DebuggerStepThrough]
#endif
    internal sealed class OperationScheduler
    {
        /// <summary>
        /// Provides access to the operation executing on each asynchronous control flow.
        /// </summary>
        private static readonly AsyncLocal<AsyncOperation> ExecutingOperation =
            new AsyncLocal<AsyncOperation>(OnAsyncLocalExecutingOperationValueChanged);

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
        internal readonly ISchedulingStrategy Strategy;

        /// <summary>
        /// Map from unique ids to asynchronous operations.
        /// </summary>
        private readonly Dictionary<ulong, AsyncOperation> OperationMap;

        /// <summary>
        /// The program schedule trace.
        /// </summary>
        internal ScheduleTrace ScheduleTrace;

        /// <summary>
        /// Object that is used to synchronize access to the scheduler.
        /// </summary>
        internal readonly object SyncObject;

        /// <summary>
        /// The scheduler completion source.
        /// </summary>
        private readonly TaskCompletionSource<bool> CompletionSource;

        /// <summary>
        /// True if the scheduler is attached to the executing program, else false.
        /// </summary>
        internal bool IsAttached { get; private set; }

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
        /// Associated with the bug report is an optional unhandled exception.
        /// </summary>
        internal Exception UnhandledException { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationScheduler"/> class.
        /// </summary>
        internal OperationScheduler(ControlledRuntime runtime, ISchedulingStrategy strategy,
            ScheduleTrace trace, Configuration configuration)
        {
            this.Configuration = configuration;
            this.Runtime = runtime;
            this.Strategy = strategy;
            this.OperationMap = new Dictionary<ulong, AsyncOperation>();
            this.ScheduleTrace = trace;
            this.SyncObject = new object();
            this.CompletionSource = new TaskCompletionSource<bool>();
            this.IsAttached = true;
            this.BugFound = false;
            this.HasFullyExploredSchedule = false;
        }

        /// <summary>
        /// Schedules the next enabled operation, which can include the currently executing operation.
        /// </summary>
        /// <remarks>
        /// An enabled operation is one that is not blocked nor completed.
        /// </remarks>
        internal void ScheduleNext() => this.ScheduleNextOperation();

        /// <summary>
        /// Yields execution to the next enabled operation, which can include the currently executing operation.
        /// </summary>
        /// <remarks>
        /// An enabled operation is one that is not blocked nor completed.
        /// </remarks>
        internal void Yield() => this.ScheduleNextOperation(true);

        /// <summary>
        /// Schedules the next enabled operation, which can include the currently executing operation.
        /// </summary>
        /// <param name="isYielding">True if the current operation is yielding, else false.</param>
        /// <remarks>
        /// An enabled operation is one that is not blocked nor completed.
        /// </remarks>
        internal void ScheduleNextOperation(bool isYielding = false)
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
                this.ThrowExecutionCanceledExceptionIfDetached();
                if (current.Status != AsyncOperationStatus.Completed)
                {
                    // Checks if the current operation is controlled by the runtime.
                    this.CheckExecutingOperationIsControlled();
                }

                // Checks if the scheduling steps bound has been reached.
                this.CheckIfSchedulingStepsBoundIsReached();

                if (this.Configuration.IsProgramStateHashingEnabled)
                {
                    // Update the current operation with the hashed program state.
                    current.HashedProgramState = this.Runtime.GetProgramState();
                }

                // Choose the next operation to schedule, if there is one enabled.
                if (!this.TryGetNextEnabledOperation(current, isYielding, out AsyncOperation next))
                {
                    IO.Debug.WriteLine("<ScheduleDebug> Schedule explored.");
                    this.HasFullyExploredSchedule = true;
                    this.Detach();
                }

                IO.Debug.WriteLine($"<ScheduleDebug> Scheduling the next operation of '{next.Name}'.");
                this.ScheduleTrace.AddSchedulingChoice(next.Id);
                if (current != next)
                {
                    // Pause the currently scheduled operation, and enable the next one.
                    this.ScheduledOperation = next;
                    this.PauseOperation(current);
                }
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
                    op.TryEnable();
                    IO.Debug.WriteLine("<ScheduleDebug> Operation '{0}' has status '{1}'.", op.Id, op.Status);
                }

                // Choose the next operation to schedule, if there is one enabled.
                if (!this.Strategy.GetNextOperation(ops, current, isYielding, out next) &&
                    this.Configuration.IsPartiallyControlledTestingEnabled &&
                    ops.Any(op => op.IsBlockedOnUncontrolledDependency()))
                {
                    // At least one operation is blocked due to uncontrolled concurrency. To try defend against
                    // this, retry after an asynchronous delay to give some time to the dependency to complete.
                    Task.Run(async () =>
                    {
                        await Task.Delay(10);
                        lock (this.SyncObject)
                        {
                            Monitor.PulseAll(this.SyncObject);
                        }
                    });

                    // Pause the current operation until the scheduler retries.
                    Monitor.Wait(this.SyncObject);
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
        /// Returns the next nondeterministic boolean choice.
        /// </summary>
        internal bool GetNextNondeterministicBooleanChoice(int maxValue)
        {
            lock (this.SyncObject)
            {
                this.ThrowExecutionCanceledExceptionIfDetached();

                // Checks if the current operation is controlled by the runtime.
                this.CheckExecutingOperationIsControlled();

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
                this.ThrowExecutionCanceledExceptionIfDetached();

                // Checks if the current operation is controlled by the runtime.
                this.CheckExecutingOperationIsControlled();

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
        /// Starts the execution of the specified asynchronous operation.
        /// </summary>
        /// <param name="op">The operation to start executing.</param>
        /// <remarks>
        /// This method performs a handshake with <see cref="WaitOperationStart"/>.
        /// </remarks>
        internal void StartOperation(AsyncOperation op)
        {
            lock (this.SyncObject)
            {
                IO.Debug.WriteLine($"<ScheduleDebug> Starting the operation of '{op.Name}' on task '{Task.CurrentId}'.");

                // Enable the operation and store it in the async local context.
                op.Status = AsyncOperationStatus.Enabled;
                ExecutingOperation.Value = op;
                this.PauseOperation(op);
            }
        }

        /// <summary>
        /// Waits for the specified asynchronous operation to start executing.
        /// </summary>
        /// <param name="op">The operation to wait.</param>
        /// <remarks>
        /// This method performs a handshake with <see cref="StartOperation"/>.
        /// </remarks>
        internal void WaitOperationStart(AsyncOperation op)
        {
            lock (this.SyncObject)
            {
                if (this.OperationMap.Count > 1)
                {
                    while (op.Status != AsyncOperationStatus.Enabled && this.IsAttached)
                    {
                        Monitor.Wait(this.SyncObject);
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
            Monitor.PulseAll(this.SyncObject);
            if (op.Status is AsyncOperationStatus.Completed ||
                op.Status is AsyncOperationStatus.Canceled)
            {
                // The operation is completed or canceled, so no need to wait.
                return;
            }

            while (op != this.ScheduledOperation && this.IsAttached)
            {
                IO.Debug.WriteLine("<ScheduleDebug> Sleeping the operation of '{0}' on task '{1}'.", op.Name, Task.CurrentId);
                Monitor.Wait(this.SyncObject);
                IO.Debug.WriteLine("<ScheduleDebug> Waking up the operation of '{0}' on task '{1}'.", op.Name, Task.CurrentId);
            }

            this.ThrowExecutionCanceledExceptionIfDetached();
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
                this.ThrowExecutionCanceledExceptionIfDetached();

                var op = ExecutingOperation.Value;
                return op != null && op == this.ScheduledOperation && op is TAsyncOperation expected ? expected : default;
            }
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
        /// Checks if the currently executing operation is controlled by the runtime.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void CheckExecutingOperationIsControlled()
        {
            lock (this.SyncObject)
            {
                var op = ExecutingOperation.Value;
                if (op is null)
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
                }
            }
        }

        internal void NotifyUnhandledException(Exception ex, string message)
        {
            lock (this.SyncObject)
            {
                if (this.UnhandledException is null)
                {
                    this.UnhandledException = ex;
                }

                this.NotifyAssertionFailure(message, killTasks: true, cancelExecution: false);
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
                    this.Runtime.LogWriter.LogAssertionFailure(string.Format("<StackTrace> {0}", ConstructStackTrace()));
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
                    this.Detach(cancelExecution);
                }
            }
        }

        private static string ConstructStackTrace()
        {
            StringBuilder sb = new StringBuilder();
            string[] lines = new StackTrace().ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                // if (!line.StartsWith("   at Microsoft.Coyote.SystematicTesting"))
                {
                    sb.AppendLine(line);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Waits until the scheduler terminates.
        /// </summary>
        internal Task WaitAsync() => this.CompletionSource.Task;

        /// <summary>
        /// Detaches the scheduler releasing all controlled operations.
        /// </summary>
        private void Detach(bool cancelExecution = true)
        {
            this.IsAttached = false;

            // Cancel any remaining operations at the end of the schedule.
            foreach (var op in this.OperationMap.Values)
            {
                if (op.Status != AsyncOperationStatus.Completed)
                {
                    op.Status = AsyncOperationStatus.Canceled;
                }
            }

            Monitor.PulseAll(this.SyncObject);

            // Check if the completion source is completed, else set its result.
            if (!this.CompletionSource.Task.IsCompleted)
            {
                this.CompletionSource.SetResult(true);
            }

            if (cancelExecution)
            {
                // Throw exception to force terminate the current operation.
                throw new ExecutionCanceledException();
            }
        }

        /// <summary>
        /// If scheduler is detached, throw exception to force terminate the caller.
        /// </summary>
        private void ThrowExecutionCanceledExceptionIfDetached()
        {
            if (!this.IsAttached)
            {
                throw new ExecutionCanceledException();
            }
        }

        private static void OnAsyncLocalExecutingOperationValueChanged(AsyncLocalValueChangedArgs<AsyncOperation> args)
        {
            if (args.ThreadContextChanged && args.PreviousValue != null && args.CurrentValue != null)
            {
                // Restore the value if it changed due to a change in the thread context,
                // but the previous and current value where not null.
                ExecutingOperation.Value = args.PreviousValue;
            }
        }
    }
}
