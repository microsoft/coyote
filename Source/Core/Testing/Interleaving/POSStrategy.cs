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
    internal sealed class POSStrategy : InterleavingStrategy
    {
        /// <summary>
        /// List of prioritized operation groups.
        /// </summary>
        private readonly List<OperationGroup> PrioritizedOperationGroups;

        // /// <summary>
        // /// Scheduling points in the current execution where a priority change should occur.
        // /// </summary>
        // private readonly HashSet<int> PriorityChangePoints;

        // /// <summary>
        // /// Number of potential priority change points in the current iteration.
        // /// </summary>
        // private int NumPriorityChangePoints;

        // /// <summary>
        // /// Max number of potential priority change points across all iterations.
        // /// </summary>
        // private int MaxPriorityChangePoints;

        // /// <summary>
        // /// Max number of priority changes per iteration.
        // /// </summary>
        // private readonly int MaxPriorityChanges;

        /// <summary>
        /// Initializes a new instance of the <see cref="POSStrategy"/> class.
        /// </summary>
        internal POSStrategy(Configuration configuration, IRandomValueGenerator generator)
            : base(configuration, generator, false)
        {
            this.PrioritizedOperationGroups = new List<OperationGroup>();
            // this.PriorityChangePoints = new HashSet<int>();
            // this.NumPriorityChangePoints = 0;
            // this.MaxPriorityChangePoints = 0;
            // this.MaxPriorityChanges = configuration.StrategyBound;
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
                this.PrioritizedOperationGroups.Clear();
            }

            this.StepCount = 0;
            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextOperation(IEnumerable<ControlledOperation> ops, ControlledOperation current,
            bool isYielding, out ControlledOperation next)
        {
            var enabled_ops = ops;
            // Set the priority of any new operation groups.
            this.SetNewOperationGroupPriorities(ops, current);

            // Check if there are at least two operation groups that can be scheduled,
            // otherwise skip the priority checking and changing logic.
            ControlledOperation next_t;
            if (this.FindNonRacingOperation(ops, out next_t))
            {
                next = next_t;
                this.StepCount++;
                return true;
            }
            else
            {
                if (ops.Select(op => op.Group).Distinct().Skip(1).Any())
                {
                    // Get the operations that belong to the highest priority group.
                    OperationGroup nextGroup = this.GetOperationGroupWithHighestPriority(ops);
                    ops = ops.Where(op => nextGroup.IsMember(op));
                }

                int idx = this.RandomValueGenerator.Next(ops.Count());
                next = ops.ElementAt(idx);

                foreach (var op in enabled_ops)
                {
                    if (op != next && ControlledOperation.IsRacing(next, op))
                    {
                        if (Debug.IsEnabled)
                        {
                            Debug.WriteLine($"<ScheduleLog> {next} and {op} are racing");
                            Debug.WriteLine($"<ScheduleLog> Resetting priority of {op.Group}");
                            System.Diagnostics.Debug.Assert(op.Group.GetMemberCount() == 1, "Error: POS requires an OperationGroup to have only one member ControlledOperation");
                        }

                        this.PrioritizedOperationGroups.Remove(op.Group);
                    }
                }

                this.StepCount++;
                return true;
            }
        }

        internal bool FindNonRacingOperation(IEnumerable<ControlledOperation> ops, out ControlledOperation next)
        {
            HashSet<ControlledOperation> racingOps = new HashSet<ControlledOperation>();
            for (int i = 0; i < ops.Count(); i++)
            {
                for (int j = i + 1; j < ops.Count(); j++)
                {
                    if (ControlledOperation.IsRacing(ops.ElementAt(i), ops.ElementAt(j)))
                    {
                        racingOps.Add(ops.ElementAt(i));
                        racingOps.Add(ops.ElementAt(j));
                    }
                }
            }

            var nonRacingOps = ops.Where(op => !racingOps.Contains(op));

            var filteredNonRacingOps = nonRacingOps.Where(op => op.LastSchedulingPoint != SchedulingPointType.Acquire); // no ops that are waiting on a lock
            if (filteredNonRacingOps.Count() == 0)
            {
                next = null;
                return false;
            }
            else if (filteredNonRacingOps.Count() == 1)
            {
                next = filteredNonRacingOps.First();
                return true;
            }
            else
            {
                OperationGroup nextGroup = this.GetOperationGroupWithHighestPriority(filteredNonRacingOps);
                var opsFromGroup = filteredNonRacingOps.Where(op => nextGroup.IsMember(op));
                int idx = this.RandomValueGenerator.Next(opsFromGroup.Count());
                next = opsFromGroup.ElementAt(idx);
                return true;
            }
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

        private void CleanupPrioritizedOperationGroups()
        {
            List<OperationGroup> groupsToRemove = new List<OperationGroup>();
            foreach (OperationGroup group in this.PrioritizedOperationGroups)
            {
                if (group.IsAllMembersCompleted())
                {
                    groupsToRemove.Add(group);
                }
            }

            foreach (OperationGroup group in groupsToRemove)
            {
                this.PrioritizedOperationGroups.Remove(group);
                if (Debug.IsEnabled)
                {
                    Debug.WriteLine($"<Coyote> Removed completed group {group} from list of prioritized operation groups.");
                }
            }
        }

        /// <summary>
        /// Sets a random priority to any new operation groups.
        /// </summary>
        private void SetNewOperationGroupPriorities(IEnumerable<ControlledOperation> ops, ControlledOperation current)
        {
            this.CleanupPrioritizedOperationGroups();

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

        /// <inheritdoc/>
        internal override bool GetNextBooleanChoice(ControlledOperation current, out bool next)
        {
            next = false;
            if (this.RandomValueGenerator.Next(2) is 0)
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
            $"POS[seed:{this.RandomValueGenerator.Seed}]";

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
            // this.NumPriorityChangePoints = 0;
            this.PrioritizedOperationGroups.Clear();
            // this.PriorityChangePoints.Clear();
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
                        Debug.WriteLine("  |_ [{0}] operation group with id '{1}' [enabled]", idx, group);
                    }
                    else if (group.Any(m => m.Status != OperationStatus.Completed))
                    {
                        Debug.WriteLine("  |_ [{0}] operation group with id '{1}'", idx, group);
                    }
                }
            }
        }
    }
}
