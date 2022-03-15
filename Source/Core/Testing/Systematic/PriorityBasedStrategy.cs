// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Systematic
{
    /// <summary>
    /// A group-based probabilistic priority-based scheduling strategy.
    /// </summary>
    /// <remarks>
    /// This strategy is based on the <see cref="PCTStrategy"/> strategy.
    /// </remarks>
    internal sealed class PriorityBasedStrategy : SystematicStrategy
    {
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
        /// Initializes a new instance of the <see cref="PriorityBasedStrategy"/> class.
        /// </summary>
        internal PriorityBasedStrategy(Configuration configuration, IRandomValueGenerator generator)
            : base(configuration, generator, false)
        {
            this.PrioritizedGroups = new List<OperationGroup>();
            this.PriorityChangePoints = new HashSet<int>();
            this.ReadAccesses = new HashSet<string>();
            this.WriteAccesses = new HashSet<string>();
            this.PriorityChangePointsCount = 0;
            this.MaxPriorityChangePointsCount = 0;
            this.MaxPriorityChanges = configuration.StrategyBound;
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

            this.PriorityChangePointsCount = 0;
            this.StepCount = 0;
            return true;
        }

        internal static int Counter = 0;

        /// <inheritdoc/>
        internal override bool GetNextOperation(IEnumerable<ControlledOperation> ops, ControlledOperation current,
            bool isYielding, out ControlledOperation next)
        {
            this.SetNewGroupPriorities(ops, current);
            if (ops.Count() > 1)
            {
                var reducedOps = this.ReduceOperations(ops);
                if (reducedOps.Any())
                {
                    ops = reducedOps;
                }

                if (ops.Count() > 1 && ops.Any(op => !op.IsReadOnly))
                {
                    Counter = this.MaxPriorityChangePointsCount;
                    // There are operations writing to shared state.
                    this.TryChangeGroupPriorities(ops);
                }

                if (this.GetNextOperationGroup(ops, out OperationGroup nextGroup))
                {
                    ops = ops.Where(op => nextGroup.IsMember(op));
                }
            }

            int idx = this.RandomValueGenerator.Next(ops.Count());
            next = ops.ElementAt(idx);

            this.StepCount++;
            return true;
        }

        /// <summary>
        /// Returns the operations with the highest priority.
        /// </summary>
        private IEnumerable<ControlledOperation> ReduceOperations(IEnumerable<ControlledOperation> ops)
        {
            // Find all operations that are not accessing any shared state.
            var noStateAccessOps = ops.Where(op => op.LastSchedulingPoint != SchedulingPointType.Read &&
                op.LastSchedulingPoint != SchedulingPointType.Write);
            if (noStateAccessOps.Any())
            {
                // There are operations that are not accessing any shared state, so prioritize them.
                ops = noStateAccessOps;
            }
            else
            {
                // Split the operations that are accessing shared state into a 'READ' and 'WRITE' group.
                var readAccessOps = ops.Where(op => op.LastSchedulingPoint is SchedulingPointType.Read);
                var writeAccessOps = ops.Where(op => op.LastSchedulingPoint is SchedulingPointType.Write);

                // Update the known 'READ' and 'WRITE' accesses so far.
                this.ReadAccesses.UnionWith(readAccessOps.Select(op => op.LastAccessedState));
                this.WriteAccesses.UnionWith(writeAccessOps.Select(op => op.LastAccessedState));

                // Find if there are any read-only accesses. Note that this is just an approximation
                // based on current knowledge. An access that is considered read-only might not be
                // considered anymore in later steps or iterations.
                var readOnlyAccessOps = readAccessOps.Where(op => !this.WriteAccesses.Any(
                    state => state == op.LastAccessedState));
                if (readOnlyAccessOps.Any())
                {
                    // Prioritize any read-only access operation.
                    ops = readOnlyAccessOps;
                }
            }

            return ops;
        }

        /// <summary>
        /// Returns the group with the highest priority that contains at least one enabled operation.
        /// </summary>
        private bool GetNextOperationGroup(IEnumerable<ControlledOperation> ops, out OperationGroup result)
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
        private void SetNewGroupPriorities(IEnumerable<ControlledOperation> ops, ControlledOperation current)
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
                Debug.WriteLine("<ScheduleLog> chose priority '{0}' for new operation group '{1}'.", index, group);
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
        private bool TryChangeGroupPriorities(IEnumerable<ControlledOperation> ops)
        {
            OperationGroup group = null;
            if (this.PriorityChangePoints.Contains(this.PriorityChangePointsCount))
            {
                this.GetNextOperationGroup(ops, out group);
                Debug.WriteLine("<ScheduleLog> operation group '{0}' is deprioritized.", group);
            }

            this.PriorityChangePointsCount++;
            if (group != null)
            {
                // Deprioritize the group by putting it in the end of the list.
                this.PrioritizedGroups.Remove(group);
                this.PrioritizedGroups.Add(group);
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
            $"priority-based[bound:{this.MaxPriorityChanges},seed:{this.RandomValueGenerator.Seed}]";

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
                Debug.WriteLine("<ScheduleLog> operation group priority list: ");
                for (int idx = 0; idx < this.PrioritizedGroups.Count; idx++)
                {
                    var group = this.PrioritizedGroups[idx];
                    if (group.Any(m => m.Status is OperationStatus.Enabled))
                    {
                        Debug.WriteLine("  |_ '{0}' [enabled]", group);
                    }
                    else if (group.Any(m => m.Status != OperationStatus.Completed))
                    {
                        Debug.WriteLine("  |_ '{0}'", group);
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
                Debug.WriteLine("<ScheduleLog> priority change points ('{0}' in total): {1}",
                    sortedChangePoints.Length, string.Join(", ", sortedChangePoints));
            }
        }
    }
}
