// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Fuzzing
{
    /// <summary>
    /// A randomized fuzzing strategy.
    /// </summary>
    internal class RandomStrategy : FuzzingStrategy
    {
        /// <summary>
        /// Random value generator.
        /// </summary>
        protected IRandomValueGenerator RandomValueGenerator;

        /// <summary>
        /// Map from tasks to total delays.
        /// </summary>
        private readonly ConcurrentDictionary<Guid, int> TotalTaskDelayMap;

        /// <summary>
        /// The maximum number of steps to explore.
        /// </summary>
        protected readonly int MaxSteps;

        /// <summary>
        /// The number of exploration steps.
        /// </summary>
        protected int StepCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomStrategy"/> class.
        /// </summary>
        internal RandomStrategy(int maxDelays, IRandomValueGenerator random)
        {
            this.RandomValueGenerator = random;
            this.TotalTaskDelayMap = new ConcurrentDictionary<Guid, int>();
            this.MaxSteps = maxDelays;
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            this.StepCount = 0;
            this.TotalTaskDelayMap.Clear();
            return true;
        }

        /// <inheritdoc/>
        /// <remarks>
        /// The delay has an injection probability of 0.05 and is in the range
        /// of [1, 100] with an upper bound of 5000ms per task.
        /// </remarks>
        internal override bool GetNextDelay(int maxValue, out int next, FuzzingState state = null, AsyncOperation operation = null)
        {
            Guid id = this.GetOperationId();

            // Ensure that the max delay per task is less than 5000ms.
            int maxDelay = this.TotalTaskDelayMap.GetOrAdd(id, 0);
            if (maxDelay < 5000 && this.RandomValueGenerator.NextDouble() < 0.05)
            {
                // There is a 0.05 probability for a 1-100ms delay.
                next = this.RandomValueGenerator.Next(100) + 1;
            }
            else
            {
                next = 0;
            }

            if (next > 0)
            {
                // Update the total delay per task.
                this.TotalTaskDelayMap.TryUpdate(id, maxDelay + next, maxDelay);
            }

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
        internal override string GetDescription() => $"random[seed '{this.RandomValueGenerator.Seed}']";
    }
}
