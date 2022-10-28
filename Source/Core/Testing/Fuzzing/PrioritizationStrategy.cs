// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Fuzzing
{
    /// <summary>
    /// A probabilistic priority-based fuzzing strategy.
    /// </summary>
    internal class PrioritizationStrategy : FuzzingStrategy
    {
        /// <summary>
        /// The maximum number of steps after which we should reshuffle the probabilities.
        /// </summary>
        protected readonly int PriorityChangePoints;

        /// <summary>
        /// Set of low priority operations.
        /// </summary>
        /// <remarks>
        /// Tasks in this set will experience more delay.
        /// </remarks>
        private readonly List<Guid> LowPrioritySet;

        /// <summary>
        /// Set of high priority operations.
        /// </summary>
        private readonly List<Guid> HighPrioritySet;

        /// <summary>
        /// Probability with which operations should be alloted to the low priority set.
        /// </summary>
        private double LowPriorityProbability;

        /// <summary>
        /// Initializes a new instance of the <see cref="PrioritizationStrategy"/> class.
        /// </summary>
        internal PrioritizationStrategy(Configuration configuration)
            : base(configuration, false)
        {
            this.PriorityChangePoints = configuration.StrategyBound;
            this.HighPrioritySet = new List<Guid>();
            this.LowPrioritySet = new List<Guid>();
            this.LowPriorityProbability = 0;
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            this.StepCount = 0;
            this.LowPrioritySet.Clear();
            this.HighPrioritySet.Clear();

            // Change the probability of a task to be assigned to the low priority set after each iteration.
            this.LowPriorityProbability = this.LowPriorityProbability >= 0.8 ? 0 : this.LowPriorityProbability + 0.1;

            return true;
        }

        /// <inheritdoc/>
        internal override bool NextDelay(IEnumerable<ControlledOperation> ops, ControlledOperation current,
            int maxValue, out int next)
        {
            Guid id = this.GetOperationId();

            this.StepCount++;

            // Reshuffle the probabilities after every (this.MaxSteps / this.PriorityChangePoints) steps.
            if (this.StepCount % (this.MaxSteps / this.PriorityChangePoints) == 0)
            {
                this.LowPrioritySet.Clear();
                this.HighPrioritySet.Clear();
            }

            // If this task is not assigned to any priority set, then randomly assign it to one of the two sets.
            if (!this.LowPrioritySet.Contains(id) && !this.HighPrioritySet.Contains(id))
            {
                if (this.RandomValueGenerator.NextDouble() < this.LowPriorityProbability)
                {
                    this.LowPrioritySet.Add(id);
                }
                else
                {
                    this.HighPrioritySet.Add(id);
                }
            }

            // Choose a random delay if this task is in the low priority set.
            if (this.LowPrioritySet.Contains(id))
            {
                next = this.RandomValueGenerator.Next(maxValue) * 5;
            }
            else
            {
                next = 0;
            }

            return true;
        }

        /// <inheritdoc/>
        internal override string GetName() => ExplorationStrategy.Prioritization.GetName();

        /// <inheritdoc/>
        internal override string GetDescription() => $"{this.GetName()}[seed:{this.RandomValueGenerator.Seed}]";
    }
}
