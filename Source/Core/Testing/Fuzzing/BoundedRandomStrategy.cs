// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Fuzzing
{
    /// <summary>
    /// A bounded randomized fuzzing strategy.
    /// </summary>
    internal class BoundedRandomStrategy : FuzzingStrategy
    {
        /// <summary>
        /// Random value generator.
        /// </summary>
        protected IRandomValueGenerator RandomValueGenerator;

        /// <summary>
        /// Map from operation ids to total delays.
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
        /// Initializes a new instance of the <see cref="BoundedRandomStrategy"/> class.
        /// </summary>
        internal BoundedRandomStrategy(int maxDelays, IRandomValueGenerator random)
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
        /// The delay has an injection probability of 0.05 and is in the range of [10, maxValue * 10]
        /// with an increment of 10 and an upper bound of 5000ms per operation.
        /// </remarks>
        internal override bool GetNextDelay(IEnumerable<AsyncOperation> ops, AsyncOperation current,
            int maxValue, bool positiveDelay, out int next)
        {
            Guid id = this.GetOperationId();

            // Ensure that the max delay per operation is less than 5000ms.
            int maxDelay = this.TotalTaskDelayMap.GetOrAdd(id, 0);

            // There is a 0.05 probability for a delay.
            if (maxDelay < 5000 && this.RandomValueGenerator.NextDouble() < 0.05)
            {
                next = (this.RandomValueGenerator.Next(maxValue) * 10) + 10;
            }
            else
            {
                next = 0;
            }

            if (next > 0)
            {
                // Update the total delay per operation.
                this.TotalTaskDelayMap.TryUpdate(id, maxDelay + next, maxDelay);
            }

            this.StepCount++;
            return true;
        }

        internal override bool GetNextRecursiveDelayChoice(IEnumerable<AsyncOperation> ops, AsyncOperation current)
        {
            return this.RandomValueGenerator.Next(2) is 0 ? true : false;
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
