// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Testing;
using Microsoft.Coyote.Testing.Fuzzing;
using Microsoft.Coyote.Testing.Interleaving;

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
        private readonly ExplorationStrategy Strategy;

        /// <summary>
        /// The installed schedule reducers, if any.
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
        /// True if the schedule is fair, else false.
        /// </summary>
        internal bool IsScheduleFair => this.Strategy.IsFair;

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

            this.Reducers = new List<IScheduleReducer>();
            if (configuration.IsSharedStateReductionEnabled)
            {
                this.Reducers.Add(new SharedStateReducer());
            }

            if (!configuration.UserExplicitlySetLivenessTemperatureThreshold &&
                configuration.MaxFairSchedulingSteps > 0)
            {
                configuration.LivenessTemperatureThreshold = configuration.MaxFairSchedulingSteps / 2;
            }

            if (this.SchedulingPolicy is SchedulingPolicy.Interleaving)
            {
                this.Strategy = InterleavingStrategy.Create(configuration, prefixTrace);
                this.IsReplaying = prefixTrace.Length > 0;
            }
            else if (this.SchedulingPolicy is SchedulingPolicy.Fuzzing)
            {
                this.Strategy = FuzzingStrategy.Create(configuration);
            }

            this.Strategy.RandomValueGenerator = generator;
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
            this.Trace.Clear();
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
        internal string GetDescription() => this.Strategy.GetDescription();

        /// <summary>
        /// Returns the last scheduling error, or the empty string if there is none.
        /// </summary>
        internal string GetLastError() => this.Strategy.ErrorText;
    }
}
