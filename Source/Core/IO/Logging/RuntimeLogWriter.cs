// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Machines;
using Microsoft.Coyote.Machines.Timers;
using Microsoft.Coyote.Runtime.Exploration;

namespace Microsoft.Coyote.IO
{
    /// <summary>
    /// Default implementation of a log writer that logs runtime
    /// messages using the installed <see cref="ILogger"/>.
    /// </summary>
    public class RuntimeLogWriter : IDisposable
    {
        /// <summary>
        /// Used to log messages. To set a custom logger, use the runtime
        /// method <see cref="IMachineRuntime.SetLogger(ILogger)"/>.
        /// </summary>
        protected internal ILogger Logger { get; internal set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeLogWriter"/> class
        /// with the default logger.
        /// </summary>
        public RuntimeLogWriter()
        {
            this.Logger = new NulLogger();
        }

        /// <summary>
        /// Called when an event is about to be enqueued to a machine.
        /// </summary>
        /// <param name="actorId">Id of the machine that the event is being enqueued to.</param>
        /// <param name="eventName">Name of the event.</param>
        public virtual void OnEnqueue(ActorId actorId, string eventName)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnEnqueueLogMessage(actorId, eventName));
            }
        }

        /// <summary>
        /// Called when an event is dequeued by a machine.
        /// </summary>
        /// <param name="actorId">Id of the machine that the event is being dequeued by.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">Name of the event.</param>
        public virtual void OnDequeue(ActorId actorId, string currStateName, string eventName)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnDequeueLogMessage(actorId, currStateName, eventName));
            }
        }

        /// <summary>
        /// Called when the default event handler for a state is about to be executed.
        /// </summary>
        /// <param name="actorId">Id of the machine that the state will execute in.</param>
        /// <param name="currStateName">Name of the current state of the machine.</param>
        public virtual void OnDefault(ActorId actorId, string currStateName)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnDefaultLogMessage(actorId, currStateName));
            }
        }

        /// <summary>
        /// Called when a machine transitions states via a 'goto'.
        /// </summary>
        /// <param name="actorId">Id of the machine.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="newStateName">The target state of goto.</param>
        public virtual void OnGoto(ActorId actorId, string currStateName, string newStateName)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnGotoLogMessage(actorId, currStateName, newStateName));
            }
        }

        /// <summary>
        /// Called when a machine is being pushed to a state.
        /// </summary>
        /// <param name="actorId">Id of the machine being pushed to the state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="newStateName">The state the machine is pushed to.</param>
        public virtual void OnPush(ActorId actorId, string currStateName, string newStateName)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnPushLogMessage(actorId, currStateName, newStateName));
            }
        }

        /// <summary>
        /// Called when a machine has been popped from a state.
        /// </summary>
        /// <param name="actorId">Id of the machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="restoredStateName">The name of the state being re-entered, if any</param>
        public virtual void OnPop(ActorId actorId, string currStateName, string restoredStateName)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnPopLogMessage(actorId, currStateName, restoredStateName));
            }
        }

        /// <summary>
        /// When an event cannot be handled in the current state, its exit handler is executed and then the state is
        /// popped and any previous "current state" is reentered. This handler is called when that pop has been done.
        /// </summary>
        /// <param name="actorId">Id of the machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">The name of the event that cannot be handled.</param>
        public virtual void OnPopUnhandledEvent(ActorId actorId, string currStateName, string eventName)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnPopUnhandledEventLogMessage(actorId, currStateName, eventName));
            }
        }

        /// <summary>
        /// Called when an event is received by a machine.
        /// </summary>
        /// <param name="actorId">Id of the machine that received the event.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="wasBlocked">The machine was waiting for one or more specific events,
        ///     and <paramref name="eventName"/> was one of them</param>
        public virtual void OnReceive(ActorId actorId, string currStateName, string eventName, bool wasBlocked)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnReceiveLogMessage(actorId, currStateName, eventName, wasBlocked));
            }
        }

        /// <summary>
        /// Called when a machine waits to receive an event of a specified type.
        /// </summary>
        /// <param name="actorId">Id of the machine that is entering the wait state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventType">The type of the event being waited for.</param>
        public virtual void OnWait(ActorId actorId, string currStateName, Type eventType)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnWaitLogMessage(actorId, currStateName, eventType));
            }
        }

        /// <summary>
        /// Called when a machine waits to receive an event of one of the specified types.
        /// </summary>
        /// <param name="actorId">Id of the machine that is entering the wait state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventTypes">The types of the events being waited for, if any.</param>
        public virtual void OnWait(ActorId actorId, string currStateName, params Type[] eventTypes)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnWaitLogMessage(actorId, currStateName, eventTypes));
            }
        }

        /// <summary>
        /// Called when an event is sent to a target machine.
        /// </summary>
        /// <param name="targetActorId">Id of the target machine.</param>
        /// <param name="senderId">The id of the machine that sent the event, if any.</param>
        /// <param name="senderStateName">The name of the current state of the sender machine, if any.</param>
        /// <param name="eventName">The event being sent.</param>
        /// <param name="opGroupId">Id used to identify the send operation.</param>
        /// <param name="isTargetHalted">Is the target machine halted.</param>
        public virtual void OnSend(ActorId targetActorId, ActorId senderId, string senderStateName, string eventName,
            Guid opGroupId, bool isTargetHalted)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnSendLogMessage(targetActorId, senderId, senderStateName, eventName, opGroupId, isTargetHalted));
            }
        }

        /// <summary>
        /// Called when a machine has been created.
        /// </summary>
        /// <param name="actorId">The id of the machine that has been created.</param>
        /// <param name="creator">Id of the creator machine, null otherwise.</param>
        public virtual void OnCreateMachine(ActorId actorId, ActorId creator)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnCreateMachineLogMessage(actorId, creator));
            }
        }

        /// <summary>
        /// Called when a monitor has been created.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor that has been created.</param>
        /// <param name="monitorId">The id of the monitor that has been created.</param>
        public virtual void OnCreateMonitor(string monitorTypeName, ActorId monitorId)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnCreateMonitorLogMessage(monitorTypeName, monitorId));
            }
        }

        /// <summary>
        /// Called when a machine timer has been created.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        public virtual void OnCreateTimer(TimerInfo info)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnCreateTimerLogMessage(info));
            }
        }

        /// <summary>
        /// Called when a machine timer has been stopped.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        public virtual void OnStopTimer(TimerInfo info)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnStopTimerLogMessage(info));
            }
        }

        /// <summary>
        /// Called when a machine has been halted.
        /// </summary>
        /// <param name="actorId">The id of the machine that has been halted.</param>
        /// <param name="inboxSize">Approximate size of the machine inbox.</param>
        public virtual void OnHalt(ActorId actorId, int inboxSize)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnHaltLogMessage(actorId, inboxSize));
            }
        }

        /// <summary>
        /// Called when a random result has been obtained.
        /// </summary>
        /// <param name="actorId">The id of the source machine, if any; otherwise, the runtime itself was the source.</param>
        /// <param name="result">The random result (may be bool or int).</param>
        public virtual void OnRandom(ActorId actorId, object result)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnRandomLogMessage(actorId, result));
            }
        }

        /// <summary>
        /// Called when a machine enters or exits a state.
        /// </summary>
        /// <param name="actorId">The id of the machine entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited.</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        public virtual void OnMachineState(ActorId actorId, string stateName, bool isEntry)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnMachineStateLogMessage(actorId, stateName, isEntry));
            }
        }

        /// <summary>
        /// Called when a machine raises an event.
        /// </summary>
        /// <param name="actorId">The id of the machine raising the event.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="eventName">The name of the event being raised.</param>
        public virtual void OnMachineEvent(ActorId actorId, string currStateName, string eventName)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnMachineEventLogMessage(actorId, currStateName, eventName));
            }
        }

        /// <summary>
        /// Called when a machine executes an action.
        /// </summary>
        /// <param name="actorId">The id of the machine executing the action.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public virtual void OnMachineAction(ActorId actorId, string currStateName, string actionName)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnMachineActionLogMessage(actorId, currStateName, actionName));
            }
        }

        /// <summary>
        /// Called when a machine throws an exception
        /// </summary>
        /// <param name="actorId">The id of the machine that threw the exception.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        public virtual void OnMachineExceptionThrown(ActorId actorId, string currStateName, string actionName, Exception ex)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnMachineExceptionThrownLogMessage(actorId, currStateName, actionName, ex));
            }
        }

        /// <summary>
        /// Called when a machine's OnException method is used to handle a thrown exception
        /// </summary>
        /// <param name="actorId">The id of the machine that threw the exception.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        public virtual void OnMachineExceptionHandled(ActorId actorId, string currStateName, string actionName, Exception ex)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnMachineExceptionHandledLogMessage(actorId, currStateName, actionName, ex));
            }
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
        public virtual void OnMonitorState(string monitorTypeName, ActorId monitorId, string stateName,
            bool isEntry, bool? isInHotState)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnMonitorStateLogMessage(monitorTypeName, monitorId, stateName, isEntry, isInHotState));
            }
        }

        /// <summary>
        /// Called when a monitor is about to process or has raised an event.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that will process or has raised the event.</param>
        /// <param name="monitorId">ID of the monitor that will process or has raised the event</param>
        /// <param name="currStateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="isProcessing">If true, the monitor is processing the event; otherwise it has raised it.</param>
        public virtual void OnMonitorEvent(string monitorTypeName, ActorId monitorId, string currStateName,
            string eventName, bool isProcessing)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnMonitorEventLogMessage(monitorTypeName, monitorId, currStateName, eventName, isProcessing));
            }
        }

        /// <summary>
        /// Called when a monitor executes an action.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that is executing the action.</param>
        /// <param name="monitorId">ID of the monitor that is executing the action</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public virtual void OnMonitorAction(string monitorTypeName, ActorId monitorId, string currStateName, string actionName)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnMonitorActionLogMessage(monitorTypeName, monitorId, currStateName, actionName));
            }
        }

        /// <summary>
        /// Called for general error reporting via pre-constructed text.
        /// </summary>
        /// <param name="text">The text of the error report.</param>
        public virtual void OnError(string text)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnErrorLogMessage(text));
            }
        }

        /// <summary>
        /// Called for errors detected by a specific scheduling strategy.
        /// </summary>
        /// <param name="strategy">The scheduling strategy that was used.</param>
        /// <param name="strategyDescription">More information about the scheduling strategy.</param>
        public virtual void OnStrategyError(SchedulingStrategy strategy, string strategyDescription)
        {
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnStrategyErrorLogMessage(strategy, strategyDescription));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnEnqueue"/> log message and its parameters.
        /// </summary>
        /// <param name="actorId">Id of the machine that the event is being enqueued to.</param>
        /// <param name="eventName">Name of the event.</param>
        protected virtual string FormatOnEnqueueLogMessage(ActorId actorId, string eventName) =>
            $"<EnqueueLog> Machine '{actorId}' enqueued event '{eventName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnDequeue"/> log message and its parameters.
        /// </summary>
        /// <param name="actorId">Id of the machine that the event is being dequeued by.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">Name of the event.</param>
        protected virtual string FormatOnDequeueLogMessage(ActorId actorId, string currStateName, string eventName) =>
            $"<DequeueLog> Machine '{actorId}' in state '{currStateName}' dequeued event '{eventName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnDefault"/> log message and its parameters.
        /// </summary>
        /// <param name="actorId">Id of the machine that the state will execute in.</param>
        /// <param name="currStateName">Name of the current state of the machine.</param>
        protected virtual string FormatOnDefaultLogMessage(ActorId actorId, string currStateName) =>
            $"<DefaultLog> Machine '{actorId}' in state '{currStateName}' is executing the default handler.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnGoto"/> log message and its parameters.
        /// </summary>
        /// <param name="actorId">Id of the machine.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="newStateName">The target state of goto.</param>
        protected virtual string FormatOnGotoLogMessage(ActorId actorId, string currStateName, string newStateName) =>
            $"<GotoLog> Machine '{actorId}' is transitioning from state '{currStateName}' to state '{newStateName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnPush"/> log message and its parameters.
        /// </summary>
        /// <param name="actorId">Id of the machine being pushed to the state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="newStateName">The state the machine is pushed to.</param>
        protected virtual string FormatOnPushLogMessage(ActorId actorId, string currStateName, string newStateName) =>
            $"<PushLog> Machine '{actorId}' pushed from state '{currStateName}' to state '{newStateName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnPop"/> log message and its parameters.
        /// </summary>
        /// <param name="actorId">Id of the machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="restoredStateName">The name of the state being re-entered, if any</param>
        protected virtual string FormatOnPopLogMessage(ActorId actorId, string currStateName, string restoredStateName)
        {
            currStateName = string.IsNullOrEmpty(currStateName) ? "[not recorded]" : currStateName;
            var reenteredStateName = restoredStateName ?? string.Empty;
            return $"<PopLog> Machine '{actorId}' popped state '{currStateName}' and reentered state '{reenteredStateName}'.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnPopUnhandledEvent"/> log message and its parameters.
        /// </summary>
        /// <param name="actorId">Id of the machine that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">The name of the event that cannot be handled.</param>
        protected virtual string FormatOnPopUnhandledEventLogMessage(ActorId actorId, string currStateName, string eventName)
        {
            var reenteredStateName = string.IsNullOrEmpty(currStateName)
                ? string.Empty
                : $" and reentered state '{currStateName}";
            return $"<PopLog> Machine '{actorId}' popped with unhandled event '{eventName}'{reenteredStateName}.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnReceive"/> log message and its parameters.
        /// </summary>
        /// <param name="actorId">Id of the machine that received the event.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="wasBlocked">The machine was waiting for one or more specific events,
        ///     and <paramref name="eventName"/> was one of them</param>
        protected virtual string FormatOnReceiveLogMessage(ActorId actorId, string currStateName, string eventName, bool wasBlocked)
        {
            var unblocked = wasBlocked ? " and unblocked" : string.Empty;
            return $"<ReceiveLog> Machine '{actorId}' in state '{currStateName}' dequeued event '{eventName}'{unblocked}.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnWait(ActorId, string, Type)"/> log message and its parameters.
        /// </summary>
        /// <param name="actorId">Id of the machine that is entering the wait state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventType">The type of the event being waited for.</param>
        protected virtual string FormatOnWaitLogMessage(ActorId actorId, string currStateName, Type eventType) =>
            $"<ReceiveLog> Machine '{actorId}' in state '{currStateName}' is waiting to dequeue an event of type '{eventType.FullName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnWait(ActorId, string, Type[])"/> log message and its parameters.
        /// </summary>
        /// <param name="actorId">Id of the machine that is entering the wait state.</param>
        /// <param name="currStateName">The name of the current state of the machine, if any.</param>
        /// <param name="eventTypes">The types of the events being waited for, if any.</param>
        protected virtual string FormatOnWaitLogMessage(ActorId actorId, string currStateName, params Type[] eventTypes)
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

            return $"<ReceiveLog> Machine '{actorId}' in state '{currStateName}' is waiting to dequeue an event of type {eventNames}.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnSend"/> log message and its parameters.
        /// </summary>
        /// <param name="targetActorId">Id of the target machine.</param>
        /// <param name="senderId">The id of the machine that sent the event, if any.</param>
        /// <param name="senderStateName">The name of the current state of the sender machine, if any.</param>
        /// <param name="eventName">The event being sent.</param>
        /// <param name="opGroupId">Id used to identify the send operation.</param>
        /// <param name="isTargetHalted">Is the target machine halted.</param>
        protected virtual string FormatOnSendLogMessage(ActorId targetActorId, ActorId senderId, string senderStateName,
            string eventName, Guid opGroupId, bool isTargetHalted)
        {
            var opGroupIdMsg = opGroupId != Guid.Empty ? $" (operation group '{opGroupId}')" : string.Empty;
            var target = isTargetHalted ? $"halted machine '{targetActorId}'" : $"machine '{targetActorId}'";
            var sender = senderId != null ? $"Machine '{senderId}' in state '{senderStateName}'" : $"The runtime";
            return $"<SendLog> {sender} sent event '{eventName}' to {target}{opGroupIdMsg}.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnCreateMachine"/> log message and its parameters.
        /// </summary>
        /// <param name="actorId">The id of the machine that has been created.</param>
        /// <param name="creator">Id of the creator machine, null otherwise.</param>
        protected virtual string FormatOnCreateMachineLogMessage(ActorId actorId, ActorId creator)
        {
            var source = creator is null ? "the runtime" : $"machine '{creator.Name}'";
            return $"<CreateLog> Machine '{actorId}' was created by {source}.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnCreateMonitor"/> log message and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor that has been created.</param>
        /// <param name="monitorId">The id of the monitor that has been created.</param>
        protected virtual string FormatOnCreateMonitorLogMessage(string monitorTypeName, ActorId monitorId) =>
            $"<CreateLog> Monitor '{monitorTypeName}' with id '{monitorId}' was created.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnCreateTimer"/> log message and its parameters.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        protected virtual string FormatOnCreateTimerLogMessage(TimerInfo info)
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
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnStopTimer"/> log message and its parameters.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        protected virtual string FormatOnStopTimerLogMessage(TimerInfo info)
        {
            var source = info.OwnerId is null ? "the runtime" : $"machine '{info.OwnerId.Name}'";
            return $"<TimerLog> Timer '{info}' was stopped and disposed by {source}.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnHalt"/> log message and its parameters.
        /// </summary>
        /// <param name="actorId">The id of the machine that has been halted.</param>
        /// <param name="inboxSize">Approximate size of the machine inbox.</param>
        protected virtual string FormatOnHaltLogMessage(ActorId actorId, int inboxSize) =>
            $"<HaltLog> Machine '{actorId}' halted with '{inboxSize}' events in its inbox.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnRandom"/> log message and its parameters.
        /// </summary>
        /// <param name="actorId">The id of the source machine, if any; otherwise, the runtime itself was the source.</param>
        /// <param name="result">The random result (may be bool or int).</param>
        protected virtual string FormatOnRandomLogMessage(ActorId actorId, object result)
        {
            var source = actorId != null ? $"Machine '{actorId}'" : "Runtime";
            return $"<RandomLog> {source} nondeterministically chose '{result}'.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnMachineState"/> log message and its parameters.
        /// </summary>
        /// <param name="actorId">The id of the machine entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited.</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        protected virtual string FormatOnMachineStateLogMessage(ActorId actorId, string stateName, bool isEntry)
        {
            var direction = isEntry ? "enters" : "exits";
            return $"<StateLog> Machine '{actorId}' {direction} state '{stateName}'.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnMachineEvent"/> log message and its parameters.
        /// </summary>
        /// <param name="actorId">The id of the machine raising the event.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="eventName">The name of the event being raised.</param>
        protected virtual string FormatOnMachineEventLogMessage(ActorId actorId, string currStateName, string eventName) =>
            $"<RaiseLog> Machine '{actorId}' in state '{currStateName}' raised event '{eventName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnMachineAction"/> log message and its parameters.
        /// </summary>
        /// <param name="actorId">The id of the machine executing the action.</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        protected virtual string FormatOnMachineActionLogMessage(ActorId actorId, string currStateName, string actionName) =>
            $"<ActionLog> Machine '{actorId}' in state '{currStateName}' invoked action '{actionName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnMachineExceptionThrown"/> log message and its parameters.
        /// </summary>
        /// <param name="actorId">The id of the machine that threw the exception.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        protected virtual string FormatOnMachineExceptionThrownLogMessage(ActorId actorId, string currStateName, string actionName, Exception ex) =>
            $"<ExceptionLog> Machine '{actorId}' in state '{currStateName}' running action '{actionName}' threw an exception '{ex.GetType().Name}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnMachineExceptionHandled"/> log message and its parameters.
        /// </summary>
        /// <param name="actorId">The id of the machine that threw the exception.</param>
        /// <param name="currStateName">The name of the current machine state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        protected virtual string FormatOnMachineExceptionHandledLogMessage(ActorId actorId, string currStateName, string actionName, Exception ex) =>
            $"<ExceptionLog> Machine '{actorId}' in state '{currStateName}' running action '{actionName}' chose to handle the exception '{ex.GetType().Name}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnMonitorState"/> log message and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor entering or exiting the state</param>
        /// <param name="monitorId">The ID of the monitor entering or exiting the state</param>
        /// <param name="stateName">The name of the state being entered or exited; if <paramref name="isInHotState"/>
        ///     is not null, then the temperature is appended to the statename in brackets, e.g. "stateName[hot]".</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        /// <param name="isInHotState">If true, the monitor is in a hot state; if false, the monitor is in a cold state;
        ///     else no liveness state is available.</param>
        protected virtual string FormatOnMonitorStateLogMessage(string monitorTypeName, ActorId monitorId, string stateName, bool isEntry, bool? isInHotState)
        {
            var liveness = isInHotState.HasValue ? (isInHotState.Value ? "'hot' " : "'cold' ") : string.Empty;
            var direction = isEntry ? "enters" : "exits";
            return $"<MonitorLog> Monitor '{monitorTypeName}' with id '{monitorId}' {direction} {liveness}state '{stateName}'.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnMonitorEvent"/> log message and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that will process or has raised the event.</param>
        /// <param name="monitorId">ID of the monitor that will process or has raised the event</param>
        /// <param name="currStateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="isProcessing">If true, the monitor is processing the event; otherwise it has raised it.</param>
        protected virtual string FormatOnMonitorEventLogMessage(string monitorTypeName, ActorId monitorId, string currStateName, string eventName, bool isProcessing)
        {
            var activity = isProcessing ? "is processing" : "raised";
            return $"<MonitorLog> Monitor '{monitorTypeName}' with id '{monitorId}' in state '{currStateName}' {activity} event '{eventName}'.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnMonitorAction"/> log message and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that is executing the action.</param>
        /// <param name="monitorId">ID of the monitor that is executing the action</param>
        /// <param name="currStateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        protected virtual string FormatOnMonitorActionLogMessage(string monitorTypeName, ActorId monitorId, string currStateName, string actionName) =>
            $"<MonitorLog> Monitor '{monitorTypeName}' with id '{monitorId}' in state '{currStateName}' executed action '{actionName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnError"/> log message and its parameters.
        /// </summary>
        /// <param name="text">The text of the error report.</param>
        protected virtual string FormatOnErrorLogMessage(string text) => text;

        /// <summary>
        /// Returns a string formatted for the <see cref="RuntimeLogWriter.OnStrategyError"/> log message and its parameters.
        /// </summary>
        /// <param name="strategy">The scheduling strategy that was used.</param>
        /// <param name="strategyDescription">More information about the scheduling strategy.</param>
        protected virtual string FormatOnStrategyErrorLogMessage(SchedulingStrategy strategy, string strategyDescription)
        {
            var desc = string.IsNullOrEmpty(strategyDescription) ? $" Description: {strategyDescription}" : string.Empty;
            return $"<StrategyLog> Found bug using '{strategy}' strategy.{desc}";
        }

        /// <summary>
        /// Disposes the log writer.
        /// </summary>
        public virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        /// Disposes the log writer.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
