// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Systematic
{
    /// <summary>
    /// A portfolio of systematic testing strategies executing sequentially across iteratioons.
    /// </summary>
    internal class PortfolioStrategy : SystematicStrategy
    {
        /// <summary>
        /// Current iteration number.
        /// </summary>
        private uint currentIterationNumber;

        /// <summary>
        /// Random value generator.
        /// </summary>
        protected IRandomValueGenerator RandomValueGenerator;

        /// <summary>
        /// Number of systemmatic testing strategies used in this sequential portfolio testing.
        /// </summary>
        private uint NumberOfstrategiesInPortfolio;

        /// <summary>
        /// The list of strategies which are part of this sequential portfolio testing.
        /// </summary>
        private readonly List<SystematicStrategy> StrategiesPortfolio;

        /// <summary>
        /// Initializes a new instance of the <see cref="PortfolioStrategy"/> class.
        /// </summary>
        internal PortfolioStrategy(int maxStepsFair, int prefixLength, IRandomValueGenerator generator)
        {
            this.RandomValueGenerator = generator;
            this.currentIterationNumber = 0;

            // Currently using 7 schedulers: 1 is random, 3 are probabilistic [bounds: 1, 2, 3], 3 are fiarpct [bounds: 1, 5, 10]
            // Currently used scheme in iterations: 0->random, 1->fairpct[1], 2->probabilistic[1], 3->fairpct[5], 4->probabilistic[2], 5->fairpct[10], 6->probabilistic[3] .. so on
            // TODO: change this hard coding (7) to something which performs best in the general case
            this.NumberOfstrategiesInPortfolio = 7;
            this.StrategiesPortfolio = new List<SystematicStrategy>();
            for (int i = 0; i < this.NumberOfstrategiesInPortfolio; i++)
            {
                if (i == 0)
                {
                    this.StrategiesPortfolio.Add(new RandomStrategy(maxStepsFair, generator));
                }
                else if (i % 2 == 0)
                {
                    this.StrategiesPortfolio.Add(new ProbabilisticRandomStrategy(maxStepsFair, i / 2, generator));
                }
                else if (i == 1)
                {
                    var prefixStrategy = new PCTStrategy(prefixLength, 1, generator);
                    var suffixStrategy = new RandomStrategy(maxStepsFair, generator);
                    this.StrategiesPortfolio.Add(new ComboStrategy(prefixStrategy, suffixStrategy));
                }
                else
                {
                    var prefixStrategy = new PCTStrategy(prefixLength, 5 * (i / 2), generator);
                    var suffixStrategy = new RandomStrategy(maxStepsFair, generator);
                    this.StrategiesPortfolio.Add(new ComboStrategy(prefixStrategy, suffixStrategy));
                }
            }
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            this.currentIterationNumber = iteration;
            uint currentStrategyIteration = this.currentIterationNumber / this.NumberOfstrategiesInPortfolio;
            int currentStrategyIndex = (int)(this.currentIterationNumber % this.NumberOfstrategiesInPortfolio);
            this.StrategiesPortfolio[currentStrategyIndex].InitializeNextIteration(currentStrategyIteration);
            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextOperation(IEnumerable<AsyncOperation> ops, AsyncOperation current,
            bool isYielding, out AsyncOperation next)
        {
            int currentStrategyIndex = (int)(this.currentIterationNumber % this.NumberOfstrategiesInPortfolio);
            this.StrategiesPortfolio[currentStrategyIndex].GetNextOperation(ops, current, isYielding, out next);
            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextBooleanChoice(AsyncOperation current, int maxValue, out bool next)
        {
            int currentStrategyIndex = (int)(this.currentIterationNumber % this.NumberOfstrategiesInPortfolio);
            this.StrategiesPortfolio[currentStrategyIndex].GetNextBooleanChoice(current, maxValue, out next);
            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextIntegerChoice(AsyncOperation current, int maxValue, out int next)
        {
            int currentStrategyIndex = (int)(this.currentIterationNumber % this.NumberOfstrategiesInPortfolio);
            this.StrategiesPortfolio[currentStrategyIndex].GetNextIntegerChoice(current, maxValue, out next);
            return true;
        }

        /// <inheritdoc/>
        internal override int GetStepCount()
        {
            int currentStrategyIndex = (int)(this.currentIterationNumber % this.NumberOfstrategiesInPortfolio);
            return this.StrategiesPortfolio[currentStrategyIndex].GetStepCount();
        }

        /// <inheritdoc/>
        internal override bool IsMaxStepsReached()
        {
            int currentStrategyIndex = (int)(this.currentIterationNumber % this.NumberOfstrategiesInPortfolio);
            return this.StrategiesPortfolio[currentStrategyIndex].IsMaxStepsReached();
        }

        /// <inheritdoc/>
        internal override bool IsFair()
        {
            int currentStrategyIndex = (int)(this.currentIterationNumber % this.NumberOfstrategiesInPortfolio);
            return this.StrategiesPortfolio[currentStrategyIndex].IsFair();
        }

        /// <inheritdoc/>
        internal override string GetDescription()
        {
            var text = $"pct[portfolio '" + this.RandomValueGenerator.Seed + "']";
            return text;
        }

        /// <inheritdoc/>
        internal override void Reset()
        {
            for (int i = 0; i < this.NumberOfstrategiesInPortfolio; i++)
            {
                this.StrategiesPortfolio[i].Reset();
            }

            this.StrategiesPortfolio.Clear();
            this.NumberOfstrategiesInPortfolio = 0;
            this.currentIterationNumber = 0;
        }
    }
}
