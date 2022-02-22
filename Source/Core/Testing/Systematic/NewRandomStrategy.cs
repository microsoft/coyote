// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Systematic
{
    /// <summary>
    /// A simple (but effective) randomized scheduling strategy.
    /// </summary>
    internal class NewRandomStrategy : SystematicStrategy
    {
        /// <summary>
        /// Random value generator.
        /// </summary>
        protected IRandomValueGenerator RandomValueGenerator;

        /// <summary>
        /// The maximum number of steps to explore.
        /// </summary>
        protected readonly int MaxSteps;

        /// <summary>
        /// Initializes a new instance of the <see cref="NewRandomStrategy"/> class.
        /// </summary>
        internal NewRandomStrategy(int maxSteps, IRandomValueGenerator generator)
        {
            this.RandomValueGenerator = generator;
            this.MaxSteps = maxSteps;
            this.KnownSchedules = new HashSet<string>();
            this.CurrentSchedule = string.Empty;
            this.Path = new List<(string, int, SchedulingPointType, OperationGroup, string, int, bool)>();
            this.OperationDebugInfo = new Dictionary<string, string>();
            this.Groups = new Dictionary<int, Dictionary<OperationGroup, (bool, bool, bool)>>();
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            // The random strategy just needs to reset the number of scheduled steps during
            // the current iretation.
            this.StepCount = 0;
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
            var enabledOps = ops.Where(op => op.Status is OperationStatus.Enabled && !op.Group.IsDisabled).ToList();
            if (enabledOps.Count is 0)
            {
                next = null;
                return false;
            }

            // Find operations with schedule that has not been seen yet.
            // var newOps = enabledOps.Where(op => !this.KnownSchedules.Contains(op.Schedule)).ToList();
            // if (enabledOps.Count > 1)
            // {
            //     var newOps = new List<ControlledOperation>();
            //     foreach (var op in enabledOps)
            //     {
            //         var potentialSchedule = this.CurrentSchedule + $" | {current.Id}";
            //         System.Console.WriteLine($">>>>> Potential schedule for '{op.Id}': {potentialSchedule}");
            //         if (!this.KnownSchedules.Contains(potentialSchedule))
            //         {
            //             newOps.Add(op);
            //         }
            //     }
            //     if (newOps.Count > 0)
            //     {
            //         System.Console.WriteLine($">>>>> Filtering ops to '{newOps.Count}' from '{enabledOps.Count}'.");
            //         enabledOps = newOps;
            //     }
            // }

            bool isFiltered = false;
            // If explicit, and there is an operation group, then choose a random operation in the same group.
            if (current.LastSchedulingPoint != SchedulingPointType.Interleave &&
                current.LastSchedulingPoint != SchedulingPointType.Yield)
            {
                // Choose an operation that has the same group as the operation that just completed.
                if (current.Group != null)
                {
                    System.Console.WriteLine($">>>>> SCHEDULE NEXT: {current.Name} (o: {current.Group.Owner}, g: {current.Group})");
                    var groupOps = enabledOps.Where(op => current.Group.IsMember(op)).ToList();
                    System.Console.WriteLine($">>>>> groupOps: {groupOps.Count}");
                    foreach (var op in groupOps)
                    {
                        System.Console.WriteLine($">>>>>>>> groupOp: {op.Name} (o: {op.Group.Owner}, g: {op.Group})");
                    }

                    if (groupOps.Count > 0)
                    {
                        enabledOps = groupOps;
                        isFiltered = true;
                    }
                }
            }

            int idx = this.RandomValueGenerator.Next(enabledOps.Count);
            next = enabledOps[idx];

            this.ProcessSchedule(current.LastSchedulingPoint, next, ops, isFiltered);
            this.PrintSchedule();
            this.StepCount++;
            return true;
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
        internal override bool IsFair() => true;

        /// <inheritdoc/>
        internal override string GetDescription() => $"new-random[seed '{this.RandomValueGenerator.Seed}']";

        /// <inheritdoc/>
        internal override void Reset()
        {
            this.StepCount = 0;
        }
    }
}
