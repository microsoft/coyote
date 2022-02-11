// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Systematic
{
    /// <summary>
    /// Abstract scheduling strategy used during systematic testing.
    /// </summary>
    internal abstract class SystematicStrategy : ExplorationStrategy
    {
        /// <summary>
        /// Creates a <see cref="SystematicStrategy"/> from the specified configuration.
        /// </summary>
        internal static SystematicStrategy Create(Configuration configuration, IRandomValueGenerator generator)
        {
            SystematicStrategy strategy = null;
            if (configuration.SchedulingStrategy is "replay")
            {
                strategy = new ReplayStrategy(configuration);
            }
            else if (configuration.SchedulingStrategy is "random")
            {
                strategy = new NewRandomStrategy(configuration.MaxFairSchedulingSteps, generator);
            }
            else if (configuration.SchedulingStrategy is "pct")
            {
                strategy = new PCTStrategy(configuration.MaxUnfairSchedulingSteps, configuration.StrategyBound, generator);
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
            else if (configuration.SchedulingStrategy is "rl")
            {
                strategy = new QLearningStrategy(configuration.MaxUnfairSchedulingSteps, generator);
            }
            else if (configuration.SchedulingStrategy is "dfs")
            {
                strategy = new DFSStrategy(configuration.MaxUnfairSchedulingSteps);
            }

            return strategy;
        }

        /// <summary>
        /// Returns the next controlled operation to schedule.
        /// </summary>
        /// <param name="ops">Operations that can be scheduled.</param>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="isYielding">True if the current operation is yielding, else false.</param>
        /// <param name="next">The next operation to schedule.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal abstract bool GetNextOperation(IEnumerable<ControlledOperation> ops, ControlledOperation current,
            bool isYielding, out ControlledOperation next);

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">The next boolean choice.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal abstract bool GetNextBooleanChoice(ControlledOperation current, int maxValue, out bool next);

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">The next integer choice.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal abstract bool GetNextIntegerChoice(ControlledOperation current, int maxValue, out int next);

        /// <summary>
        /// Resets the strategy.
        /// </summary>
        /// <remarks>
        /// This is typically invoked by parent strategies to reset child strategies.
        /// </remarks>
        internal abstract void Reset();
    }
}
