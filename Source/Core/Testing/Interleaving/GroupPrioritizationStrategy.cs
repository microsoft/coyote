// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Interleaving
{
    /// <summary>
    /// A (fair) group-based probabilistic priority-based scheduling strategy.
    /// </summary>
    internal sealed class GroupPrioritizationStrategy : PrioritizationStrategy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GroupPrioritizationStrategy"/> class.
        /// </summary>
        internal GroupPrioritizationStrategy(Configuration configuration, IRandomValueGenerator generator, bool isFair)
            : base(configuration, generator, isFair)
        {
        }

        /// <inheritdoc/>
        internal override string GetDescription() =>
            $"group-prioritization[fair:{this.IsFair},bound:{this.MaxPriorityChanges},seed:{this.RandomValueGenerator.Seed}]";
    }
}
