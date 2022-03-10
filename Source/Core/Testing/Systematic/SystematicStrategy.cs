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
        /// Initializes a new instance of the <see cref="SystematicStrategy"/> class.
        /// </summary>
        protected SystematicStrategy(Configuration configuration, IRandomValueGenerator generator, bool isFair)
            : base(configuration, generator, isFair)
        {
        }

        /// <summary>
        /// Creates a <see cref="SystematicStrategy"/> from the specified configuration.
        /// </summary>
        internal static SystematicStrategy Create(Configuration configuration, IRandomValueGenerator generator)
        {
            SystematicStrategy strategy = null;
            if (configuration.SchedulingStrategy is "replay")
            {
                var trace = ScheduleTrace.Deserialize(configuration, out bool isFair);
                strategy = new ReplayStrategy(configuration, generator, trace, isFair);
            }
            else if (configuration.SchedulingStrategy is "random")
            {
                strategy = new RandomStrategy(configuration, generator);
            }
            else if (configuration.SchedulingStrategy is "pct")
            {
                strategy = new PCTStrategy(configuration, generator);
            }
            else if (configuration.SchedulingStrategy is "fairpct")
            {
                var prefixStrategy = new PCTStrategy(configuration, generator);
                var suffixStrategy = new RandomStrategy(configuration, generator);
                strategy = new ComboStrategy(configuration, generator, prefixStrategy, suffixStrategy);
            }
            else if (configuration.SchedulingStrategy is "probabilistic")
            {
                strategy = new ProbabilisticRandomStrategy(configuration, generator);
            }
            else if (configuration.SchedulingStrategy is "rl")
            {
                strategy = new QLearningStrategy(configuration, generator);
            }
            else if (configuration.SchedulingStrategy is "dfs")
            {
                strategy = new DFSStrategy(configuration, generator);
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
