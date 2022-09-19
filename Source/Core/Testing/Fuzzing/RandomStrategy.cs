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
        /// Initializes a new instance of the <see cref="RandomStrategy"/> class.
        /// </summary>
        internal RandomStrategy(Configuration configuration, IRandomValueGenerator generator)
            : base(configuration, generator, true)
        {
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            this.StepCount = 0;
            return true;
        }

        /// <inheritdoc/>
        internal override bool NextDelay(IEnumerable<ControlledOperation> ops, ControlledOperation current,
            int maxValue, out int next)
        {
            next = this.RandomValueGenerator.Next(maxValue);
            this.StepCount++;
            return true;
        }

        /// <inheritdoc/>
        internal override string GetDescription() => $"random[seed:{this.RandomValueGenerator.Seed}]";
    }
}
