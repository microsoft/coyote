// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Systematic
{
    /// <summary>
    /// A simple (but effective) randomized scheduling strategy.
    /// </summary>
    internal class RandomStrategy : SchedulingStrategy
    {
        /// <summary>
        /// Random value generator.
        /// </summary>
        protected IRandomValueGenerator RandomValueGenerator;

        /// <summary>
        /// The maximum number of steps to schedule.
        /// </summary>
        protected int MaxScheduledSteps;

        /// <summary>
        /// The number of scheduled steps.
        /// </summary>
        protected int ScheduledSteps;

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomStrategy"/> class.
        /// </summary>
        internal RandomStrategy(int maxSteps, IRandomValueGenerator random)
        {
            this.RandomValueGenerator = random;
            this.MaxScheduledSteps = maxSteps;
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            // The random strategy just needs to reset the number of scheduled steps during
            // the current iretation.
            this.ScheduledSteps = 0;
            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextOperation(IEnumerable<AsyncOperation> ops, AsyncOperation current,
            bool isYielding, out AsyncOperation next)
        {
            var enabledOps = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
            if (enabledOps.Count is 0)
            {
                next = null;
                return false;
            }

            int idx = this.RandomValueGenerator.Next(enabledOps.Count);
            next = enabledOps[idx];

            this.ScheduledSteps++;

            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextBooleanChoice(AsyncOperation current, int maxValue, out bool next)
        {
            next = false;
            if (this.RandomValueGenerator.Next(maxValue) is 0)
            {
                next = true;
            }

            this.ScheduledSteps++;

            return true;
        }

        /// <inheritdoc/>
        internal override bool GetNextIntegerChoice(AsyncOperation current, int maxValue, out int next)
        {
            next = this.RandomValueGenerator.Next(maxValue);
            this.ScheduledSteps++;
            return true;
        }

        /// <inheritdoc/>
        internal override int GetScheduledSteps() => this.ScheduledSteps;

        /// <inheritdoc/>
        internal override bool IsMaxStepsReached()
        {
            if (this.MaxScheduledSteps is 0)
            {
                return false;
            }

            return this.ScheduledSteps >= this.MaxScheduledSteps;
        }

        /// <inheritdoc/>
        internal override bool IsFair() => true;

        /// <inheritdoc/>
        internal override string GetDescription() => $"random[seed '{this.RandomValueGenerator.Seed}']";

        /// <inheritdoc/>
        internal override void Reset()
        {
            this.ScheduledSteps = 0;
        }
    }
}
