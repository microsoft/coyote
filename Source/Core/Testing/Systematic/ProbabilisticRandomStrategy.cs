// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Systematic
{
    /// <summary>
    /// A randomized scheduling strategy with increased probability
    /// to remain in the same scheduling choice.
    /// </summary>
    internal sealed class ProbabilisticRandomStrategy : RandomStrategy
    {
        /// <summary>
        /// Number of coin flips.
        /// </summary>
        private readonly int NumberOfCoinFlips;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProbabilisticRandomStrategy"/> class.
        /// </summary>
        internal ProbabilisticRandomStrategy(Configuration configuration, IRandomValueGenerator generator)
            : base(configuration, generator)
        {
            this.NumberOfCoinFlips = configuration.StrategyBound;
        }

        /// <inheritdoc/>
        internal override bool GetNextOperation(List<ControlledOperation> ops, ControlledOperation current,
            bool isYielding, out ControlledOperation next)
        {
            this.StepCount++;
            if (ops.Count > 1)
            {
                if (!this.ShouldCurrentMachineChange() && current.Status is OperationStatus.Enabled)
                {
                    next = current;
                    return true;
                }
            }

            int idx = this.RandomValueGenerator.Next(ops.Count);
            next = ops[idx];
            return true;
        }

        /// <inheritdoc/>
        internal override string GetDescription() =>
            $"probabilistic[seed '{this.RandomValueGenerator.Seed}', coin flips '{this.NumberOfCoinFlips}']";

        /// <summary>
        /// Flip the coin a specified number of times.
        /// </summary>
        private bool ShouldCurrentMachineChange()
        {
            for (int idx = 0; idx < this.NumberOfCoinFlips; idx++)
            {
                if (this.RandomValueGenerator.Next(2) is 1)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
