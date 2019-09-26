// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.Coyote.Machines;

using EventInfo = Microsoft.Coyote.Runtime.EventInfo;

namespace Microsoft.Coyote.TestingServices.Tracing.Error
{
    /// <summary>
    /// Class implementing a bug trace step.
    /// </summary>
    [DataContract(IsReference = true)]
    internal sealed class BugTraceStep
    {
        /// <summary>
        /// The unique index of this bug trace step.
        /// </summary>
        internal int Index;

        /// <summary>
        /// The type of this bug trace step.
        /// </summary>
        [DataMember]
        internal BugTraceStepType Type { get; private set; }

        /// <summary>
        /// The machine initiating the action.
        /// </summary>
        [DataMember]
        internal MachineId Machine;

        /// <summary>
        /// The machine state.
        /// </summary>
        [DataMember]
        internal string MachineState;

        /// <summary>
        /// Information about the event being sent.
        /// </summary>
        [DataMember]
        internal EventInfo EventInfo;

        /// <summary>
        /// The invoked action.
        /// </summary>
        [DataMember]
        internal string InvokedAction;

        /// <summary>
        /// The target machine.
        /// </summary>
        [DataMember]
        internal MachineId TargetMachine;

        /// <summary>
        /// The taken nondeterministic boolean choice.
        /// </summary>
        [DataMember]
        internal bool? RandomBooleanChoice;

        /// <summary>
        /// The taken nondeterministic integer choice.
        /// </summary>
        [DataMember]
        internal int? RandomIntegerChoice;

        /// <summary>
        /// Extra information that can be used to
        /// enhance the trace reported to the user.
        /// </summary>
        [DataMember]
        internal string ExtraInfo;

        /// <summary>
        /// Previous bug trace step.
        /// </summary>
        internal BugTraceStep Previous;

        /// <summary>
        /// Next bug trace step.
        /// </summary>
        internal BugTraceStep Next;

        /// <summary>
        /// Creates a bug trace step.
        /// </summary>
        internal static BugTraceStep Create(int index, BugTraceStepType type, MachineId machine,
            string machineState, EventInfo eventInfo, MethodInfo action, MachineId targetMachine,
            bool? boolChoice, int? intChoice, string extraInfo)
        {
            var traceStep = new BugTraceStep();

            traceStep.Index = index;
            traceStep.Type = type;

            traceStep.Machine = machine;
            traceStep.MachineState = machineState;

            traceStep.EventInfo = eventInfo;

            if (action != null)
            {
                traceStep.InvokedAction = action.Name;
            }

            traceStep.TargetMachine = targetMachine;
            traceStep.RandomBooleanChoice = boolChoice;
            traceStep.RandomIntegerChoice = intChoice;
            traceStep.ExtraInfo = extraInfo;

            traceStep.Previous = null;
            traceStep.Next = null;

            return traceStep;
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is BugTraceStep traceStep)
            {
                return this.Index == traceStep.Index;
            }

            return false;
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        public override int GetHashCode() => this.Index.GetHashCode();
    }
}
