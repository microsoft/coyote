// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.TestingServices.Runtime;
using Microsoft.Coyote.TestingServices.Scheduling.Strategies;
using Microsoft.Coyote.TestingServices.Tracing.Schedule;

namespace Microsoft.Coyote.TestingServices.Scheduling
{
    /// <summary>
    /// Implements a scheduler that serializes and schedules controlled operations.
    /// </summary>
    [DebuggerStepThrough]
    internal sealed class OperationScheduler
    {
        /// <summary>
        /// The configuration used by the scheduler.
        /// </summary>
        internal readonly Configuration Configuration;

        /// <summary>
        /// The testing runtime.
        /// </summary>
        private readonly SystematicTestingRuntime Runtime;

        /// <summary>
        /// The scheduling strategy to be used for state-space exploration.
        /// </summary>
        private readonly ISchedulingStrategy Strategy;

        /// <summary>
        /// Map from unique ids to asynchronous operations.
        /// </summary>
        private readonly Dictionary<ulong, MachineOperation> OperationMap;

        /// <summary>
        /// Map from ids of tasks that are controlled by the runtime to operations.
        /// </summary>
        internal readonly ConcurrentDictionary<int, MachineOperation> ControlledTaskMap;

        /// <summary>
        /// The program schedule trace.
        /// </summary>
        internal ScheduleTrace ScheduleTrace;

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
        internal MachineOperation ScheduledOperation { get; private set; }

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
        internal OperationScheduler(SystematicTestingRuntime runtime, ISchedulingStrategy strategy,
            ScheduleTrace trace, Configuration configuration)
        {
            this.Configuration = configuration;
            this.Runtime = runtime;
            this.Strategy = strategy;
            this.OperationMap = new Dictionary<ulong, MachineOperation>();
            this.ControlledTaskMap = new ConcurrentDictionary<int, MachineOperation>();
            this.ScheduleTrace = trace;
            this.CompletionSource = new TaskCompletionSource<bool>();
            this.IsRunning = true;
            this.BugFound = false;
            this.HasFullyExploredSchedule = false;
        }

