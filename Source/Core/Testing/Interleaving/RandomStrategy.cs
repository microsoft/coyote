// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Interleaving
{
    /// <summary>
    /// A simple (but effective) randomized exploration strategy.
    /// </summary>
    internal class RandomStrategy : InterleavingStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RandomStrategy"/> class.
        /// </summary>
        internal RandomStrategy(Configuration configuration, bool isFair = true)
            : base(configuration, isFair)
        {
        }

        /// <inheritdoc/>
        internal override bool NextOperation(IEnumerable<ControlledOperation> ops, ControlledOperation current, ulong state,
            bool isYielding, out ControlledOperation next)
        {
            int idx = this.RandomValueGenerator.Next(ops.Count());
            next = ops.ElementAt(idx);
            return true;
        }

        /// <inheritdoc/>
        internal override bool NextBoolean(ControlledOperation current, ulong state, out bool next)
        {
            next = this.RandomValueGenerator.Next(2) is 0 ? true : false;
            return true;
        }

        /// <inheritdoc/>
        internal override bool NextInteger(int maxValue, ControlledOperation current, ulong state, out int next)
        {
            next = this.RandomValueGenerator.Next(maxValue);
            return true;
        }

        /// <inheritdoc/>
        internal override string GetName() => ExplorationStrategy.Random.GetName();

        /// <inheritdoc/>
        internal override string GetDescription() => $"{this.GetName()}[seed:{this.RandomValueGenerator.Seed}]";
    }
}
