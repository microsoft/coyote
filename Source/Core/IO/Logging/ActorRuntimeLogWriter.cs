// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Runtime.Exploration;

namespace Microsoft.Coyote.IO
{
    /// <summary>
    /// Default implementation of a log writer that logs runtime
    /// messages using the installed <see cref="ILogger"/>.
    /// </summary>
    public class ActorRuntimeLogWriter : IActorRuntimeLog, IDisposable
    {
        /// <summary>
        /// Used to log messages. To set a custom logger, use the runtime
        /// method <see cref="IActorRuntime.SetLogger(ILogger)"/>.
        /// </summary>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorRuntimeLogWriter"/> class
        /// with the default logger.
        /// </summary>
        public ActorRuntimeLogWriter()
        {
            this.Logger = new NulLogger();
        }

        /// <summary>
        /// Allows you to chain log writers.
        /// </summary>
        public IActorRuntimeLog Next { get; set; }

        /// <summary>
        /// Called when an actor has been created.
        /// </summary>
        /// <param name="id">The id of the actor that has been created.</param>
        /// <param name="creator">The id of the creator, null otherwise.</param>
        public virtual void OnCreateActor(ActorId id, ActorId creator)
        {
            this.Next?.OnCreateActor(id, creator);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnCreateActorLogMessage(id, creator));
            }
        }

        /// <summary>
        /// Called when an actor executes an action.
        /// </summary>
        /// <param name="id">The id of the actor executing the action.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public virtual void OnExecuteAction(ActorId id, string stateName, string actionName)
        {
            this.Next?.OnExecuteAction(id, stateName, actionName);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnExecuteActionLogMessage(id, stateName, actionName));
            }
        }

        /// <summary>
        /// Called when an event is sent to a target actor.
        /// </summary>
        /// <param name="targetActorId">The id of the target actor.</param>
        /// <param name="senderId">The id of the actor that sent the event, if any.</param>
        /// <param name="senderStateName">The state name, if the sender actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">The event being sent.</param>
        /// <param name="opGroupId">The id used to identify the send operation.</param>
        /// <param name="isTargetHalted">Is the target actor halted.</param>
        public virtual void OnSendEvent(ActorId targetActorId, ActorId senderId, string senderStateName, string eventName,
            Guid opGroupId, bool isTargetHalted)
        {
            this.Next?.OnSendEvent(targetActorId, senderId, senderStateName, eventName, opGroupId, isTargetHalted);

            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnSendLogMessage(targetActorId, senderId, senderStateName, eventName, opGroupId, isTargetHalted));
            }
        }

        /// <summary>
        /// Called when an actor raises an event.
        /// </summary>
        /// <param name="id">The id of the actor raising the event.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">The name of the event being raised.</param>
        public virtual void OnRaiseEvent(ActorId id, string stateName, string eventName)
        {
            this.Next?.OnRaiseEvent(id, stateName, eventName);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnRaiseEventLogMessage(id, stateName, eventName));
            }
        }

        /// <summary>
        /// Called when an event is about to be enqueued to an actor.
        /// </summary>
        /// <param name="id">The id of the actor that the event is being enqueued to.</param>
        /// <param name="eventName">Name of the event.</param>
        public virtual void OnEnqueueEvent(ActorId id, string eventName)
        {
            this.Next?.OnEnqueueEvent(id, eventName);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnEnqueueLogMessage(id, eventName));
            }
        }

        /// <summary>
        /// Called when an event is dequeued by an actor.
        /// </summary>
        /// <param name="id">The id of the actor that the event is being dequeued by.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">Name of the event.</param>
        public virtual void OnDequeueEvent(ActorId id, string stateName, string eventName)
        {
            this.Next?.OnDequeueEvent(id, stateName, eventName);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnDequeueLogMessage(id, stateName, eventName));
            }
        }

        /// <summary>
        /// Called when an event is received by an actor.
        /// </summary>
        /// <param name="id">The id of the actor that received the event.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="wasBlocked">The state machine was waiting for one or more specific events,
        /// and <paramref name="eventName"/> was one of them</param>
        public virtual void OnReceiveEvent(ActorId id, string stateName, string eventName, bool wasBlocked)
        {
            this.Next?.OnReceiveEvent(id, stateName, eventName, wasBlocked);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnReceiveLogMessage(id, stateName, eventName, wasBlocked));
            }
        }

        /// <summary>
        /// Called when an actor waits to receive an event of a specified type.
        /// </summary>
        /// <param name="id">The id of the actor that is entering the wait state.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventType">The type of the event being waited for.</param>
        public virtual void OnWaitEvent(ActorId id, string stateName, Type eventType)
        {
            this.Next?.OnWaitEvent(id, stateName, eventType);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnWaitEventLogMessage(id, stateName, eventType));
            }
        }

        /// <summary>
        /// Called when an actor waits to receive an event of one of the specified types.
        /// </summary>
        /// <param name="id">The id of the actor that is entering the wait state.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventTypes">The types of the events being waited for, if any.</param>
        public virtual void OnWaitEvent(ActorId id, string stateName, params Type[] eventTypes)
        {
            this.Next?.OnWaitEvent(id, stateName, eventTypes);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnWaitEventLogMessage(id, stateName, eventTypes));
            }
        }

        /// <summary>
        /// Called when a random result has been obtained.
        /// </summary>
        /// <param name="id">The id of the source actor, if any; otherwise, the runtime itself was the source.</param>
        /// <param name="result">The random result (may be bool or int).</param>
        public virtual void OnRandom(ActorId id, object result)
        {
            this.Next?.OnRandom(id, result);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnRandomLogMessage(id, result));
            }
        }

        /// <summary>
        /// Called when a state machine enters or exits a state.
        /// </summary>
        /// <param name="id">The id of the actor entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited.</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        public virtual void OnStateTransition(ActorId id, string stateName, bool isEntry)
        {
            this.Next?.OnStateTransition(id, stateName, isEntry);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnStateTransitionLogMessage(id, stateName, isEntry));
            }
        }

        /// <summary>
        /// Called when a state machine transitions states via a 'goto'.
        /// </summary>
        /// <param name="id">The id of the actor.</param>
        /// <param name="currStateName">The name of the current state.</param>
        /// <param name="newStateName">The target state of the transition.</param>
        public virtual void OnGotoState(ActorId id, string currStateName, string newStateName)
        {
            this.Next?.OnGotoState(id, currStateName, newStateName);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnGotoLogMessage(id, currStateName, newStateName));
            }
        }

        /// <summary>
        /// Called when a state machine is being pushed to a state.
        /// </summary>
        /// <param name="id">The id of the actor being pushed to the state.</param>
        /// <param name="currStateName">The name of the current state.</param>
        /// <param name="newStateName">The target state of the transition.</param>
        public virtual void OnPushState(ActorId id, string currStateName, string newStateName)
        {
            this.Next?.OnPushState(id, currStateName, newStateName);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnPushLogMessage(id, currStateName, newStateName));
            }
        }

        /// <summary>
        /// Called when a state machine has been popped from a state.
        /// </summary>
        /// <param name="id">The id of the actor that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state.</param>
        /// <param name="restoredStateName">The name of the state being re-entered, if any.</param>
        public virtual void OnPopState(ActorId id, string currStateName, string restoredStateName)
        {
            this.Next?.OnPopState(id, currStateName, restoredStateName);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnPopLogMessage(id, currStateName, restoredStateName));
            }
        }

        /// <summary>
        /// Called when an actor has been halted.
        /// </summary>
        /// <param name="id">The id of the actor that has been halted.</param>
        /// <param name="inboxSize">Approximate size of the inbox.</param>
        public virtual void OnHalt(ActorId id, int inboxSize)
        {
            this.Next?.OnHalt(id, inboxSize);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnHaltLogMessage(id, inboxSize));
            }
        }

        /// <summary>
        /// Called when an actor is idle (there is nothing to dequeue) and the default
        /// event handler is about to be executed.
        /// </summary>
        /// <param name="id">The id of the actor that the state will execute in.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        public virtual void OnDefaultEventHandler(ActorId id, string stateName)
        {
            this.Next?.OnDefaultEventHandler(id, stateName);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnDefaultEventHandlerLogMessage(id, stateName));
            }
        }

        /// <summary>
        /// Called when an actor handled a raised event.
        /// </summary>
        /// <param name="id">The id of the actor handling the event.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">The name of the event being handled.</param>
        public virtual void OnHandleRaisedEvent(ActorId id, string stateName, string eventName)
        {
            this.Next?.OnHandleRaisedEvent(id, stateName, eventName);
        }

        /// <summary>
        /// When an event cannot be handled in the current state, its exit handler is executed and then the state is
        /// popped and any previous "current state" is reentered. This handler is called when that pop has been done.
        /// </summary>
        /// <param name="id">The id of the actor that the pop executed in.</param>
        /// <param name="currStateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">The name of the event that cannot be handled.</param>
        public virtual void OnPopUnhandledEvent(ActorId id, string currStateName, string eventName)
        {
            this.Next?.OnPopUnhandledEvent(id, currStateName, eventName);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnPopUnhandledEventLogMessage(id, currStateName, eventName));
            }
        }

        /// <summary>
        /// Called when an actor throws an exception.
        /// </summary>
        /// <param name="id">The id of the actor that threw the exception.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        public virtual void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
        {
            this.Next?.OnExceptionThrown(id, stateName, actionName, ex);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnExceptionThrownLogMessage(id, stateName, actionName, ex));
            }
        }

        /// <summary>
        /// Called when an OnException method is used to handle a thrown exception.
        /// </summary>
        /// <param name="id">The id of the actor that threw the exception.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        public virtual void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
        {
            this.Next?.OnExceptionHandled(id, stateName, actionName, ex);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnExceptionHandledLogMessage(id, stateName, actionName, ex));
            }
        }

        /// <summary>
        /// Called when an actor timer has been created.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        public virtual void OnCreateTimer(TimerInfo info)
        {
            this.Next?.OnCreateTimer(info);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnCreateTimerLogMessage(info));
            }
        }

        /// <summary>
        /// Called when an actor timer has been stopped.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        public virtual void OnStopTimer(TimerInfo info)
        {
            this.Next?.OnStopTimer(info);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnStopTimerLogMessage(info));
            }
        }

        /// <summary>
        /// Called when a monitor has been created.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor that has been created.</param>
        /// <param name="id">The id of the monitor that has been created.</param>
        public virtual void OnCreateMonitor(string monitorTypeName, ActorId id)
        {
            this.Next?.OnCreateMonitor(monitorTypeName, id);

            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnCreateMonitorLogMessage(monitorTypeName, id));
            }
        }

        /// <summary>
        /// Called when a monitor executes an action.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that is executing the action.</param>
        /// <param name="id">The id of the monitor that is executing the action</param>
        /// <param name="stateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public virtual void OnMonitorExecuteAction(string monitorTypeName, ActorId id, string stateName, string actionName)
        {
            this.Next?.OnMonitorExecuteAction(monitorTypeName, id, stateName, actionName);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnMonitorExecuteActionLogMessage(monitorTypeName, id, stateName, actionName));
            }
        }

        /// <summary>
        /// Called when a monitor is about to process an event.
        /// </summary>
        /// <param name="senderId">The sender of the event.</param>
        /// <param name="senderStateName">The name of the state the sender is in.</param>
        /// <param name="monitorTypeName">Name of type of the monitor that will process the event.</param>
        /// <param name="id">The id of the monitor that will process the event.</param>
        /// <param name="stateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        public virtual void OnMonitorProcessEvent(ActorId senderId, string senderStateName, string monitorTypeName,
            ActorId id, string stateName, string eventName)
        {
            this.Next?.OnMonitorProcessEvent(senderId, senderStateName, monitorTypeName, id, stateName, eventName);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnMonitorProcessEventLogMessage(monitorTypeName, id, stateName, eventName));
            }
        }

        /// <summary>
        /// Called when a monitor raised an event.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor raising the event.</param>
        /// <param name="id">The id of the monitor raising the event.</param>
        /// <param name="stateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        public virtual void OnMonitorRaiseEvent(string monitorTypeName, ActorId id, string stateName, string eventName)
        {
            this.Next?.OnMonitorRaiseEvent(monitorTypeName, id, stateName, eventName);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnMonitorRaiseEventLogMessage(monitorTypeName, id, stateName, eventName));
            }
        }

        /// <summary>
        /// Called when a monitor enters or exits a state.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor entering or exiting the state</param>
        /// <param name="id">The id of the monitor entering or exiting the state</param>
        /// <param name="stateName">The name of the state being entered or exited; if <paramref name="isInHotState"/>
        /// is not null, then the temperature is appended to the statename in brackets, e.g. "stateName[hot]".</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        /// <param name="isInHotState">If true, the monitor is in a hot state; if false, the monitor is in a cold state;
        /// else no liveness state is available.</param>
        public virtual void OnMonitorStateTransition(string monitorTypeName, ActorId id, string stateName,
            bool isEntry, bool? isInHotState)
        {
            this.Next?.OnMonitorStateTransition(monitorTypeName, id, stateName, isEntry, isInHotState);

            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnMonitorStateTransitionLogMessage(monitorTypeName, id, stateName, isEntry, isInHotState));
            }
        }

        /// <summary>
        /// Called for general error reporting via pre-constructed text.
        /// </summary>
        /// <param name="text">The text of the error report.</param>
        public virtual void OnError(string text)
        {
            this.Next?.OnError(text);
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
            this.Next?.OnStrategyError(strategy, strategyDescription);
            if (this.Logger.IsVerbose)
            {
                this.Logger.WriteLine(this.FormatOnStrategyErrorLogMessage(strategy, strategyDescription));
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnCreateActor"/> log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the actor that has been created.</param>
        /// <param name="creator">The id of the creator, null otherwise.</param>
        protected virtual string FormatOnCreateActorLogMessage(ActorId id, ActorId creator)
        {
            var source = creator is null ? "the runtime" : $"'{creator.Name}'";
            return $"<CreateLog> '{id}' was created by {source}.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnExecuteAction"/> log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the actor executing the action.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        protected virtual string FormatOnExecuteActionLogMessage(ActorId id, string stateName, string actionName)
        {
            if (stateName is null)
            {
                return $"<ActionLog> '{id}' invoked action '{actionName}'.";
            }
            else
            {
                return $"<ActionLog> '{id}' invoked action '{actionName}' in state '{stateName}'.";
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnSendEvent"/> log message and its parameters.
        /// </summary>
        /// <param name="targetActorId">The id of the target actor.</param>
        /// <param name="senderId">The id of the actor that sent the event, if any.</param>
        /// <param name="senderStateName">The state name, if the sender actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">The event being sent.</param>
        /// <param name="opGroupId">The id used to identify the send operation.</param>
        /// <param name="isTargetHalted">Is the target actor halted.</param>
        protected virtual string FormatOnSendLogMessage(ActorId targetActorId, ActorId senderId, string senderStateName,
            string eventName, Guid opGroupId, bool isTargetHalted)
        {
            var opGroupIdMsg = opGroupId != Guid.Empty ? $" (operation group '{opGroupId}')" : string.Empty;
            var isHalted = isTargetHalted ? $" which has halted" : string.Empty;
            var sender = senderId != null ? $"'{senderId}' in state '{senderStateName}'" : $"The runtime";
            return $"<SendLog> {sender} sent event '{eventName}' to '{targetActorId}'{isHalted}{opGroupIdMsg}.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnRaiseEvent"/> log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the actor raising the event.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">The name of the event being raised.</param>
        protected virtual string FormatOnRaiseEventLogMessage(ActorId id, string stateName, string eventName)
        {
            if (stateName is null)
            {
                return $"<RaiseLog> '{id}' raised event '{eventName}'.";
            }
            else
            {
                return $"<RaiseLog> '{id}' raised event '{eventName}' in state '{stateName}'.";
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnEnqueueEvent"/> log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the actor that the event is being enqueued to.</param>
        /// <param name="eventName">Name of the event.</param>
        protected virtual string FormatOnEnqueueLogMessage(ActorId id, string eventName) =>
            $"<EnqueueLog> '{id}' enqueued event '{eventName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="OnDequeueEvent(ActorId, string, string)"/>
        /// log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the actor that the event is being dequeued by.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">Name of the event.</param>
        protected virtual string FormatOnDequeueLogMessage(ActorId id, string stateName, string eventName)
        {
            if (stateName is null)
            {
                return $"<DequeueLog> '{id}' dequeued event '{eventName}'.";
            }
            else
            {
                return $"<DequeueLog> '{id}' dequeued event '{eventName}' in state '{stateName}'.";
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnReceiveEvent(ActorId, string, string, bool)"/>
        /// log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the actor that received the event.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="wasBlocked">The actor was waiting for one or more specific events,
        /// and <paramref name="eventName"/> was one of them</param>
        protected virtual string FormatOnReceiveLogMessage(ActorId id, string stateName, string eventName, bool wasBlocked)
        {
            var unblocked = wasBlocked ? " and unblocked" : string.Empty;
            if (stateName is null)
            {
                return $"<ReceiveLog> '{id}' dequeued event '{eventName}'{unblocked}.";
            }
            else
            {
                return $"<ReceiveLog> '{id}' dequeued event '{eventName}'{unblocked} in state '{stateName}'.";
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnWaitEvent(ActorId, string, Type)"/>
        /// log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the actor that is entering the wait state.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventType">The type of the event being waited for.</param>
        protected virtual string FormatOnWaitEventLogMessage(ActorId id, string stateName, Type eventType)
        {
            if (stateName is null)
            {
                return $"<ReceiveLog> '{id}' is waiting to dequeue an event of type '{eventType.FullName}'.";
            }
            else
            {
                return $"<ReceiveLog> '{id}' is waiting to dequeue an event of type '{eventType.FullName}' in state '{stateName}'.";
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnWaitEvent(ActorId, string, Type[])"/>
        /// log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the actor that is entering the wait state.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventTypes">The types of the events being waited for, if any.</param>
        protected virtual string FormatOnWaitEventLogMessage(ActorId id, string stateName, params Type[] eventTypes)
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

            if (stateName is null)
            {
                return $"<ReceiveLog> '{id}' is waiting to dequeue an event of type {eventNames}.";
            }
            else
            {
                return $"<ReceiveLog> '{id}' is waiting to dequeue an event of type {eventNames} in state '{stateName}'.";
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnRandom"/> log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the source actor, if any; otherwise, the runtime itself was the source.</param>
        /// <param name="result">The random result (may be bool or int).</param>
        protected virtual string FormatOnRandomLogMessage(ActorId id, object result)
        {
            var source = id != null ? $"'{id}'" : "Runtime";
            return $"<RandomLog> {source} nondeterministically chose '{result}'.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnStateTransition"/> log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the state machine entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited.</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        protected virtual string FormatOnStateTransitionLogMessage(ActorId id, string stateName, bool isEntry)
        {
            var direction = isEntry ? "enters" : "exits";
            return $"<StateLog> '{id}' {direction} state '{stateName}'.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnGotoState"/> log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the actor.</param>
        /// <param name="currStateName">The name of the current state.</param>
        /// <param name="newStateName">The target state of the transition.</param>
        protected virtual string FormatOnGotoLogMessage(ActorId id, string currStateName, string newStateName) =>
            $"<GotoLog> '{id}' is transitioning from state '{currStateName}' to state '{newStateName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="OnPushState"/> log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the actor being pushed to the state.</param>
        /// <param name="currStateName">The name of the current state.</param>
        /// <param name="newStateName">The target state of the transition.</param>
        protected virtual string FormatOnPushLogMessage(ActorId id, string currStateName, string newStateName) =>
            $"<PushLog> '{id}' pushed from state '{currStateName}' to state '{newStateName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="OnPopState"/> log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the actor that the pop executed in.</param>
        /// <param name="currStateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="restoredStateName">The name of the state being re-entered, if any</param>
        protected virtual string FormatOnPopLogMessage(ActorId id, string currStateName, string restoredStateName)
        {
            currStateName = string.IsNullOrEmpty(currStateName) ? "[not recorded]" : currStateName;
            var reenteredStateName = restoredStateName ?? string.Empty;
            return $"<PopLog> '{id}' popped state '{currStateName}' and reentered state '{reenteredStateName}'.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnHalt"/> log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the actor that has been halted.</param>
        /// <param name="inboxSize">Approximate size of the inbox.</param>
        protected virtual string FormatOnHaltLogMessage(ActorId id, int inboxSize) =>
            $"<HaltLog> '{id}' halted with '{inboxSize}' events in its inbox.";

        /// <summary>
        /// Returns a string formatted for the <see cref="OnDefaultEventHandler"/> log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the actor that the state will execute in.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        protected virtual string FormatOnDefaultEventHandlerLogMessage(ActorId id, string stateName)
        {
            if (stateName is null)
            {
                return $"<DefaultLog> '{id}' is executing the default handler.";
            }
            else
            {
                return $"<DefaultLog> '{id}' is executing the default handler in state '{stateName}'.";
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnPopUnhandledEvent"/> log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the actor that the pop executed in.</param>
        /// <param name="currStateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">The name of the event that cannot be handled.</param>
        protected virtual string FormatOnPopUnhandledEventLogMessage(ActorId id, string currStateName, string eventName)
        {
            var reenteredStateName = string.IsNullOrEmpty(currStateName)
                ? string.Empty
                : $" and reentered state '{currStateName}";
            return $"<PopLog> '{id}' popped with unhandled event '{eventName}'{reenteredStateName}.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnExceptionThrown"/> log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the actor that threw the exception.</param>
        /// <param name="stateName">The name of the current state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        protected virtual string FormatOnExceptionThrownLogMessage(ActorId id, string stateName, string actionName, Exception ex)
        {
            if (stateName is null)
            {
                return $"<ExceptionLog> '{id}' running action '{actionName}' threw exception '{ex.GetType().Name}'.";
            }
            else
            {
                return $"<ExceptionLog> '{id}' running action '{actionName}' in state '{stateName}' threw exception '{ex.GetType().Name}'.";
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnExceptionHandled"/> log message and its parameters.
        /// </summary>
        /// <param name="id">The id of the actor that threw the exception.</param>
        /// <param name="stateName">The name of the current state.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        protected virtual string FormatOnExceptionHandledLogMessage(ActorId id, string stateName, string actionName, Exception ex)
        {
            if (stateName is null)
            {
                return $"<ExceptionLog> '{id}' running action '{actionName}' chose to handle exception '{ex.GetType().Name}'.";
            }
            else
            {
                return $"<ExceptionLog> '{id}' running action '{actionName}' in state '{stateName}' chose to handle exception '{ex.GetType().Name}'.";
            }
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnCreateTimer"/> log message and its parameters.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        protected virtual string FormatOnCreateTimerLogMessage(TimerInfo info)
        {
            var source = info.OwnerId is null ? "the runtime" : $"'{info.OwnerId.Name}'";
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
        /// Returns a string formatted for the <see cref="OnStopTimer"/> log message and its parameters.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        protected virtual string FormatOnStopTimerLogMessage(TimerInfo info)
        {
            var source = info.OwnerId is null ? "the runtime" : $"'{info.OwnerId.Name}'";
            return $"<TimerLog> Timer '{info}' was stopped and disposed by {source}.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnCreateMonitor"/> log message and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor that has been created.</param>
        /// <param name="id">The id of the monitor that has been created.</param>
        protected virtual string FormatOnCreateMonitorLogMessage(string monitorTypeName, ActorId id) =>
            $"<CreateLog> Monitor '{monitorTypeName}' with id '{id}' was created.";

        /// <summary>
        /// Returns a string formatted for the <see cref="OnMonitorExecuteAction"/> log message and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that is executing the action.</param>
        /// <param name="id">The id of the monitor that is executing the action</param>
        /// <param name="stateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        protected virtual string FormatOnMonitorExecuteActionLogMessage(string monitorTypeName, ActorId id,
            string stateName, string actionName) =>
            $"<MonitorLog> Monitor '{monitorTypeName}' with id '{id}' executed action '{actionName}' in state '{stateName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="OnMonitorProcessEvent"/> log message and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that will process the event.</param>
        /// <param name="id">The id of the monitor that will process the event.</param>
        /// <param name="stateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        protected virtual string FormatOnMonitorProcessEventLogMessage(string monitorTypeName, ActorId id,
            string stateName, string eventName) =>
            $"<MonitorLog> Monitor '{monitorTypeName}' with id '{id}' is processing event '{eventName}' in state '{stateName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="OnMonitorRaiseEvent"/> log message and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor raising the event.</param>
        /// <param name="id">The id of the monitor raising the event.</param>
        /// <param name="stateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        protected virtual string FormatOnMonitorRaiseEventLogMessage(string monitorTypeName, ActorId id,
            string stateName, string eventName) =>
            $"<MonitorLog> Monitor '{monitorTypeName}' with id '{id}' raised event '{eventName}' in state '{stateName}'.";

        /// <summary>
        /// Returns a string formatted for the <see cref="OnMonitorStateTransition"/> log message and its parameters.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor entering or exiting the state</param>
        /// <param name="id">The id of the monitor entering or exiting the state</param>
        /// <param name="stateName">The name of the state being entered or exited; if <paramref name="isInHotState"/>
        /// is not null, then the temperature is appended to the statename in brackets, e.g. "stateName[hot]".</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        /// <param name="isInHotState">If true, the monitor is in a hot state; if false, the monitor is in a cold state;
        /// else no liveness state is available.</param>
        protected virtual string FormatOnMonitorStateTransitionLogMessage(string monitorTypeName, ActorId id, string stateName,
            bool isEntry, bool? isInHotState)
        {
            var liveness = isInHotState.HasValue ? (isInHotState.Value ? "'hot' " : "'cold' ") : string.Empty;
            var direction = isEntry ? "enters" : "exits";
            return $"<MonitorLog> Monitor '{monitorTypeName}' with id '{id}' {direction} {liveness}state '{stateName}'.";
        }

        /// <summary>
        /// Returns a string formatted for the <see cref="OnError"/> log message and its parameters.
        /// </summary>
        /// <param name="text">The text of the error report.</param>
        protected virtual string FormatOnErrorLogMessage(string text) => text;

        /// <summary>
        /// Returns a string formatted for the <see cref="OnStrategyError"/> log message and its parameters.
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
