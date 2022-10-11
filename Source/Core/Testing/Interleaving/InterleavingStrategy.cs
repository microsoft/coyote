// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Interleaving
{
    /// <summary>
    /// Abstract scheduling strategy used to interleave the schedule of operations.
    /// </summary>
    internal abstract class InterleavingStrategy : ExplorationStrategy
    {
        /// <summary>
        /// The execution prefix trace to try reproduce.
        /// </summary>
        private ExecutionTrace TracePrefix;

        /// <summary>
        /// Initializes a new instance of the <see cref="InterleavingStrategy"/> class.
        /// </summary>
        protected InterleavingStrategy(Configuration configuration, IRandomValueGenerator generator, bool isFair)
            : base(configuration, generator, isFair)
        {
        }

        /// <summary>
        /// Creates a <see cref="InterleavingStrategy"/> from the specified configuration.
        /// </summary>
        internal static InterleavingStrategy Create(Configuration configuration, IRandomValueGenerator generator, ExecutionTrace tracePrefix)
        {
            InterleavingStrategy strategy = null;
            if (configuration.SchedulingStrategy is "random")
            {
                strategy = new RandomStrategy(configuration, generator);
            }
            else if (configuration.SchedulingStrategy is "prioritization")
            {
                strategy = new PrioritizationStrategy(configuration, generator, false);
            }
            else if (configuration.SchedulingStrategy is "fair-prioritization")
            {
                strategy = new PrioritizationStrategy(configuration, generator, true);
            }
            else if (configuration.SchedulingStrategy is "group-prioritization")
            {
                strategy = new GroupPrioritizationStrategy(configuration, generator, false);
            }
            else if (configuration.SchedulingStrategy is "fair-group-prioritization")
            {
                strategy = new GroupPrioritizationStrategy(configuration, generator, true);
            }
            else if (configuration.SchedulingStrategy is "probabilistic")
            {
                strategy = new ProbabilisticRandomStrategy(configuration, generator);
            }
            else if (configuration.SchedulingStrategy is "delay-bounding")
            {
                strategy = new DelayBoundingStrategy(configuration, generator, false);
            }
            else if (configuration.SchedulingStrategy is "fair-delay-bounding")
            {
                strategy = new DelayBoundingStrategy(configuration, generator, true);
            }
            else if (configuration.SchedulingStrategy is "rl")
            {
                strategy = new QLearningStrategy(configuration, generator);
            }
            else if (configuration.SchedulingStrategy is "dfs")
            {
                strategy = new DFSStrategy(configuration, generator);
            }

            strategy.TracePrefix = tracePrefix;
            return strategy;
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            this.StepCount = 0;
            return true;
        }

        /// <summary>
        /// Returns the next controlled operation to schedule.
        /// </summary>
        /// <param name="ops">Operations that can be scheduled.</param>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="isYielding">True if the current operation is yielding, else false.</param>
        /// <param name="next">The next operation to schedule.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal bool GetNextOperation(IEnumerable<ControlledOperation> ops, ControlledOperation current,
            bool isYielding, out ControlledOperation next)
        {
            try
            {
                bool result = true;
                if (this.StepCount < this.TracePrefix.Length)
                {
                    ExecutionTrace.Step nextStep = this.TracePrefix[this.StepCount];
                    if (nextStep.Type != ExecutionTrace.DecisionType.SchedulingChoice)
                    {
                        this.ErrorText = this.FormatError("next step is not a scheduling choice");
                        throw new InvalidOperationException(this.ErrorText);
                    }

                    next = ops.FirstOrDefault(op => op.Id == nextStep.ScheduledOperationId);
                    if (next is null)
                    {
                        this.ErrorText = this.FormatError($"cannot detect id '{nextStep.ScheduledOperationId}'");
                        throw new InvalidOperationException(this.ErrorText);
                    }
                }
                else
                {
                    result = this.NextOperation(ops, current, isYielding, out next);
                }

                this.StepCount++;
                return result;
            }
            catch (InvalidOperationException ex)
            {
                Error.Report(ex.Message);
                next = null;
                return false;
            }
        }

        /// <summary>
        /// Returns the next controlled operation to schedule.
        /// </summary>
        /// <param name="ops">Operations that can be scheduled.</param>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="isYielding">True if the current operation is yielding, else false.</param>
        /// <param name="next">The next operation to schedule.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal abstract bool NextOperation(IEnumerable<ControlledOperation> ops, ControlledOperation current,
            bool isYielding, out ControlledOperation next);

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="next">The next boolean choice.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal bool GetNextBoolean(ControlledOperation current, out bool next)
        {
            try
            {
                bool result = true;
                if (this.StepCount < this.TracePrefix.Length)
                {
                    ExecutionTrace.Step nextStep = this.TracePrefix[this.StepCount];
                    if (nextStep.Type != ExecutionTrace.DecisionType.NondeterministicChoice)
                    {
                        this.ErrorText = this.FormatError("next step is not a nondeterministic choice");
                        throw new InvalidOperationException(this.ErrorText);
                    }

                    if (nextStep.BooleanChoice is null)
                    {
                        this.ErrorText = this.FormatError("next step is not a nondeterministic boolean choice");
                        throw new InvalidOperationException(this.ErrorText);
                    }

                    next = nextStep.BooleanChoice.Value;
                }
                else
                {
                    result = this.NextBoolean(current, out next);
                }

                this.StepCount++;
                return result;
            }
            catch (InvalidOperationException ex)
            {
                Error.Report(ex.Message);
                next = false;
                return false;
            }
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="next">The next boolean choice.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal abstract bool NextBoolean(ControlledOperation current, out bool next);

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">The next integer choice.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal bool GetNextInteger(ControlledOperation current, int maxValue, out int next)
        {
            try
            {
                bool result = true;
                if (this.StepCount < this.TracePrefix.Length)
                {
                    ExecutionTrace.Step nextStep = this.TracePrefix[this.StepCount];
                    if (nextStep.Type != ExecutionTrace.DecisionType.NondeterministicChoice)
                    {
                        this.ErrorText = this.FormatError("next step is not a nondeterministic choice");
                        throw new InvalidOperationException(this.ErrorText);
                    }

                    if (nextStep.IntegerChoice is null)
                    {
                        this.ErrorText = this.FormatError("next step is not a nondeterministic integer choice");
                        throw new InvalidOperationException(this.ErrorText);
                    }

                    next = nextStep.IntegerChoice.Value;
                }
                else
                {
                    result = this.NextInteger(current, maxValue, out next);
                }

                this.StepCount++;
                return result;
            }
            catch (InvalidOperationException ex)
            {
                Error.Report(ex.Message);
                next = 0;
                return false;
            }
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="maxValue">The max value.</param>
        /// <param name="next">The next integer choice.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal abstract bool NextInteger(ControlledOperation current, int maxValue, out int next);

        /// <summary>
        /// Resets the strategy.
        /// </summary>
        /// <remarks>
        /// This is typically invoked by parent strategies to reset child strategies.
        /// </remarks>
        internal virtual void Reset()
        {
            this.StepCount = 0;
        }

        /// <summary>
        /// Formats the error message.
        /// </summary>
        private string FormatError(string reason) => this.Configuration.RandomGeneratorSeed.HasValue ?
            $"Trace from execution with random seed '{this.Configuration.RandomGeneratorSeed}' is not reproducible: {reason}." :
            $"Trace is not reproducible: {reason}.";
    }
}