        /// <summary>
        /// Schedules the next enabled operation.
        /// </summary>
        internal void ScheduleNextEnabledOperation()
        {
            int? taskId = Task.CurrentId;

            // If the caller is the root task, then return.
            if (taskId != null && taskId == this.Runtime.RootTaskId)
            {
                return;
            }

            if (!this.IsRunning)
            {
                this.Stop();

                // If scheduler is not running, throw exception to force terminate the caller.
                throw new ExecutionCanceledException();
            }

            // Checks if concurrency not controlled by the runtime was used.
            this.CheckNoExternalConcurrencyUsed();

            // Checks if the scheduling steps bound has been reached.
            this.CheckIfSchedulingStepsBoundIsReached();

            // Get and order the operations by their id.
            var ops = this.OperationMap.Values.OrderBy(op => op.SourceId).Select(op => op as IAsyncOperation).ToList();

            // Try enable any operation that is currently waiting, but has its dependencies already satisfied.
            foreach (var op in ops)
            {
                if (op is MachineOperation machineOp)
                {
                    machineOp.TryEnable();
                    IO.Debug.WriteLine("<ScheduleDebug> Operation '{0}' has status '{1}'.", op.SourceId, op.Status);
                }
            }

            MachineOperation current = this.ScheduledOperation;
            if (!this.Strategy.GetNext(out IAsyncOperation next, ops, current))
            {
                // Checks if the program has deadlocked.
                this.CheckIfProgramHasDeadlocked(ops.Select(op => op as MachineOperation));

                IO.Debug.WriteLine("<ScheduleDebug> Schedule explored.");
                this.HasFullyExploredSchedule = true;
                this.Stop();
                throw new ExecutionCanceledException();
            }

            this.ScheduledOperation = next as MachineOperation;
            this.ScheduleTrace.AddSchedulingChoice(next.SourceId);

            IO.Debug.WriteLine($"<ScheduleDebug> Scheduling the next operation of '{next.SourceName}'.");

            if (current != next)
            {
                current.IsActive = false;
                lock (next)
                {
                    this.ScheduledOperation.IsActive = true;
                    System.Threading.Monitor.PulseAll(next);
                }

                lock (current)
                {
                    if (!current.IsHandlerRunning)
                    {
                        return;
                    }

                    if (!this.ControlledTaskMap.ContainsKey(Task.CurrentId.Value))
                    {
                        this.ControlledTaskMap.TryAdd(Task.CurrentId.Value, current);
                        IO.Debug.WriteLine($"<ScheduleDebug> Operation '{current.SourceId}' is associated with task '{Task.CurrentId}'.");
                    }

                    while (!current.IsActive)
                    {
                        IO.Debug.WriteLine($"<ScheduleDebug> Sleeping the current operation of '{current.SourceName}' on task '{Task.CurrentId}'.");
                        System.Threading.Monitor.Wait(current);
                        IO.Debug.WriteLine($"<ScheduleDebug> Waking up the current operation of '{current.SourceName}' on task '{Task.CurrentId}'.");
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
        internal bool GetNextNondeterministicBooleanChoice(int maxValue, string uniqueId = null)
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

            if (!this.Strategy.GetNextBooleanChoice(maxValue, out bool choice))
            {
                IO.Debug.WriteLine("<ScheduleDebug> Schedule explored.");
                this.Stop();
                throw new ExecutionCanceledException();
            }

            if (uniqueId is null)
            {
                this.ScheduleTrace.AddNondeterministicBooleanChoice(choice);
            }
            else
            {
                this.ScheduleTrace.AddFairNondeterministicBooleanChoice(uniqueId, choice);
            }

            return choice;
        }

        /// <summary>
        /// Returns the next nondeterministic integer choice.
        /// </summary>
        internal int GetNextNondeterministicIntegerChoice(int maxValue)
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

            if (!this.Strategy.GetNextIntegerChoice(maxValue, out int choice))
            {
                IO.Debug.WriteLine("<ScheduleDebug> Schedule explored.");
                this.Stop();
                throw new ExecutionCanceledException();
            }

            this.ScheduleTrace.AddNondeterministicIntegerChoice(choice);

            return choice;
        }

        /// <summary>
        /// Waits for the specified asynchronous operation to start.
        /// </summary>
        internal void WaitForOperationToStart(MachineOperation op)
        {
            lock (op)
            {
                if (this.OperationMap.Count == 1)
                {
                    op.IsActive = true;
                    System.Threading.Monitor.PulseAll(op);
                }
                else
                {
                    while (!op.IsHandlerRunning)
                    {
                        System.Threading.Monitor.Wait(op);
                    }
                }
            }
        }

        /// <summary>
        /// Notify that the specified asynchronous operation has been created
        /// and will start executing on the specified task.
        /// </summary>
        internal void NotifyOperationCreated(MachineOperation op, Task task)
        {
            IO.Debug.WriteLine($"<ScheduleDebug> Mapping operation '{op.SourceName}' to task '{task.Id}'.");
            this.ControlledTaskMap.TryAdd(task.Id, op);
            if (!this.OperationMap.ContainsKey(op.SourceId))
            {
                if (this.OperationMap.Count == 0)
                {
                    this.ScheduledOperation = op;
                }

                this.OperationMap.Add(op.SourceId, op);
            }
        }

        /// <summary>
        /// Notify that the specified asynchronous operation has started.
        /// </summary>
        internal static void NotifyOperationStarted(MachineOperation op)
        {
            IO.Debug.WriteLine($"<ScheduleDebug> Starting the current operation of '{op.SourceName}' on task '{Task.CurrentId}'.");

            lock (op)
            {
                op.IsHandlerRunning = true;
                System.Threading.Monitor.PulseAll(op);
                while (!op.IsActive)
                {
                    IO.Debug.WriteLine($"<ScheduleDebug> Sleeping the current operation of '{op.SourceName}' on task '{Task.CurrentId}'.");
                    System.Threading.Monitor.Wait(op);
                    IO.Debug.WriteLine($"<ScheduleDebug> Waking up the current operation of '{op.SourceName}' on task '{Task.CurrentId}'.");
                }

                if (op.Status != AsyncOperationStatus.Enabled)
                {
                    throw new ExecutionCanceledException();
                }
            }
        }

        /// <summary>
        /// Notify that an assertion has failed.
        /// </summary>
        [DebuggerHidden]
        internal void NotifyAssertionFailure(string text, bool killTasks = true, bool cancelExecution = true)
        {
            if (!this.BugFound)
            {
                this.BugReport = text;

                this.Runtime.LogWriter.OnError($"<ErrorLog> {text}");
                this.Runtime.LogWriter.OnStrategyError(this.Configuration.SchedulingStrategy, this.Strategy.GetDescription());

                this.BugFound = true;

                if (this.Configuration.AttachDebugger)
                {
                    Debugger.Break();
                }
            }

            if (killTasks)
            {
                this.Stop();
            }

            if (cancelExecution)
            {
                throw new ExecutionCanceledException();
            }
        }

        /// <summary>
        /// Returns the enabled schedulable ids.
        /// </summary>
        internal HashSet<ulong> GetEnabledSchedulableIds()
        {
            var enabledSchedulableIds = new HashSet<ulong>();
            foreach (var machineInfo in this.OperationMap.Values)
            {
                if (machineInfo.Status is AsyncOperationStatus.Enabled)
                {
                    enabledSchedulableIds.Add(machineInfo.SourceId);
                }
            }

            return enabledSchedulableIds;
        }

        /// <summary>
        /// Returns a test report with the scheduling statistics.
        /// </summary>
        internal TestReport GetReport()
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

        /// <summary>
        /// Checks that no task that is not controlled by the runtime is currently executing.
        /// </summary>
        [DebuggerHidden]
        internal void CheckNoExternalConcurrencyUsed()
        {
            if (!Task.CurrentId.HasValue || !this.ControlledTaskMap.ContainsKey(Task.CurrentId.Value))
            {
                this.NotifyAssertionFailure(string.Format(CultureInfo.InvariantCulture,
                   "Uncontrolled task with id '{0}' invoked a runtime method. Please make sure to avoid using concurrency APIs " +
                   "such as 'Task.Run', 'Task.Delay' or 'Task.Yield' inside machine handlers or controlled tasks. If you are " +
                   "using external libraries that are executing concurrently, you will need to mock them during testing.",
                   Task.CurrentId.HasValue ? Task.CurrentId.Value.ToString() : "<unknown>"));
            }
        }

        /// <summary>
        /// Checks for a deadlock. This happens when there are no more enabled operations,
        /// but there is one or more blocked operations that are waiting to receive an event
        /// or for a task to complete.
        /// </summary>
        [DebuggerHidden]
        private void CheckIfProgramHasDeadlocked(IEnumerable<MachineOperation> ops)
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
                    message += string.Format(CultureInfo.InvariantCulture, " '{0}'", blockedOnReceiveOperations[i].SourceName);
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
                    message += string.Format(CultureInfo.InvariantCulture, " '{0}'", blockedOnWaitOperations[i].SourceName);
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
                    message += string.Format(CultureInfo.InvariantCulture, " '{0}'", blockedOnResourceSynchronization[i].SourceName);
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
                message += "waiting to access a concurrent resource that is acquired by another task, ";
                message += "but no other controlled tasks are enabled.";
            }

            this.NotifyAssertionFailure(message);
        }

