// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Class implementing an execution trace. A trace is a series of transitions
    /// from some initial program state to some end state.
    /// </summary>
    internal sealed class ScheduleTrace : IEnumerable, IEnumerable<ScheduleStep>
    {
        /// <summary>
        /// The steps of the trace.
        /// </summary>
        private readonly List<ScheduleStep> Steps;

        /// <summary>
        /// The number of steps in the trace.
        /// </summary>
        internal int Length
        {
            get { return this.Steps.Count; }
        }

        /// <summary>
        /// Indexes the trace.
        /// </summary>
        internal ScheduleStep this[int index]
        {
            get { return this.Steps[index]; }
            set { this.Steps[index] = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScheduleTrace"/> class.
        /// </summary>
        private ScheduleTrace()
        {
            this.Steps = new List<ScheduleStep>();
        }

        /// <summary>
        /// Creates a new <see cref="ScheduleTrace"/>.
        /// </summary>
        internal static ScheduleTrace Create() => new ScheduleTrace();

        /// <summary>
        /// Adds a scheduling choice.
        /// </summary>
        internal void AddSchedulingChoice(ulong scheduledActorId)
        {
            var scheduleStep = ScheduleStep.CreateSchedulingChoice(this.Length, scheduledActorId);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a nondeterministic boolean choice.
        /// </summary>
        internal void AddNondeterministicBooleanChoice(bool choice)
        {
            var scheduleStep = ScheduleStep.CreateNondeterministicBooleanChoice(
                this.Length, choice);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a nondeterministic integer choice.
        /// </summary>
        internal void AddNondeterministicIntegerChoice(int choice)
        {
            var scheduleStep = ScheduleStep.CreateNondeterministicIntegerChoice(
                this.Length, choice);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Returns the latest schedule step and removes it from the trace.
        /// </summary>
        internal ScheduleStep Pop()
        {
            if (this.Length > 0)
            {
                this.Steps[this.Length - 1].Next = null;
            }

            var step = this.Steps[this.Length - 1];
            this.Steps.RemoveAt(this.Length - 1);

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
                step = this.Steps[this.Length - 1];
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
            if (this.Length > 0)
            {
                this.Steps[this.Length - 1].Next = step;
                step.Previous = this.Steps[this.Length - 1];
            }

            this.Steps.Add(step);
        }

        /// <summary>
        /// Clears the trace.
        /// </summary>
        internal void Clear()
        {
            this.Steps.Clear();
        }
    }
}
