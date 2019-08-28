// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;

using Microsoft.Coyote.Timers;
using Microsoft.Coyote.Utilities;

namespace Microsoft.Coyote.IO
{
    /// <summary>
    /// An implementation of <see cref="ILogger"/> that by default passes all logging
    /// commands to the variants of the <see cref="Write(string)"/> method.
    /// </summary>
    public abstract class MachineLogger : ILogger
    {
        /// <summary>
        /// If true, then messages are logged. The default value is false.
        /// </summary>
        public bool IsVerbose { get; set; } = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="MachineLogger"/> class.
        /// </summary>
        /// <param name="isVerbose">If true, then messages are logged. The default value is false.</param>
        public MachineLogger(bool isVerbose = false)
        {
            this.IsVerbose = isVerbose;
        }

        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        public abstract void Write(string value);

        /// <summary>
        /// Writes the text representation of the specified argument.
        /// </summary>
        public abstract void Write(string format, object arg0);

        /// <summary>
        /// Writes the text representation of the specified arguments.
        /// </summary>
        public abstract void Write(string format, object arg0, object arg1);

        /// <summary>
        /// Writes the text representation of the specified arguments.
        /// </summary>
        public abstract void Write(string format, object arg0, object arg1, object arg2);

        /// <summary>
        /// Writes the text representation of the specified array of objects.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public abstract void Write(string format, params object[] args);

        /// <summary>
        /// Writes the specified string value, followed by the
        /// current line terminator.
        /// </summary>
        public abstract void WriteLine(string value);

        /// <summary>
        /// Writes the text representation of the specified argument, followed by the
        /// current line terminator.
        /// </summary>
        public abstract void WriteLine(string format, object arg0);

        /// <summary>
        /// Writes the text representation of the specified arguments, followed by the
        /// current line terminator.
        /// </summary>
        public abstract void WriteLine(string format, object arg0, object arg1);

        /// <summary>
        /// Writes the text representation of the specified arguments, followed by the
        /// current line terminator.
        /// </summary>
        public abstract void WriteLine(string format, object arg0, object arg1, object arg2);

        /// <summary>
        /// Writes the text representation of the specified array of objects,
        /// followed by the current line terminator.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public abstract void WriteLine(string format, params object[] args);

        /// <summary>
        /// Called when an event is about to be enqueued to a machine.
        /// </summary>
        /// <param name="machineId">Id of the machine that the event is being enqueued to.</param>
        /// <param name="eventName">Name of the event.</param>
        public virtual void OnEnqueue(MachineId machineId, string eventName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnEnqueueString(machineId, eventName));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnEnqueue"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that the event is being enqueued to.</param>
        /// <param name="eventName">Name of the event.</param>
        public virtual string FormatOnEnqueueString(MachineId machineId, string eventName)
        {
            return $"<EnqueueLog> Machine '{machineId}' enqueued event '{eventName}'.";
        }

        /// <summary>
        /// Called when an event is dequeued by a machine.
        /// </summary>
        /// <param name="machineId">Id of the machine that the event is being dequeued by.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">Name of the event.</param>
        public virtual void OnDequeue(MachineId machineId, string currStateName, string eventName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnDequeueString(machineId, currStateName, eventName));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnDequeue"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that the event is being dequeued by.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">Name of the event.</param>
        public virtual string FormatOnDequeueString(MachineId machineId, string currStateName, string eventName)
        {
            return $"<DequeueLog> Machine '{machineId}' in state '{currStateName}' dequeued event '{eventName}'.";
        }

        /// <summary>
        /// Called when the default event handler for a state is about to be executed.
        /// </summary>
        /// <param name="machineId">Id of the machine that the state will execute in.</param>
        /// <param name="currStateName">Name of the current state of the machine.</param>
        public virtual void OnDefault(MachineId machineId, string currStateName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnDefaultString(machineId, currStateName));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnDefault"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that the state will execute in.</param>
        /// <param name="currStateName">Name of the current state of the machine.</param>
        public virtual string FormatOnDefaultString(MachineId machineId, string currStateName)
        {
            return $"<DefaultLog> Machine '{machineId}' in state '{currStateName}' is executing the default handler.";
        }

