// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Testing;
using Microsoft.Coyote.Testing.Fuzzing;
using Microsoft.Coyote.Testing.Interleaving;
using BoundedRandomFuzzingStrategy = Microsoft.Coyote.Testing.Fuzzing.BoundedRandomStrategy;
using RandomInterleavingStrategy = Microsoft.Coyote.Testing.Interleaving.RandomStrategy;
using PrioritizationFuzzingStrategy = Microsoft.Coyote.Testing.Fuzzing.PrioritizationStrategy;
using PrioritizationInterleavingStrategy = Microsoft.Coyote.Testing.Interleaving.PrioritizationStrategy;
using ProbabilisticRandomInterleavingStrategy = Microsoft.Coyote.Testing.Interleaving.ProbabilisticRandomStrategy;
using QLearningInterleavingStrategy = Microsoft.Coyote.Testing.Interleaving.QLearningStrategy;
using DFSInterleavingStrategy = Microsoft.Coyote.Testing.Interleaving.DFSStrategy;

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
        /// The portfolio of exploration strategies.
        /// </summary>
        private readonly LinkedList<ExplorationStrategy> Portfolio;

        /// <summary>
        /// The exploration strategy used in the current iteration.
        /// </summary>
        private ExplorationStrategy Strategy => this.Portfolio.First.Value;

        /// <summary>
        /// The pipeline of schedule reducers.
        /// </summary>
        private readonly List<IScheduleReducer> Reducers;

        /// <summary>
        /// Responsible for generating random values.
        /// </summary>
        internal IRandomValueGenerator ValueGenerator { get; private set; }

        /// <summary>
        /// The installed operation scheduling policy.
        /// </summary>
        internal SchedulingPolicy SchedulingPolicy { get; private set; }

        /// <summary>
        /// The trace explored in the current iteration.
        /// </summary>
        internal readonly ExecutionTrace Trace;

        /// <summary>
        /// The prefix trace, if there is any specified. The scheduler will attempt
        /// to reproduce this trace, before performing any new exploration.
        /// </summary>
        private ExecutionTrace PrefixTrace;

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
        /// True if the current iteration is fair, else false.
        /// </summary>
        internal bool IsIterationFair => this.Strategy.IsFair;

        /// <summary>
        /// Checks if the scheduler is replaying the schedule trace.
        /// </summary>
        internal bool IsReplaying { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="OperationScheduler"/> class.
        /// </summary>
        private OperationScheduler(Configuration configuration, SchedulingPolicy policy, IRandomValueGenerator generator, ExecutionTrace prefixTrace)
        {
            this.Configuration = configuration;
            this.SchedulingPolicy = policy;
            this.PrefixTrace = prefixTrace;
            this.ValueGenerator = generator;
            this.Trace = ExecutionTrace.Create();

            this.Portfolio = new LinkedList<ExplorationStrategy>();
            this.Reducers = new List<IScheduleReducer>();
            if (configuration.IsSharedStateReductionEnabled)
            {
                this.Reducers.Add(new SharedStateReducer());
            }

            this.IsReplaying = this.SchedulingPolicy is SchedulingPolicy.Interleaving && prefixTrace.Length > 0;
            if (!configuration.UserExplicitlySetLivenessTemperatureThreshold &&
                configuration.MaxFairSchedulingSteps > 0)
            {
                configuration.LivenessTemperatureThreshold = configuration.MaxFairSchedulingSteps / 2;
            }

            // Portfolio mode only works with interleaving exploration strategies and no replay.
            if (this.Configuration.PortfolioMode.IsEnabled() && !this.IsReplaying &&
                this.SchedulingPolicy is SchedulingPolicy.Interleaving )
            {
                bool isFair = this.Configuration.PortfolioMode.IsFair();
                this.Portfolio.AddLast(new RandomInterleavingStrategy(configuration));
                this.Portfolio.AddLast(new PrioritizationInterleavingStrategy(configuration, 5, isFair));
                this.Portfolio.AddLast(new PrioritizationInterleavingStrategy(configuration, 10, isFair));
                this.Portfolio.AddLast(new ProbabilisticRandomInterleavingStrategy(configuration, 2));
                this.Portfolio.AddLast(new ProbabilisticRandomInterleavingStrategy(configuration, 3));
            }
            else
            {
                if (this.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    switch (configuration.SchedulingStrategy)
                    {
                        case "prioritization":
                            this.Portfolio.AddLast(new PrioritizationInterleavingStrategy(configuration, configuration.StrategyBound, false));
                            break;
                        case "fair-prioritization":
                            this.Portfolio.AddLast(new PrioritizationInterleavingStrategy(configuration, configuration.StrategyBound, true));
                            break;
                        case "probabilistic":
                            this.Portfolio.AddLast(new ProbabilisticRandomInterleavingStrategy(configuration, configuration.StrategyBound));
                            break;
                        case "rl":
                            this.Portfolio.AddLast(new QLearningInterleavingStrategy(configuration));
                            break;
                        case "dfs":
                            this.Portfolio.AddLast(new DFSInterleavingStrategy(configuration));
                            break;
                        case "random":
                        default:
                            this.Portfolio.AddLast(new RandomInterleavingStrategy(configuration));
                            break;
                    }
                }
                else if (this.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                {
                    switch (configuration.SchedulingStrategy)
                    {
                        case "prioritization":
                            this.Portfolio.AddLast(new PrioritizationFuzzingStrategy(configuration));
                            break;
                        default:
                            this.Portfolio.AddLast(new BoundedRandomFuzzingStrategy(configuration));
                            break;
                    }
                }
            }

            // Setup all instantiated exploration strategies with additional features.
            foreach (var strategy in this.Portfolio)
            {
                strategy.RandomValueGenerator = generator;
                (strategy as InterleavingStrategy).TracePrefix = prefixTrace;
            }
        }

        /// <summary>
        /// Creates a new instance of the <see cref="OperationScheduler"/> class.
        /// </summary>
        internal static OperationScheduler Setup(Configuration configuration, ExecutionTrace prefixTrace) =>
            new OperationScheduler(configuration,
                configuration.IsSystematicFuzzingEnabled ? SchedulingPolicy.Fuzzing : SchedulingPolicy.Interleaving,
                new RandomValueGenerator(configuration), prefixTrace);

        /// <summary>
        /// Creates a new instance of the <see cref="OperationScheduler"/> class.
        /// </summary>
        internal static OperationScheduler Setup(Configuration configuration, SchedulingPolicy policy,
            IRandomValueGenerator valueGenerator) =>
            new OperationScheduler(configuration, policy, valueGenerator, ExecutionTrace.Create());

        /// <summary>
        /// Initializes the next test iteration.
        /// </summary>
        /// <param name="iteration">The id of the next iteration.</param>
        /// <param name="logWriter">The log writer associated with the current test iteration.</param>
        /// <returns>True to start the specified test iteration, else false to stop exploring.</returns>
        internal bool InitializeNextIteration(uint iteration, LogWriter logWriter)
        {
            if (iteration > 0)
            {
                // Rotate the portfolio strategies using round-robin.
                var strategy = this.Portfolio.First.Value;
                this.Portfolio.RemoveFirst();
                this.Portfolio.AddLast(strategy);

                this.Trace.Clear();
            }

            this.Strategy.LogWriter = logWriter;
            return this.Strategy.InitializeNextIteration(iteration);
        }

        /// <summary>
        /// Returns the next controlled operation to schedule.
        /// </summary>
        /// <param name="ops">The set of available operations.</param>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="isYielding">True if the current operation is yielding, else false.</param>
        /// <param name="next">The next operation to schedule.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal bool GetNextOperation(IEnumerable<ControlledOperation> ops, ControlledOperation current,
            bool isYielding, out ControlledOperation next)
        {
            // Filter out any operations that cannot be scheduled.
            var enabledOps = ops.Where(op => op.Status is OperationStatus.Enabled);
            if (enabledOps.Any())
            {
                // Invoke any installed schedule reducers.
                foreach (var reducer in this.Reducers)
                {
                    var reducedOps = reducer.ReduceOperations(enabledOps, current);
                    if (reducedOps.Any())
                    {
                        enabledOps = reducedOps;
                    }
                }

                // Invoke the strategy to choose the next operation.
                if (this.Strategy is InterleavingStrategy strategy &&
                    strategy.GetNextOperation(enabledOps, current, isYielding, out next))
                {
                    this.Trace.AddSchedulingChoice(next.Id);
                    return true;
                }
            }

            next = null;
            return false;
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="next">The next boolean choice.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal bool GetNextBoolean(ControlledOperation current, out bool next)
        {
            if (this.Strategy is InterleavingStrategy strategy &&
                strategy.GetNextBoolean(current, out next))
            {
                this.Trace.AddNondeterministicBooleanChoice(next);
                return true;
            }

            next = false;
            return false;
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">The next integer choice.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal bool GetNextInteger(ControlledOperation current, int maxValue, out int next)
        {
            if (this.Strategy is InterleavingStrategy strategy &&
                strategy.GetNextInteger(current, maxValue, out next))
            {
                this.Trace.AddNondeterministicIntegerChoice(next);
                return true;
            }

            next = 0;
            return false;
        }

        /// <summary>
        /// Returns the next delay.
        /// </summary>
        /// <param name="ops">Operations executing during the current test iteration.</param>
        /// <param name="current">The operation requesting the delay.</param>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">The next delay.</param>
        /// <returns>True if there is a next delay, else false.</returns>
        internal bool GetNextDelay(IEnumerable<ControlledOperation> ops, ControlledOperation current,
            int maxValue, out int next) =>
            (this.Strategy as FuzzingStrategy).GetNextDelay(ops, current, maxValue, out next);

        /// <summary>
        /// Sets a checkpoint in the currently explored execution trace, that allows replaying all
        /// scheduling decisions until the checkpoint in subsequent iterations.
        /// </summary>
        internal ExecutionTrace CheckpointExecutionTrace() => this.PrefixTrace.ExtendOrReplace(this.Trace);

        /// <summary>
        /// Returns a description of the scheduling strategy in text format.
        /// </summary>
        internal string GetDescription() => this.Portfolio.Count > 1 ?
            $"portfolio[fair:{this.Configuration.PortfolioMode.IsFair()},seed:{this.Strategy.RandomValueGenerator.Seed}]" :
            this.Strategy.GetDescription();

        /// <summary>
        /// Returns the last scheduling error, or the empty string if there is none.
        /// </summary>
        internal string GetLastError() => this.Strategy.ErrorText;
    }
}
