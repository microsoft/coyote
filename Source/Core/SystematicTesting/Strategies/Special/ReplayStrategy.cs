// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.IO;

namespace Microsoft.Coyote.SystematicTesting.Strategies
{
    /// <summary>
    /// A replaying scheduling strategy.
    /// </summary>
    internal sealed class ReplayStrategy : SchedulingStrategy
    {
        /// <summary>
        /// The configuration.
        /// </summary>
        private readonly Configuration Configuration;

        /// <summary>
        /// The Coyote program schedule trace.
        /// </summary>
        private readonly ScheduleTrace ScheduleTrace;

        /// <summary>
        /// The suffix strategy.
        /// </summary>
        private readonly SchedulingStrategy SuffixStrategy;

        /// <summary>
        /// True if the scheduler that produced the schedule trace is fair, else false.
        /// </summary>
        private readonly bool IsSchedulerFair;

        /// <summary>
        /// True if the scheduler is replaying the trace, else false.
        /// </summary>
        private bool IsReplaying;

        /// <summary>
        /// The number of scheduled steps.
        /// </summary>
        private int ScheduledSteps;

        /// <summary>
        /// Text describing a replay error.
        /// </summary>
        internal string ErrorText { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayStrategy"/> class.
        /// </summary>
        internal ReplayStrategy(Configuration configuration, ScheduleTrace trace, bool isFair)
            : this(configuration, trace, isFair, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayStrategy"/> class.
        /// </summary>
        internal ReplayStrategy(Configuration configuration, ScheduleTrace trace, bool isFair, SchedulingStrategy suffixStrategy)
        {
            this.Configuration = configuration;
            this.ScheduleTrace = trace;
            this.ScheduledSteps = 0;
            this.IsSchedulerFair = isFair;
            this.IsReplaying = true;
            this.SuffixStrategy = suffixStrategy;
            this.ErrorText = string.Empty;
        }

        /// <inheritdoc/>
        internal override bool InitializeNextIteration(uint iteration)
        {
            this.ScheduledSteps = 0;

            if (iteration is 0)
            {
                return true;
            }
            else if (this.SuffixStrategy != null)
            {
                return this.SuffixStrategy.InitializeNextIteration(iteration);
            }

            return false;
        }

        /// <inheritdoc/>
        internal override bool GetNextOperation(IEnumerable<AsyncOperation> ops, AsyncOperation current,
            bool isYielding, out AsyncOperation next)
        {
            if (this.IsReplaying)
            {
                var enabledOps = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
                if (enabledOps.Count is 0)
                {
                    next = null;
                    return false;
                }

                try
                {
                    if (this.ScheduledSteps >= this.ScheduleTrace.Count)
                    {
                        this.ErrorText = "Trace is not reproducible: execution is longer than trace.";
                        throw new InvalidOperationException(this.ErrorText);
                    }

                    ScheduleStep nextStep = this.ScheduleTrace[this.ScheduledSteps];
                    if (nextStep.Type != ScheduleStepType.SchedulingChoice)
                    {
                        this.ErrorText = "Trace is not reproducible: next step is not a scheduling choice.";
                        throw new InvalidOperationException(this.ErrorText);
                    }

                    next = enabledOps.FirstOrDefault(op => op.Id == nextStep.ScheduledOperationId);
                    if (next is null)
                    {
                        this.ErrorText = $"Trace is not reproducible: cannot detect id '{nextStep.ScheduledOperationId}'.";
                        throw new InvalidOperationException(this.ErrorText);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    if (this.SuffixStrategy is null)
                    {
                        if (!this.Configuration.DisableEnvironmentExit)
                        {
                            Error.ReportAndExit(ex.Message);
                        }

                        next = null;
                        return false;
                    }
                    else
                    {
                        this.IsReplaying = false;
                        return this.SuffixStrategy.GetNextOperation(ops, current, isYielding, out next);
                    }
                }

                this.ScheduledSteps++;
                return true;
            }

            return this.SuffixStrategy.GetNextOperation(ops, current, isYielding, out next);
        }

        /// <inheritdoc/>
        internal override bool GetNextBooleanChoice(AsyncOperation current, int maxValue, out bool next)
        {
            if (this.IsReplaying)
            {
                ScheduleStep nextStep;

                try
                {
                    if (this.ScheduledSteps >= this.ScheduleTrace.Count)
                    {
                        this.ErrorText = "Trace is not reproducible: execution is longer than trace.";
                        throw new InvalidOperationException(this.ErrorText);
                    }

                    nextStep = this.ScheduleTrace[this.ScheduledSteps];
                    if (nextStep.Type != ScheduleStepType.NondeterministicChoice)
                    {
                        this.ErrorText = "Trace is not reproducible: next step is not a nondeterministic choice.";
                        throw new InvalidOperationException(this.ErrorText);
                    }

                    if (nextStep.BooleanChoice is null)
                    {
                        this.ErrorText = "Trace is not reproducible: next step is not a nondeterministic boolean choice.";
                        throw new InvalidOperationException(this.ErrorText);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    if (this.SuffixStrategy is null)
                    {
                        if (!this.Configuration.DisableEnvironmentExit)
                        {
                            Error.ReportAndExit(ex.Message);
                        }

                        next = false;
                        return false;
                    }
                    else
                    {
                        this.IsReplaying = false;
                        return this.SuffixStrategy.GetNextBooleanChoice(current, maxValue, out next);
                    }
                }

                next = nextStep.BooleanChoice.Value;
                this.ScheduledSteps++;
                return true;
            }

            return this.SuffixStrategy.GetNextBooleanChoice(current, maxValue, out next);
        }

        /// <inheritdoc/>
        internal override bool GetNextIntegerChoice(AsyncOperation current, int maxValue, out int next)
        {
            if (this.IsReplaying)
            {
                ScheduleStep nextStep;

                try
                {
                    if (this.ScheduledSteps >= this.ScheduleTrace.Count)
                    {
                        this.ErrorText = "Trace is not reproducible: execution is longer than trace.";
                        throw new InvalidOperationException(this.ErrorText);
                    }

                    nextStep = this.ScheduleTrace[this.ScheduledSteps];
                    if (nextStep.Type != ScheduleStepType.NondeterministicChoice)
                    {
                        this.ErrorText = "Trace is not reproducible: next step is not a nondeterministic choice.";
                        throw new InvalidOperationException(this.ErrorText);
                    }

                    if (nextStep.IntegerChoice is null)
                    {
                        this.ErrorText = "Trace is not reproducible: next step is not a nondeterministic integer choice.";
                        throw new InvalidOperationException(this.ErrorText);
                    }
                }
                catch (InvalidOperationException ex)
                {
                    if (this.SuffixStrategy is null)
                    {
                        if (!this.Configuration.DisableEnvironmentExit)
                        {
                            Error.ReportAndExit(ex.Message);
                        }

                        next = 0;
                        return false;
                    }
                    else
                    {
                        this.IsReplaying = false;
                        return this.SuffixStrategy.GetNextIntegerChoice(current, maxValue, out next);
                    }
                }

                next = nextStep.IntegerChoice.Value;
                this.ScheduledSteps++;
                return true;
            }

            return this.SuffixStrategy.GetNextIntegerChoice(current, maxValue, out next);
        }

        /// <inheritdoc/>
        internal override int GetScheduledSteps()
        {
            if (this.SuffixStrategy != null)
            {
                return this.ScheduledSteps + this.SuffixStrategy.GetScheduledSteps();
            }
            else
            {
                return this.ScheduledSteps;
            }
        }

        /// <inheritdoc/>
        internal override bool HasReachedMaxSchedulingSteps()
        {
            if (this.SuffixStrategy != null)
            {
                return this.SuffixStrategy.HasReachedMaxSchedulingSteps();
            }
            else
            {
                return false;
            }
        }

        /// <inheritdoc/>
        internal override bool IsFair()
        {
            if (this.SuffixStrategy != null)
            {
                return this.SuffixStrategy.IsFair();
            }
            else
            {
                return this.IsSchedulerFair;
            }
        }

        /// <inheritdoc/>
        internal override string GetDescription()
        {
            if (this.SuffixStrategy != null)
            {
                return "replay(" + this.SuffixStrategy.GetDescription() + ")";
            }
            else
            {
                return "replay";
            }
        }

        /// <inheritdoc/>
        internal override void Reset()
        {
            this.ScheduledSteps = 0;
            this.SuffixStrategy?.Reset();
        }
    }
}
