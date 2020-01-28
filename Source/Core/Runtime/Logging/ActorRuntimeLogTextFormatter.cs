// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime.Exploration;

namespace Microsoft.Coyote.Runtime.Logging
{
    /// <summary>
    /// This class implements IActorRuntimeLog and generates output in a a human readable text format.
    /// </summary>
    public class ActorRuntimeLogTextFormatter : IActorRuntimeLog
    {
        /// <summary>
        /// Get or set the TextWriter to write to.
        /// </summary>
        public TextWriter Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActorRuntimeLogTextFormatter"/> class.
        /// </summary>
        public ActorRuntimeLogTextFormatter()
        {
            this.Logger = new ConsoleLogger();
        }

        /// <summary>
        /// Invoked when the specified assertion failure has occurred.
        /// </summary>
        /// <param name="error">The text of the error.</param>
        public virtual void OnAssertionFailure(string error)
        {
            this.Logger.WriteLine(error);
        }

        /// <summary>
        /// Invoked when the specified actor has been created.
        /// </summary>
        /// <param name="id">The id of the actor that has been created.</param>
        /// <param name="creator">The id of the creator, or null.</param>
        public virtual void OnCreateActor(ActorId id, ActorId creator)
        {
            var source = creator is null ? "the runtime" : $"'{creator.Name}'";
            var text = $"<CreateLog> '{id}' was created by {source}.";
            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified monitor has been created.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor that has been created.</param>
        /// <param name="id">The id of the monitor that has been created.</param>
        public virtual void OnCreateMonitor(string monitorTypeName, ActorId id)
        {
            var text = $"<CreateLog> Monitor '{monitorTypeName}' with id '{id}' was created.";
            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified actor timer has been created.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        public virtual void OnCreateTimer(TimerInfo info)
        {
            string text = null;
            var source = info.OwnerId is null ? "the runtime" : $"'{info.OwnerId.Name}'";
            if (info.Period.TotalMilliseconds >= 0)
            {
                text = $"<TimerLog> Timer '{info}' (due-time:{info.DueTime.TotalMilliseconds}ms; " +
                    $"period :{info.Period.TotalMilliseconds}ms) was created by {source}.";
            }
            else
            {
                text = $"<TimerLog> Timer '{info}' (due-time:{info.DueTime.TotalMilliseconds}ms) was created by {source}.";
            }

            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified actor is idle (there is nothing to dequeue) and the default
        /// event handler is about to be executed.
        /// </summary>
        /// <param name="id">The id of the actor that the state will execute in.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        public virtual void OnDefaultEventHandler(ActorId id, string stateName)
        {
            string text = null;
            if (stateName is null)
            {
                text = $"<DefaultLog> '{id}' is executing the default handler.";
            }
            else
            {
                text = $"<DefaultLog> '{id}' is executing the default handler in state '{stateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified event is dequeued by an actor.
        /// </summary>
        /// <param name="id">The id of the actor that the event is being dequeued by.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="e">The event being dequeued.</param>
        public virtual void OnDequeueEvent(ActorId id, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            string text = null;
            if (stateName is null)
            {
                text = $"<DequeueLog> '{id}' dequeued event '{eventName}'.";
            }
            else
            {
                text = $"<DequeueLog> '{id}' dequeued event '{eventName}' in state '{stateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified event is about to be enqueued to an actor.
        /// </summary>
        /// <param name="id">The id of the actor that the event is being enqueued to.</param>
        /// <param name="e">The event being enqueued.</param>
        public virtual void OnEnqueueEvent(ActorId id, Event e)
        {
            string eventName = e.GetType().FullName;
            string text = $"<EnqueueLog> '{id}' enqueued event '{eventName}'.";
            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified OnException method is used to handle a thrown exception.
        /// </summary>
        /// <param name="id">The id of the actor that threw the exception.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        public virtual void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
        {
            string text = null;
            if (stateName is null)
            {
                text = $"<ExceptionLog> '{id}' running action '{actionName}' chose to handle exception '{ex.GetType().Name}'.";
            }
            else
            {
                text = $"<ExceptionLog> '{id}' running action '{actionName}' in state '{stateName}' chose to handle exception '{ex.GetType().Name}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified actor throws an exception.
        /// </summary>
        /// <param name="id">The id of the actor that threw the exception.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        public virtual void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
        {
            string text = null;
            if (stateName is null)
            {
                text = $"<ExceptionLog> '{id}' running action '{actionName}' threw exception '{ex.GetType().Name}'.";
            }
            else
            {
                text = $"<ExceptionLog> '{id}' running action '{actionName}' in state '{stateName}' threw exception '{ex.GetType().Name}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified actor executes an action.
        /// </summary>
        /// <param name="id">The id of the actor executing the action.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public virtual void OnExecuteAction(ActorId id, string stateName, string actionName)
        {
            string text = null;
            if (stateName is null)
            {
                text = $"<ActionLog> '{id}' invoked action '{actionName}'.";
            }
            else
            {
                text = $"<ActionLog> '{id}' invoked action '{actionName}' in state '{stateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified state machine performs a goto transition to the specified state.
        /// </summary>
        /// <param name="id">The id of the actor.</param>
        /// <param name="currStateName">The name of the current state.</param>
        /// <param name="newStateName">The target state of the transition.</param>
        public virtual void OnGotoState(ActorId id, string currStateName, string newStateName)
        {
            string text = $"<GotoLog> '{id}' is transitioning from state '{currStateName}' to state '{newStateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified actor has been halted.
        /// </summary>
        /// <param name="id">The id of the actor that has been halted.</param>
        /// <param name="inboxSize">Approximate size of the inbox.</param>
        public virtual void OnHalt(ActorId id, int inboxSize)
        {
            string text = $"<HaltLog> '{id}' halted with '{inboxSize}' events in its inbox.";
            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified actor handled a raised event.
        /// </summary>
        /// <param name="id">The id of the actor handling the event.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="e">The event being handled.</param>
        public virtual void OnHandleRaisedEvent(ActorId id, string stateName, Event e)
        {
            // TODO: do we want to log raised events (they weren't before).
        }

        /// <summary>
        /// Invoked when the specified monitor executes an action.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that is executing the action.</param>
        /// <param name="id">The id of the monitor that is executing the action</param>
        /// <param name="stateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        public virtual void OnMonitorExecuteAction(string monitorTypeName, ActorId id, string stateName, string actionName)
        {
            string text = $"<MonitorLog> Monitor '{monitorTypeName}' with id '{id}' executed action '{actionName}' in state '{stateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified monitor is about to process an event.
        /// </summary>
        /// <param name="senderId">The sender of the event.</param>
        /// <param name="senderStateName">The name of the state the sender is in.</param>
        /// <param name="monitorTypeName">Name of type of the monitor that will process the event.</param>
        /// <param name="id">The id of the monitor that will process the event.</param>
        /// <param name="stateName">The name of the state in which the event is being raised.</param>
        /// <param name="e">The event being processed.</param>
        public virtual void OnMonitorProcessEvent(ActorId senderId, string senderStateName, string monitorTypeName, ActorId id, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            string text = $"<MonitorLog> Monitor '{monitorTypeName}' with id '{id}' is processing event '{eventName}' in state '{stateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified monitor raised an event.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor raising the event.</param>
        /// <param name="id">The id of the monitor raising the event.</param>
        /// <param name="stateName">The name of the state in which the event is being raised.</param>
        /// <param name="e">The event being raised.</param>
        public virtual void OnMonitorRaiseEvent(string monitorTypeName, ActorId id, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            string text = $"<MonitorLog> Monitor '{monitorTypeName}' with id '{id}' raised event '{eventName}' in state '{stateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified monitor enters or exits a state.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor entering or exiting the state</param>
        /// <param name="id">The id of the monitor entering or exiting the state</param>
        /// <param name="stateName">The name of the state being entered or exited; if <paramref name="isInHotState"/>
        /// is not null, then the temperature is appended to the statename in brackets, e.g. "stateName[hot]".</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        /// <param name="isInHotState">If true, the monitor is in a hot state; if false, the monitor is in a cold state;
        /// else no liveness state is available.</param>
        public virtual void OnMonitorStateTransition(string monitorTypeName, ActorId id, string stateName, bool isEntry, bool? isInHotState)
        {
            var liveness = isInHotState.HasValue ? (isInHotState.Value ? "'hot' " : "'cold' ") : string.Empty;
            var direction = isEntry ? "enters" : "exits";
            var text = $"<MonitorLog> Monitor '{monitorTypeName}' with id '{id}' {direction} {liveness}state '{stateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified state machine has popped its current state.
        /// </summary>
        /// <param name="id">The id of the actor that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state.</param>
        /// <param name="restoredStateName">The name of the state being re-entered, if any.</param>
        public virtual void OnPopState(ActorId id, string currStateName, string restoredStateName)
        {
            currStateName = string.IsNullOrEmpty(currStateName) ? "[not recorded]" : currStateName;
            var reenteredStateName = restoredStateName ?? string.Empty;
            var text = $"<PopLog> '{id}' popped state '{currStateName}' and reentered state '{reenteredStateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified event cannot be handled in the current state, its exit
        /// handler is executed and then the state is popped and any previous "current state"
        /// is reentered. This handler is called when that pop has been done.
        /// </summary>
        /// <param name="id">The id of the actor that the pop executed in.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="e">The event that cannot be handled.</param>
        public virtual void OnPopUnhandledEvent(ActorId id, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            var reenteredStateName = string.IsNullOrEmpty(stateName)
                ? string.Empty
                : $" and reentered state '{stateName}";
            var text = $"<PopLog> '{id}' popped with unhandled event '{eventName}'{reenteredStateName}.";
            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified state machine is being pushed to a state.
        /// </summary>
        /// <param name="id">The id of the actor being pushed to the state.</param>
        /// <param name="currStateName">The name of the current state.</param>
        /// <param name="newStateName">The target state of the transition.</param>
        public virtual void OnPushState(ActorId id, string currStateName, string newStateName)
        {
            string text = $"<PushLog> '{id}' pushed from state '{currStateName}' to state '{newStateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified state machine raises an event.
        /// </summary>
        /// <param name="id">The id of the actor raising the event.</param>
        /// <param name="stateName">The name of the current state.</param>
        /// <param name="e">The event being raised.</param>
        public virtual void OnRaiseEvent(ActorId id, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            string text = null;
            if (stateName is null)
            {
                text = $"<RaiseLog> '{id}' raised event '{eventName}'.";
            }
            else
            {
                text = $"<RaiseLog> '{id}' raised event '{eventName}' in state '{stateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified random result has been obtained.
        /// </summary>
        /// <param name="id">The id of the source actor, if any; otherwise, the runtime itself was the source.</param>
        /// <param name="result">The random result (may be bool or int).</param>
        public virtual void OnRandom(ActorId id, object result)
        {
            var source = id != null ? $"'{id}'" : "Runtime";
            var text = $"<RandomLog> {source} nondeterministically chose '{result}'.";
            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified event is received by an actor.
        /// </summary>
        /// <param name="id">The id of the actor that received the event.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="e">The event being received.</param>
        /// <param name="wasBlocked">The actor was waiting for one or more specific events,
        /// and <paramref name="e"/> was one of them.</param>
        public virtual void OnReceiveEvent(ActorId id, string stateName, Event e, bool wasBlocked)
        {
            string eventName = e.GetType().FullName;
            string text = null;
            var unblocked = wasBlocked ? " and unblocked" : string.Empty;
            if (stateName is null)
            {
                text = $"<ReceiveLog> '{id}' dequeued event '{eventName}'{unblocked}.";
            }
            else
            {
                text = $"<ReceiveLog> '{id}' dequeued event '{eventName}'{unblocked} in state '{stateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified event is sent to a target actor.
        /// </summary>
        /// <param name="targetActorId">The id of the target actor.</param>
        /// <param name="senderId">The id of the actor that sent the event, if any.</param>
        /// <param name="senderStateName">The state name, if the sender actor is a state machine and a state exists, else null.</param>
        /// <param name="e">The event being sent.</param>
        /// <param name="opGroupId">The id used to identify the send operation.</param>
        /// <param name="isTargetHalted">Is the target actor halted.</param>
        public virtual void OnSendEvent(ActorId targetActorId, ActorId senderId, string senderStateName, Event e, Guid opGroupId, bool isTargetHalted)
        {
            var opGroupIdMsg = opGroupId != Guid.Empty ? $" (operation group '{opGroupId}')" : string.Empty;
            var isHalted = isTargetHalted ? $" which has halted" : string.Empty;
            var sender = senderId != null ? $"'{senderId}' in state '{senderStateName}'" : $"The runtime";
            var eventName = e.GetType().FullName;
            var text = $"<SendLog> {sender} sent event '{eventName}' to '{targetActorId}'{isHalted}{opGroupIdMsg}.";
            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified state machine enters or exits a state.
        /// </summary>
        /// <param name="id">The id of the actor entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited.</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        public virtual void OnStateTransition(ActorId id, string stateName, bool isEntry)
        {
            var direction = isEntry ? "enters" : "exits";
            var text = $"<StateLog> '{id}' {direction} state '{stateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified actor timer has been stopped.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        public virtual void OnStopTimer(TimerInfo info)
        {
            var source = info.OwnerId is null ? "the runtime" : $"'{info.OwnerId.Name}'";
            var text = $"<TimerLog> Timer '{info}' was stopped and disposed by {source}.";
            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked to describe the specified scheduling strategy.
        /// </summary>
        /// <param name="strategy">The scheduling strategy that was used.</param>
        /// <param name="description">More information about the scheduling strategy.</param>
        public virtual void OnStrategyDescription(SchedulingStrategy strategy, string description)
        {
            var desc = string.IsNullOrEmpty(description) ? $" Description: {description}" : string.Empty;
            var text = $"<StrategyLog> Found bug using '{strategy}' strategy.{desc}";
            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified actor waits to receive an event of a specified type.
        /// </summary>
        /// <param name="id">The id of the actor that is entering the wait state.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventType">The type of the event being waited for.</param>
        public virtual void OnWaitEvent(ActorId id, string stateName, Type eventType)
        {
            string text = null;
            if (stateName is null)
            {
                text = $"<ReceiveLog> '{id}' is waiting to dequeue an event of type '{eventType.FullName}'.";
            }
            else
            {
                text = $"<ReceiveLog> '{id}' is waiting to dequeue an event of type '{eventType.FullName}' in state '{stateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when the specified actor waits to receive an event of one of the specified types.
        /// </summary>
        /// <param name="id">The id of the actor that is entering the wait state.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventTypes">The types of the events being waited for, if any.</param>
        public virtual void OnWaitEvent(ActorId id, string stateName, params Type[] eventTypes)
        {
            string text = null;
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
                text = $"<ReceiveLog> '{id}' is waiting to dequeue an event of type {eventNames}.";
            }
            else
            {
                text = $"<ReceiveLog> '{id}' is waiting to dequeue an event of type {eventNames} in state '{stateName}'.";
            }

            this.Logger.WriteLine(text);
        }

        /// <summary>
        /// Invoked when a log is complete (and is about to be closed).
        /// </summary>
        public virtual void OnCompleted()
        {
        }
    }
}
