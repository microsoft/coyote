// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Systematic
{
    /// <summary>
    /// This strategy combines two given strategies, using them to schedule
    /// the prefix and suffix of an execution.
    /// </summary>
    internal sealed class ComboStrategy : SystematicStrategy
    {
        /// <summary>
        /// The prefix strategy.
        /// </summary>
        private readonly SystematicStrategy PrefixStrategy;

        /// <summary>
        /// The suffix strategy.
        /// </summary>
        private readonly SystematicStrategy SuffixStrategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComboStrategy"/> class.
        /// </summary>
        internal ComboStrategy(Configuration configuration, IRandomValueGenerator generator,
            SystematicStrategy prefixStrategy, SystematicStrategy suffixStrategy)
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
        internal override bool GetNextOperation(IEnumerable<ControlledOperation> ops, ControlledOperation current,
            bool isYielding, out ControlledOperation next)
        {
            if (this.PrefixStrategy.IsMaxStepsReached())
            {
                return this.SuffixStrategy.GetNextOperation(ops, current, isYielding, out next);
            }
            else
            {
                return this.PrefixStrategy.GetNextOperation(ops, current, isYielding, out next);
            }
        }

        /// <inheritdoc/>
        internal override bool GetNextBooleanChoice(ControlledOperation current, int maxValue, out bool next)
        {
            if (this.PrefixStrategy.IsMaxStepsReached())
            {
                return this.SuffixStrategy.GetNextBooleanChoice(current, maxValue, out next);
            }
            else
            {
                return this.PrefixStrategy.GetNextBooleanChoice(current, maxValue, out next);
            }
        }

        /// <inheritdoc/>
        internal override bool GetNextIntegerChoice(ControlledOperation current, int maxValue, out int next)
        {
            if (this.PrefixStrategy.IsMaxStepsReached())
            {
                return this.SuffixStrategy.GetNextIntegerChoice(current, maxValue, out next);
            }
            else
            {
                return this.PrefixStrategy.GetNextIntegerChoice(current, maxValue, out next);
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
            string.Format("combo[{0},{1}]", this.PrefixStrategy.GetDescription(), this.SuffixStrategy.GetDescription());

        /// <inheritdoc/>
        internal override void Reset()
        {
            this.PrefixStrategy.Reset();
            this.SuffixStrategy.Reset();
        }
    }
}