        /// <summary>
        /// Called when a machine transitions states via a 'goto'.
        /// </summary>
        /// <param name="machineId">Id of the machine.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="newStateName">The target state of goto.</param>
        public void OnGoto(MachineId machineId, string currStateName, string newStateName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnGotoString(machineId, currStateName, newStateName));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnGoto"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="newStateName">The target state of goto.</param>
        public virtual string FormatOnGotoString(MachineId machineId, string currStateName, string newStateName)
        {
            return $"<GotoLog> Machine '{machineId}' is transitioning from state '{currStateName}' to state '{newStateName}'.";
        }

        /// <summary>
        /// Called when a machine is being pushed to a state.
        /// </summary>
        /// <param name="machineId">Id of the machine being pushed to the state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="newStateName">The state the machine is pushed to.</param>
        public virtual void OnPush(MachineId machineId, string currStateName, string newStateName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnPushString(machineId, currStateName, newStateName));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnPush"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine being pushed to the state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="newStateName">The state the machine is pushed to.</param>
        public virtual string FormatOnPushString(MachineId machineId, string currStateName, string newStateName)
        {
            return $"<PushLog> Machine '{machineId}' pushed from state '{currStateName}' to state '{newStateName}'.";
        }

        /// <summary>
        /// Called when a machine has been popped from a state.
        /// </summary>
        /// <param name="machineId">Id of the machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="restoredStateName">The name of the state being re-entered, if any</param>
        public virtual void OnPop(MachineId machineId, string currStateName, string restoredStateName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnPopString(machineId, currStateName, restoredStateName));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnPop"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="restoredStateName">The name of the state being re-entered, if any</param>
        public virtual string FormatOnPopString(MachineId machineId, string currStateName, string restoredStateName)
        {
            currStateName = string.IsNullOrEmpty(currStateName) ? "[not recorded]" : currStateName;
            var reenteredStateName = restoredStateName ?? string.Empty;
            return $"<PopLog> Machine '{machineId}' popped state '{currStateName}' and reentered state '{reenteredStateName}'.";
        }

        /// <summary>
        /// When an event cannot be handled in the current state, its exit handler is executed and then the state is
        /// popped and any previous "current state" is reentered. This handler is called when that pop has been done.
        /// </summary>
        /// <param name="machineId">Id of the machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">The name of the event that cannot be handled.</param>
        public virtual void OnPopUnhandledEvent(MachineId machineId, string currStateName, string eventName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnPopUnhandledEventString(machineId, currStateName, eventName));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnPopUnhandledEvent"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">The name of the event that cannot be handled.</param>
        public virtual string FormatOnPopUnhandledEventString(MachineId machineId, string currStateName, string eventName)
        {
            var reenteredStateName = string.IsNullOrEmpty(currStateName)
                ? string.Empty
                : $" and reentered state '{currStateName}";
            return $"<PopLog> Machine '{machineId}' popped with unhandled event '{eventName}'{reenteredStateName}.";
        }

        /// <summary>
        /// Called when an event is received by a machine.
        /// </summary>
        /// <param name="machineId">Id of the machine that received the event.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="wasBlocked">The machine was waiting for one or more specific events,
        ///     and <paramref name="eventName"/> was one of them</param>
        public virtual void OnReceive(MachineId machineId, string currStateName, string eventName, bool wasBlocked)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnReceiveString(machineId, currStateName, eventName, wasBlocked));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnReceive"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that received the event.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="wasBlocked">The machine was waiting for one or more specific events,
        ///     and <paramref name="eventName"/> was one of them</param>
        public virtual string FormatOnReceiveString(MachineId machineId, string currStateName, string eventName, bool wasBlocked)
        {
            var unblocked = wasBlocked ? " and unblocked" : string.Empty;
            return $"<ReceiveLog> Machine '{machineId}' in state '{currStateName}' dequeued event '{eventName}'{unblocked}.";
        }

