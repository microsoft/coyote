// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Coyote.IO;
using Microsoft.Coyote.TestingServices.Tracing.Schedule;

namespace Microsoft.Coyote.TestingServices.Scheduling.Strategies
{
    /// <summary>
    /// Class representing a replaying scheduling strategy.
    /// </summary>
    internal sealed class ReplayStrategy : ISchedulingStrategy
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
        private readonly ISchedulingStrategy SuffixStrategy;

        /// <summary>
        /// Is the scheduler that produced the
        /// schedule trace fair?
        /// </summary>
        private readonly bool IsSchedulerFair;

        /// <summary>
        /// Is the scheduler replaying the trace?
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
        public ReplayStrategy(Configuration configuration, ScheduleTrace trace, bool isFair)
            : this(configuration, trace, isFair, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayStrategy"/> class.
        /// </summary>
        public ReplayStrategy(Configuration configuration, ScheduleTrace trace, bool isFair, ISchedulingStrategy suffixStrategy)
        {
            this.Configuration = configuration;
            this.ScheduleTrace = trace;
            this.ScheduledSteps = 0;
            this.IsSchedulerFair = isFair;
            this.IsReplaying = true;
            this.SuffixStrategy = suffixStrategy;
            this.ErrorText = string.Empty;
        }

        /// <summary>
        /// Returns the next asynchronous operation to schedule.
        /// </summary>
        public bool GetNext(out IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            if (this.IsReplaying)
            {
                var enabledOperations = ops.Where(op => op.Status is AsyncOperationStatus.Enabled).ToList();
                if (enabledOperations.Count == 0)
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

                    next = enabledOperations.FirstOrDefault(op => op.Id == nextStep.ScheduledOperationId);
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
                        return this.SuffixStrategy.GetNext(out next, ops, current);
                    }
                }

                this.ScheduledSteps++;
                return true;
            }

            return this.SuffixStrategy.GetNext(out next, ops, current);
        }

        /// <summary>
        /// Returns the next boolean choice.
        /// </summary>
        public bool GetNextBooleanChoice(int maxValue, out bool next)
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
                        return this.SuffixStrategy.GetNextBooleanChoice(maxValue, out next);
                    }
                }

                next = nextStep.BooleanChoice.Value;
                this.ScheduledSteps++;
                return true;
            }

            return this.SuffixStrategy.GetNextBooleanChoice(maxValue, out next);
        }

        /// <summary>
        /// Returns the next integer choice.
        /// </summary>
        public bool GetNextIntegerChoice(int maxValue, out int next)
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
                        return this.SuffixStrategy.GetNextIntegerChoice(maxValue, out next);
                    }
                }

                next = nextStep.IntegerChoice.Value;
                this.ScheduledSteps++;
                return true;
            }

            return this.SuffixStrategy.GetNextIntegerChoice(maxValue, out next);
        }

        /// <summary>
        /// Forces the next asynchronous operation to be scheduled.
        /// </summary>
        public void ForceNext(IAsyncOperation next, List<IAsyncOperation> ops, IAsyncOperation current)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Forces the next boolean choice.
        /// </summary>
        public void ForceNextBooleanChoice(int maxValue, bool next)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Forces the next integer choice.
        /// </summary>
        public void ForceNextIntegerChoice(int maxValue, int next)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Prepares for the next scheduling iteration. This is invoked
        /// at the end of a scheduling iteration. It must return false
        /// if the scheduling strategy should stop exploring.
        /// </summary>
        public bool PrepareForNextIteration()
        {
            this.ScheduledSteps = 0;
            if (this.SuffixStrategy != null)
            {
                return this.SuffixStrategy.PrepareForNextIteration();
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Resets the scheduling strategy. This is typically invoked by
        /// parent strategies to reset child strategies.
        /// </summary>
        public void Reset()
        {
            this.ScheduledSteps = 0;
            this.SuffixStrategy?.Reset();
        }

        /// <summary>
        /// Returns the scheduled steps.
        /// </summary>
        public int GetScheduledSteps()
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

        /// <summary>
        /// True if the scheduling strategy has reached the depth
        /// bound for the given scheduling iteration.
        /// </summary>
        public bool HasReachedMaxSchedulingSteps()
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

        /// <summary>
        /// Checks if this is a fair scheduling strategy.
        /// </summary>
        public bool IsFair()
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

        /// <summary>
        /// Returns a textual description of the scheduling strategy.
        /// </summary>
        public string GetDescription()
        {
            if (this.SuffixStrategy != null)
            {
                return "Replay(" + this.SuffixStrategy.GetDescription() + ")";
            }
            else
            {
                return "Replay";
            }
        }
    }
}
