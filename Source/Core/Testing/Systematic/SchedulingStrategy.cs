// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Systematic
{
    /// <summary>
    /// Abstract scheduling strategy used during controlled testing.
    /// </summary>
    internal abstract class SchedulingStrategy
    {
        internal static SchedulingStrategy Setup(Configuration configuration, IRandomValueGenerator generator, ILogger logger)
        {
            SchedulingStrategy strategy = null;

            if (!configuration.UserExplicitlySetLivenessTemperatureThreshold &&
                configuration.MaxFairSchedulingSteps > 0)
            {
                configuration.LivenessTemperatureThreshold = configuration.MaxFairSchedulingSteps / 2;
            }

            if (configuration.SchedulingStrategy is "replay")
            {
                strategy = new ReplayStrategy(configuration);
            }
            else if (configuration.SchedulingStrategy is "interactive")
            {
                configuration.TestingIterations = 1;
                configuration.PerformFullExploration = false;
                configuration.IsVerbose = true;
                strategy = new InteractiveStrategy(configuration, logger);
            }
            else if (configuration.SchedulingStrategy is "random")
            {
                strategy = new RandomStrategy(configuration.MaxFairSchedulingSteps, generator);
            }
            else if (configuration.SchedulingStrategy is "pct")
            {
                strategy = new PCTStrategy(configuration.MaxUnfairSchedulingSteps, configuration.StrategyBound,
                    generator);
            }
            else if (configuration.SchedulingStrategy is "fairpct")
            {
                var prefixLength = configuration.SafetyPrefixBound is 0 ?
                    configuration.MaxUnfairSchedulingSteps : configuration.SafetyPrefixBound;
                var prefixStrategy = new PCTStrategy(prefixLength, configuration.StrategyBound, generator);
                var suffixStrategy = new RandomStrategy(configuration.MaxFairSchedulingSteps, generator);
                strategy = new ComboStrategy(prefixStrategy, suffixStrategy);
            }
            else if (configuration.SchedulingStrategy is "probabilistic")
            {
                strategy = new ProbabilisticRandomStrategy(configuration.MaxFairSchedulingSteps,
                    configuration.StrategyBound, generator);
            }
            else if (configuration.SchedulingStrategy is "dfs")
            {
                strategy = new DFSStrategy(configuration.MaxUnfairSchedulingSteps);
            }

            if (configuration.SchedulingStrategy != "replay" &&
                configuration.ScheduleFile.Length > 0)
            {
                strategy = new ReplayStrategy(configuration, strategy);
            }

            return strategy;
        }

        /// <summary>
        /// Initializes the next iteration.
        /// </summary>
        /// <param name="iteration">The id of the next iteration.</param>
        /// <returns>True to start the specified iteration, else false to stop exploring.</returns>
        internal abstract bool InitializeNextIteration(uint iteration);

        /// <summary>
        /// Returns the next asynchronous operation to schedule.
        /// </summary>
        /// <param name="ops">Operations that can be scheduled.</param>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="isYielding">True if the current operation is yielding, else false.</param>
        /// <param name="next">The next operation to schedule.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal abstract bool GetNextOperation(IEnumerable<AsyncOperation> ops, AsyncOperation current,
            bool isYielding, out AsyncOperation next);

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">The next boolean choice.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal abstract bool GetNextBooleanChoice(AsyncOperation current, int maxValue, out bool next);

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">The next integer choice.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal abstract bool GetNextIntegerChoice(AsyncOperation current, int maxValue, out int next);

        /// <summary>
        /// Returns the scheduled steps.
        /// </summary>
        internal abstract int GetScheduledSteps();

        /// <summary>
        /// True if the scheduling strategy has reached the max
        /// scheduling steps for the given scheduling iteration.
        /// </summary>
        internal abstract bool HasReachedMaxSchedulingSteps();

        /// <summary>
        /// Checks if this is a fair scheduling strategy.
        /// </summary>
        internal abstract bool IsFair();

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        internal abstract string GetDescription();

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        internal abstract void Reset();
    }
}
