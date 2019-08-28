// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Coyote.TestingServices.Scheduling.Strategies
{
    /// <summary>
    /// A randomized scheduling strategy with increased probability
    /// to remain in the same scheduling choice.
    /// </summary>
    public sealed class ProbabilisticRandomStrategy : RandomStrategy
    {
        /// <summary>
        /// Number of coin flips.
        /// </summary>
        private readonly int NumberOfCoinFlips;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProbabilisticRandomStrategy"/> class.
        /// It uses the default random number generator (seed is based on current time).
        /// </summary>
        public ProbabilisticRandomStrategy(int maxSteps, int numberOfCoinFlips)
            : base(maxSteps)
        {
            this.NumberOfCoinFlips = numberOfCoinFlips;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProbabilisticRandomStrategy"/> class.
        /// It uses the specified random number generator.
        /// </summary>
        public ProbabilisticRandomStrategy(int maxSteps, int numberOfCoinFlips, IRandomNumberGenerator random)
            : base(maxSteps, random)
        {
            this.NumberOfCoinFlips = numberOfCoinFlips;
        }

        /// <summary>
        /// Returns the next asynchronous operation to schedule.
        /// </summary>
        public override bool GetNext(out IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            var enabledOperations = ops.Where(op => op.IsEnabled).ToList();
            if (enabledOperations.Count == 0)
            {
                next = null;
                return false;
            }

            this.ScheduledSteps++;

            if (enabledOperations.Count > 1)
            {
                if (!this.ShouldCurrentMachineChange() && current.IsEnabled)
                {
                    next = current;
                    return true;
                }
            }

            int idx = this.RandomNumberGenerator.Next(enabledOperations.Count);
            next = enabledOperations[idx];

            return true;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        public override string GetDescription() =>
            $"ProbabilisticRandom[seed '{this.RandomNumberGenerator.Seed}', coin flips '{this.NumberOfCoinFlips}']";

        /// <summary>
        /// Flip the coin a specified number of times.
        /// </summary>
        private bool ShouldCurrentMachineChange()
        {
            for (int idx = 0; idx < this.NumberOfCoinFlips; idx++)
            {
                if (this.RandomNumberGenerator.Next(2) == 1)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
