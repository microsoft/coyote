// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
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
            this.MaxSteps = maxDelays;
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            this.StepCount = 0;
            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextDelay(IEnumerable<AsyncOperation> ops, AsyncOperation current,
            int maxValue, bool positiveDelay, bool isRecursive, out int next)
        {
            next = this.RandomValueGenerator.Next(maxValue);

            if (!isRecursive)
            {
                this.StepCount++;
            }

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
