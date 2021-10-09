// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Testing;
using Microsoft.Coyote.Testing.Fuzzing;
using Microsoft.Coyote.Testing.Systematic;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Scheduler that controls the execution of operations during testing.
    /// </summary>
    internal sealed class OperationScheduler
    {
        /// <summary>
        /// The configuration used by the runtime.
        /// </summary>
        private readonly Configuration Configuration;

        /// <summary>
        /// The installed program exploration strategy.
        /// </summary>
        private ExplorationStrategy Strategy;

        /// <summary>
        /// The installed replay strategy, if any.
        /// </summary>
        private readonly ReplayStrategy ReplayStrategy;

        /// <summary>
        /// Responsible for generating random values.
        /// </summary>
        internal IRandomValueGenerator ValueGenerator { get; private set; }

        /// <summary>
        /// The installed operation scheduling policy.
        /// </summary>
        internal SchedulingPolicy SchedulingPolicy { get; private set; }

        /// <summary>
        /// The count of exploration steps in the current iteration.
        /// </summary>
        internal int StepCount => this.Strategy.GetStepCount();

        /// <summary>
        /// True if the max number of steps that should be explored has been
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
        /// Initializes a new instance of the <see cref="OperationScheduler"/> class.
        /// </summary>
        private OperationScheduler(Configuration configuration)
        {
            this.Configuration = configuration;
            this.SchedulingPolicy = configuration.IsConcurrencyFuzzingEnabled ? SchedulingPolicy.Fuzzing :
                SchedulingPolicy.Systematic;

            this.ValueGenerator = new RandomValueGenerator(configuration);

            if (!configuration.UserExplicitlySetLivenessTemperatureThreshold &&
                configuration.MaxFairSchedulingSteps > 0)
            {
                configuration.LivenessTemperatureThreshold = configuration.MaxFairSchedulingSteps / 2;
            }

            if (this.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                this.Strategy = SystematicStrategy.Create(configuration, this.ValueGenerator);
                if (this.Strategy is ReplayStrategy replayStrategy)
                {
                    this.ReplayStrategy = replayStrategy;
                    this.IsReplayingSchedule = true;
                }

                // Wrap the strategy inside a liveness checking strategy.
                if (this.Configuration.IsLivenessCheckingEnabled)
                {
                    this.Strategy = new TemperatureCheckingStrategy(this.Configuration, this.Strategy as SystematicStrategy);
                }
            }
            else if (this.SchedulingPolicy is SchedulingPolicy.Fuzzing)
            {
                this.Strategy = FuzzingStrategy.Create(configuration, this.ValueGenerator);
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="OperationScheduler"/> class.
        /// </summary>
        internal static OperationScheduler Setup(Configuration configuration) => new OperationScheduler(configuration);

        /// <summary>
        /// Sets the specification engine.
        /// </summary>
        internal void SetSpecificationEngine(SpecificationEngine specificationEngine)
        {
            if (this.Strategy is TemperatureCheckingStrategy temperatureCheckingStrategy)
            {
                temperatureCheckingStrategy.SetSpecificationEngine(specificationEngine);
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
            (this.Strategy as SystematicStrategy).GetNextOperation(ops, current, isYielding, out next);

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">The next boolean choice.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal bool GetNextBooleanChoice(AsyncOperation current, int maxValue, out bool next) =>
            (this.Strategy as SystematicStrategy).GetNextBooleanChoice(current, maxValue, out next);

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">The next integer choice.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal bool GetNextIntegerChoice(AsyncOperation current, int maxValue, out int next) =>
            (this.Strategy as SystematicStrategy).GetNextIntegerChoice(current, maxValue, out next);

        /// <summary>
        /// Returns the next delay.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">The next delay.</param>
        /// <returns>True if there is a next delay, else false.</returns>
        internal bool GetNextDelay(int maxValue, out int next) =>
            (this.Strategy as FuzzingStrategy).GetNextDelay(maxValue, out next);

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
