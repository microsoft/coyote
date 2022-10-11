// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// An execution trace implemented as a sequence of steps denoting controlled
    /// scheduling and nondeterministic decisions taken during testing.
    /// </summary>
    internal sealed class ExecutionTrace : IEnumerable, IEnumerable<ExecutionTrace.Step>
    {
        /// <summary>
        /// The steps of this execution trace.
        /// </summary>
        private readonly List<Step> Steps;

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
        internal Step this[int index]
        {
            get { return this.Steps[index]; }
            set { this.Steps[index] = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExecutionTrace"/> class.
        /// </summary>
        private ExecutionTrace()
        {
            this.Steps = new List<Step>();
        }

        /// <summary>
        /// Creates a new <see cref="ExecutionTrace"/>.
        /// </summary>
        internal static ExecutionTrace Create() => new ExecutionTrace();

        /// <summary>
        /// Adds a scheduling choice.
        /// </summary>
        internal void AddSchedulingChoice(ulong scheduledOperationId)
        {
            var scheduleStep = Step.CreateSchedulingChoice(this.Length, scheduledOperationId);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a nondeterministic boolean choice.
        /// </summary>
        internal void AddNondeterministicBooleanChoice(bool choice)
        {
            var scheduleStep = Step.CreateNondeterministicBooleanChoice(
                this.Length, choice);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a nondeterministic integer choice.
        /// </summary>
        internal void AddNondeterministicIntegerChoice(int choice)
        {
            var scheduleStep = Step.CreateNondeterministicIntegerChoice(
                this.Length, choice);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Returns the latest schedule step and removes it from the trace.
        /// </summary>
        internal Step Pop()
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
        internal Step Peek()
        {
            Step step = null;

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
        IEnumerator<Step> IEnumerable<Step>.GetEnumerator()
        {
            return this.Steps.GetEnumerator();
        }

        /// <summary>
        /// Pushes a new step to the trace.
        /// </summary>
        private void Push(Step step)
        {
            if (this.Length > 0)
            {
                if (this.Steps[this.Length - 1] is null)
                {
                    return;
                }

                this.Steps[this.Length - 1].Next = step;
                step.Previous = this.Steps[this.Length - 1];
            }

            this.Steps.Add(step);
        }

        /// <summary>
        /// Appends the steps from the specified trace.
        /// </summary>
        internal ExecutionTrace Append(ExecutionTrace trace)
        {
            foreach (var step in trace.Steps)
            {
                if (step.Type is DecisionType.SchedulingChoice)
                {
                    this.AddSchedulingChoice(step.ScheduledOperationId);
                }
                else if (step.Type is DecisionType.NondeterministicChoice && step.BooleanChoice.HasValue)
                {
                    this.AddNondeterministicBooleanChoice(step.BooleanChoice.Value);
                }
                else if (step.Type is DecisionType.NondeterministicChoice && step.IntegerChoice.HasValue)
                {
                    this.AddNondeterministicIntegerChoice(step.IntegerChoice.Value);
                }
            }

            return this;
        }

        /// <summary>
        /// Extends the trace with any new steps from the specified trace, or replaces the trace
        /// with the new trace if the two traces diverge.
        /// </summary>
        internal ExecutionTrace ExtendOrReplace(ExecutionTrace trace)
        {
            // Find the index of the first new or diverging step.
            int appendIndex = 0;
            while (appendIndex < trace.Length && appendIndex < this.Length &&
                this[appendIndex].Equals(trace[appendIndex]))
            {
                appendIndex++;
            }

            // If the index is 0 or less than the length of both traces, then the traces diverge,
            // so remove the diverging steps from the trace before appending the new steps.
            if (appendIndex < this.Length && (appendIndex is 0 || appendIndex < trace.Length))
            {
                this.Steps.RemoveRange(appendIndex, this.Length - appendIndex);
            }

            // Extend the trace with any new or diverging steps.
            while (appendIndex < trace.Length)
            {
                Step step = trace[appendIndex];
                if (step.Type is DecisionType.SchedulingChoice)
                {
                    this.AddSchedulingChoice(step.ScheduledOperationId);
                }
                else if (step.Type is DecisionType.NondeterministicChoice && step.BooleanChoice.HasValue)
                {
                    this.AddNondeterministicBooleanChoice(step.BooleanChoice.Value);
                }
                else if (step.Type is DecisionType.NondeterministicChoice && step.IntegerChoice.HasValue)
                {
                    this.AddNondeterministicIntegerChoice(step.IntegerChoice.Value);
                }

                appendIndex++;
            }

            return this;
        }

        /// <summary>
        /// Clears the trace.
        /// </summary>
        internal void Clear() => this.Steps.Clear();

        /// <summary>
        /// The type of decision taken during an execution step.
        /// </summary>
        internal enum DecisionType
        {
            SchedulingChoice = 0,
            NondeterministicChoice
        }

        /// <summary>
        /// Contains metadata related to a single execution step.
        /// </summary>
        internal sealed class Step : IEquatable<Step>, IComparable<Step>
        {
            /// <summary>
            /// The unique index of this execution step.
            /// </summary>
            internal int Index;

            /// <summary>
            /// The type of the decision taken in this execution step.
            /// </summary>
            internal DecisionType Type { get; private set; }

            /// <summary>
            /// The id of the scheduled operation. Only relevant if this is
            /// a regular execution step.
            /// </summary>
            internal ulong ScheduledOperationId;

            /// <summary>
            /// The non-deterministic boolean choice value. Only relevant if
            /// this is a choice execution step.
            /// </summary>
            internal bool? BooleanChoice;

            /// <summary>
            /// The non-deterministic integer choice value. Only relevant if
            /// this is a choice execution step.
            /// </summary>
            internal int? IntegerChoice;

            /// <summary>
            /// The previous execution step.
            /// </summary>
            internal Step Previous;

            /// <summary>
            /// The next execution step.
            /// </summary>
            internal Step Next;

            /// <summary>
            /// Creates an execution step.
            /// </summary>
            internal static Step CreateSchedulingChoice(int index, ulong scheduledOperationId)
            {
                var scheduleStep = new Step();

                scheduleStep.Index = index;
                scheduleStep.Type = DecisionType.SchedulingChoice;

                scheduleStep.ScheduledOperationId = scheduledOperationId;

                scheduleStep.BooleanChoice = null;
                scheduleStep.IntegerChoice = null;

                scheduleStep.Previous = null;
                scheduleStep.Next = null;

                return scheduleStep;
            }

            /// <summary>
            /// Creates a nondeterministic boolean choice execution step.
            /// </summary>
            internal static Step CreateNondeterministicBooleanChoice(int index, bool choice)
            {
                var scheduleStep = new Step();

                scheduleStep.Index = index;
                scheduleStep.Type = DecisionType.NondeterministicChoice;

                scheduleStep.BooleanChoice = choice;
                scheduleStep.IntegerChoice = null;

                scheduleStep.Previous = null;
                scheduleStep.Next = null;

                return scheduleStep;
            }

            /// <summary>
            /// Creates a nondeterministic integer choice execution step.
            /// </summary>
            internal static Step CreateNondeterministicIntegerChoice(int index, int choice)
            {
                var scheduleStep = new Step();

                scheduleStep.Index = index;
                scheduleStep.Type = DecisionType.NondeterministicChoice;

                scheduleStep.BooleanChoice = null;
                scheduleStep.IntegerChoice = choice;

                scheduleStep.Previous = null;
                scheduleStep.Next = null;

                return scheduleStep;
            }

            /// <summary>
            /// Returns the hash code for this instance.
            /// </summary>
            public override int GetHashCode() => this.Index.GetHashCode();

            /// <summary>
            /// Indicates whether the specified <see cref="Step"/> is equal
            /// to the current <see cref="Step"/>.
            /// </summary>
            internal bool Equals(Step other) => other != null ?
                this.Index == other.Index && this.Type == other.Type &&
                this.ScheduledOperationId == other.ScheduledOperationId &&
                this.BooleanChoice == other.BooleanChoice &&
                this.IntegerChoice == other.IntegerChoice :
                false;

            /// <summary>
            /// Determines whether the specified object is equal to the current object.
            /// </summary>
            public override bool Equals(object obj) => this.Equals(obj as Step);

            /// <summary>
            /// Indicates whether the specified <see cref="Step"/> is equal
            /// to the current <see cref="Step"/>.
            /// </summary>
            bool IEquatable<Step>.Equals(Step other) => this.Equals(other);

            /// <summary>
            /// Compares the specified <see cref="Step"/> with the current
            /// <see cref="Step"/> for ordering or sorting purposes.
            /// </summary>
            int IComparable<Step>.CompareTo(Step other) => this.Index - other.Index;
        }
    }
}
