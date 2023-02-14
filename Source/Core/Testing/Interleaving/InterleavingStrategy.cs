// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Testing.Interleaving
{
    /// <summary>
    /// Abstract exploration strategy used during controlled testing.
    /// </summary>
    internal abstract class InterleavingStrategy : Strategy
    {
        /// <summary>
        /// The execution prefix trace to try reproduce.
        /// </summary>
        internal ExecutionTrace TracePrefix;

        /// <summary>
        /// Initializes a new instance of the <see cref="InterleavingStrategy"/> class.
        /// </summary>
        protected InterleavingStrategy(Configuration configuration, bool isFair)
            : base(configuration, isFair)
        {
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
        /// <param name="state">Hash representing the current state of the program.</param>
        /// <param name="isYielding">True if the current operation is yielding, else false.</param>
        /// <param name="next">The next operation to schedule.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal bool GetNextOperation(IEnumerable<ControlledOperation> ops, ControlledOperation current, ulong state,
            bool isYielding, out ControlledOperation next)
        {
            try
            {
                bool result = true;
                if (this.StepCount < this.TracePrefix.Length)
                {
                    ExecutionTrace.Step nextStep = this.TracePrefix[this.StepCount];
                    if (nextStep is ExecutionTrace.SchedulingStep step)
                    {
                        next = ops.FirstOrDefault(op => op.Id == step.Value);
                        if (next is null)
                        {
                            this.ErrorText = this.FormatReplayError(nextStep.Index, $"cannot detect id '{step.Value}'");
                            throw new InvalidOperationException(this.ErrorText);
                        }
                        else if (step.SchedulingPoint != current.LastSchedulingPoint)
                        {
                            this.ErrorText = this.FormatReplayError(nextStep.Index,
                                $"expected scheduling point '{step.SchedulingPoint}' instead of '{current.LastSchedulingPoint}'");
                            throw new InvalidOperationException(this.ErrorText);
                        }
                    }
                    else
                    {
                        this.ErrorText = this.FormatReplayError(nextStep.Index, "next step is not a scheduling choice");
                        throw new InvalidOperationException(this.ErrorText);
                    }
                }
                else
                {
                    result = this.NextOperation(ops, current, state, isYielding, out next);
                }

                this.StepCount++;
                return result;
            }
            catch (InvalidOperationException ex)
            {
                this.LogWriter.LogError(ex.Message);
                next = null;
                return false;
            }
        }

        /// <summary>
        /// Returns the next controlled operation to schedule.
        /// </summary>
        /// <param name="ops">Operations that can be scheduled.</param>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="state">Hash representing the current state of the program.</param>
        /// <param name="isYielding">True if the current operation is yielding, else false.</param>
        /// <param name="next">The next operation to schedule.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal abstract bool NextOperation(IEnumerable<ControlledOperation> ops, ControlledOperation current, ulong state,
            bool isYielding, out ControlledOperation next);

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="state">Hash representing the current state of the program.</param>
        /// <param name="next">The next boolean choice.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal bool GetNextBoolean(ControlledOperation current, ulong state, out bool next)
        {
            try
            {
                bool result = true;
                if (this.StepCount < this.TracePrefix.Length)
                {
                    ExecutionTrace.Step nextStep = this.TracePrefix[this.StepCount];
                    if (nextStep is ExecutionTrace.BooleanChoiceStep step)
                    {
                        next = step.Value;
                    }
                    else
                    {
                        this.ErrorText = this.FormatReplayError(nextStep.Index, "next step is not a nondeterministic choice");
                        throw new InvalidOperationException(this.ErrorText);
                    }
                }
                else
                {
                    result = this.NextBoolean(current, state, out next);
                }

                this.StepCount++;
                return result;
            }
            catch (InvalidOperationException ex)
            {
                this.LogWriter.LogError(ex.Message);
                next = false;
                return false;
            }
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="state">Hash representing the current state of the program.</param>
        /// <param name="next">The next boolean choice.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal abstract bool NextBoolean(ControlledOperation current, ulong state, out bool next);

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="state">Hash representing the current state of the program.</param>
        /// <param name="next">The next integer choice.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal bool GetNextInteger(int maxValue, ControlledOperation current, ulong state, out int next)
        {
            try
            {
                bool result = true;
                if (this.StepCount < this.TracePrefix.Length)
                {
                    ExecutionTrace.Step nextStep = this.TracePrefix[this.StepCount];
                    if (nextStep is ExecutionTrace.IntegerChoiceStep step)
                    {
                        next = step.Value;
                    }
                    else
                    {
                        this.ErrorText = this.FormatReplayError(nextStep.Index, "next step is not a nondeterministic choice");
                        throw new InvalidOperationException(this.ErrorText);
                    }
                }
                else
                {
                    result = this.NextInteger(maxValue, current, state, out next);
                }

                this.StepCount++;
                return result;
            }
            catch (InvalidOperationException ex)
            {
                this.LogWriter.LogError(ex.Message);
                next = 0;
                return false;
            }
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        /// <param name="maxValue">The max value.</param>
        /// <param name="current">The currently scheduled operation.</param>
        /// <param name="state">Hash representing the current state of the program.</param>
        /// <param name="next">The next integer choice.</param>
        /// <returns>True if there is a next choice, else false.</returns>
        internal abstract bool NextInteger(int maxValue, ControlledOperation current, ulong state, out int next);

        /// <summary>
        /// Resets the strategy.
        /// </summary>
        /// <remarks>
        /// This is typically invoked by parent strategies to reset child strategies.
        /// </remarks>
        internal virtual void Reset() => this.StepCount = 0;

        /// <summary>
        /// Formats the error message.
        /// </summary>
        private string FormatReplayError(int step, string reason)
        {
#if NET || NETCOREAPP3_1
            string[] traceTokens = new StackTrace().ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
#else
            string[] traceTokens = new StackTrace().ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
#endif
            string trace = string.Join(Environment.NewLine, traceTokens.Where(line => !line.Contains("at Microsoft.Coyote")));
            string info = this.Configuration.RandomGeneratorSeed.HasValue ?
                $" from execution with random seed '{this.Configuration.RandomGeneratorSeed}'" : string.Empty;
            return $"The trace{info} is not reproducible at execution step '{step}': {reason}." + Environment.NewLine + trace;
        }
    }
}
