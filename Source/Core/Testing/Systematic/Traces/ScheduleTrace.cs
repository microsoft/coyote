// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.Coyote.Testing.Systematic
{
    /// <summary>
    /// Class implementing a program schedule trace. A trace is a series
    /// of transitions from some initial state to some end state.
    /// </summary>
    internal sealed class ScheduleTrace : IEnumerable, IEnumerable<ScheduleStep>
    {
        /// <summary>
        /// The steps of the schedule trace.
        /// </summary>
        private readonly List<ScheduleStep> Steps;

        /// <summary>
        /// The number of steps in the schedule trace.
        /// </summary>
        internal int Count
        {
            get { return this.Steps.Count; }
        }

        /// <summary>
        /// Index for the schedule trace.
        /// </summary>
        internal ScheduleStep this[int index]
        {
            get { return this.Steps[index]; }
            set { this.Steps[index] = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleTrace"/> class.
        /// </summary>
        internal ScheduleTrace()
        {
            this.Steps = new List<ScheduleStep>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleTrace"/> class.
        /// </summary>
        internal ScheduleTrace(string[] traceDump)
        {
            this.Steps = new List<ScheduleStep>();

            foreach (var step in traceDump)
            {
                if (step.StartsWith("--") || step.Length is 0)
                {
                    continue;
                }
                else if (step.Equals("True"))
                {
                    this.AddNondeterministicBooleanChoice(true);
                }
                else if (step.Equals("False"))
                {
                    this.AddNondeterministicBooleanChoice(false);
                }
                else if (int.TryParse(step, out int intChoice))
                {
                    this.AddNondeterministicIntegerChoice(intChoice);
                }
                else
                {
                    string id = step.TrimStart('(').TrimEnd(')');
                    this.AddSchedulingChoice(ulong.Parse(id));
                }
            }
        }

        /// <summary>
        /// Adds a scheduling choice.
        /// </summary>
        internal void AddSchedulingChoice(ulong scheduledActorId)
        {
            var scheduleStep = ScheduleStep.CreateSchedulingChoice(this.Count, scheduledActorId);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a nondeterministic boolean choice.
        /// </summary>
        internal void AddNondeterministicBooleanChoice(bool choice)
        {
            var scheduleStep = ScheduleStep.CreateNondeterministicBooleanChoice(
                this.Count, choice);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a nondeterministic integer choice.
        /// </summary>
        internal void AddNondeterministicIntegerChoice(int choice)
        {
            var scheduleStep = ScheduleStep.CreateNondeterministicIntegerChoice(
                this.Count, choice);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Returns the latest schedule step and removes
        /// it from the trace.
        /// </summary>
        internal ScheduleStep Pop()
        {
            if (this.Count > 0)
            {
                this.Steps[this.Count - 1].Next = null;
            }

            var step = this.Steps[this.Count - 1];
            this.Steps.RemoveAt(this.Count - 1);

            return step;
        }

        /// <summary>
        /// Returns the latest schedule step without removing it.
        /// </summary>
        internal ScheduleStep Peek()
        {
            ScheduleStep step = null;

            if (this.Steps.Count > 0)
            {
                step = this.Steps[this.Count - 1];
            }

            return step;
        }

        /// <summary>
        /// Returns an enumerator.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.Steps.GetEnumerator();
        }

        /// <summary>
        /// Returns an enumerator.
        /// </summary>
        IEnumerator<ScheduleStep> IEnumerable<ScheduleStep>.GetEnumerator()
        {
            return this.Steps.GetEnumerator();
        }

        /// <summary>
        /// Pushes a new step to the trace.
        /// </summary>
        private void Push(ScheduleStep step)
        {
            if (this.Count > 0)
            {
                this.Steps[this.Count - 1].Next = step;
                step.Previous = this.Steps[this.Count - 1];
            }

            this.Steps.Add(step);
        }

        /// <summary>
        /// Serializes the trace to text format.
        /// </summary>
        internal string Serialize(Configuration configuration, bool isFair)
        {
            StringBuilder stringBuilder = new StringBuilder();

            if (isFair)
            {
                stringBuilder.Append("--fair-scheduling").Append(Environment.NewLine);
            }

            if (configuration.IsLivenessCheckingEnabled)
            {
                stringBuilder.Append("--liveness-temperature-threshold:" +
                    configuration.LivenessTemperatureThreshold).
                    Append(Environment.NewLine);
            }

            if (!string.IsNullOrEmpty(configuration.TestMethodName))
            {
                stringBuilder.Append("--test-method:" + configuration.TestMethodName).Append(Environment.NewLine);
            }

            for (int idx = 0; idx < this.Count; idx++)
            {
                ScheduleStep step = this[idx];
                if (step.Type == ScheduleStepType.SchedulingChoice)
                {
                    stringBuilder.Append($"({step.ScheduledOperationId})");
                }
                else if (step.BooleanChoice != null)
                {
                    stringBuilder.Append(step.BooleanChoice.Value);
                }
                else
                {
                    stringBuilder.Append(step.IntegerChoice.Value);
                }

                if (idx < this.Count - 1)
                {
                    stringBuilder.Append(Environment.NewLine);
                }
            }

            return stringBuilder.ToString();
        }

        /// <summary>
        /// Deserializes the trace from the configuration.
        /// </summary>
        internal static ScheduleTrace Deserialize(Configuration configuration, out bool isFair)
        {
            string[] scheduleDump;
            if (configuration.ScheduleTrace.Length > 0)
            {
                scheduleDump = configuration.ScheduleTrace.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            }
            else
            {
                scheduleDump = File.ReadAllLines(configuration.ScheduleFile);
            }

            isFair = false;
            foreach (var line in scheduleDump)
            {
                if (!line.StartsWith("--"))
                {
                    break;
                }

                if (line.Equals("--fair-scheduling"))
                {
                    isFair = true;
                }
                else if (line.StartsWith("--liveness-temperature-threshold:"))
                {
                    configuration.LivenessTemperatureThreshold =
                        int.Parse(line.Substring("--liveness-temperature-threshold:".Length));
                }
                else if (line.StartsWith("--test-method:"))
                {
                    configuration.TestMethodName = line.Substring("--test-method:".Length);
                }
            }

            return new ScheduleTrace(scheduleDump);
        }
    }
}
