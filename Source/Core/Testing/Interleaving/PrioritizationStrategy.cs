// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Interleaving
{
    /// <summary>
    /// A probabilistic priority-based scheduling strategy.
    /// </summary>
    /// <remarks>
    /// This strategy is based on the PCT algorithm described in the following paper:
    /// https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/asplos277-pct.pdf.
    /// </remarks>
    internal sealed class PrioritizationStrategy : InterleavingStrategy
    {
        /// <summary>
        /// List of prioritized operation groups.
        /// </summary>
        private readonly List<OperationGroup> PrioritizedOperationGroups;

        /// <summary>
        /// Scheduling points in the current execution where a priority change should occur.
        /// </summary>
        private readonly HashSet<int> PriorityChangePoints;

        /// <summary>
        /// Number of potential priority change points in the current iteration.
        /// </summary>
        private int NumPriorityChangePoints;

        /// <summary>
        /// Max number of potential priority change points across all iterations.
        /// </summary>
        private int MaxPriorityChangePoints;

        /// <summary>
        /// Max number of priority changes per iteration.
        /// </summary>
        private readonly int MaxPriorityChanges;

        private int ActualNumberOfPrioritySwitches = 0;

        private readonly HashSet<ControlledOperation> registeredOps;

        private int ContextSwitchNumber = 0;

        private int MaxNumberOfOperationGroups = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrioritizationStrategy"/> class.
        /// </summary>
        internal PrioritizationStrategy(Configuration configuration, IRandomValueGenerator generator)
            : base(configuration, generator, false)
        {
            this.PrioritizedOperationGroups = new List<OperationGroup>();
            this.PriorityChangePoints = new HashSet<int>();
            this.NumPriorityChangePoints = 0;
            this.MaxPriorityChangePoints = 0;
            this.MaxPriorityChanges = configuration.StrategyBound;
            this.ActualNumberOfPrioritySwitches = 0;
            this.registeredOps = new HashSet<ControlledOperation>();
            this.ContextSwitchNumber = 0;
            this.MaxNumberOfOperationGroups = 0;
        }

        internal void PrintTaskPCTStatsForIteration(uint iteration)
        {
            Console.WriteLine(string.Empty);
            Console.WriteLine($"===========<IMP_TaskPCTStrategy> [PrintTaskPCTStatsForIteration] TASK-PCT STATS for ITERATION: {iteration}");
            Console.WriteLine($"                  TOTAL ASYNC OPS at the end: {this.registeredOps.Count}");
            Console.WriteLine($"                  MAX TOTAL (#PRIORITIES) throughout the iteration: {this.MaxNumberOfOperationGroups}");
            Console.WriteLine($"                  TOTAL (#PRIORITIES) at the end: {this.PrioritizedOperationGroups.Count}");
            Console.WriteLine($"                  #PRIORITY_SWITCHES: {this.ActualNumberOfPrioritySwitches}");

            for (int idx = 0; idx < this.PrioritizedOperationGroups.Count; idx++)
            {
                var group = this.PrioritizedOperationGroups[idx];
                if (group.Any(m => m.Status is OperationStatus.Enabled))
                {
                    Console.WriteLine("  |_ [{0}] operation group with id '{1}' [enabled] whose OWNER is: {2} has PRIORITY: '{3}'.", idx, group, group.Owner, this.PrioritizedOperationGroups.IndexOf(group));
                    group.DebugPrintMembers();
                }
                else if (group.Any(m => m.Status != OperationStatus.Completed))
                {
                    Console.WriteLine("  |_ [{0}] operation group with id '{1}' whose OWNER is: {2} has PRIORITY: '{3}'.", idx, group, group.Owner, this.PrioritizedOperationGroups.IndexOf(group));
                    group.DebugPrintMembers();
                }
            }

            // this.DebugPrintOperationPriorityList();
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            // The first iteration has no knowledge of the execution, so only initialize from the second
            // iteration and onwards. Note that although we could initialize the first length based on a
            // heuristic, its not worth it, as the strategy will typically explore thousands of iterations,
            // plus its also interesting to explore a schedule with no forced priority switch points.
            if (iteration > 0)
            {
                this.ActualNumberOfPrioritySwitches = 0;

                // FN_TODO: print the stat for the last iteration also
                this.PrintTaskPCTStatsForIteration(iteration - 1);

                this.PrioritizedOperationGroups.Clear();
                this.PriorityChangePoints.Clear();

                this.MaxPriorityChangePoints = Math.Max(
                    this.MaxPriorityChangePoints, this.NumPriorityChangePoints);
                if (this.MaxPriorityChanges > 0)
                {
                    var priorityChanges = this.RandomValueGenerator.Next(this.MaxPriorityChanges) + 1;
                    var range = Enumerable.Range(0, this.MaxPriorityChangePoints);
                    foreach (int point in this.Shuffle(range).Take(priorityChanges))
                    {
                        this.PriorityChangePoints.Add(point);
                    }
                }

                this.DebugPrintPriorityChangePoints();
                this.registeredOps.Clear();
                this.ContextSwitchNumber = 0;
                this.MaxNumberOfOperationGroups = 0;
            }

            this.NumPriorityChangePoints = 0;
            this.StepCount = 0;
            return true;
        }

        // FOR DEBUGGING
        private void DebugPrintBeforeGetNextOperation(IEnumerable<ControlledOperation> opss)
        {
            this.ContextSwitchNumber += 1;
            var ops = opss.ToList();
            IO.Debug.WriteLine($"          ops.Count = {ops.Count}");
            int countt = 0;
            foreach (var op in ops)
            {
                if (countt == 0)
                {
                    IO.Debug.Write($"          {op}");
                }
                else
                {
                    IO.Debug.Write($", {op}");
                }

                countt++;
            }

            IO.Debug.WriteLine(string.Empty);

            countt = 0;
            foreach (var op in ops)
            {
                if (countt == 0)
                {
                    IO.Debug.Write($"          {op.Status}");
                }
                else
                {
                    IO.Debug.Write($", {op.Status}");
                }

                countt++;
            }

            IO.Debug.WriteLine(string.Empty);

            countt = 0;
            foreach (var op in ops)
            {
                if (countt == 0)
                {
                    IO.Debug.Write($"          {op.GetType()}");
                }
                else
                {
                    IO.Debug.Write($", {op.GetType()}");
                }

                countt++;
            }

            IO.Debug.WriteLine(string.Empty);

            HashSet<ControlledOperation> newConcurrentOps = new HashSet<ControlledOperation>();
            foreach (var op in ops)
            {
                if (!this.registeredOps.Contains(op))
                {
                    newConcurrentOps.Add(op);
                    this.registeredOps.Add(op);
                }
            }

            IO.Debug.WriteLine($"          # new operations added {newConcurrentOps.Count}");
            // Specification.Assert((newConcurrentOps.Count <= 1) || (newConcurrentOps.Count == 2 && this.ContextSwitchNumber == 1),
            //     $"     <TaskSummaryLog-ERROR> At most one new operation must be added across context switch.");
            if (!((newConcurrentOps.Count <= 1) || (newConcurrentOps.Count == 2 && this.ContextSwitchNumber == 1)))
            {
                Console.WriteLine($"     <TaskSummaryLog-ERROR> At most one new operation must be added across context switch.");
            }

            int cases = 0;

            // FOR TASKS_EXECUTION_VISUALIZATION_GRAPHS WORK
            if (newConcurrentOps.Count == 0)
            {
                Console.WriteLine($"     <TaskSummaryLog> T-case 1.): No new task added.");
                cases = 1;
            }

            foreach (var op in newConcurrentOps)
            {
                IO.Debug.WriteLine($"          newConcurrentOps: {op}, Spawner: {op.ParentTask}");
                if (op.IsContinuationTask)
                {
                    if (op.ParentTask == null)
                    {
                        Console.WriteLine($"     <TaskSummaryLog> T-case 3.): Continuation task {op} (id = {op.Id}) is the first task to be created!");
                    }
                    else
                    {
                        Console.WriteLine($"     <TaskSummaryLog> T-case 3.): Continuation task {op} (id = {op.Id}) created by {op.ParentTask} (id = {op.ParentTask.Id}).");
                    }

                    cases = 3;
                }
                else
                {
                    if (op.ParentTask == null)
                    {
                        Console.WriteLine($"     <TaskSummaryLog> T-case 2.): Spawn task {op} (id = {op.Id}) is the first task to be created!");
                    }
                    else
                    {
                        Console.WriteLine($"     <TaskSummaryLog> T-case 2.): Spawn task {op} (id = {op.Id}) created by {op.ParentTask} (id = {op.ParentTask.Id}).");
                    }

                    cases = 2;
                }
            }

            // Specification.Assert( (cases == 1) || (cases == 2) || (cases == 3),
            //     $"     <TaskSummaryLog-ERROR> At most one new operation must be added across context switch.");
            if (!((cases == 1) || (cases == 2) || (cases == 3)))
            {
                Console.WriteLine($"     <TaskSummaryLog-ERROR> At most one new operation must be added across context switch.");
            }

            // IO.Debug.WriteLine(string.Empty);
        }

        // FOR DEBUGGING
        private static void DebugPrintAfterGetNextOperation(ControlledOperation next)
        {
            IO.Debug.WriteLine($"          next = {next}");
            Console.WriteLine($"     <TaskSummaryLog> Scheduled: {next}");
        }

        private void RemoveEmptyOperationGroups()
        {
            List<OperationGroup> operationGroupsToRemove = new List<OperationGroup>();
            foreach (var group in this.PrioritizedOperationGroups)
            {
                if (group.GetMembersCount() == 0)
                {
                    operationGroupsToRemove.Add(group);
                }
            }

            foreach (var group in operationGroupsToRemove)
            {
                this.PrioritizedOperationGroups.Remove(group);
            }
        }

        private void HandleOperationsDoingMoveNext()
        {
            foreach (var op in this.registeredOps.Where(op => !op.LastMoveNextHandled))
            {
                op.Group.RemoveMember(op);
                op.Group = op.ParentTask.Group;
                op.ParentTask.Group.RegisterMember(op);
            }
        }

        private void HandleOperationsDoingDelay(int delaysDeprioritizationThresholdInt)
        {
            // Deprioritize all the groups of all the operations which called a Task.delay API
            foreach (var op in this.registeredOps.Where(op => op.NumDelayTasksExecuted > delaysDeprioritizationThresholdInt))
            {
                Console.WriteLine($"####################<F_PrioritizationStrategy> [HandleOperationsDoingDelay] priority of group of op: {op} = {this.PrioritizedOperationGroups.IndexOf(op.Group)} is lowered as it called Task.Delay() {op.NumDelayTasksExecuted} times which is >= than DelaysDeprioritizationThresholdInt = {delaysDeprioritizationThresholdInt}.");

                // FN_TODO: should we deprioritice groups of those tasks whose parent has also called Task.delay?

                string envDelaysDeprioritizeBy1 = Environment.GetEnvironmentVariable("DELAYS_DEPRIORITIZE_BY_1"); // NOTE: OLP_TEST_VERBOSITY muse be string, either "true" or "false"
                bool envDelaysDeprioritizeBy1Bool = false;
                if (envDelaysDeprioritizeBy1 != null)
                {
                    envDelaysDeprioritizeBy1Bool = bool.Parse(envDelaysDeprioritizeBy1);
                }

                // FN_TODO: should we make sure that we deprioritize only highest priority groups calling Delays?
                if (envDelaysDeprioritizeBy1Bool)
                {
                    int index = this.PrioritizedOperationGroups.IndexOf(op.Group);
                    this.PrioritizedOperationGroups.Remove(op.Group);
                    if (this.PrioritizedOperationGroups.Count == 0)
                    {
                        this.PrioritizedOperationGroups.Add(op.Group);
                    }
                    else
                    {
                        this.PrioritizedOperationGroups.Insert(index + 1, op.Group);
                    }
                }
                else
                {
                    this.PrioritizedOperationGroups.Remove(op.Group);
                    this.PrioritizedOperationGroups.Add(op.Group);
                }

                // op.LowerPriorityDueToDelay = false;
                op.NumDelayTasksExecuted = 0;
            }
        }

        /// <inheritdoc/>
        internal override bool GetNextOperation(IEnumerable<ControlledOperation> ops, ControlledOperation current,
            bool isYielding, out ControlledOperation next)
        {
            this.DebugPrintBeforeGetNextOperation(ops);
            this.HandleOperationsDoingMoveNext();

            // keep DELAYS_DEPRIORITIZATION_THRESHOLD <= 0 if do not want to deprioritize on delays (INFINITE_THRESHOLD).
            string envDelaysDeprioritizationThreshold = Environment.GetEnvironmentVariable("DELAYS_DEPRIORITIZATION_THRESHOLD"); // NOTE: OLP_TEST_VERBOSITY muse be string, either "true" or "false"
            int envDelaysDeprioritizationThresholdInt = 1;
            if (envDelaysDeprioritizationThreshold != null)
            {
                envDelaysDeprioritizationThresholdInt = int.Parse(envDelaysDeprioritizationThreshold);
            }

            if (envDelaysDeprioritizationThresholdInt > 0)
            {
                this.HandleOperationsDoingDelay(envDelaysDeprioritizationThresholdInt);
            }

            this.RemoveEmptyOperationGroups();
            // Set the priority of any new operation groups.
            this.SetNewOperationGroupPriorities(ops, current);

            // Check if there are at least two operation groups that can be scheduled,
            // otherwise skip the priority checking and changing logic.
            if (ops.Select(op => op.Group).Skip(1).Any())
            {
                // Try to change the priority of the highest priority operation group.
                // If the shared-state reduction is enabled, check if there is at least
                // one 'WRITE' operation, before trying to change the priority.
                if (!this.Configuration.IsSharedStateReductionEnabled ||
                    ops.Any(op => op.LastSchedulingPoint is SchedulingPointType.Write))
                {
                    this.TryPrioritizeNextOperationGroup(ops);
                }

                // Get the operations that belong to the highest priority group.
                OperationGroup nextGroup = this.GetOperationGroupWithHighestPriority(ops);
                ops = ops.Where(op => nextGroup.IsMember(op));
            }

            int idx = 0;
            string envRandomChains = System.Environment.GetEnvironmentVariable("TASK_PCT_RANDOM_INSIDE_CHAINS");
            bool envRandomChainsBool = false;
            if (envRandomChains != null)
            {
                envRandomChainsBool = bool.Parse(envRandomChains);
            }

            if (envRandomChainsBool)
            {
                idx = this.RandomValueGenerator.Next(ops.Count());
            }

            next = ops.ElementAt(idx);
            this.StepCount++;
            DebugPrintAfterGetNextOperation(next);
            this.DebugPrintOperationPriorityList();
            this.MaxNumberOfOperationGroups = Math.Max(this.MaxNumberOfOperationGroups, this.PrioritizedOperationGroups.Count);
            return true;
        }

        /// <summary>
        /// Returns the operation group with the highest priority.
        /// </summary>
        private OperationGroup GetOperationGroupWithHighestPriority(IEnumerable<ControlledOperation> ops)
        {
            foreach (var group in this.PrioritizedOperationGroups)
            {
                if (ops.Any(op => op.Group == group))
                {
                    return group;
                }
            }

            return null;
        }

        /// <summary>
        /// Sets a random priority to any new operation groups.
        /// </summary>
        private void SetNewOperationGroupPriorities(IEnumerable<ControlledOperation> ops, ControlledOperation current)
        {
            int count = this.PrioritizedOperationGroups.Count;
            if (count is 0)
            {
                this.PrioritizedOperationGroups.Add(current.Group);
            }

            // Randomize the priority of all new operation groups.
            foreach (var group in ops.Select(op => op.Group).Where(g => !this.PrioritizedOperationGroups.Contains(g)))
            {
                // Randomly choose a priority for this group.
                int index = this.RandomValueGenerator.Next(this.PrioritizedOperationGroups.Count) + 1;
                this.PrioritizedOperationGroups.Insert(index, group);
                Debug.WriteLine("<ScheduleLog> Assigned priority '{0}' for operation group '{1}'.", index, group);
            }

            if (this.PrioritizedOperationGroups.Count > count)
            {
                this.DebugPrintOperationPriorityList();
            }
        }

        /// <summary>
        /// Reduces the priority of highest priority operation group, if there is a priority
        /// change point installed on the current execution step.
        /// </summary>
        private bool TryPrioritizeNextOperationGroup(IEnumerable<ControlledOperation> ops)
        {
            OperationGroup group = null;
            if (this.PriorityChangePoints.Contains(this.NumPriorityChangePoints))
            {
                // This scheduling step was chosen as a priority change point.
                group = this.GetOperationGroupWithHighestPriority(ops);
                Debug.WriteLine("<ScheduleLog> Reduced the priority of operation group '{0}'.", group);
            }

            this.NumPriorityChangePoints++;
            if (group != null)
            {
                this.ActualNumberOfPrioritySwitches++;
                // Reduce the priority of the group by putting it in the end of the list.
                this.PrioritizedOperationGroups.Remove(group);
                this.PrioritizedOperationGroups.Add(group);
                return true;
            }

            return false;
        }

        /// <inheritdoc/>
        internal override bool GetNextBooleanChoice(ControlledOperation current, int maxValue, out bool next)
        {
            next = false;
            if (this.RandomValueGenerator.Next(maxValue) is 0)
            {
                next = true;
            }

            this.StepCount++;
            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextIntegerChoice(ControlledOperation current, int maxValue, out int next)
        {
            next = this.RandomValueGenerator.Next(maxValue);
            this.StepCount++;
            return true;
        }

        /// <inheritdoc/>
        internal override int GetStepCount() => this.StepCount;

        /// <inheritdoc/>
        internal override bool IsMaxStepsReached()
        {
            if (this.MaxSteps is 0)
            {
                return false;
            }

            return this.StepCount >= this.MaxSteps;
        }

        /// <inheritdoc/>
        internal override string GetDescription() =>
            $"prioritization[bound:{this.MaxPriorityChanges},seed:{this.RandomValueGenerator.Seed}]";

        /// <summary>
        /// Shuffles the specified range using the Fisher-Yates algorithm.
        /// </summary>
        /// <remarks>
        /// See https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle.
        /// </remarks>
        private IList<int> Shuffle(IEnumerable<int> range)
        {
            var result = new List<int>(range);
            for (int idx = result.Count - 1; idx >= 1; idx--)
            {
                int point = this.RandomValueGenerator.Next(result.Count);
                int temp = result[idx];
                result[idx] = result[point];
                result[point] = temp;
            }

            return result;
        }

        /// <inheritdoc/>
        internal override void Reset()
        {
            this.StepCount = 0;
            this.NumPriorityChangePoints = 0;
            this.PrioritizedOperationGroups.Clear();
            this.PriorityChangePoints.Clear();
        }

        /// <summary>
        /// Print the operation group priority list, if debug is enabled.
        /// </summary>
        private void DebugPrintOperationPriorityList()
        {
            if (Debug.IsEnabled)
            {
                Debug.WriteLine("<ScheduleLog> Updated operation group priority list: ");
                for (int idx = 0; idx < this.PrioritizedOperationGroups.Count; idx++)
                {
                    var group = this.PrioritizedOperationGroups[idx];
                    if (group.Any(m => m.Status is OperationStatus.Enabled))
                    {
                        Debug.WriteLine("  |_ [{0}] operation group with id '{1}' [enabled] whose OWNER is: {2} has PRIORITY: '{3}'.", idx, group, group.Owner, this.PrioritizedOperationGroups.IndexOf(group));
                        group.DebugPrintMembers();
                    }
                    else if (group.Any(m => m.Status != OperationStatus.Completed))
                    {
                        Debug.WriteLine("  |_ [{0}] operation group with id '{1}' whose OWNER is: {2} has PRIORITY: '{3}'.", idx, group, group.Owner, this.PrioritizedOperationGroups.IndexOf(group));
                        group.DebugPrintMembers();
                    }
                }
            }
        }

        /// <summary>
        /// Print the priority change points, if debug is enabled.
        /// </summary>
        private void DebugPrintPriorityChangePoints()
        {
            if (Debug.IsEnabled && this.PriorityChangePoints.Count > 0)
            {
                if (this.PriorityChangePoints.Count > 0)
                {
                    // Sort them before printing for readability.
                    var sortedChangePoints = this.PriorityChangePoints.ToArray();
                    Array.Sort(sortedChangePoints);
                    Debug.WriteLine("<ScheduleLog> Assigned {0} priority change points: {1}.",
                        sortedChangePoints.Length, string.Join(", ", sortedChangePoints));
                }
                else
                {
                    Debug.WriteLine("<ScheduleLog> Assigned 0 priority change points.");
                }
            }
        }
    }
}