        /// <summary>
        /// Called when a machine waits to receive an event of a specified type.
        /// </summary>
        /// <param name="machineId">Id of the machine that is entering the wait state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventType">The type of the event being waited for.</param>
        public virtual void OnWait(MachineId machineId, string currStateName, Type eventType)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnWaitString(machineId, currStateName, eventType));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnWait(MachineId, string, Type)"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that is entering the wait state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventType">The type of the event being waited for.</param>
        public virtual string FormatOnWaitString(MachineId machineId, string currStateName, Type eventType) =>
            $"<ReceiveLog> Machine '{machineId}' in state '{currStateName}' is waiting to dequeue an event of type '{eventType.FullName}'.";

        /// <summary>
        /// Called when a machine waits to receive an event of one of the specified types.
        /// </summary>
        /// <param name="machineId">Id of the machine that is entering the wait state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventTypes">The types of the events being waited for, if any.</param>
        public virtual void OnWait(MachineId machineId, string currStateName, params Type[] eventTypes)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnWaitString(machineId, currStateName, eventTypes));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnWait(MachineId, string, Type[])"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">Id of the machine that is entering the wait state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventTypes">The types of the events being waited for, if any.</param>
        public virtual string FormatOnWaitString(MachineId machineId, string currStateName, params Type[] eventTypes)
        {
            string eventNames;
            if (eventTypes.Length == 0)
            {
                eventNames = "'<missing>'";
            }
            else if (eventTypes.Length == 1)
            {
                eventNames = "'" + eventTypes[0].FullName + "'";
            }
            else if (eventTypes.Length == 2)
            {
                eventNames = "'" + eventTypes[0].FullName + "' or '" + eventTypes[1].FullName + "'";
            }
            else if (eventTypes.Length == 3)
            {
                eventNames = "'" + eventTypes[0].FullName + "', '" + eventTypes[1].FullName + "' or '" + eventTypes[2].FullName + "'";
            }
            else
            {
                string[] eventNameArray = new string[eventTypes.Length - 1];
                for (int i = 0; i < eventTypes.Length - 2; i++)
                {
                    eventNameArray[i] = eventTypes[i].FullName;
                }

                eventNames = "'" + string.Join("', '", eventNameArray) + "' or '" + eventTypes[eventTypes.Length - 1].FullName + "'";
            }

            return $"<ReceiveLog> Machine '{machineId}' in state '{currStateName}' is waiting to dequeue an event of type {eventNames}.";
        }

        /// <summary>
        /// Called when an event is sent to a target machine.
        /// </summary>
        /// <param name="targetMachineId">Id of the target machine.</param>
        /// <param name="senderId">The id of the machine that sent the event, if any.</param>
        /// <param name="senderStateName">The name of the current state of the sender machine, if any.</param>
        /// <param name="eventName">The event being sent.</param>
        /// <param name="opGroupId">Id used to identify the send operation.</param>
        /// <param name="isTargetHalted">Is the target machine halted.</param>
        public virtual void OnSend(MachineId targetMachineId, MachineId senderId, string senderStateName, string eventName,
            Guid opGroupId, bool isTargetHalted)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnSendString(targetMachineId, senderId, senderStateName, eventName, opGroupId, isTargetHalted));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnSend"/> event and its parameters.
        /// </summary>
        /// <param name="targetMachineId">Id of the target machine.</param>
        /// <param name="senderId">The id of the machine that sent the event, if any.</param>
        /// <param name="senderStateName">The name of the current state of the sender machine, if any.</param>
        /// <param name="eventName">The event being sent.</param>
        /// <param name="opGroupId">Id used to identify the send operation.</param>
        /// <param name="isTargetHalted">Is the target machine halted.</param>
        public virtual string FormatOnSendString(MachineId targetMachineId, MachineId senderId, string senderStateName,
            string eventName, Guid opGroupId, bool isTargetHalted)
        {
            var opGroupIdMsg = opGroupId != Guid.Empty ? $" (operation group '{opGroupId}')" : string.Empty;
            var target = isTargetHalted ? $"halted machine '{targetMachineId}'" : $"machine '{targetMachineId}'";
            var sender = senderId != null ? $"Machine '{senderId}' in state '{senderStateName}'" : $"The runtime";
            return $"<SendLog> {sender} sent event '{eventName}' to {target}{opGroupIdMsg}.";
        }

        /// <summary>
        /// Called when a machine has been created.
        /// </summary>
        /// <param name="machineId">The id of the machine that has been created.</param>
        /// <param name="creator">Id of the creator machine, null otherwise.</param>
        public virtual void OnCreateMachine(MachineId machineId, MachineId creator)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnCreateMachineString(machineId, creator));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnCreateMachine"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine that has been created.</param>
        /// <param name="creator">Id of the creator machine, null otherwise.</param>
        public virtual string FormatOnCreateMachineString(MachineId machineId, MachineId creator)
        {
            var source = creator is null ? "the runtime" : $"machine '{creator.Name}'";
            return $"<CreateLog> Machine '{machineId}' was created by {source}.";
        }

        /// <summary>
        /// Called when a monitor has been created.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor that has been created.</param>
        /// <param name="monitorId">The id of the monitor that has been created.</param>
        public virtual void OnCreateMonitor(string monitorTypeName, MachineId monitorId)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnCreateMonitorString(monitorTypeName, monitorId));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnCreateMonitor"/> event and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor that has been created.</param>
        /// <param name="monitorId">The id of the monitor that has been created.</param>
        public virtual string FormatOnCreateMonitorString(string monitorTypeName, MachineId monitorId)
        {
            return $"<CreateLog> Monitor '{monitorTypeName}' with id '{monitorId}' was created.";
        }

        /// <summary>
        /// Called when a machine timer has been created.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        public void OnCreateTimer(TimerInfo info)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnCreateTimerString(info));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnCreateTimer"/> event and its parameters.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        public virtual string FormatOnCreateTimerString(TimerInfo info)
        {
            var source = info.OwnerId is null ? "the runtime" : $"machine '{info.OwnerId.Name}'";
            if (info.Period.TotalMilliseconds >= 0)
            {
                return $"<TimerLog> Timer '{info}' (due-time:{info.DueTime.TotalMilliseconds}ms; " +
                    $"period :{info.Period.TotalMilliseconds}ms) was created by {source}.";
            }
            else
            {
                return $"<TimerLog> Timer '{info}' (due-time:{info.DueTime.TotalMilliseconds}ms) was created by {source}.";
            }
        }

        /// <summary>
        /// Called when a machine timer has been stopped.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        public void OnStopTimer(TimerInfo info)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnStopTimerString(info));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnStopTimer"/> event and its parameters.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        public virtual string FormatOnStopTimerString(TimerInfo info)
        {
            var source = info.OwnerId is null ? "the runtime" : $"machine '{info.OwnerId.Name}'";
            return $"<TimerLog> Timer '{info}' was stopped and disposed by {source}.";
        }

        /// <summary>
        /// Called when a machine has been halted.
        /// </summary>
        /// <param name="machineId">The id of the machine that has been halted.</param>
        /// <param name="inboxSize">Approximate size of the machine inbox.</param>
        public virtual void OnHalt(MachineId machineId, int inboxSize)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnHaltString(machineId, inboxSize));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnHalt"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine that has been halted.</param>
        /// <param name="inboxSize">Approximate size of the machine inbox.</param>
        public virtual string FormatOnHaltString(MachineId machineId, int inboxSize)
        {
            return $"<HaltLog> Machine '{machineId}' halted with '{inboxSize}' events in its inbox.";
        }

        /// <summary>
        /// Called when a random result has been obtained.
        /// </summary>
        /// <param name="machineId">The id of the source machine, if any; otherwise, the runtime itself was the source.</param>
        /// <param name="result">The random result (may be bool or int).</param>
        public virtual void OnRandom(MachineId machineId, object result)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnRandomString(machineId, result));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnRandom"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the source machine, if any; otherwise, the runtime itself was the source.</param>
        /// <param name="result">The random result (may be bool or int).</param>
        public virtual string FormatOnRandomString(MachineId machineId, object result)
        {
            var source = machineId != null ? $"Machine '{machineId}'" : "Runtime";
            return $"<RandomLog> {source} nondeterministically chose '{result}'.";
        }

        /// <summary>
        /// Called when a machine enters or exits a state.
        /// </summary>
        /// <param name="machineId">The id of the machine entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited.</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        public virtual void OnMachineState(MachineId machineId, string stateName, bool isEntry)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnMachineStateString(machineId, stateName, isEntry));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnMachineState"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited.</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        public virtual string FormatOnMachineStateString(MachineId machineId, string stateName, bool isEntry)
        {
            var direction = isEntry ? "enters" : "exits";
            return $"<StateLog> Machine '{machineId}' {direction} state '{stateName}'.";
        }

        /// <summary>
        /// Called when a machine raises an event.
        /// </summary>
        /// <param name="machineId">The id of the machine raising the event.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="eventName">The name of the event being raised.</param>
        public virtual void OnMachineEvent(MachineId machineId, string currStateName, string eventName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnMachineEventString(machineId, currStateName, eventName));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnMachineEvent"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine raising the event.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="eventName">The name of the event being raised.</param>
        public virtual string FormatOnMachineEventString(MachineId machineId, string currStateName, string eventName)
        {
            return $"<RaiseLog> Machine '{machineId}' in state '{currStateName}' raised event '{eventName}'.";
        }

        /// <summary>
        /// Called when a machine executes an action.
        /// </summary>
        /// <param name="machineId">The id of the machine executing the action.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public virtual void OnMachineAction(MachineId machineId, string currStateName, string actionName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnMachineActionString(machineId, currStateName, actionName));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnMachineAction"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine executing the action.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public virtual string FormatOnMachineActionString(MachineId machineId, string currStateName, string actionName)
        {
            return $"<ActionLog> Machine '{machineId}' in state '{currStateName}' invoked action '{actionName}'.";
        }

        /// <summary>
        /// Called when a machine throws an exception
        /// </summary>
        /// <param name="machineId">The id of the machine that threw the exception.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        public virtual void OnMachineExceptionThrown(MachineId machineId, string currStateName, string actionName, Exception ex)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnMachineExceptionThrownString(machineId, currStateName, actionName, ex));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnMachineExceptionThrown"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine that threw the exception.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        public virtual string FormatOnMachineExceptionThrownString(MachineId machineId, string currStateName, string actionName, Exception ex)
        {
            return $"<ExceptionLog> Machine '{machineId}' in state '{currStateName}' running action '{actionName}' threw an exception '{ex.GetType().Name}'.";
        }

        /// <summary>
        /// Called when a machine's OnException method is used to handle a thrown exception
        /// </summary>
        /// <param name="machineId">The id of the machine that threw the exception.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        public virtual void OnMachineExceptionHandled(MachineId machineId, string currStateName, string actionName, Exception ex)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnMachineExceptionHandledString(machineId, currStateName, actionName, ex));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnMachineExceptionHandled"/> event and its parameters.
        /// </summary>
        /// <param name="machineId">The id of the machine that threw the exception.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        public virtual string FormatOnMachineExceptionHandledString(MachineId machineId, string currStateName, string actionName, Exception ex)
        {
            return $"<ExceptionLog> Machine '{machineId}' in state '{currStateName}' running action '{actionName}' chose to handle the exception '{ex.GetType().Name}'.";
        }

        /// <summary>
        /// Called when a monitor enters or exits a state.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor entering or exiting the state</param>
        /// <param name="monitorId">The ID of the monitor entering or exiting the state</param>
        /// <param name="stateName">The name of the state being entered or exited; if <paramref name="isInHotState"/>
        ///     is not null, then the temperature is appended to the statename in brackets, e.g. "stateName[hot]".</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        /// <param name="isInHotState">If true, the monitor is in a hot state; if false, the monitor is in a cold state;
        ///     else no liveness state is available.</param>
        public virtual void OnMonitorState(string monitorTypeName, MachineId monitorId, string stateName,
            bool isEntry, bool? isInHotState)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnMonitorStateString(monitorTypeName, monitorId, stateName, isEntry, isInHotState));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnMonitorState"/> event and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor entering or exiting the state</param>
        /// <param name="monitorId">The ID of the monitor entering or exiting the state</param>
        /// <param name="stateName">The name of the state being entered or exited; if <paramref name="isInHotState"/>
        ///     is not null, then the temperature is appended to the statename in brackets, e.g. "stateName[hot]".</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        /// <param name="isInHotState">If true, the monitor is in a hot state; if false, the monitor is in a cold state;
        ///     else no liveness state is available.</param>
        public virtual string FormatOnMonitorStateString(string monitorTypeName, MachineId monitorId, string stateName, bool isEntry, bool? isInHotState)
        {
            var liveness = isInHotState.HasValue ? (isInHotState.Value ? "'hot' " : "'cold' ") : string.Empty;
            var direction = isEntry ? "enters" : "exits";
            return $"<MonitorLog> Monitor '{monitorTypeName}' with id '{monitorId}' {direction} {liveness}state '{stateName}'.";
        }

        /// <summary>
        /// Called when a monitor is about to process or has raised an event.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that will process or has raised the event.</param>
        /// <param name="monitorId">ID of the monitor that will process or has raised the event</param>
        /// <param name="currStateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="isProcessing">If true, the monitor is processing the event; otherwise it has raised it.</param>
        public virtual void OnMonitorEvent(string monitorTypeName, MachineId monitorId, string currStateName,
            string eventName, bool isProcessing)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnMonitorEventString(monitorTypeName, monitorId, currStateName, eventName, isProcessing));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnMonitorEvent"/> event and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that will process or has raised the event.</param>
        /// <param name="monitorId">ID of the monitor that will process or has raised the event</param>
        /// <param name="currStateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="isProcessing">If true, the monitor is processing the event; otherwise it has raised it.</param>
        public virtual string FormatOnMonitorEventString(string monitorTypeName, MachineId monitorId, string currStateName, string eventName, bool isProcessing)
        {
            var activity = isProcessing ? "is processing" : "raised";
            return $"<MonitorLog> Monitor '{monitorTypeName}' with id '{monitorId}' in state '{currStateName}' {activity} event '{eventName}'.";
        }

        /// <summary>
        /// Called when a monitor executes an action.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that is executing the action.</param>
        /// <param name="monitorId">ID of the monitor that is executing the action</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public virtual void OnMonitorAction(string monitorTypeName, MachineId monitorId, string currStateName, string actionName)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnMonitorActionString(monitorTypeName, monitorId, currStateName, actionName));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnMonitorAction"/> event and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that is executing the action.</param>
        /// <param name="monitorId">ID of the monitor that is executing the action</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public virtual string FormatOnMonitorActionString(string monitorTypeName, MachineId monitorId, string currStateName, string actionName)
        {
            return $"<MonitorLog> Monitor '{monitorTypeName}' with id '{monitorId}' in state '{currStateName}' executed action '{actionName}'.";
        }

        /// <summary>
        /// Called for general error reporting via pre-constructed text.
        /// </summary>
        /// <param name="text">The text of the error report.</param>
        public virtual void OnError(string text)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnErrorString(text));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnError"/> event and its parameters.
        /// </summary>
        /// <param name="text">The text of the error report.</param>
        public virtual string FormatOnErrorString(string text)
        {
            // We do nothing here, but a subclass may override.
            return text;
        }

        /// <summary>
        /// Called for errors detected by a specific scheduling strategy.
        /// </summary>
        /// <param name="strategy">The scheduling strategy that was used.</param>
        /// <param name="strategyDescription">More information about the scheduling strategy.</param>
        public virtual void OnStrategyError(SchedulingStrategy strategy, string strategyDescription)
        {
            if (this.IsVerbose)
            {
                this.WriteLine(this.FormatOnStrategyErrorString(strategy, strategyDescription));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnStrategyError"/> event and its parameters.
        /// </summary>
        /// <param name="strategy">The scheduling strategy that was used.</param>
        /// <param name="strategyDescription">More information about the scheduling strategy.</param>
        public virtual string FormatOnStrategyErrorString(SchedulingStrategy strategy, string strategyDescription)
        {
            var desc = string.IsNullOrEmpty(strategyDescription) ? $" Description: {strategyDescription}" : string.Empty;
            return $"<StrategyLog> Found bug using '{strategy}' strategy.{desc}";
        }

        /// <summary>
        /// Disposes the logger.
        /// </summary>
        public abstract void Dispose();
    }
}
