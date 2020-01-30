// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;

using Monitor = Microsoft.Coyote.Specifications.Monitor;

namespace Microsoft.Coyote.TestingServices.Scheduling.Strategies
{
    /// <summary>
    /// Abstract strategy for detecting liveness property violations. It
    /// contains a nested <see cref="ISchedulingStrategy"/> that is used
    /// for scheduling decisions.
    /// </summary>
    internal abstract class LivenessCheckingStrategy : ISchedulingStrategy
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
        protected ISchedulingStrategy SchedulingStrategy;

        /// <summary>
        /// Initializes a new instance of the <see cref="LivenessCheckingStrategy"/> class.
        /// </summary>
        internal LivenessCheckingStrategy(Configuration configuration, List<Monitor> monitors, ISchedulingStrategy strategy)
        {
            this.Configuration = configuration;
            this.Monitors = monitors;
            this.SchedulingStrategy = strategy;
        }

        /// <inheritdoc/>
        public abstract bool GetNext(out IAsyncOperation next, IEnumerable<IAsyncOperation> ops, IAsyncOperation current);

        /// <inheritdoc/>
        public abstract bool GetNextBooleanChoice(int maxValue, out bool next);

        /// <inheritdoc/>
        public abstract bool GetNextIntegerChoice(int maxValue, out int next);

        /// <inheritdoc/>
        public void ForceNext(IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void ForceNextBooleanChoice(int maxValue, bool next)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void ForceNextIntegerChoice(int maxValue, int next)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public virtual bool PrepareForNextIteration()
        {
            return this.SchedulingStrategy.PrepareForNextIteration();
        }

        /// <inheritdoc/>
        public virtual void Reset()
        {
            this.SchedulingStrategy.Reset();
        }

        /// <inheritdoc/>
        public virtual int GetScheduledSteps()
        {
            return this.SchedulingStrategy.GetScheduledSteps();
        }

        /// <inheritdoc/>
        public virtual bool HasReachedMaxSchedulingSteps()
        {
            return this.SchedulingStrategy.HasReachedMaxSchedulingSteps();
        }

        /// <inheritdoc/>
        public virtual bool IsFair()
        {
            return this.SchedulingStrategy.IsFair();
        }

        /// <inheritdoc/>
        public virtual string GetDescription()
        {
            return this.SchedulingStrategy.GetDescription();
        }
    }
}
