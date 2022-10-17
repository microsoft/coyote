// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Logging;

namespace Microsoft.Coyote.Testing
{
    /// <summary>
    /// Abstract program exploration strategy.
    /// </summary>
    internal abstract class ExplorationStrategy
    {
        /// <summary>
        /// The runtime configuration.
        /// </summary>
        protected readonly Configuration Configuration;

        /// <summary>
        /// The maximum number of steps to explore.
        /// </summary>
        protected readonly int MaxSteps;

        /// <summary>
        /// The number of exploration steps.
        /// </summary>
        protected int StepCount;

        /// <summary>
        /// True if this is a fair strategy, else false.
        /// </summary>
        internal readonly bool IsFair;

        /// <summary>
        /// A random value generator that can be used by the strategy.
        /// </summary>
        protected internal IRandomValueGenerator RandomValueGenerator { get; internal set; }

        /// <summary>
        /// Responsible for writing to the installed <see cref="ILogger"/>.
        /// </summary>
        protected internal LogWriter LogWriter { get; internal set; }

        /// <summary>
        /// Text describing the last exploration error, if there was any.
        /// </summary>
        protected internal string ErrorText { get; protected set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExplorationStrategy"/> class.
        /// </summary>
        protected ExplorationStrategy(Configuration configuration, bool isFair)
        {
            this.Configuration = configuration;
            this.MaxSteps = isFair ? configuration.MaxFairSchedulingSteps : configuration.MaxUnfairSchedulingSteps;
            this.StepCount = 0;
            this.IsFair = isFair;
            this.ErrorText = string.Empty;
        }

        /// <summary>
        /// Initializes the next iteration.
        /// </summary>
        /// <param name="iteration">The id of the next iteration.</param>
        /// <returns>True to start the specified iteration, else false to stop exploring.</returns>
        internal abstract bool InitializeNextIteration(uint iteration);

        /// <summary>
        /// Returns the count of explored steps.
        /// </summary>
        internal virtual int GetStepCount() => this.StepCount;

        /// <summary>
        /// True if the strategy has reached the max exploration steps for the current iteration.
        /// </summary>
        internal virtual bool IsMaxStepsReached() => this.MaxSteps is 0 ? false : this.StepCount >= this.MaxSteps;

        /// <summary>
        /// Returns a description of the strategy.
        /// </summary>
        internal abstract string GetDescription();
    }
}
