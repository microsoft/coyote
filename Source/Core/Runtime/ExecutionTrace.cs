// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

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
        /// Adds a scheduling decision.
        /// </summary>
        internal void AddSchedulingDecision(ControlledOperation current, SchedulingPointType sp, ControlledOperation next) =>
            this.AddSchedulingDecision(current.Id, current.Group.Id, sp, next.Id, next.Group.Id);

        /// <summary>
        /// Adds a scheduling decision.
        /// </summary>
        internal void AddSchedulingDecision(ulong current, Guid currentGroup, SchedulingPointType sp, ulong next, Guid nextGroup)
        {
            var scheduleStep = new SchedulingStep(this.Length, current, currentGroup, sp, next, nextGroup);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a nondeterministic boolean decision.
        /// </summary>
        internal void AddNondeterministicBooleanDecision(ControlledOperation current, bool value) =>
            this.AddNondeterministicBooleanDecision(current.Id, current.Group.Id, value);

        /// <summary>
        /// Adds a nondeterministic boolean decision.
        /// </summary>
        internal void AddNondeterministicBooleanDecision(ulong current, Guid currentGroup, bool value)
        {
            var scheduleStep = new BooleanChoiceStep(this.Length, current, currentGroup, value);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a nondeterministic integer decision.
        /// </summary>
        internal void AddNondeterministicIntegerDecision(ControlledOperation current, int value) =>
            this.AddNondeterministicIntegerDecision(current.Id, current.Group.Id, value);

        /// <summary>
        /// Adds a nondeterministic integer decision.
        /// </summary>
        internal void AddNondeterministicIntegerDecision(ulong current, Guid currentGroup, int value)
        {
            var scheduleStep = new IntegerChoiceStep(this.Length, current, currentGroup, value);
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
                if (step is SchedulingStep schedulingStep)
                {
                    this.AddSchedulingDecision(schedulingStep.Current, schedulingStep.CurrentGroup, schedulingStep.SchedulingPoint,
                        schedulingStep.Value, schedulingStep.Group);
                }
                else if (step is BooleanChoiceStep boolChoiceStep)
                {
                    this.AddNondeterministicBooleanDecision(boolChoiceStep.Current, boolChoiceStep.CurrentGroup, boolChoiceStep.Value);
                }
                else if (step is IntegerChoiceStep intChoiceStep)
                {
                    this.AddNondeterministicIntegerDecision(intChoiceStep.Current, intChoiceStep.CurrentGroup, intChoiceStep.Value);
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
                if (step is SchedulingStep schedulingStep)
                {
                    this.AddSchedulingDecision(schedulingStep.Current, schedulingStep.CurrentGroup, schedulingStep.SchedulingPoint,
                        schedulingStep.Value, schedulingStep.Group);
                }
                else if (step is BooleanChoiceStep boolChoiceStep)
                {
                    this.AddNondeterministicBooleanDecision(boolChoiceStep.Current, boolChoiceStep.CurrentGroup, boolChoiceStep.Value);
                }
                else if (step is IntegerChoiceStep intChoiceStep)
                {
                    this.AddNondeterministicIntegerDecision(intChoiceStep.Current, intChoiceStep.CurrentGroup, intChoiceStep.Value);
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
        /// Returns a string that represents the execution trace with all operations
        /// identified by their deterministic sequence hashes.
        /// </summary>
        public override string ToString()
        {
            var sb = new StringBuilder();
            for (int idx = 0; idx < this.Length; ++idx)
            {
                sb.Append(this.Steps[idx].ToSequenceString());
                if (idx < this.Length - 1)
                {
                    sb.Append(";");
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Contains metadata related to a single execution step.
        /// </summary>
        internal abstract class Step : IEquatable<Step>, IComparable<Step>
        {
            /// <summary>
            /// The unique index of this execution step.
            /// </summary>
            internal int Index;

            /// <summary>
            /// The id of the currently executing operation.
            /// </summary>
            internal ulong Current;

            /// <summary>
            /// The group id of the currently executing operation.
            /// </summary>
            internal Guid CurrentGroup;

            /// <summary>
            /// The previous execution step.
            /// </summary>
            internal Step Previous;

            /// <summary>
            /// The next execution step.
            /// </summary>
            internal Step Next;

            /// <summary>
            /// Initializes a new instance of the <see cref="Step"/> class.
            /// </summary>
            protected Step(int index, ulong current, Guid currentGroup)
            {
                this.Index = index;
                this.Current = current;
                this.CurrentGroup = currentGroup;
                this.Previous = null;
                this.Next = null;
            }

            /// <inheritdoc/>
            public override int GetHashCode() => this.Index.GetHashCode();

            /// <summary>
            /// Indicates whether the specified <see cref="Step"/> is equal
            /// to the current <see cref="Step"/>.
            /// </summary>
            internal abstract bool Equals(Step other);

            /// <inheritdoc/>
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

            /// <summary>
            /// Returns a string that represents the execution step with all operations
            /// identified by their deterministic sequence hashes.
            /// </summary>
            public abstract string ToSequenceString();
        }

        /// <summary>
        /// Contains metadata related to a single scheduling step.
        /// </summary>
        internal sealed class SchedulingStep : Step
        {
            /// <summary>
            /// The type of scheduling point encountered in this execution step.
            /// </summary>
            internal SchedulingPointType SchedulingPoint { get; private set; }

            /// <summary>
            /// The id of the chosen operation.
            /// </summary>
            internal ulong Value;

            /// <summary>
            /// The group id of the chosen operation.
            /// </summary>
            internal Guid Group;

            /// <summary>
            /// Initializes a new instance of the <see cref="SchedulingStep"/> class.
            /// </summary>
            internal SchedulingStep(int index, ulong current, Guid currentGroup, SchedulingPointType sp, ulong next, Guid seqHash)
                : base(index, current, currentGroup)
            {
                this.SchedulingPoint = sp;
                this.Value = next;
                this.Group = seqHash;
            }

            /// <inheritdoc/>
            internal override bool Equals(Step other) => other is SchedulingStep step ?
                this.Index == step.Index &&
                this.Current == step.Current &&
                this.SchedulingPoint == step.SchedulingPoint &&
                this.Value == step.Value :
                false;

            /// <summary>
            /// Returns a string that represents the execution step.
            /// </summary>
            public override string ToString() =>
                $"op({this.Current}:{this.CurrentGroup}),sp({this.SchedulingPoint}),next({this.Value}:{this.Group})";

            /// <inheritdoc/>
            public override string ToSequenceString() => $"op({this.CurrentGroup}),sp({this.SchedulingPoint}),next({this.Group})";
        }

        /// <summary>
        /// Contains metadata related to a single boolean choice step.
        /// </summary>
        internal sealed class BooleanChoiceStep : Step
        {
            /// <summary>
            /// The chosen boolean value.
            /// </summary>
            internal bool Value;

            /// <summary>
            /// Initializes a new instance of the <see cref="BooleanChoiceStep"/> class.
            /// </summary>
            internal BooleanChoiceStep(int index, ulong current, Guid currentGroup, bool value)
                : base(index, current, currentGroup)
            {
                this.Value = value;
            }

            /// <inheritdoc/>
            internal override bool Equals(Step other) => other is BooleanChoiceStep step ?
                this.Index == step.Index &&
                this.Current == step.Current &&
                this.Value == step.Value :
                false;

            /// <summary>
            /// Returns a string that represents the execution step.
            /// </summary>
            public override string ToString() => $"op({this.Current}:{this.CurrentGroup}),bool({this.Value})";

            /// <inheritdoc/>
            public override string ToSequenceString() => $"op({this.CurrentGroup}),bool({this.Value})";
        }

        /// <summary>
        /// Contains metadata related to a single integer choice step.
        /// </summary>
        internal sealed class IntegerChoiceStep : Step
        {
            /// <summary>
            /// The chosen integer value.
            /// </summary>
            internal int Value;

            /// <summary>
            /// Initializes a new instance of the <see cref="IntegerChoiceStep"/> class.
            /// </summary>
            internal IntegerChoiceStep(int index, ulong current, Guid currentGroup, int value)
                : base(index, current, currentGroup)
            {
                this.Value = value;
            }

            /// <inheritdoc/>
            internal override bool Equals(Step other) => other is IntegerChoiceStep step ?
                this.Index == step.Index &&
                this.Current == step.Current &&
                this.Value == step.Value :
                false;

            /// <summary>
            /// Returns a string that represents the execution step.
            /// </summary>
            public override string ToString() => $"op({this.Current}:{this.CurrentGroup}),int({this.Value})";

            /// <inheritdoc/>
            public override string ToSequenceString() => $"op({this.CurrentGroup}),int({this.Value})";
        }
    }
}
