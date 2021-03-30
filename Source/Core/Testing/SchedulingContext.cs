// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Testing.Systematic;

namespace Microsoft.Coyote.Testing
{
    /// <summary>
    /// The context of the scheduler during controlled testing.
    /// </summary>
    internal sealed class SchedulingContext
    {
        /// <summary>
        /// The configuration used by the runtime.
        /// </summary>
        private readonly Configuration Configuration;

        /// <summary>
        /// The installed scheduling strategy.
        /// </summary>
        private SchedulingStrategy Strategy;

        /// <summary>
        /// The installed replay strategy, if any.
        /// </summary>
        private readonly ReplayStrategy ReplayStrategy;

        /// <summary>
        /// Responsible for generating random values.
        /// </summary>
        internal IRandomValueGenerator ValueGenerator { get; private set; }

        /// <summary>
        /// The count of steps scheduled in the current iteration.
        /// </summary>
        internal int StepCount => this.Strategy.GetScheduledSteps();

        /// <summary>
        /// True if the max number of steps that should be scheduled has been
        /// reached in the current iteration, else false.
        /// </summary>
        internal bool IsMaxStepsReached => this.Strategy.IsMaxStepsReached();

        /// <summary>
        /// True if the schedule is fair, else false.
        /// </summary>
        internal bool IsScheduleFair => this.Strategy.IsFair();

        /// <summary>
        /// Checks if the scheduler is replaying the schedule trace.
        /// </summary>
        internal bool IsReplayingSchedule { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SchedulingContext"/> class.
        /// </summary>
        private SchedulingContext(Configuration configuration, ILogger logger)
        {
            this.Configuration = configuration;
            this.ValueGenerator = new RandomValueGenerator(configuration);

            if (!configuration.UserExplicitlySetLivenessTemperatureThreshold &&
                configuration.MaxFairSchedulingSteps > 0)
            {
                configuration.LivenessTemperatureThreshold = configuration.MaxFairSchedulingSteps / 2;
            }

            SchedulingStrategy strategy = null;
            if (configuration.SchedulingStrategy is "replay")
            {
                this.ReplayStrategy = new ReplayStrategy(configuration);
                strategy = this.ReplayStrategy;
                this.IsReplayingSchedule = true;
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
                strategy = new RandomStrategy(configuration.MaxFairSchedulingSteps, this.ValueGenerator);
            }
            else if (configuration.SchedulingStrategy is "pct")
            {
                strategy = new PCTStrategy(configuration.MaxUnfairSchedulingSteps, configuration.StrategyBound,
                    this.ValueGenerator);
            }
            else if (configuration.SchedulingStrategy is "fairpct")
            {
                var prefixLength = configuration.SafetyPrefixBound is 0 ?
                    configuration.MaxUnfairSchedulingSteps : configuration.SafetyPrefixBound;
                var prefixStrategy = new PCTStrategy(prefixLength, configuration.StrategyBound, this.ValueGenerator);
                var suffixStrategy = new RandomStrategy(configuration.MaxFairSchedulingSteps, this.ValueGenerator);
                strategy = new ComboStrategy(prefixStrategy, suffixStrategy);
            }
            else if (configuration.SchedulingStrategy is "probabilistic")
            {
                strategy = new ProbabilisticRandomStrategy(configuration.MaxFairSchedulingSteps,
                    configuration.StrategyBound, this.ValueGenerator);
            }
            else if (configuration.SchedulingStrategy is "dfs")
            {
                strategy = new DFSStrategy(configuration.MaxUnfairSchedulingSteps);
            }
            else if (configuration.SchedulingStrategy is "rl")
            {
                this.Strategy = new QLearningStrategy(configuration.AbstractionLevel, configuration.MaxUnfairSchedulingSteps, this.ValueGenerator);
            }

            if (configuration.SchedulingStrategy != "replay" &&
                configuration.ScheduleFile.Length > 0)
            {
                this.ReplayStrategy = new ReplayStrategy(configuration, strategy);
                strategy = this.ReplayStrategy;
                this.IsReplayingSchedule = true;
            }

            this.Strategy = strategy;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="SchedulingContext"/> class.
        /// </summary>
        internal static SchedulingContext Setup(Configuration configuration, ILogger logger) =>
            new SchedulingContext(configuration, logger);

        /// <summary>
        /// Sets the specification engine.
        /// </summary>
        internal void SetSpecificationEngine(SpecificationEngine specificationEngine)
        {
            if (this.Configuration.IsLivenessCheckingEnabled)
            {
                this.Strategy = new TemperatureCheckingStrategy(this.Configuration, specificationEngine, this.Strategy);
            }
        }

        /// <summary>
        /// Initializes the next iteration.
        /// </summary>
        /// <param name="iteration">The id of the next iteration.</param>
        /// <returns>True to start the specified iteration, else false to stop exploring.</returns>
        internal bool InitializeNextIteration(uint iteration) => this.Strategy.InitializeNextIteration(iteration);

        /// <summary>
        /// Returns the next asynchronous operation to schedule.
        /// </summary>
        /// <param name="ops">Operations that can be scheduled.</param>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="isYielding">True if the current operation is yielding, else false.</param>
        /// <param name="next">The next operation to schedule.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal bool GetNextOperation(IEnumerable<AsyncOperation> ops, AsyncOperation current,
            bool isYielding, out AsyncOperation next) =>
            this.Strategy.GetNextOperation(ops, current, isYielding, out next);

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">The next boolean choice.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal bool GetNextBooleanChoice(AsyncOperation current, int maxValue, out bool next) =>
            this.Strategy.GetNextBooleanChoice(current, maxValue, out next);

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">The next integer choice.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal bool GetNextIntegerChoice(AsyncOperation current, int maxValue, out int next) =>
            this.Strategy.GetNextIntegerChoice(current, maxValue, out next);

        /// <summary>
        /// Returns a description of the scheduling strategy in text format.
        /// </summary>
        internal string GetDescription() => this.Strategy.GetDescription();

        /// <summary>
        /// Returns the replay error, if there is any.
        /// </summary>
        internal string GetReplayError() => this.ReplayStrategy?.ErrorText ?? string.Empty;
    }
}
