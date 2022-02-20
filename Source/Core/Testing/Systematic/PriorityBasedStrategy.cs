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
        /// Random value generator.
        /// </summary>
        private readonly IRandomValueGenerator RandomValueGenerator;

        /// <summary>
        /// The maximum number of steps to explore.
        /// </summary>
        private readonly int MaxSteps;

        /// <summary>
        /// Max number of priority switch points.
        /// </summary>
        private readonly int MaxPrioritySwitchPoints;

        /// <summary>
        /// Approximate length of the schedule across all iterations.
        /// </summary>
        private int ScheduleLength;

        /// <summary>
        /// List of prioritized groups.
        /// </summary>
        private readonly List<OperationGroup> PrioritizedGroups;

        /// <summary>
        /// Scheduling points in the current execution where a priority change should occur.
        /// </summary>
        private readonly HashSet<int> PriorityChangePoints;

        /// <summary>
        /// Initializes a new instance of the <see cref="PriorityBasedStrategy"/> class.
        /// </summary>
        internal PriorityBasedStrategy(int maxSteps, int maxPrioritySwitchPoints, IRandomValueGenerator generator)
        {
            this.RandomValueGenerator = generator;
            this.MaxSteps = maxSteps;
            this.StepCount = 0;
            this.ScheduleLength = 0;
            this.MaxPrioritySwitchPoints = maxPrioritySwitchPoints;
            this.PrioritizedGroups = new List<OperationGroup>();
            this.PriorityChangePoints = new HashSet<int>();

            this.KnownSchedules = new HashSet<string>();
            this.CurrentSchedule = string.Empty;
            this.Path = new List<(string, int, SchedulingPointType, OperationGroup, string, int, bool)>();
            this.OperationDebugInfo = new Dictionary<string, string>();
            this.Groups = new Dictionary<int, Dictionary<OperationGroup, (bool, bool, bool)>>();
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
                this.ScheduleLength = Math.Max(this.ScheduleLength, this.StepCount);
                this.StepCount = 0;

                this.PrioritizedGroups.Clear();
                this.PriorityChangePoints.Clear();

                var range = Enumerable.Range(0, this.ScheduleLength);
                foreach (int point in this.Shuffle(range).Take(this.MaxPrioritySwitchPoints))
                {
                    this.PriorityChangePoints.Add(point);
                }

                this.DebugPrintPriorityChangePoints();
            }

            this.CurrentSchedule = string.Empty;
            this.Path.Clear();
            this.OperationDebugInfo.Clear();
            this.Groups.Clear();
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
            this.DeprioritizeEnabledGroupWithHighestPriority(enabledOps, current, isYielding);

            OperationGroup highestEnabledGroup = this.GetEnabledGroupWithHighestPriority(enabledOps);
            enabledOps = enabledOps.Where(op => op.Group == highestEnabledGroup).ToList();
            if (Debug.IsEnabled)
            {
                Debug.WriteLine("<PCTLog> prioritized group '{0}' ({1}): ", highestEnabledGroup, highestEnabledGroup?.Msg);
                foreach (var op in enabledOps)
                {
                    Debug.WriteLine("  |_ '{0}'", op);
                }
            }

            // bool isForced = false;
            // // If explicit, and there is an operation group, then choose a random operation in the same group.
            // if (current.SchedulingPoint != SchedulingPointType.Interleave &&
            //     current.SchedulingPoint != SchedulingPointType.Yield)
            // {
            //     // Choose an operation that has the same group as the operation that just completed.
            //     if (current.Group != null)
            //     {
            //         System.Console.WriteLine($">>>>> SCHEDULE NEXT: {current.Name} (o: {current.Group.Owner}, g: {current.Group})");
            //         var groupOps = enabledOps.Where(op => current.Group.IsMember(op)).ToList();
            //         System.Console.WriteLine($">>>>> groupOps: {groupOps.Count}");
            //         foreach (var op in groupOps)
            //         {
            //             System.Console.WriteLine($">>>>>>>> groupOp: {op.Name} (o: {op.Group.Owner}, g: {op.Group})");
            //         }
            //         if (groupOps.Count > 0)
            //         {
            //             enabledOps = groupOps;
            //             isForced = true;
            //         }
            //     }
            // }

            int idx = this.RandomValueGenerator.Next(enabledOps.Count);
            next = enabledOps[idx];

            this.ProcessSchedule(current.SchedulingPoint, next, ops, false);
            this.PrintSchedule();
            this.StepCount++;
            return true;
        }

        /// <summary>
        /// Sets the priority of new groups, if there are any.
        /// </summary>
        private void SetNewGroupPriorities(List<ControlledOperation> ops, ControlledOperation current)
        {
            int count = 0;
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
                Debug.WriteLine("<PCTLog> chose priority '{0}' for new operation group '{1}' ({2}).", index, group, group.Msg);
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
        private void DeprioritizeEnabledGroupWithHighestPriority(List<ControlledOperation> ops,
            ControlledOperation current, bool isYielding)
        {
            if (ops.Count <= 1)
            {
                // Nothing to do, there is only one enabled group available.
                return;
            }

            OperationGroup deprioritizedGroup = null;
            if (this.PriorityChangePoints.Contains(this.StepCount))
            {
                // This scheduling step was chosen as a priority switch point.
                deprioritizedGroup = this.GetEnabledGroupWithHighestPriority(ops);
                Debug.WriteLine("<PCTLog> operation group '{0}' ({1}) is deprioritized.", deprioritizedGroup, deprioritizedGroup.Msg);
            }
            else if (isYielding)
            {
                // The current group is yielding its execution to the next prioritized group.
                deprioritizedGroup = current.Group;
                Debug.WriteLine("<PCTLog> operation group '{0}' ({1}) yields its priority.", deprioritizedGroup, deprioritizedGroup.Msg);
            }

            if (deprioritizedGroup != null)
            {
                // Deprioritize the group by putting it in the end of the list.
                this.PrioritizedGroups.Remove(deprioritizedGroup);
                this.PrioritizedGroups.Add(deprioritizedGroup);
            }
        }

        /// <summary>
        /// Returns the group with the highest priority that contains at least one enabled operation.
        /// </summary>
        private OperationGroup GetEnabledGroupWithHighestPriority(List<ControlledOperation> ops)
        {
            foreach (var group in this.PrioritizedGroups)
            {
                if (ops.Any(op => op.Group == group && !group.IsDisabled))
                {
                    return group;
                }
            }

            return null;
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
            var text = $"pct[seed '" + this.RandomValueGenerator.Seed + "']";
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
            this.ScheduleLength = 0;
            this.StepCount = 0;
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
                Debug.WriteLine("<PCTLog> operation group priority list: ");
                for (int idx = 0; idx < this.PrioritizedGroups.Count; idx++)
                {
                    var group = this.PrioritizedGroups[idx];
                    if (idx < this.PrioritizedGroups.Count - 1)
                    {
                        Debug.WriteLine("  |_ '{0}' ({1})", group, group.Msg);
                    }
                    else
                    {
                        Debug.WriteLine("  |_ '{0}' ({1})", group, group.Msg);
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
                Debug.WriteLine("<PCTLog> next priority change points ('{0}' in total): {1}",
                    sortedChangePoints.Length, string.Join(", ", sortedChangePoints));
            }
        }
    }
}
