// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Interleaving
{
    /// <summary>
    /// This strategy combines two given strategies, using them to schedule
    /// the prefix and suffix of an execution.
    /// </summary>
    internal sealed class ComboStrategy : InterleavingStrategy
    {
        /// <summary>
        /// The prefix strategy.
        /// </summary>
        private readonly InterleavingStrategy PrefixStrategy;

        /// <summary>
        /// The suffix strategy.
        /// </summary>
        private readonly InterleavingStrategy SuffixStrategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComboStrategy"/> class.
        /// </summary>
        internal ComboStrategy(Configuration configuration, IRandomValueGenerator generator,
            InterleavingStrategy prefixStrategy, InterleavingStrategy suffixStrategy)
            : base(configuration, generator, suffixStrategy.IsFair)
        {
            this.PrefixStrategy = prefixStrategy;
            this.SuffixStrategy = suffixStrategy;
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            bool doNext = this.PrefixStrategy.InitializeNextIteration(iteration);
            doNext |= this.SuffixStrategy.InitializeNextIteration(iteration);
            return doNext;
        }

        /// <inheritdoc/>
        internal override bool NextOperation(IEnumerable<ControlledOperation> ops, ControlledOperation current,
            bool isYielding, out ControlledOperation next)
        {
            if (this.PrefixStrategy.IsMaxStepsReached())
            {
                return this.SuffixStrategy.NextOperation(ops, current, isYielding, out next);
            }
            else
            {
                return this.PrefixStrategy.NextOperation(ops, current, isYielding, out next);
            }
        }

        /// <inheritdoc/>
        internal override bool NextBoolean(ControlledOperation current, out bool next)
        {
            if (this.PrefixStrategy.IsMaxStepsReached())
            {
                return this.SuffixStrategy.NextBoolean(current, out next);
            }
            else
            {
                return this.PrefixStrategy.NextBoolean(current, out next);
            }
        }

        /// <inheritdoc/>
        internal override bool NextInteger(ControlledOperation current, int maxValue, out int next)
        {
            if (this.PrefixStrategy.IsMaxStepsReached())
            {
                return this.SuffixStrategy.NextInteger(current, maxValue, out next);
            }
            else
            {
                return this.PrefixStrategy.NextInteger(current, maxValue, out next);
            }
        }

        /// <inheritdoc/>
        internal override int GetStepCount()
        {
            if (this.PrefixStrategy.IsMaxStepsReached())
            {
                return this.SuffixStrategy.GetStepCount() + this.PrefixStrategy.GetStepCount();
            }
            else
            {
                return this.PrefixStrategy.GetStepCount();
            }
        }

        /// <inheritdoc/>
        internal override bool IsMaxStepsReached() => this.SuffixStrategy.IsMaxStepsReached();

        /// <inheritdoc/>
        internal override string GetDescription() =>
            string.Format("combo({0},{1})", this.PrefixStrategy.GetDescription(), this.SuffixStrategy.GetDescription());

        /// <inheritdoc/>
        internal override void Reset()
        {
            this.PrefixStrategy.Reset();
            this.SuffixStrategy.Reset();
        }
    }
}
