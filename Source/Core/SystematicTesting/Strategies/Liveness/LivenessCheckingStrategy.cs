// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Monitor = Microsoft.Coyote.Specifications.Monitor;

namespace Microsoft.Coyote.SystematicTesting.Strategies
{
    /// <summary>
    /// Abstract strategy for detecting liveness property violations. It
    /// contains a nested <see cref="SchedulingStrategy"/> that is used
    /// for scheduling decisions.
    /// </summary>
    internal abstract class LivenessCheckingStrategy : SchedulingStrategy
    {
        /// <summary>
        /// The configuration.
        /// </summary>
        protected Configuration Configuration;

        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        protected List<Monitor> Monitors;

        /// <summary>
        /// Strategy used for scheduling decisions.
        /// </summary>
        protected SchedulingStrategy SchedulingStrategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="LivenessCheckingStrategy"/> class.
        /// </summary>
        internal LivenessCheckingStrategy(Configuration configuration, List<Monitor> monitors, SchedulingStrategy strategy)
        {
            this.Configuration = configuration;
            this.Monitors = monitors;
            this.SchedulingStrategy = strategy;
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration) =>
            this.SchedulingStrategy.InitializeNextIteration(iteration);

        /// <inheritdoc/>
        internal override int GetScheduledSteps() => this.SchedulingStrategy.GetScheduledSteps();

        /// <inheritdoc/>
        internal override bool HasReachedMaxSchedulingSteps() =>
            this.SchedulingStrategy.HasReachedMaxSchedulingSteps();

        /// <inheritdoc/>
        internal override bool IsFair() => this.SchedulingStrategy.IsFair();

        /// <inheritdoc/>
        internal override string GetDescription() => this.SchedulingStrategy.GetDescription();

        /// <inheritdoc/>
        internal override void Reset() => this.SchedulingStrategy.Reset();
    }
}
