// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.Coyote.Machines;

using EventInfo = Microsoft.Coyote.Runtime.EventInfo;

namespace Microsoft.Coyote.TestingServices.Tracing.Error
{
    /// <summary>
    /// Class implementing a bug trace. A trace is a series of transitions
    /// from some initial state to some end state.
    /// </summary>
    [DataContract]
    internal sealed class BugTrace : IEnumerable, IEnumerable<BugTraceStep>
    {
        /// <summary>
        /// The steps of the bug trace.
        /// </summary>
        [DataMember]
        private readonly List<BugTraceStep> Steps;

        /// <summary>
        /// The number of steps in the bug trace.
        /// </summary>
        internal int Count
        {
            get { return this.Steps.Count; }
        }

        /// <summary>
        /// Index for the bug trace.
        /// </summary>
        internal BugTraceStep this[int index]
        {
            get { return this.Steps[index]; }
            set { this.Steps[index] = value; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BugTrace"/> class.
        /// </summary>
        internal BugTrace()
        {
            this.Steps = new List<BugTraceStep>();
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        internal void AddCreateMachineStep(Machine machine, MachineId targetMachine, EventInfo eventInfo)
        {
            MachineId mid = null;
            string machineState = null;
            if (machine != null)
            {
                mid = machine.Id;
                machineState = machine.CurrentStateName;
            }

            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.CreateMachine,
                mid, machineState, eventInfo, null, targetMachine, null, null, null);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        internal void AddCreateMonitorStep(MachineId monitor)
        {
            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.CreateMonitor,
                null, null, null, null, monitor, null, null, null);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        internal void AddSendEventStep(MachineId machine, string machineState,
            EventInfo eventInfo, MachineId targetMachine)
        {
            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.SendEvent,
                machine, machineState, eventInfo, null, targetMachine, null, null, null);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        internal void AddDequeueEventStep(MachineId machine, string machineState, EventInfo eventInfo)
        {
            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.DequeueEvent,
                machine, machineState, eventInfo, null, null, null, null, null);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        internal void AddRaiseEventStep(MachineId machine, string machineState, EventInfo eventInfo)
        {
            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.RaiseEvent,
                machine, machineState, eventInfo, null, null, null, null, null);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        internal void AddGotoStateStep(MachineId machine, string machineState)
        {
            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.GotoState,
                machine, machineState, null, null, null, null, null, null);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        internal void AddInvokeActionStep(MachineId machine, string machineState, MethodInfo action)
        {
            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.InvokeAction,
                machine, machineState, null, action, null, null, null, null);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        internal void AddWaitToReceiveStep(MachineId machine, string machineState, string eventNames)
        {
            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.WaitToReceive,
                machine, machineState, null, null, null, null, null, eventNames);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        internal void AddReceivedEventStep(MachineId machine, string machineState, EventInfo eventInfo)
        {
            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.ReceiveEvent,
                machine, machineState, eventInfo, null, null, null, null, null);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        internal void AddRandomChoiceStep(MachineId machine, string machineState, bool choice)
        {
            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.RandomChoice,
                machine, machineState, null, null, null, choice, null, null);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        internal void AddRandomChoiceStep(MachineId machine, string machineState, int choice)
        {
            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.RandomChoice,
                machine, machineState, null, null, null, null, choice, null);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Adds a bug trace step.
        /// </summary>
        internal void AddHaltStep(MachineId machine, string machineState)
        {
            var scheduleStep = BugTraceStep.Create(this.Count, BugTraceStepType.Halt,
                machine, machineState, null, null, null, null, null, null);
            this.Push(scheduleStep);
        }

        /// <summary>
        /// Returns the latest bug trace step and removes it from the trace.
        /// </summary>
        internal BugTraceStep Pop()
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
        /// Returns the latest bug trace step without removing it.
        /// </summary>
        internal BugTraceStep Peek()
        {
            BugTraceStep step = null;

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
        IEnumerator<BugTraceStep> IEnumerable<BugTraceStep>.GetEnumerator()
        {
            return this.Steps.GetEnumerator();
        }

        /// <summary>
        /// Pushes a new step to the trace.
        /// </summary>
        private void Push(BugTraceStep step)
        {
            if (this.Count > 0)
            {
                this.Steps[this.Count - 1].Next = step;
                step.Previous = this.Steps[this.Count - 1];
            }

            this.Steps.Add(step);
        }
    }
}