        /// <summary>
        /// Checks if the scheduling steps bound has been reached. If yes,
        /// it stops the scheduler and kills all enabled machines.
        /// </summary>
        [DebuggerHidden]
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
                    this.Stop();
                    throw new ExecutionCanceledException();
                }
            }
        }

        /// <summary>
        /// Waits until the scheduler terminates.
        /// </summary>
        internal Task WaitAsync() => this.CompletionSource.Task;

        /// <summary>
        /// Stops the scheduler.
        /// </summary>
        private void Stop()
        {
            this.IsRunning = false;
            this.KillRemainingOperations();

            // Check if the completion source is completed. If not synchronize on
            // it (as it can only be set once) and set its result.
            if (!this.CompletionSource.Task.IsCompleted)
            {
                lock (this.CompletionSource)
                {
                    if (!this.CompletionSource.Task.IsCompleted)
                    {
                        this.CompletionSource.SetResult(true);
                    }
                }
            }
        }

        /// <summary>
        /// Kills any remaining operations at the end of the schedule.
        /// </summary>
        private void KillRemainingOperations()
        {
            foreach (var op in this.OperationMap.Values)
            {
                op.IsActive = true;
                op.Status = AsyncOperationStatus.Completed;

                if (op.IsHandlerRunning)
                {
                    lock (op)
                    {
                        System.Threading.Monitor.PulseAll(op);
                    }
                }
            }
        }
    }
}
