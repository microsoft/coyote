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
    /// Logger that disposes all written text.
    /// </summary>
    public sealed class DisposingLogger : ILogger
    {
        /// <summary>
        /// If true, then messages are logged. This logger ignores
        /// this value and always disposes any written text.
        /// </summary>
        public bool IsVerbose { get; set; } = false;

        /// <summary>
        /// Writes the specified string value.
        /// </summary>
        public void Write(string value)
        {
        }

        /// <summary>
        /// Writes the text representation of the specified argument.
        /// </summary>
        public void Write(string format, object arg0)
        {
        }

        /// <summary>
        /// Writes the text representation of the specified arguments.
        /// </summary>
        public void Write(string format, object arg0, object arg1)
        {
        }

        /// <summary>
        /// Writes the text representation of the specified arguments.
        /// </summary>
        public void Write(string format, object arg0, object arg1, object arg2)
        {
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public void Write(string format, params object[] args)
        {
        }

        /// <summary>
        /// Writes the specified string value, followed by the
        /// current line terminator.
        /// </summary>
        public void WriteLine(string value)
        {
        }

        /// <summary>
        /// Writes the text representation of the specified argument, followed by the
        /// current line terminator.
        /// </summary>
        public void WriteLine(string format, object arg0)
        {
        }

        /// <summary>
        /// Writes the text representation of the specified arguments, followed by the
        /// current line terminator.
        /// </summary>
        public void WriteLine(string format, object arg0, object arg1)
        {
        }

        /// <summary>
        /// Writes the text representation of the specified arguments, followed by the
        /// current line terminator.
        /// </summary>
        public void WriteLine(string format, object arg0, object arg1, object arg2)
        {
        }

        /// <summary>
        /// Writes the text representation of the specified array of objects,
        /// followed by the current line terminator.
        /// </summary>
        /// <param name="format">Text</param>
        /// <param name="args">Arguments</param>
        public void WriteLine(string format, params object[] args)
        {
        }

        /// <summary>
        /// Called when an event is about to be enqueued to a machine.
        /// </summary>
        /// <param name="machineId">Id of the machine that the event is being enqueued to.</param>
        /// <param name="eventName">Name of the event.</param>
        public void OnEnqueue(MachineId machineId, string eventName)
        {
        }

        /// <summary>
        /// Called when an event is dequeued by a machine.
        /// </summary>
        /// <param name="machineId">Id of the machine that the event is being dequeued by.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">Name of the event.</param>
        public void OnDequeue(MachineId machineId, string currStateName, string eventName)
        {
        }

        /// <summary>
        /// Called when the default event handler for a state is about to be executed.
        /// </summary>
        /// <param name="machineId">Id of the machine that the state will execute in.</param>
        /// <param name="currStateName">Name of the current state of the machine.</param>
        public void OnDefault(MachineId machineId, string currStateName)
        {
        }

        /// <summary>
        /// Called when a machine transitions states via a 'goto'.
        /// </summary>
        /// <param name="machineId">Id of the machine.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="newStateName">The target state of goto.</param>
        public void OnGoto(MachineId machineId, string currStateName, string newStateName)
        {
        }

        /// <summary>
        /// Called when a machine is being pushed to a state.
        /// </summary>
        /// <param name="machineId">Id of the machine being pushed to the state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="newStateName">The state the machine is pushed to.</param>
        public void OnPush(MachineId machineId, string currStateName, string newStateName)
        {
        }

        /// <summary>
        /// Called when a machine has been popped from a state.
        /// </summary>
        /// <param name="machineId">Id of the machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="restoredStateName">The name of the state being restored, if any.</param>
        public void OnPop(MachineId machineId, string currStateName, string restoredStateName)
        {
        }

        /// <summary>
        /// When an event cannot be handled in the current state, its exit handler is executed and then the state is
        /// popped and any previous "current state" is reentered. This handler is called when that pop has been done.
        /// </summary>
        /// <param name="machineId">Id of the machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">The name of the event that cannot be handled.</param>
        public void OnPopUnhandledEvent(MachineId machineId, string currStateName, string eventName)
        {
        }

        /// <summary>
        /// Called when an event is received by a machine.
        /// </summary>
        /// <param name="machineId">Id of the machine that received the event.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="wasBlocked">The machine was waiting for one or more specific events, and <paramref name="eventName"/> was one of them.</param>
        public void OnReceive(MachineId machineId, string currStateName, string eventName, bool wasBlocked)
        {
        }

        /// <summary>
        /// Called when a machine waits to receive an event of a specified type.
        /// </summary>
        /// <param name="machineId">Id of the machine that is entering the wait state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventType">The type of the event being waited for.</param>
        public void OnWait(MachineId machineId, string currStateName, Type eventType)
        {
        }

        /// <summary>
        /// Called when a machine waits to receive an event of one of the specified types.
        /// </summary>
        /// <param name="machineId">Id of the machine that is entering the wait state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventTypes">The types of the events being waited for, if any.</param>
        public void OnWait(MachineId machineId, string currStateName, params Type[] eventTypes)
        {
        }

        /// <summary>
        /// Called when an event is sent to a target machine.
        /// </summary>
        /// <param name="targetMachineId">Id of the target machine.</param>
        /// <param name="senderId">The machine that sent the event, if any.</param>
        /// <param name="senderStateName">The name of the current state of the sender machine, if any.</param>
        /// <param name="eventName">The event being sent.</param>
        /// <param name="opGroupId">Id used to identify the send operation.</param>
        /// <param name="isTargetHalted">Is the target machine halted.</param>
        public void OnSend(MachineId targetMachineId, MachineId senderId, string senderStateName, string eventName,
            Guid opGroupId, bool isTargetHalted)
        {
        }

        /// <summary>
        /// Called when a machine has been created.
        /// </summary>
        /// <param name="machineId">The id of the machine that has been created.</param>
        /// <param name="creator">Id of the creator machine, null otherwise.</param>
        public void OnCreateMachine(MachineId machineId, MachineId creator)
        {
        }

        /// <summary>
        /// Called when a monitor has been created.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor that has been created.</param>
        /// <param name="monitorId">The id of the monitor that has been created.</param>
        public void OnCreateMonitor(string monitorTypeName, MachineId monitorId)
        {
        }

        /// <summary>
        /// Called when a machine timer has been created.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        public void OnCreateTimer(TimerInfo info)
        {
        }

        /// <summary>
        /// Called when a machine timer has been stopped.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        public void OnStopTimer(TimerInfo info)
        {
        }

        /// <summary>
        /// Called when a machine has been halted.
        /// </summary>
        /// <param name="machineId">The id of the machine that has been halted.</param>
        /// <param name="inboxSize">Approximate size of the machine inbox.</param>
        public void OnHalt(MachineId machineId, int inboxSize)
        {
        }

        /// <summary>
        /// Called when a random result has been obtained.
        /// </summary>
        /// <param name="machineId">The id of the source machine, if any; otherwise, the runtime itself was the source.</param>
        /// <param name="result">The random result (may be bool or int).</param>
        public void OnRandom(MachineId machineId, object result)
        {
        }

        /// <summary>
        /// Called when a machine enters or exits a state.
        /// </summary>
        /// <param name="machineId">The id of the machine entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited.</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        public void OnMachineState(MachineId machineId, string stateName, bool isEntry)
        {
        }

        /// <summary>
        /// Called when a machine raises an event.
        /// </summary>
        /// <param name="machineId">The id of the machine raising the event.</param>
        /// <param name="currStateName">The name of the current state of the machine raising the event.</param>
        /// <param name="eventName">The name of the event being raised.</param>
        public void OnMachineEvent(MachineId machineId, string currStateName, string eventName)
        {
        }

        /// <summary>
        /// Called when a machine executes an action.
        /// </summary>
        /// <param name="machineId">The id of the machine executing the action.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public void OnMachineAction(MachineId machineId, string currStateName, string actionName)
        {
        }

        /// <summary>
        /// Called when a machine throws an exception
        /// </summary>
        /// <param name="machineId">The id of the machine that threw the exception.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        public void OnMachineExceptionThrown(MachineId machineId, string currStateName, string actionName, Exception ex)
        {
        }

        /// <summary>
        /// Called when a machine's OnException method is used to handle a thrown exception
        /// </summary>
        /// <param name="machineId">The id of the machine that threw the exception.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        public void OnMachineExceptionHandled(MachineId machineId, string currStateName, string actionName, Exception ex)
        {
        }

        /// <summary>
        /// Called when a monitor enters or exits a state.
        /// </summary>
        /// <param name="monitorTypeName">The name of the monitor entering or exiting the state.</param>
        /// <param name="monitorId">The id of the monitor entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited; if <paramref name="isInHotState"/>
        ///     is not null, then the temperature is appended to the statename in brackets, e.g. "stateName[hot]".</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        /// <param name="isInHotState">If true, the monitor is in a hot state; if false, the monitor is in a cold state;
        ///     else no liveness state is available.</param>
        public void OnMonitorState(string monitorTypeName, MachineId monitorId, string stateName, bool isEntry, bool? isInHotState)
        {
        }

        /// <summary>
        /// Called when a monitor is about to process or has raised an event.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that will process or has raised the event.</param>
        /// <param name="monitorId">Id of the monitor that will process or has raised the event.</param>
        /// <param name="currStateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="isProcessing">If true, the monitor is processing the event; otherwise it has raised it.</param>
        public void OnMonitorEvent(string monitorTypeName, MachineId monitorId, string currStateName, string eventName, bool isProcessing)
        {
        }

        /// <summary>
        /// Called when a monitor executes an action.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that is executing the action.</param>
        /// <param name="monitorId">Id of the monitor that is executing the action.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public void OnMonitorAction(string monitorTypeName, MachineId monitorId, string currStateName, string actionName)
        {
        }

        /// <summary>
        /// Called for general error reporting via pre-constructed text.
        /// </summary>
        /// <param name="text">The text of the error report.</param>
        public void OnError(string text)
        {
        }

        /// <summary>
        /// Called for errors detected by a specific scheduling strategy.
        /// </summary>
        /// <param name="strategy">The scheduling strategy that was used.</param>
        /// <param name="strategyDescription">More information about the scheduling strategy.</param>
        public void OnStrategyError(SchedulingStrategy strategy, string strategyDescription)
        {
        }

        /// <summary>
        /// Disposes the logger.
        /// </summary>
        public void Dispose()
        {
        }
    }
}
