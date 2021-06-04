// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Testing.Fuzzing
{
    /// <summary>
    /// Abstract fuzzing strategy used during testing.
    /// </summary>
    internal abstract class FuzzingStrategy : ExplorationStrategy
    {
        /// <summary>
        /// Creates a <see cref="FuzzingStrategy"/> from the specified configuration.
        /// </summary>
        internal static FuzzingStrategy Create(Configuration configuration, IRandomValueGenerator generator)
        {
            switch (configuration.SchedulingStrategy)
            {
                case "random":
                    return new RandomStrategy(configuration.MaxUnfairSchedulingSteps, generator);
                case "pct":
                    return new PCTStrategy(configuration.MaxUnfairSchedulingSteps, generator, configuration.StrategyBound);
                default:
                    return new PortfolioStrategy(configuration.MaxUnfairSchedulingSteps, generator, configuration.StrategyBound);
            }
        }

        /// <summary>
        /// Returns the next delay.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">The next delay.</param>
        /// <returns>True if there is a next delay, else false.</returns>
        internal abstract bool GetNextDelay(int maxValue, out int next);
    }
}
