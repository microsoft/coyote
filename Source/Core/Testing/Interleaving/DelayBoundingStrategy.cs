// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Interleaving
{
    /// <summary>
    /// A (fair) exploration strategy using delay-bounding.
    /// </summary>
    /// <remarks>
    /// This strategy is based on the algorithm described in the following paper:
    /// https://dl.acm.org/doi/10.1145/1925844.1926432.
    /// </remarks>
    internal sealed class DelayBoundingStrategy : RandomStrategy
    {
        /// <summary>
        /// Ordered list of operation groups.
        /// </summary>
        private readonly List<OperationGroup> OperationGroups;

        /// <summary>
        /// Scheduling points in the current iteration where a delay should occur.
        /// </summary>
        private readonly List<int> DelayPoints;

        /// <summary>
        /// Max number of delays per iteration.
        /// </summary>
        private readonly int MaxDelaysPerIteration;

        /// <summary>
        /// Max number of potential delay points across all iterations.
        /// </summary>
        private int MaxDelayPoints;

        /// <summary>
        /// Number of potential delay points in the current iteration.
        /// </summary>
        private int NumDelayPoints;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayBoundingStrategy"/> class.
        /// </summary>
        internal DelayBoundingStrategy(Configuration configuration, int maxDelays, bool isFair)
            : base(configuration, isFair)
        {
            this.OperationGroups = new List<OperationGroup>();
            this.DelayPoints = new List<int>();
            this.MaxDelaysPerIteration = maxDelays;
            this.MaxDelayPoints = 0;
            this.NumDelayPoints = 0;
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            if (this.NumDelayPoints > 0)
            {
                this.OperationGroups.Clear();
                this.DelayPoints.Clear();

                this.MaxDelayPoints = Math.Max(this.MaxDelayPoints, this.NumDelayPoints);
                if (this.MaxDelaysPerIteration > 0)
                {
                    var delays = this.RandomValueGenerator.Next(this.MaxDelaysPerIteration) + 1;
                    for (int i = 0; i < delays; i++)
                    {
                        this.DelayPoints.Add(this.RandomValueGenerator.Next(this.MaxDelayPoints + 1));
                    }

                    this.DelayPoints.Sort();
                }

                this.DebugPrintDelayPoints();
            }

            this.NumDelayPoints = 0;
            return base.InitializeNextIteration(iteration);
        }

        /// <inheritdoc/>
        internal override bool NextOperation(IEnumerable<ControlledOperation> ops, ControlledOperation current,
            bool isYielding, out ControlledOperation next)
        {
            if (this.IsFair && this.StepCount >= this.Configuration.MaxUnfairSchedulingSteps)
            {
                return base.NextOperation(ops, current, isYielding, out next);
            }

            this.RegisterNewOperationGroup(ops);

            // Check if there are at least two operation groups that can be scheduled,
            // otherwise skip the delay checking and changing logic.
            if (ops.Select(op => op.Group).Distinct().Skip(1).Any())
            {
                // Delay the next enabled priority operation group.
                this.DelayOperationGroup(ops);
                this.NumDelayPoints++;

                // Get the operations that belong to the next enabled group.
                OperationGroup nextGroup = this.GetNextEnabledOperationGroup(ops);
                ops = ops.Where(op => nextGroup.IsMember(op));
            }

            int idx = this.RandomValueGenerator.Next(ops.Count());
            next = ops.ElementAt(idx);
            return true;
        }

        /// <summary>
        /// Registers any new operation groups.
        /// </summary>
        private void RegisterNewOperationGroup(IEnumerable<ControlledOperation> ops)
        {
            this.OperationGroups.RemoveAll(group => group.IsCompleted());

            int count = this.OperationGroups.Count;
            foreach (var group in ops.Select(op => op.Group).Where(g => !this.OperationGroups.Contains(g)))
            {
                this.OperationGroups.Add(group);
            }

            if (this.OperationGroups.Count > count)
            {
                this.DebugPrintRoundRobinList();
            }
        }

        /// <summary>
        /// Delays the next enabled operation group, if there is a delay point installed on the current execution step.
        /// </summary>
        private void DelayOperationGroup(IEnumerable<ControlledOperation> ops)
        {
            while (this.DelayPoints.Count > 0 && this.NumDelayPoints == this.DelayPoints.First())
            {
                // This scheduling step was chosen as a delay point.
                this.DelayPoints.RemoveAt(0);

                OperationGroup group = this.GetNextEnabledOperationGroup(ops);
                if (group != null)
                {
                    this.LogWriter.LogDebug("[coyote::strategy] Delayed operation group '{0}' with '{1}' delays remaining.",
                        group, this.DelayPoints.Count);
                    this.OperationGroups.Remove(group);
                    this.OperationGroups.Add(group);
                }
            }
        }

        /// <summary>
        /// Returns the next enabled operation group.
        /// </summary>
        private OperationGroup GetNextEnabledOperationGroup(IEnumerable<ControlledOperation> ops)
        {
            foreach (var group in this.OperationGroups)
            {
                if (ops.Any(op => op.Group == group))
                {
                    return group;
                }
            }

            return null;
        }

        /// <inheritdoc/>
        internal override string GetName() => (this.IsFair ? ExplorationStrategy.FairDelayBounding : ExplorationStrategy.DelayBounding).GetName();

        /// <inheritdoc/>
        internal override string GetDescription() =>
            $"{this.GetName()}[bound:{this.MaxDelaysPerIteration},seed:{this.RandomValueGenerator.Seed}]";

        /// <inheritdoc/>
        internal override void Reset()
        {
            this.NumDelayPoints = 0;
            this.DelayPoints.Clear();
            base.Reset();
        }

        /// <summary>
        /// Print the operation group round-robin list, if debug is enabled.
        /// </summary>
        private void DebugPrintRoundRobinList()
        {
            this.LogWriter.LogDebug(() =>
            {
                var sb = new StringBuilder();
                sb.AppendLine("[coyote::strategy] Updated operation group round-robin list: ");
                for (int idx = 0; idx < this.OperationGroups.Count; idx++)
                {
                    var group = this.OperationGroups[idx];
                    if (group.Any(m => m.Status is OperationStatus.Enabled))
                    {
                        sb.AppendLine($"  |_ [{idx}] operation group with id '{group}' [enabled]");
                    }
                    else if (group.Any(m => m.Status != OperationStatus.Completed))
                    {
                        sb.AppendLine($"  |_ [{idx}] operation group with id '{group}'");
                    }
                }

                return sb.ToString();
            });
        }

        /// <summary>
        /// Print the delay points, if debug is enabled.
        /// </summary>
        private void DebugPrintDelayPoints()
        {
            this.LogWriter.LogDebug(() =>
            {
                var sb = new StringBuilder();
                if (this.DelayPoints.Count > 0)
                {
                    var delayPoints = this.DelayPoints.ToArray();
                    var points = string.Join(", ", delayPoints);
                    sb.AppendLine($"[coyote::strategy] Assigned '{delayPoints.Length}' delay points: {points}.");
                }
                else
                {
                    sb.AppendLine("[coyote::strategy] Assigned '0' delay points.");
                }

                return sb.ToString();
            });
        }
    }
}
