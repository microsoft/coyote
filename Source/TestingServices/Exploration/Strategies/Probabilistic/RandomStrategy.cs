// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Coyote.TestingServices.Scheduling.Strategies
{
    /// <summary>
    /// A simple (but effective) randomized scheduling strategy.
    /// </summary>
    public class RandomStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// Random number generator.
        /// </summary>
        protected IRandomNumberGenerator RandomNumberGenerator;

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
        /// It uses the default random number generator (seed is based on current time).
        /// </summary>
        public RandomStrategy(int maxSteps)
            : this(maxSteps, new DefaultRandomNumberGenerator(DateTime.Now.Millisecond))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomStrategy"/> class.
        /// It uses the specified random number generator.
        /// </summary>
        public RandomStrategy(int maxSteps, IRandomNumberGenerator random)
        {
            this.RandomNumberGenerator = random;
            this.MaxScheduledSteps = maxSteps;
            this.ScheduledSteps = 0;
        }

        /// <inheritdoc/>
        public virtual bool GetNext(out IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            var enabledOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
            if (enabledOperations.Count == 0)
            {
                next = null;
                return false;
            }

            int idx = this.RandomNumberGenerator.Next(enabledOperations.Count);
            next = enabledOperations[idx];

            this.ScheduledSteps++;

            return true;
        }

        /// <inheritdoc/>
        public virtual bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            next = false;
            if (this.RandomNumberGenerator.Next(maxValue) == 0)
            {
                next = true;
            }

            this.ScheduledSteps++;

            return true;
        }

        /// <inheritdoc/>
        public virtual bool GetNextIntegerChoice(int maxValue, out int next)
        {
            next = this.RandomNumberGenerator.Next(maxValue);
            this.ScheduledSteps++;
            return true;
        }

        /// <inheritdoc/>
        public void ForceNext(IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            this.ScheduledSteps++;
        }

        /// <inheritdoc/>
        public void ForceNextBooleanChoice(int maxValue, bool next)
        {
            this.ScheduledSteps++;
        }

        /// <inheritdoc/>
        public void ForceNextIntegerChoice(int maxValue, int next)
        {
            this.ScheduledSteps++;
        }

        /// <inheritdoc/>
        public virtual bool PrepareForNextIteration()
        {
            this.ScheduledSteps = 0;
            return true;
        }

        /// <inheritdoc/>
        public virtual void Reset()
        {
            this.ScheduledSteps = 0;
        }

        /// <inheritdoc/>
        public int GetScheduledSteps() => this.ScheduledSteps;

        /// <inheritdoc/>
        public bool HasReachedMaxSchedulingSteps()
        {
            if (this.MaxScheduledSteps == 0)
            {
                return false;
            }

            return this.ScheduledSteps >= this.MaxScheduledSteps;
        }

        /// <inheritdoc/>
        public bool IsFair() => true;

        /// <inheritdoc/>
        public virtual string GetDescription() => $"Random[seed '{this.RandomNumberGenerator.Seed}']";
    }
}
