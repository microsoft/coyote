// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

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
        /// A random value generator that can be used by the strategy.
        /// </summary>
        protected readonly IRandomValueGenerator RandomValueGenerator;

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
        internal bool IsFair { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExplorationStrategy"/> class.
        /// </summary>
        protected ExplorationStrategy(Configuration configuration, IRandomValueGenerator generator, bool isFair)
        {
            this.Configuration = configuration;
            this.RandomValueGenerator = generator;
            this.MaxSteps = isFair ? configuration.MaxFairSchedulingSteps : configuration.MaxUnfairSchedulingSteps;
            this.StepCount = 0;
            this.IsFair = isFair;
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
        internal abstract int GetStepCount();

        /// <summary>
        /// True if the strategy has reached the max exploration steps for the current iteration.
        /// </summary>
        internal abstract bool IsMaxStepsReached();

        /// <summary>
        /// Returns a description of the strategy.
        /// </summary>
        internal abstract string GetDescription();
    }
}
