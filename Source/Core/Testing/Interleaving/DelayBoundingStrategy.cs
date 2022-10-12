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
    internal sealed class DelayBoundingStrategy : InterleavingStrategy
    {
        /// <summary>
        /// Scheduling points in the current execution where a delay should occur.
        /// </summary>
        private readonly List<ControlledOperation> OperationList;

        /// <summary>
        /// Scheduling points in the current execution where a delay should occur.
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
        internal DelayBoundingStrategy(Configuration configuration, IRandomValueGenerator generator)
            : base(configuration, generator, false)
        {
            this.OperationList = new List<ControlledOperation>();
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
            // plus its also interesting to explore a schedule with no forced priority switch points.
            if (iteration > 0)
            {
                this.OperationList.Clear();
                this.RemainingDelays.Clear();

                this.MaxDelayPoints = Math.Max(
                    this.MaxDelayPoints, this.NumDelayPoints);
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
            this.StepCount = 0;
            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextOperation(IEnumerable<ControlledOperation> ops, ControlledOperation current,
            bool isYielding, out ControlledOperation next)
        {
            foreach (var op in ops)
            {
                if (!this.OperationList.Contains(op))
                {
                    this.OperationList.Add(op);
                }
            }

            var opList = this.OperationList;
            var currentIdx = opList.IndexOf(current);
            var orderedOps = opList.GetRange(currentIdx, opList.Count - currentIdx);
            if (currentIdx != 0)
            {
                orderedOps.AddRange(opList.GetRange(0, currentIdx));
            }

            var enabledOps = orderedOps.Where(op => op.Status == OperationStatus.Enabled).ToList();
            if (enabledOps.Count == 0)
            {
                // there is no enabled operation.
                next = null;
                return true;
            }

            int t_idx = 0;
            while (this.RemainingDelays.Count > 0 && this.NumDelayPoints == this.RemainingDelays[0])
            {
                t_idx = (t_idx + 1) % enabledOps.Count;
                this.RemainingDelays.RemoveAt(0);
                Debug.WriteLine($"<DelayLog> Inserted delay, '{this.RemainingDelays.Count}' remaining.");
            }

            next = enabledOps[t_idx];
            this.NumDelayPoints++;
            this.StepCount++;
            return true;
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
            $"DelayBounding[bound:{this.MaxDelays},seed:{this.RandomValueGenerator.Seed}]";

        // /// <summary>
        // /// Shuffles the specified range using the Fisher-Yates algorithm.
        // /// </summary>
        // /// <remarks>
        // /// See https://en.wikipedia.org/wiki/Fisher%E2%80%93Yates_shuffle.
        // /// </remarks>
        // private IList<int> Shuffle(IEnumerable<int> range)
        // {
        //     var result = new List<int>(range);
        //     for (int idx = result.Count - 1; idx >= 1; idx--)
        //     {
        //         int point = this.RandomValueGenerator.Next(result.Count);
        //         int temp = result[idx];
        //         result[idx] = result[point];
        //         result[point] = temp;
        //     }
        //     return result;
        // }

        /// <inheritdoc/>
        internal override void Reset()
        {
            this.StepCount = 0;
            this.NumDelayPoints = 0;
            this.RemainingDelays.Clear();
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
                    // Sort them before printing for readability.
                    var sortedChangePoints = this.RemainingDelays.ToArray();
                    // Array.Sort(sortedChangePoints);
                    Debug.WriteLine("<ScheduleLog> Assigned {0} delay points: {1}.",
                        sortedChangePoints.Length, string.Join(", ", sortedChangePoints));
                }
                else
                {
                    Debug.WriteLine("<ScheduleLog> Assigned 0 delay points.");
                }
            }
        }
    }
}
