// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Coyote.TestingServices.Scheduling.Strategies
{
    /// <summary>
    /// An exhaustive delay-bounding scheduling strategy.
    /// </summary>
    public sealed class ExhaustiveDelayBoundingStrategy : DelayBoundingStrategy, ISchedulingStrategy
    {
        /// <summary>
        /// Cache of delays across iterations.
        /// </summary>
        private List<int> DelaysCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExhaustiveDelayBoundingStrategy"/> class.
        /// It uses the default random number generator (seed is based on current time).
        /// </summary>
        public ExhaustiveDelayBoundingStrategy(int maxSteps, int maxDelays)
            : this(maxSteps, maxDelays, new DefaultRandomNumberGenerator(DateTime.Now.Millisecond))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExhaustiveDelayBoundingStrategy"/> class.
        /// It uses the specified random number generator.
        /// </summary>
        public ExhaustiveDelayBoundingStrategy(int maxSteps, int maxDelays, IRandomNumberGenerator random)
            : base(maxSteps, maxDelays, random)
        {
            this.DelaysCache = Enumerable.Repeat(0, this.MaxDelays).ToList();
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        public override bool PrepareForNextIteration()
        {
            this.ScheduleLength = Math.Max(this.ScheduleLength, this.ScheduledSteps);
            this.ScheduledSteps = 0;

            var bound = Math.Min(this.MaxScheduledSteps, this.ScheduleLength);
            for (var idx = 0; idx < this.MaxDelays; idx++)
            {
                if (this.DelaysCache[idx] < bound)
                {
                    this.DelaysCache[idx] = this.DelaysCache[idx] + 1;
                    break;
                }

                this.DelaysCache[idx] = 0;
            }

            this.RemainingDelays.Clear();
            this.RemainingDelays.AddRange(this.DelaysCache);
            this.RemainingDelays.Sort();

            return true;
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public override void Reset()
        {
            this.DelaysCache = Enumerable.Repeat(0, this.MaxDelays).ToList();
            base.Reset();
        }

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        public override string GetDescription()
        {
            var text = this.MaxDelays + "' delays, delays '[";
            for (int idx = 0; idx < this.DelaysCache.Count; idx++)
            {
                text += this.DelaysCache[idx];
                if (idx < this.DelaysCache.Count - 1)
                {
                    text += ", ";
                }
            }

            text += "]'";
            return text;
        }
    }
}
