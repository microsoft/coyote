// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Systematic
{
#pragma warning disable SA1005
#pragma warning disable SA1028
#pragma warning disable CA1801
#pragma warning disable CA1822
    /// <summary>
    /// A group-based probabilistic priority-based scheduling strategy.
    /// </summary>
    /// <remarks>
    /// This strategy is based on the <see cref="PCTStrategy"/> strategy.
    /// </remarks>
    internal sealed class PriorityBasedStrategy : SystematicStrategy
    {
        /// <summary>
        /// Random value generator.
        /// </summary>
        private readonly IRandomValueGenerator RandomValueGenerator;

        /// <summary>
        /// List of prioritized groups.
        /// </summary>
        private readonly List<OperationGroup> PrioritizedGroups;

        /// <summary>
        /// Scheduling points in the current execution where a priority change should occur.
        /// </summary>
        private readonly HashSet<int> PriorityChangePoints;

        /// <summary>
        /// Set of values corresponding to shared state that has been accessed
        /// by 'READ' operations across all iterations.
        /// </summary>
        private readonly HashSet<string> ReadAccesses;

        /// <summary>
        /// Set of values corresponding to shared state that has been accessed
        /// by 'WRITE' operations across all iterations.
        /// </summary>
        private readonly HashSet<string> WriteAccesses;

        /// <summary>
        /// Number of potential priority change points in the current iteration.
        /// </summary>
        private int PriorityChangePointsCount;

        /// <summary>
        /// Max potential priority change points across all iterations.
        /// </summary>
        private int MaxPriorityChangePointsCount;

        /// <summary>
        /// Max number of priority changes per iteration.
        /// </summary>
        private readonly int MaxPriorityChanges;

        /// <summary>
        /// The maximum number of steps to explore.
        /// </summary>
        private readonly int MaxSteps;

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityBasedStrategy"/> class.
        /// </summary>
        internal PriorityBasedStrategy(int maxSteps, int maxPriorityChanges, IRandomValueGenerator generator)
        {
            this.RandomValueGenerator = generator;
            this.PrioritizedGroups = new List<OperationGroup>();
            this.PriorityChangePoints = new HashSet<int>();
            this.ReadAccesses = new HashSet<string>();
            this.WriteAccesses = new HashSet<string>();
            this.PriorityChangePointsCount = 0;
            this.MaxPriorityChangePointsCount = 0;
            this.MaxPriorityChanges = maxPriorityChanges;
            this.MaxSteps = maxSteps;
            this.StepCount = 0;

            this.CurrentSchedule = string.Empty;
            this.Path = new List<(string, int, SchedulingPointType, OperationGroup, int, string)>();
            this.Groups = new Dictionary<int, Dictionary<OperationGroup, (bool, bool, bool)>>();
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            this.MustChangeCount.Add(0);

            // The first iteration has no knowledge of the execution, so only initialize from the second
            // iteration and onwards. Note that although we could initialize the first length based on a
            // heuristic, its not worth it, as the strategy will typically explore thousands of iterations,
            // plus its also interesting to explore a schedule with no forced priority switch points.
            if (iteration > 0)
            {
                this.MaxPriorityChangePointsCount = Math.Max(
                    this.MaxPriorityChangePointsCount, this.PriorityChangePointsCount);

                this.PrioritizedGroups.Clear();
                this.PriorityChangePoints.Clear();

                var range = Enumerable.Range(0, this.MaxPriorityChangePointsCount);
                foreach (int point in this.Shuffle(range).Take(this.MaxPriorityChanges))
                {
                    this.PriorityChangePoints.Add(point);
                }

                this.DebugPrintPriorityChangePoints();
            }

            this.CurrentSchedule = string.Empty;
            this.Path.Clear();
            this.Groups.Clear();
            this.PriorityChangePointsCount = 0;
            this.StepCount = 0;
            this.Phase = 0;

            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextOperation(IEnumerable<ControlledOperation> ops, ControlledOperation current,
            bool isYielding, out ControlledOperation next)
        {
            next = null;
            var enabledOps = ops.Where(op => op.Status is OperationStatus.Enabled).ToList();
            if (enabledOps.Count is 0)
            {
                return false;
            }

            this.SetNewGroupPriorities(enabledOps, current);
            if (enabledOps.Count > 1 &&
                this.GetPrioritizedOperations(enabledOps, current, out var prioritizedOps))
            {
                enabledOps = prioritizedOps;
            }

            int idx = this.RandomValueGenerator.Next(enabledOps.Count);
            next = enabledOps[idx];

            this.ProcessSchedule(current.LastSchedulingPoint, next, ops,
                next.Id == current.Id ? "FORCED[CUR]" :
                next.Group == current.Group ? "FORCED[GRP]" :
                string.Empty);
            this.PrintSchedule();
            this.StepCount++;
            return true;
        }

        /// <summary>
        /// Returns the operations with the highest priority.
        /// </summary>
        private bool GetPrioritizedOperations(List<ControlledOperation> ops, ControlledOperation current,
            out List<ControlledOperation> result)
        {
            result = ops;

            // Find all operations that are not accessing any shared state.
            var noStateAccessOps = result.Where(op => op.LastSchedulingPoint != SchedulingPointType.Read &&
                op.LastSchedulingPoint != SchedulingPointType.Write);
            if (noStateAccessOps.Any())
            {
                // There are operations that are not accessing any shared state, so prioritize them.
                result = noStateAccessOps.ToList();
            }
            else
            {
                // Split the operations that are accessing shared state into a 'READ' and 'WRITE' group.
                var readAccessOps = result.Where(op => op.LastSchedulingPoint is SchedulingPointType.Read);
                var writeAccessOps = result.Where(op => op.LastSchedulingPoint is SchedulingPointType.Write);
                
                // Update the known 'READ' and 'WRITE' accesses so far.
                this.ReadAccesses.UnionWith(readAccessOps.Select(op => op.LastAccessedState));
                this.WriteAccesses.UnionWith(writeAccessOps.Select(op => op.LastAccessedState));

                this.ReadAccessSet.UnionWith(readAccessOps.Select(op => op.LastAccessedState));
                this.WriteAccessSet.UnionWith(writeAccessOps.Select(op => op.LastAccessedState));

                // Find if there are any read-only accesses. Note that this is just an approximation
                // based on current knowledge. An access that is considered read-only might not be
                // considered anymore in later steps or iterations.
                var readOnlyAccessOps = readAccessOps.Where(op => !this.WriteAccesses.Any(
                    state => state.Contains(op.LastAccessedState) || op.LastAccessedState.Contains(state)));
                if (readOnlyAccessOps.Any())
                {
                    // Prioritize any read-only access operation.
                    Console.WriteLine($">>> [FILTER] {readOnlyAccessOps.Count()} operations are read-only.");
                    foreach (var op in readOnlyAccessOps)
                    {
                        System.Console.WriteLine($">>>>>>>> op: {op.Name} | is-write: {!op.Group.IsReadOnly} | state: {op.LastAccessedState} (sp: {op.LastSchedulingPoint} | o: {op.Group.Owner} | g: {op.Group})");
                    }

                    result = readOnlyAccessOps.ToList();
                }
                else
                {
                    // There are operations writing to shared state.
                    Console.WriteLine($">>> [FILTER] {result.Count} operations are all accessing shared state.");
                    foreach (var op in result)
                    {
                        System.Console.WriteLine($">>>>>>>> op: {op.Name} | is-write: {!op.Group.IsReadOnly} | state: {op.LastAccessedState} (sp: {op.LastSchedulingPoint} | o: {op.Group.Owner} | g: {op.Group})");
                    }

                    var stateAccessingGroups = result.Select(op => op.Group).Distinct();
                    if (stateAccessingGroups.Any(group => !group.IsReadOnly))
                    {
                        Console.WriteLine($">>>>>> Must change priority.");
                        this.MustChangeCount[this.MustChangeCount.Count - 1]++;
                        this.TryChangeGroupPriorities(result, current);
                    }
                }
            }

            if (this.GetPrioritizedOperationGroup(result, out OperationGroup nextGroup))
            {
                Console.WriteLine($">>> [FILTER] Prioritized group {nextGroup} (o: {nextGroup.Owner}).");
                if (nextGroup == current.Group)
                {
                    // Maybe dont need this ...
                    // Choose the current operation, if it is enabled.
                    var currentGroupOps = result.Where(op => op.Id == current.Id).ToList();
                    if (currentGroupOps.Count is 1)
                    {
                        result = currentGroupOps;
                        return true;
                    }
                }

                result = result.Where(op => nextGroup.IsMember(op)).ToList();
                foreach (var op in result)
                {
                    System.Console.WriteLine($">>>>>>>> op: {op.Name} (sp: {op.LastSchedulingPoint} | o: {op.Group.Owner} | g: {op.Group})");
                }
            }

            return result.Count > 0;
        }

        /// <summary>
        /// Returns the group with the highest priority that contains at least one enabled operation.
        /// </summary>
        private bool GetPrioritizedOperationGroup(List<ControlledOperation> ops, out OperationGroup result)
        {
            foreach (var group in this.PrioritizedGroups)
            {
                if (ops.Any(op => op.Group == group))
                {
                    result = group;
                    return true;
                }
            }

            result = null;
            return false;
        }

        /// <summary>
        /// Sets the priority of new groups, if there are any.
        /// </summary>
        private void SetNewGroupPriorities(List<ControlledOperation> ops, ControlledOperation current)
        {
            int count = this.PrioritizedGroups.Count;
            if (this.PrioritizedGroups.Count is 0)
            {
                this.PrioritizedGroups.Add(current.Group);
            }

            // Randomize the priority of all new groups.
            foreach (var group in ops.Select(op => op.Group).Where(g => !this.PrioritizedGroups.Contains(g)))
            {
                // Randomly choose a priority for this group.
                int index = this.RandomValueGenerator.Next(this.PrioritizedGroups.Count) + 1;
                this.PrioritizedGroups.Insert(index, group);
                Debug.WriteLine("<PriorityLog> chose priority '{0}' for new operation group '{1}'.", index, group);
            }

            if (this.PrioritizedGroups.Count > count)
            {
                this.DebugPrintOperationPriorityList();
            }
        }

        /// <summary>
        /// Deprioritizes the group with the highest priority that contains at least
        /// one enabled operation, if there is a priority change point installed on
        /// the current execution step.
        /// </summary>
        private bool TryChangeGroupPriorities(List<ControlledOperation> ops, ControlledOperation current)
        {
            if (ops.Count > 1)
            {
                OperationGroup group = null;
                if (this.PriorityChangePoints.Contains(this.PriorityChangePointsCount))
                {
                    this.GetPrioritizedOperationGroup(ops, out group);
                    this.ChangeCount.Add(this.PriorityChangePointsCount);
                    Console.WriteLine($">>> [DISABLE] group {group} is disabled.");
                    Debug.WriteLine("<PriorityLog> operation group '{0}' is deprioritized.", group);
                }

                this.PriorityChangePointsCount++;
                if (group != null)
                {
                    // Deprioritize the group by putting it in the end of the list.
                    this.PrioritizedGroups.Remove(group);
                    this.PrioritizedGroups.Add(group);
                    return true;
                }
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
        internal override bool IsFair() => false;

        /// <inheritdoc/>
        internal override string GetDescription()
        {
            var text = $"priority-based[seed '" + this.RandomValueGenerator.Seed + "']";
            return text;
        }

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
            this.PriorityChangePointsCount = 0;
            this.PrioritizedGroups.Clear();
            this.PriorityChangePoints.Clear();
        }

        /// <summary>
        /// Print the operation group priority list, if debug is enabled.
        /// </summary>
        private void DebugPrintOperationPriorityList()
        {
            if (Debug.IsEnabled)
            {
                Debug.WriteLine("<PriorityLog> operation group priority list: ");
                for (int idx = 0; idx < this.PrioritizedGroups.Count; idx++)
                {
                    var group = this.PrioritizedGroups[idx];
                    if (group.Any(m => m.Status is OperationStatus.Enabled))
                    {
                        Debug.WriteLine("  |_ '{0}' ({1}) [enabled]", group);
                    }
                    else if (group.Any(m => m.Status != OperationStatus.Completed))
                    {
                        Debug.WriteLine("  |_ '{0}' ({1})", group);
                    }
                }
            }
        }

        /// <summary>
        /// Print the priority change points, if debug is enabled.
        /// </summary>
        private void DebugPrintPriorityChangePoints()
        {
            if (Debug.IsEnabled)
            {
                // Sort them before printing for readability.
                var sortedChangePoints = this.PriorityChangePoints.ToArray();
                Array.Sort(sortedChangePoints);
                Debug.WriteLine("<PriorityLog> priority change points ('{0}' in total): {1}",
                    sortedChangePoints.Length, string.Join(", ", sortedChangePoints));
            }
        }

        protected override void Callback()
        {
            this.DebugPrintOperationPriorityList();
        }
    }
}
