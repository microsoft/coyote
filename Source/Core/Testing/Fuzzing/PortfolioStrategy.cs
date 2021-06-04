// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Testing.Fuzzing
{
    internal class PortfolioStrategy : FuzzingStrategy
    {
        /// <summary>
        /// Random value generator.
        /// </summary>
        protected readonly IRandomValueGenerator RandomValueGenerator;

        /// <summary>
        /// The maximum number of steps to explore.
        /// </summary>
        protected readonly int MaxSteps;

        protected FuzzingStrategy CurrentStrategy;
        protected enum Strategy
        {
            Random,
            PPCT
        }

        protected Strategy NextStrategy;

        protected int PriorityChangePoints;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortfolioStrategy"/> class.
        /// </summary>
        internal PortfolioStrategy(int maxDelays, IRandomValueGenerator random, int priorityChangePoint)
        {
            this.RandomValueGenerator = random;
            this.MaxSteps = maxDelays;
            this.PriorityChangePoints = priorityChangePoint;

            this.CurrentStrategy = new RandomStrategy(maxDelays, random);
            this.NextStrategy = Strategy.Random;
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            switch (this.NextStrategy)
            {
                case Strategy.Random:
                    this.CurrentStrategy = new RandomStrategy(this.MaxSteps, this.RandomValueGenerator);
                    break;
                case Strategy.PPCT:
                    this.CurrentStrategy = new PCTStrategy(this.MaxSteps, this.RandomValueGenerator, this.PriorityChangePoints);
                    break;
                default:
                    this.CurrentStrategy = new RandomStrategy(this.MaxSteps, this.RandomValueGenerator);
                    this.NextStrategy = Strategy.Random;
                    break;
            }

            this.NextStrategy++;

            return this.CurrentStrategy.InitializeNextIteration(iteration);
        }

        /// <inheritdoc/>
        internal override bool GetNextDelay(int maxValue, out int next)
        {
            return this.CurrentStrategy.GetNextDelay(maxValue, out next);
        }

        /// <inheritdoc/>
        internal override int GetStepCount() => this.CurrentStrategy.GetStepCount();

        /// <inheritdoc/>
        internal override bool IsMaxStepsReached() => this.CurrentStrategy.IsMaxStepsReached();

        /// <inheritdoc/>
        internal override bool IsFair() => this.CurrentStrategy.IsFair();

        /// <inheritdoc/>
        internal override string GetDescription() => this.CurrentStrategy.GetDescription();
    }
}
