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
#pragma warning disable CA1822 // Mark members as static
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
            this.PriorityChangePointsCount = 0;
            this.MaxPriorityChangePointsCount = 0;
            this.MaxPriorityChanges = maxPriorityChanges;
            this.MaxSteps = maxSteps;
            this.StepCount = 0;

            this.KnownSchedules = new HashSet<string>();
            this.CurrentSchedule = string.Empty;
            this.Path = new List<(string, int, SchedulingPointType, OperationGroup, string, int, string)>();
            this.OperationDebugInfo = new Dictionary<string, string>();
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

                // var range = Enumerable.Range(0, this.MaxPriorityChangePointsCount);
                // foreach (int point in this.Shuffle(range).Take(this.MaxPriorityChanges))
                // {
                //     this.PriorityChangePoints.Add(point);
                // }

                this.PriorityChangePoints.Add(2);
                this.PriorityChangePoints.Add(23);
                this.PriorityChangePoints.Add(42);

                this.DebugPrintPriorityChangePoints();
            }

            this.CurrentSchedule = string.Empty;
            this.Path.Clear();
            this.OperationDebugInfo.Clear();
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
            if (this.GetPrioritizedOperations(enabledOps, current, out var prioritizedOps))
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
            // result = ops.Where(op => !op.Group.IsDisabled).ToList();
            result = ops;
            if (result.Count is 1)
            {
                Console.WriteLine($">>> [FILTER] only one operation left after filtering.");
                return true;
            }
            else if (result.Count is 0)
            {
                return false;
            }

            // Find all operations that are not explicitly interleaving.
            var nonInterleavingOps = result.Where(op => op.LastSchedulingPoint != SchedulingPointType.Interleave).ToList();
            if (nonInterleavingOps.Count is 0)
            {
                // All operations are explicitly interleaving.
                Console.WriteLine($">>> [FILTER] {result.Count} operations are all interleaving.");
                foreach (var op in result)
                {
                    System.Console.WriteLine($">>>>>>>> op: {op.Name} (sp: {op.LastSchedulingPoint} | o: {op.Group.Owner} | g: {op.Group} | msg: {op.Group.Msg})");
                }

                var interleavingGroups = result.Select(op => op.Group).Distinct();
                if (interleavingGroups.Any(group => group.IsWriting))
                {
                    Console.WriteLine($">>>>>> Write operation exists.");
                    Console.WriteLine($">>>>>> Must change priority.");
                    this.MustChangeCount[this.MustChangeCount.Count - 1]++;
                    if (this.TryChangeGroupPriorities(result, current))
                    {
                        if (!current.Group.IsDisabledNext)
                        {
                            Console.WriteLine($">>>>>> Current group should not have been disabled.");
                            // CoyoteRuntime.Current.Fail();
                        }

                        // current.Group.IsDisabled = true;
                        current.Group.IsDisabledNext = false;
                        // result = result.Where(op => !op.Group.IsDisabled).ToList();
                    }
                    else
                    {
                        if (current.Group.IsDisabledNext)
                        {
                            Console.WriteLine($">>>>>> Current group should have been disabled.");
                            // CoyoteRuntime.Current.Fail();
                        }
                    }

                    // return result.Count > 0;
                }
            }
            else
            {
                result = nonInterleavingOps;
            }

            if (this.GetPrioritizedOperationGroup(result, out OperationGroup nextGroup))
            {
                Console.WriteLine($">>> [FILTER] Prioritized group {nextGroup} (o: {nextGroup.Owner} | msg: {nextGroup.Msg}).");
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
                    System.Console.WriteLine($">>>>>>>> op: {op.Name} (sp: {op.LastSchedulingPoint} | o: {op.Group.Owner} | g: {op.Group} | msg: {op.Group.Msg})");
                }
            }

            // // Choose the current operation, if it is enabled.
            // var currentGroupOps = result.Where(op => op.Id == current.Id).ToList();
            // if (currentGroupOps.Count is 1)
            // {
            //     result = currentGroupOps;
            //     return true;
            // }

            // If the current operation is not enabled, then choose operations that have
            // the same group as the current operation.
            // currentGroupOps = result.Where(op => current.Group.IsMember(op)).ToList();
            // if (currentGroupOps.Count > 0)
            // {
            //     Console.WriteLine($">>> [FILTER] {currentGroupOps.Count}/{result.Count} operations remain after choosing same group ones.");
            //     result = currentGroupOps;
            //     foreach (var op in result)
            //     {
            //         System.Console.WriteLine($">>>>>>>> op: {op.Name} (sp: {op.LastSchedulingPoint} | o: {op.Group.Owner} | g: {op.Group} | msg: {op.Group.Msg})");
            //     }
            // }
            // else if (this.GetPrioritizedOperationGroup(result, out OperationGroup nextGroup))
            // {
            //     // Else, ask the scheduler to give the next group if there is any.
            //     Console.WriteLine($">>> [FILTER] Prioritized group {nextGroup} (o: {nextGroup.Owner} | msg: {nextGroup.Msg}).");
            //     result = result.Where(op => nextGroup.IsMember(op)).ToList();
            //     foreach (var op in result)
            //     {
            //         System.Console.WriteLine($">>>>>>>> op: {op.Name} (sp: {op.LastSchedulingPoint} | o: {op.Group.Owner} | g: {op.Group} | msg: {op.Group.Msg})");
            //     }
            // }

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
                Debug.WriteLine("<PriorityLog> chose priority '{0}' for new operation group '{1}' ({2}).", index, group, group.Msg);
            }

            if (this.PrioritizedGroups.Count > count)
            {
                this.DebugPrintOperationPriorityList();
            }
        }

        // /// <summary>
        // /// Tries to update the group priorities.
        // /// </summary>
        // private bool TryUpdateGroupPriorities(ControlledOperation current)
        // {
        //     if (current.Group.Any(m => m.Status is OperationStatus.Enabled))
        //     {
        //         if (this.RandomValueGenerator.Next(10) is 0)
        //         {
        //             this.DisabledGroups.Clear();
        //             this.DisabledGroups.Add(current.Group);
        //             Console.WriteLine($">>> [DISABLE] group {current.Group} is disabled.");
        //             return true;
        //         }
        //     }

        //     return false;
        // }

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
                // this.GetPrioritizedOperationGroup(ops, out group);
                // if (group == current.Group && current.Group.IsDisabledNext)
                if (this.PriorityChangePoints.Contains(this.PriorityChangePointsCount))
                {
                    group = current.Group;
                    group.IsDisabled = true;
                    this.ChangeCount.Add(this.PriorityChangePointsCount);
                    Console.WriteLine($">>> [DISABLE] group {group} is disabled.");
                    Debug.WriteLine("<PriorityLog> operation group '{0}' ({1}) is deprioritized.", group, group.Msg);
                }
                else
                {
                    group = null;
                }

                // if (this.PriorityChangePoints.Contains(this.PriorityChangePointsCount))
                // {
                //     this.GetPrioritizedOperationGroup(ops, out group);
                //     this.ChangeCount.Add(this.PriorityChangePointsCount);
                //     Console.WriteLine($">>> [DISABLE] group {group} is disabled.");
                //     Debug.WriteLine("<PriorityLog> operation group '{0}' ({1}) is deprioritized.", group, group.Msg);
                // }

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
                        Debug.WriteLine("  |_ '{0}' ({1}) [enabled]", group, group.Msg);
                    }
                    else if (group.Any(m => m.Status != OperationStatus.Completed))
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
