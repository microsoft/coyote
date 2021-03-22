// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Systematic
{
    /// <summary>
    /// Scheduler that controls the execution of the attached program. Under this scheduler, the execution
    /// is sequentialized and split into turns. In each turn, the scheduler uses the installed scheduling
    /// strategy to choose a single operation to execute until the end of the turn, which is also called
    /// a scheduling point. This turn-based program execution continues until the program naturaly terminates,
    /// a bug is found, or a (user defined) scheduling budget has been reached.
    /// </summary>
    /// <remarks>
    /// Invoking the scheduler is thread-safe.
    /// </remarks>
#if !DEBUG
    [DebuggerStepThrough]
#endif
    internal sealed class TurnBasedScheduler
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
        /// Responsible for controlling the program execution during systematic testing.
        /// </summary>
        private readonly CoyoteRuntime Runtime;

        /// <summary>
        /// The scheduling strategy used for program exploration.
        /// </summary>
        private readonly SchedulingStrategy Strategy;

        /// <summary>
        /// Map from unique ids to asynchronous operations.
        /// </summary>
        private readonly Dictionary<ulong, AsyncOperation> OperationMap;

        /// <summary>
        /// The program schedule trace.
        /// </summary>
        private readonly ScheduleTrace ScheduleTrace;

        /// <summary>
        /// Object that is used to synchronize access to the scheduler.
        /// </summary>
        private readonly object SyncObject;

        /// <summary>
        /// The scheduler completion source.
        /// </summary>
        private readonly TaskCompletionSource<bool> CompletionSource;

        /// <summary>
        /// The currently scheduled operation.
        /// </summary>
        private AsyncOperation ScheduledOperation;

        /// <summary>
        /// The number of scheduled steps.
        /// </summary>
        private int ScheduledSteps => this.Strategy.GetScheduledSteps();

        /// <summary>
        /// True if the max scheduling steps bound has been reached in the current execution.
        /// </summary>
        private bool IsMaxScheduledStepsBoundReached => this.Strategy.HasReachedMaxSchedulingSteps();

        /// <summary>
        /// Checks if the schedule has been fully explored.
        /// </summary>
        internal bool HasFullyExploredSchedule { get; private set; }

        /// <summary>
        /// True if a bug was found, else false.
        /// </summary>
        internal bool IsBugFound { get; private set; }

        /// <summary>
        /// Bug report.
        /// </summary>
        internal string BugReport { get; private set; }

        /// <summary>
        /// Associated with the bug report is an optional unhandled exception.
        /// </summary>
        internal Exception UnhandledException { get; private set; }

        /// <summary>
        /// True if the scheduler is attached to the executing program, else false.
        /// </summary>
        internal bool IsAttached { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TurnBasedScheduler"/> class.
        /// </summary>
        internal TurnBasedScheduler(CoyoteRuntime runtime, SchedulingStrategy strategy, Configuration configuration)
        {
            this.Configuration = configuration;
            this.Runtime = runtime;
            this.Strategy = strategy;
            this.OperationMap = new Dictionary<ulong, AsyncOperation>();
            this.ScheduleTrace = new ScheduleTrace();
            this.SyncObject = new object();
            this.CompletionSource = new TaskCompletionSource<bool>();
            this.IsAttached = true;
            this.IsBugFound = false;
            this.HasFullyExploredSchedule = false;
        }

        /// <summary>
        /// Initializes the next testing iteration.
        /// </summary>
        internal bool InitializeNextIteration(uint iteration) => this.Strategy.InitializeNextIteration(iteration);

        /// <summary>
        /// Creates the specified operation.
        /// </summary>
        /// <param name="op">The operation to create.</param>
        /// <returns>True if the operation was successfully created, else false if it already exists.</returns>
        internal bool CreateOperation(AsyncOperation op)
        {
            lock (this.SyncObject)
            {
                if (this.OperationMap.Count is 0)
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
        /// Starts the execution of the specified operation.
        /// </summary>
        /// <param name="op">The operation to start executing.</param>
        /// <remarks>
        /// This method performs a handshake with <see cref="WaitOperationStart"/>.
        /// </remarks>
        internal void StartOperation(AsyncOperation op)
        {
            lock (this.SyncObject)
            {
                IO.Debug.WriteLine($"<ScheduleDebug> Starting the operation of '{0}' on task '{1}'.", op.Name, Task.CurrentId);

                // Enable the operation and store it in the async local context.
                op.Status = AsyncOperationStatus.Enabled;
                ExecutingOperation.Value = op;
                this.PauseOperation(op);
            }
        }

        /// <summary>
        /// Waits for the specified operation to start executing.
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
        /// Completes the specified operation.
        /// </summary>
        /// <param name="op">The operation to complete.</param>
        internal void CompleteOperation(AsyncOperation op)
        {
            lock (this.SyncObject)
            {
                IO.Debug.WriteLine("<ScheduleDebug> Completed the operation of '{0}' on task '{1}'.", op.Name, Task.CurrentId);
                op.Status = AsyncOperationStatus.Completed;
            }
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
                    if (ExecutingOperation.Value is null)
                    {
                        ThrowUncontrolledTaskException();
                    }
                }

                // Checks if the scheduling steps bound has been reached.
                this.CheckIfSchedulingStepsBoundIsReached();

                if (this.Configuration.IsProgramStateHashingEnabled)
                {
                    // Update the current operation with the hashed program state.
                    current.HashedProgramState = this.Runtime.GetHashedProgramState();
                }

                // Choose the next operation to schedule, if there is one enabled.
                if (!this.TryGetNextEnabledOperation(current, isYielding, out AsyncOperation next))
                {
                    IO.Debug.WriteLine("<ScheduleDebug> Schedule explored.");
                    this.HasFullyExploredSchedule = true;
                    this.Detach();
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
                this.Runtime.CheckIfExecutionHasDeadlocked(ops);
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
                if (ExecutingOperation.Value is null)
                {
                    ThrowUncontrolledTaskException();
                }

                // Checks if the scheduling steps bound has been reached.
                this.CheckIfSchedulingStepsBoundIsReached();

                if (this.Configuration.IsProgramStateHashingEnabled)
                {
                    // Update the current operation with the hashed program state.
                    this.ScheduledOperation.HashedProgramState = this.Runtime.GetHashedProgramState();
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
                if (ExecutingOperation.Value is null)
                {
                    ThrowUncontrolledTaskException();
                }

                // Checks if the scheduling steps bound has been reached.
                this.CheckIfSchedulingStepsBoundIsReached();

                if (this.Configuration.IsProgramStateHashingEnabled)
                {
                    // Update the current operation with the hashed program state.
                    this.ScheduledOperation.HashedProgramState = this.Runtime.GetHashedProgramState();
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
        /// Tries to enable the specified operation.
        /// </summary>
        /// <remarks>
        /// It is assumed that this method runs in the scope of a 'lock (this.SyncObject)' statement.
        /// </remarks>
        private void TryEnableOperation(AsyncOperation op)
        {
            if (op.Status is AsyncOperationStatus.Delayed && op is TaskDelayOperation delayOp)
            {
                if (!delayOp.TryEnable() && !this.OperationMap.Any(kvp => kvp.Value.Status is AsyncOperationStatus.Enabled))
                {
                    op.Status = AsyncOperationStatus.Enabled;
                }
            }
            else if (op.Status != AsyncOperationStatus.Enabled)
            {
                op.TryEnable();
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
                if (op is null)
                {
                    ThrowUncontrolledTaskException();
                }

                return op.Equals(this.ScheduledOperation) && op is TAsyncOperation expected ? expected : default;
            }
        }

        /// <summary>
        /// Returns all registered operations.
        /// </summary>
        internal IEnumerable<AsyncOperation> GetRegisteredOperations()
        {
            lock (this.SyncObject)
            {
                return this.OperationMap.Values;
            }
        }

        /// <summary>
        /// Checks if the scheduling steps bound has been reached. If yes,
        /// it stops the scheduler and kills all enabled operations.
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

        /// <summary>
        /// Checks if the currently executing operation is controlled by the runtime.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal static void ThrowUncontrolledTaskException()
        {
            // TODO: figure out if there is a way to get more information about the creator of the
            // uncontrolled task to ease the user debugging experience.
            // Report the invalid operation and then throw it to fail the uncontrolled task.
            // This will most likely crash the program, but we try to fail as cleanly and fast as possible.
            string uncontrolledTask = Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>";
            throw new InvalidOperationException($"Uncontrolled task with id '{uncontrolledTask}' was detected, " +
                "which is not allowed as it can lead to race conditions or deadlocks: either mock the creator " +
                "of the uncontrolled task, or rewrite its assembly.");
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
                if (!this.IsBugFound)
                {
                    this.BugReport = text;

                    this.Runtime.LogWriter.LogAssertionFailure($"<ErrorLog> {text}");
                    this.Runtime.LogWriter.LogAssertionFailure(string.Format("<StackTrace> {0}", ConstructStackTrace()));
                    this.Runtime.RaiseOnFailureEvent(new AssertionFailureException(text));
                    this.Runtime.LogWriter.LogStrategyDescription(this.Configuration.SchedulingStrategy,
                        this.Strategy.GetDescription());

                    this.IsBugFound = true;

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

        /// <summary>
        /// Returns scheduling statistics and results.
        /// </summary>
        internal void GetSchedulingStatisticsAndResults(out bool isBugFound, out string bugReport, out int scheduledSteps,
            out bool isMaxScheduledStepsBoundReached, out bool isScheduleFair, out Exception unhandledException)
        {
            lock (this.SyncObject)
            {
                scheduledSteps = this.ScheduledSteps;
                isMaxScheduledStepsBoundReached = this.IsMaxScheduledStepsBoundReached;
                isScheduleFair = this.Strategy.IsFair();
                isBugFound = this.IsBugFound;
                bugReport = this.BugReport;
                unhandledException = this.UnhandledException;
            }
        }

        /// <summary>
        /// Returns the schedule trace in text format.
        /// </summary>
        internal string GetTrace() => this.ScheduleTrace.GetText(this.Configuration, this.Strategy.IsFair());

        /// <summary>
        /// Creates a new stack trace.
        /// </summary>
        private static string ConstructStackTrace()
        {
            StringBuilder sb = new StringBuilder();
            string[] lines = new StackTrace().ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                if (!line.Contains("at Microsoft.Coyote.SystematicTesting"))
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
