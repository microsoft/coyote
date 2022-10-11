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
    /// A delay-bounded scheduling strategy.
    /// </summary>
    /// <remarks>
    /// This strategy implements delay-bounded scheduling as described in the following paper:
    /// https://dl.acm.org/doi/10.1145/1925844.1926432.
    /// </remarks>
    internal sealed class DelayBoundingStrategy : RandomStrategy
    {
        /// <summary>
        /// Ordered list of controlled operations.
        /// </summary>
        private readonly List<ControlledOperation> Operations;

        /// <summary>
        /// Scheduling points in the current iteration where a delay should occur.
        /// </summary>
        private readonly List<int> RemainingDelays;

        /// <summary>
        /// Number of potential delay points in the current iteration.
        /// </summary>
        private int NumDelayPoints;

        /// <summary>
        /// Max number of potential delay points across all iterations.
        /// </summary>
        private int MaxDelayPoints;

        /// <summary>
        /// Max number of delays per iteration.
        /// </summary>
        private readonly int MaxDelays;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayBoundingStrategy"/> class.
        /// </summary>
        internal DelayBoundingStrategy(Configuration configuration, IRandomValueGenerator generator, bool isFair)
            : base(configuration, generator, isFair)
        {
            this.Operations = new List<ControlledOperation>();
            this.RemainingDelays = new List<int>();
            this.NumDelayPoints = 0;
            this.MaxDelayPoints = 0;
            this.MaxDelays = configuration.StrategyBound;
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            // The first iteration has no knowledge of the execution, so only initialize from the second
            // iteration and onwards. Note that although we could initialize the first length based on a
            // heuristic, its not worth it, as the strategy will typically explore thousands of iterations,
            // plus its also interesting to explore a schedule with no delay points inserted.
            if (iteration > 0)
            {
                this.Operations.Clear();
                this.RemainingDelays.Clear();

                this.MaxDelayPoints = Math.Max(this.MaxDelayPoints, this.NumDelayPoints);
                if (this.MaxDelays > 0)
                {
                    var delays = this.MaxDelays;
                    for (int i = 0; i < delays; i++)
                    {
                        this.RemainingDelays.Add(this.RandomValueGenerator.Next(this.MaxDelayPoints + 1));
                    }

                    this.RemainingDelays.Sort();
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

            foreach (var op in ops)
            {
                if (!this.Operations.Contains(op))
                {
                    this.Operations.Add(op);
                }
            }

            // Get a shifted ordered list starting at the current operation to perform round-robin.
            var currentIdx = this.Operations.IndexOf(current);
            var orderedOps = this.Operations.GetRange(currentIdx, this.Operations.Count - currentIdx);
            if (currentIdx != 0)
            {
                orderedOps.AddRange(this.Operations.GetRange(0, currentIdx));
            }

            int idx = 0;
            var enabledOps = orderedOps.Where(op => op.Status == OperationStatus.Enabled).ToList();
            while (this.RemainingDelays.Count > 0 && this.NumDelayPoints == this.RemainingDelays.First())
            {
                idx = (idx + 1) % enabledOps.Count;
                this.RemainingDelays.RemoveAt(0);
                Debug.WriteLine($"<DelayLog> Inserted delay, '{this.RemainingDelays.Count}' remaining.");
            }

            next = enabledOps[idx];
            this.NumDelayPoints++;
            return true;
        }

        /// <inheritdoc/>
        internal override string GetDescription() =>
            $"DelayBounding[fair:{this.IsFair},bound:{this.MaxDelays},seed:{this.RandomValueGenerator.Seed}]";

        /// <inheritdoc/>
        internal override void Reset()
        {
            this.NumDelayPoints = 0;
            this.RemainingDelays.Clear();
            base.Reset();
        }

        /// <summary>
        /// Print the delay points, if debug is enabled.
        /// </summary>
        private void DebugPrintDelayPoints()
        {
            if (Debug.IsEnabled && this.RemainingDelays.Count > 0)
            {
                if (this.RemainingDelays.Count > 0)
                {
                    var delayPoints = this.RemainingDelays.ToArray();
                    Debug.WriteLine("<ScheduleLog> Assigned {0} delay points: {1}.",
                        delayPoints.Length, string.Join(", ", delayPoints));
                }
                else
                {
                    Debug.WriteLine("<ScheduleLog> Assigned 0 delay points.");
                }
            }
        }
    }
}
