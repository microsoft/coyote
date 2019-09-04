// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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

        /// <summary>
        /// Returns the next asynchronous operation to schedule.
        /// </summary>
        public virtual bool GetNext(out IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            var currentMachineIdx = ops.IndexOf(current);
            var orderedMachines = ops.GetRange(currentMachineIdx, ops.Count - currentMachineIdx);
            if (currentMachineIdx != 0)
            {
                orderedMachines.AddRange(ops.GetRange(0, currentMachineIdx));
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

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
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

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        public virtual bool GetNextIntegerChoice(int maxValue, out int next)
        {
            next = this.RandomNumberGenerator.Next(maxValue);
            this.ScheduledSteps++;
            return true;
        }

        /// <summary>
        /// Forces the next asynchronous operation to be scheduled.
        /// </summary>
        public void ForceNext(IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            this.ScheduledSteps++;
        }

        /// <summary>
        /// Forces the next boolean choice.
        /// </summary>
        public void ForceNextBooleanChoice(int maxValue, bool next)
        {
            this.ScheduledSteps++;
        }

        /// <summary>
        /// Forces the next integer choice.
        /// </summary>
        public void ForceNextIntegerChoice(int maxValue, int next)
        {
            this.ScheduledSteps++;
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        public abstract bool PrepareForNextIteration();

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public virtual void Reset()
        {
            this.ScheduleLength = 0;
            this.ScheduledSteps = 0;
            this.RemainingDelays.Clear();
        }

        /// <summary>
        /// Returns the scheduled steps.
        /// </summary>
        public int GetScheduledSteps() => this.ScheduledSteps;

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        public bool HasReachedMaxSchedulingSteps()
        {
            if (this.MaxScheduledSteps == 0)
            {
                return false;
            }

            return this.ScheduledSteps >= this.MaxScheduledSteps;
        }

        /// <summary>
        /// Checks if this is a fair scheduling strategy.
        /// </summary>
        public bool IsFair() => false;

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        public abstract string GetDescription();
    }
}
