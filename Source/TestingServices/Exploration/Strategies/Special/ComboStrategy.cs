// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace Microsoft.Coyote.TestingServices.Scheduling.Strategies
{
    /// <summary>
    /// This strategy combines two given strategies, using them to schedule
    /// the prefix and suffix of an execution.
    /// </summary>
    public sealed class ComboStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// The prefix strategy.
        /// </summary>
        private readonly ISchedulingStrategy PrefixStrategy;

        /// <summary>
        /// The suffix strategy.
        /// </summary>
        private readonly ISchedulingStrategy SuffixStrategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComboStrategy"/> class.
        /// </summary>
        public ComboStrategy(ISchedulingStrategy prefixStrategy, ISchedulingStrategy suffixStrategy)
        {
            this.PrefixStrategy = prefixStrategy;
            this.SuffixStrategy = suffixStrategy;
        }

        /// <inheritdoc/>
        public bool GetNext(out IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            if (this.PrefixStrategy.HasReachedMaxSchedulingSteps())
            {
                return this.SuffixStrategy.GetNext(out next, ops, current);
            }
            else
            {
                return this.PrefixStrategy.GetNext(out next, ops, current);
            }
        }

        /// <inheritdoc/>
        public bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            if (this.PrefixStrategy.HasReachedMaxSchedulingSteps())
            {
                return this.SuffixStrategy.GetNextBooleanChoice(maxValue, out next);
            }
            else
            {
                return this.PrefixStrategy.GetNextBooleanChoice(maxValue, out next);
            }
        }

        /// <inheritdoc/>
        public bool GetNextIntegerChoice(int maxValue, out int next)
        {
            if (this.PrefixStrategy.HasReachedMaxSchedulingSteps())
            {
                return this.SuffixStrategy.GetNextIntegerChoice(maxValue, out next);
            }
            else
            {
                return this.PrefixStrategy.GetNextIntegerChoice(maxValue, out next);
            }
        }

        /// <inheritdoc/>
        public void ForceNext(IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            if (this.PrefixStrategy.HasReachedMaxSchedulingSteps())
            {
                this.SuffixStrategy.ForceNext(next, ops, current);
            }
            else
            {
                this.PrefixStrategy.ForceNext(next, ops, current);
            }
        }

        /// <inheritdoc/>
        public void ForceNextBooleanChoice(int maxValue, bool next)
        {
            if (this.PrefixStrategy.HasReachedMaxSchedulingSteps())
            {
                this.SuffixStrategy.ForceNextBooleanChoice(maxValue, next);
            }
            else
            {
                this.PrefixStrategy.ForceNextBooleanChoice(maxValue, next);
            }
        }

        /// <inheritdoc/>
        public void ForceNextIntegerChoice(int maxValue, int next)
        {
            if (this.PrefixStrategy.HasReachedMaxSchedulingSteps())
            {
                this.SuffixStrategy.ForceNextIntegerChoice(maxValue, next);
            }
            else
            {
                this.PrefixStrategy.ForceNextIntegerChoice(maxValue, next);
            }
        }

        /// <inheritdoc/>
        public bool PrepareForNextIteration()
        {
            bool doNext = this.PrefixStrategy.PrepareForNextIteration();
            doNext |= this.SuffixStrategy.PrepareForNextIteration();
            return doNext;
        }

        /// <inheritdoc/>
        public void Reset()
        {
            this.PrefixStrategy.Reset();
            this.SuffixStrategy.Reset();
        }

        /// <inheritdoc/>
        public int GetScheduledSteps()
        {
            if (this.PrefixStrategy.HasReachedMaxSchedulingSteps())
            {
                return this.SuffixStrategy.GetScheduledSteps() + this.PrefixStrategy.GetScheduledSteps();
            }
            else
            {
                return this.PrefixStrategy.GetScheduledSteps();
            }
        }

        /// <inheritdoc/>
        public bool HasReachedMaxSchedulingSteps() => this.SuffixStrategy.HasReachedMaxSchedulingSteps();

        /// <inheritdoc/>
        public bool IsFair() => this.SuffixStrategy.IsFair();

        /// <inheritdoc/>
        public string GetDescription() =>
            string.Format("Combo[{0},{1}]", this.PrefixStrategy.GetDescription(), this.SuffixStrategy.GetDescription());
    }
}
