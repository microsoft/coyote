// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
        /// The number of exploration steps.
        /// </summary>
        protected int StepCount;

        private readonly HashSet<string> KnownSchedules;

        private string CurrentSchedule;

        /// <summary>
        /// Initializes a new instance of the <see cref="NewRandomStrategy"/> class.
        /// </summary>
        internal NewRandomStrategy(int maxSteps, IRandomValueGenerator generator)
        {
            this.RandomValueGenerator = generator;
            this.MaxSteps = maxSteps;
            this.KnownSchedules = new HashSet<string>();
            this.CurrentSchedule = string.Empty;
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            // The random strategy just needs to reset the number of scheduled steps during
            // the current iretation.
            this.StepCount = 0;
            this.CurrentSchedule = string.Empty;
            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextOperation(IEnumerable<ControlledOperation> ops, ControlledOperation current,
            bool isYielding, out ControlledOperation next)
        {
            var enabledOps = ops.Where(op => op.Status is OperationStatus.Enabled).ToList();
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

            int idx = this.RandomValueGenerator.Next(enabledOps.Count);
            next = enabledOps[idx];

            if (string.IsNullOrEmpty(this.CurrentSchedule))
            {
                this.CurrentSchedule = $"{current.Id} ({current.Msg})";
            }
            else
            {
                this.CurrentSchedule += $" | {current.Id} ({current.Msg})";
            }

            System.Console.WriteLine($">>> Schedule so far: {this.CurrentSchedule}");
            this.KnownSchedules.Add(this.CurrentSchedule);
            System.Console.WriteLine($">>> Visited '{this.KnownSchedules.Count}' schedules so far.");
            RuntimeStats.NumVisitedSchedules = this.KnownSchedules.Count;

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
