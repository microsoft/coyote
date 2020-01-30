// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Coyote.IO;

namespace Microsoft.Coyote.TestingServices.Scheduling.Strategies
{
    /// <summary>
    /// An abstract delay-bounding scheduling strategy.
    /// </summary>
    public abstract class DelayBoundingStrategy : ISchedulingStrategy
    {
        /// <summary>
        /// The random number generator used by the strategy.
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
        /// Length of the explored schedule across all iterations.
        /// </summary>
        protected int ScheduleLength;

        /// <summary>
        /// The maximum number of delays.
        /// </summary>
        protected int MaxDelays;

        /// <summary>
        /// The remaining delays.
        /// </summary>
        protected List<int> RemainingDelays;

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayBoundingStrategy"/> class.
        /// It uses the default random number generator (seed is based on current time).
        /// </summary>
        public DelayBoundingStrategy(int maxSteps, int maxDelays)
            : this(maxSteps, maxDelays, new DefaultRandomNumberGenerator(DateTime.Now.Millisecond))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DelayBoundingStrategy"/> class.
        /// It uses the specified random number generator.
        /// </summary>
        public DelayBoundingStrategy(int maxSteps, int maxDelays, IRandomNumberGenerator random)
        {
            this.RandomNumberGenerator = random;
            this.MaxScheduledSteps = maxSteps;
            this.ScheduledSteps = 0;
            this.MaxDelays = maxDelays;
            this.ScheduleLength = 0;
            this.RemainingDelays = new List<int>();
        }

        /// <inheritdoc/>
        public virtual bool GetNext(out IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            var currentActorIdx = ops.IndexOf(current);
            var orderedMachines = ops.GetRange(currentActorIdx, ops.Count - currentActorIdx);
            if (currentActorIdx != 0)
            {
                orderedMachines.AddRange(ops.GetRange(0, currentActorIdx));
            }

            var enabledOperations = orderedMachines.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
            if (enabledOperations.Count == 0)
            {
                next = null;
                return false;
            }

            int idx = 0;
            while (this.RemainingDelays.Count > 0 && this.ScheduledSteps == this.RemainingDelays[0])
            {
                idx = (idx + 1) % enabledOperations.Count;
                this.RemainingDelays.RemoveAt(0);
                Debug.WriteLine("<DelayLog> Inserted delay, '{0}' remaining.", this.RemainingDelays.Count);
            }

            next = enabledOperations[idx];

            this.ScheduledSteps++;

            return true;
        }

        //// <inheritdoc/>
        public virtual bool GetNextBooleanChoice(int maxValue, out bool next)
        {
            next = false;
            if (this.RemainingDelays.Count > 0 && this.ScheduledSteps == this.RemainingDelays[0])
            {
                next = true;
                this.RemainingDelays.RemoveAt(0);
                Debug.WriteLine("<DelayLog> Inserted delay, '{0}' remaining.", this.RemainingDelays.Count);
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
        public abstract bool PrepareForNextIteration();

        /// <inheritdoc/>
        public virtual void Reset()
        {
            this.ScheduleLength = 0;
            this.ScheduledSteps = 0;
            this.RemainingDelays.Clear();
        }

        //// <inheritdoc/>
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
        public bool IsFair() => false;

        /// <inheritdoc/>
        public abstract string GetDescription();
    }
}
