// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Logging;

namespace Microsoft.Coyote.Testing
{
    /// <summary>
    /// The type of exploration strategy.
    /// </summary>
    internal enum ExplorationStrategy
    {
        /// <summary>
        /// A simple (but effective) randomized exploration strategy.
        /// </summary>
        Random = 0,

        /// <summary>
        /// A randomized exploration strategy with increased probability
        /// to remain in the same scheduling choice.
        /// </summary>
        Probabilistic,

        /// <summary>
        /// An unfair probabilistic priority-based exploration strategy.
        /// </summary>
        /// <remarks>
        /// This strategy is based on the PCT algorithm described in the following paper:
        /// https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/asplos277-pct.pdf.
        /// </remarks>
        Prioritization,

        /// <summary>
        /// A fair probabilistic priority-based exploration strategy. It uses the
        /// <see cref="Random"/> strategy as a fair execution suffix.
        /// </summary>
        /// <remarks>
        /// This strategy is based on the PCT algorithm described in the following paper:
        /// https://www.microsoft.com/en-us/research/wp-content/uploads/2016/02/asplos277-pct.pdf.
        /// </remarks>
        FairPrioritization,

        /// <summary>
        /// An exploration strategy using delay-bounding.
        /// </summary>
        /// <remarks>
        /// This strategy is based on the algorithm described in the following paper:
        /// https://dl.acm.org/doi/10.1145/1925844.1926432.
        /// </remarks>
        DelayBounding,

        /// <summary>
        /// A fair exploration strategy using delay-bounding.
        /// </summary>
        /// <remarks>
        /// This strategy is based on the algorithm described in the following paper:
        /// https://dl.acm.org/doi/10.1145/1925844.1926432.
        /// </remarks>
        FairDelayBounding,

        /// <summary>
        /// A probabilistic exploration strategy that uses Q-learning.
        /// </summary>
        /// <remarks>
        /// This strategy is described in the following paper:
        /// https://dl.acm.org/doi/10.1145/3428298.
        /// </remarks>
        QLearning,

        /// <summary>
        /// An exploration strategy that uses depth-first search.
        /// </summary>
        DFS,
    }

    /// <summary>
    /// Extension methods for the <see cref="ExplorationStrategy"/>.
    /// </summary>
    internal static class ExplorationStrategyExtensions
    {
        /// <summary>
        /// Returns the name of the exploration strategy.
        /// </summary>
        internal static string GetName(this ExplorationStrategy strategy) => strategy switch
            {
                ExplorationStrategy.Random => "random",
                ExplorationStrategy.Probabilistic => "probabilistic",
                ExplorationStrategy.Prioritization => "prioritization",
                ExplorationStrategy.FairPrioritization => "fair-prioritization",
                ExplorationStrategy.DelayBounding => "delay-bounding",
                ExplorationStrategy.FairDelayBounding => "fair-delay-bounding",
                ExplorationStrategy.QLearning => "q-learning",
                ExplorationStrategy.DFS => "dfs",
                _ => throw new NotSupportedException($"The strategy '{strategy}' is not expected.")
            };

        /// <summary>
        /// Returns the <see cref="ExplorationStrategy"/> associated with the specified name.
        /// </summary>
        internal static ExplorationStrategy FromName(string name) => name switch
            {
                "random" => ExplorationStrategy.Random,
                "probabilistic" => ExplorationStrategy.Probabilistic,
                "prioritization" => ExplorationStrategy.Prioritization,
                "fair-prioritization" => ExplorationStrategy.FairPrioritization,
                "delay-bounding" => ExplorationStrategy.DelayBounding,
                "fair-delay-bounding" => ExplorationStrategy.FairDelayBounding,
                "q-learning" => ExplorationStrategy.QLearning,
                "dfs" => ExplorationStrategy.DFS,
                _ => throw new ArgumentOutOfRangeException($"The name '{name}' is not expected.")
            };
    }

    /// <summary>
    /// Abstract exploration strategy.
    /// </summary>
    internal abstract class Strategy
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
        /// Initializes a new instance of the <see cref="Strategy"/> class.
        /// </summary>
        protected Strategy(Configuration configuration, bool isFair)
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
        /// Returns the name of the strategy.
        /// </summary>
        internal abstract string GetName();

        /// <summary>
        /// Returns a description of the strategy.
        /// </summary>
        internal abstract string GetDescription();
    }
}
