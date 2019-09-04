// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote.IO;

namespace Microsoft.Coyote.TestingServices.Scheduling.Strategies
{
    /// <summary>
    /// A depth-first search scheduling strategy that uses iterative deepening.
    /// </summary>
    public sealed class IterativeDeepeningDFSStrategy : DFSStrategy, ISchedulingStrategy
    {
        /// <summary>
        /// The max depth.
        /// </summary>
        private readonly int MaxDepth;

        /// <summary>
        /// The current depth.
        /// </summary>
        private int CurrentDepth;

        /// <summary>
        /// Initializes a new instance of the <see cref="IterativeDeepeningDFSStrategy"/> class.
        /// </summary>
        public IterativeDeepeningDFSStrategy(int maxSteps)
            : base(maxSteps)
        {
            this.MaxDepth = maxSteps;
            this.CurrentDepth = 1;
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        public override bool PrepareForNextIteration()
        {
            bool doNext = this.PrepareForNextIteration();
            if (!doNext)
            {
                this.Reset();
                this.CurrentDepth++;
                if (this.CurrentDepth <= this.MaxDepth)
                {
                    Debug.WriteLine($"<IterativeDeepeningDFSLog> Depth bound increased to {this.CurrentDepth} (max is {this.MaxDepth}).");
                    doNext = true;
                }
            }

            return doNext;
        }

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        public new bool HasReachedMaxSchedulingSteps() => this.ScheduledSteps == this.CurrentDepth;

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        public new string GetDescription() => $"DFS with iterative deepening (max depth is {this.MaxDepth})";
    }
}
