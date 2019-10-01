// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

namespace Microsoft.Coyote.TestingServices.Scheduling.Strategies
{
    /// <summary>
    /// A randomized delay-bounding scheduling strategy.
    /// </summary>
    public sealed class RandomDelayBoundingStrategy : DelayBoundingStrategy, ISchedulingStrategy
    {
        /// <summary>
        /// Delays during this iteration.
        /// </summary>
        private readonly List<int> CurrentIterationDelays;

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomDelayBoundingStrategy"/> class.
        /// It uses the default random number generator (seed is based on current time).
        /// </summary>
        public RandomDelayBoundingStrategy(int maxSteps, int maxDelays)
            : this(maxSteps, maxDelays, new DefaultRandomNumberGenerator(DateTime.Now.Millisecond))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RandomDelayBoundingStrategy"/> class.
        /// It uses the specified random number generator.
        /// </summary>
        public RandomDelayBoundingStrategy(int maxSteps, int maxDelays, IRandomNumberGenerator random)
            : base(maxSteps, maxDelays, random)
        {
            this.CurrentIterationDelays = new List<int>();
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        /// <returns>True to start the next iteration.</returns>
        public override bool PrepareForNextIteration()
        {
            this.ScheduleLength = Math.Max(this.ScheduleLength, this.ScheduledSteps);
            this.ScheduledSteps = 0;

            this.RemainingDelays.Clear();
            for (int idx = 0; idx < this.MaxDelays; idx++)
            {
                this.RemainingDelays.Add(this.RandomNumberGenerator.Next(this.ScheduleLength));
            }

            this.RemainingDelays.Sort();

            this.CurrentIterationDelays.Clear();
            this.CurrentIterationDelays.AddRange(this.RemainingDelays);

            return true;
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        public override string GetDescription()
        {
            var text = "Random seed '" + this.RandomNumberGenerator.Seed + "', '" + this.MaxDelays + "' delays, delays '[";
            for (int idx = 0; idx < this.CurrentIterationDelays.Count; idx++)
            {
                text += this.CurrentIterationDelays[idx];
                if (idx < this.CurrentIterationDelays.Count - 1)
                {
                    text += ", ";
                }
            }

            text += "]'";
            return text;
        }
    }
}
