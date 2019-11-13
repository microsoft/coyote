// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Runtime.Exploration;

namespace Microsoft.Coyote.IO
{
    /// <summary>
    /// This interface makes it possible for an external module to track
    /// what is happening in the actor runtime.
    /// </summary>
    public interface IActorRuntimeLog
    {
        /// <summary>
        /// Get or set the underlying logging object.
        /// </summary>
        ILogger Logger { get; set; }

        /// <summary>
        /// Allows you to chain log writers.
        /// </summary>
        IActorRuntimeLog Next { get; set; }

        /// <summary>
        /// Called when an actor has been created.
        /// </summary>
        /// <param name="id">The id of the actor that has been created.</param>
        /// <param name="creator">The id of the creator, or null.</param>
        void OnCreateActor(ActorId id, ActorId creator);

        /// <summary>
        /// Called when an actor executes an action.
        /// </summary>
        /// <param name="id">The id of the actor executing the action.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        void OnExecuteAction(ActorId id, string stateName, string actionName);

        /// <summary>
        /// Called when an event is sent to a target actor.
        /// </summary>
        /// <param name="targetActorId">The id of the target actor.</param>
        /// <param name="senderId">The id of the actor that sent the event, if any.</param>
        /// <param name="senderStateName">The state name, if the sender actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">The event being sent.</param>
        /// <param name="opGroupId">The id used to identify the send operation.</param>
        /// <param name="isTargetHalted">Is the target actor halted.</param>
        void OnSendEvent(ActorId targetActorId, ActorId senderId, string senderStateName, string eventName,
            Guid opGroupId, bool isTargetHalted);

        /// <summary>
        /// Called when an actor raises an event.
        /// </summary>
        /// <param name="id">The id of the actor raising the event.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">The name of the event being raised.</param>
        void OnRaiseEvent(ActorId id, string stateName, string eventName);

        /// <summary>
        /// Called when an event is about to be enqueued to an actor.
        /// </summary>
        /// <param name="id">The id of the actor that the event is being enqueued to.</param>
        /// <param name="eventName">Name of the event.</param>
        void OnEnqueueEvent(ActorId id, string eventName);

        /// <summary>
        /// Called when an event is dequeued by an actor.
        /// </summary>
        /// <param name="id">The id of the actor that the event is being dequeued by.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">Name of the event.</param>
        void OnDequeueEvent(ActorId id, string stateName, string eventName);

        /// <summary>
        /// Called when an event is received by an actor.
        /// </summary>
        /// <param name="id">The id of the actor that received the event.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">The name of the event.</param>
        /// <param name="wasBlocked">The actor was waiting for one or more specific events,
        /// and <paramref name="eventName"/> was one of them</param>
        void OnReceiveEvent(ActorId id, string stateName, string eventName, bool wasBlocked);

        /// <summary>
        /// Called when an actor waits to receive an event of a specified type.
        /// </summary>
        /// <param name="id">The id of the actor that is entering the wait state.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventType">The type of the event being waited for.</param>
        void OnWaitEvent(ActorId id, string stateName, Type eventType);

        /// <summary>
        /// Called when an actor waits to receive an event of one of the specified types.
        /// </summary>
        /// <param name="id">The id of the actor that is entering the wait state.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventTypes">The types of the events being waited for, if any.</param>
        void OnWaitEvent(ActorId id, string stateName, params Type[] eventTypes);

        /// <summary>
        /// Called when a random result has been obtained.
        /// </summary>
        /// <param name="id">The id of the source actor, if any; otherwise, the runtime itself was the source.</param>
        /// <param name="result">The random result (may be bool or int).</param>
        void OnRandom(ActorId id, object result);

        /// <summary>
        /// Called when a state machine enters or exits a state.
        /// </summary>
        /// <param name="id">The id of the actor entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited.</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        void OnStateTransition(ActorId id, string stateName, bool isEntry);

        /// <summary>
        /// Called when a state machine transitions states via a 'goto'.
        /// </summary>
        /// <param name="id">The id of the actor.</param>
        /// <param name="currStateName">The name of the current state.</param>
        /// <param name="newStateName">The target state of the transition.</param>
        void OnGotoState(ActorId id, string currStateName, string newStateName);

        /// <summary>
        /// Called when a state machine is being pushed to a state.
        /// </summary>
        /// <param name="id">The id of the actor being pushed to the state.</param>
        /// <param name="currStateName">The name of the current state.</param>
        /// <param name="newStateName">The target state of the transition.</param>
        void OnPushState(ActorId id, string currStateName, string newStateName);

        /// <summary>
        /// Called when a state machine has been popped from a state.
        /// </summary>
        /// <param name="id">The id of the actor that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state.</param>
        /// <param name="restoredStateName">The name of the state being re-entered, if any.</param>
        void OnPopState(ActorId id, string currStateName, string restoredStateName);

        /// <summary>
        /// Called when an actor is idle (there is nothing to dequeue) and the default
        /// event handler is about to be executed.
        /// </summary>
        /// <param name="id">The id of the actor that the state will execute in.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        void OnDefaultEventHandler(ActorId id, string stateName);

        /// <summary>
        /// Called when an actor has been halted.
        /// </summary>
        /// <param name="id">The id of the actor that has been halted.</param>
        /// <param name="inboxSize">Approximate size of the inbox.</param>
        void OnHalt(ActorId id, int inboxSize);

        /// <summary>
        /// Called when an actor handled a raised event.
        /// </summary>
        /// <param name="id">The id of the actor handling the event.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">The name of the event being handled.</param>
        void OnHandleRaisedEvent(ActorId id, string stateName, string eventName);

        /// <summary>
        /// When an event cannot be handled in the current state, its exit handler is executed and then the state is
        /// popped and any previous "current state" is reentered. This handler is called when that pop has been done.
        /// </summary>
        /// <param name="id">The id of the actor that the pop executed in.</param>
        /// <param name="currStateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventName">The name of the event that cannot be handled.</param>
        void OnPopUnhandledEvent(ActorId id, string currStateName, string eventName);

        /// <summary>
        /// Called when an actor throws an exception.
        /// </summary>
        /// <param name="id">The id of the actor that threw the exception.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex);

        /// <summary>
        /// Called when an OnException method is used to handle a thrown exception.
        /// </summary>
        /// <param name="id">The id of the actor that threw the exception.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex);

        /// <summary>
        /// Called when an actor timer has been created.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        void OnCreateTimer(TimerInfo info);

        /// <summary>
        /// Called when an actor timer has been stopped.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        void OnStopTimer(TimerInfo info);

        /// <summary>
        /// Called when a monitor has been created.
        /// </summary>
        /// <param name="monitorTypeName">The name of the type of the monitor that has been created.</param>
        /// <param name="id">The id of the monitor that has been created.</param>
        void OnCreateMonitor(string monitorTypeName, ActorId id);

        /// <summary>
        /// Called when a monitor executes an action.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor that is executing the action.</param>
        /// <param name="id">The id of the monitor that is executing the action</param>
        /// <param name="stateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        void OnMonitorExecuteAction(string monitorTypeName, ActorId id, string stateName, string actionName);

        /// <summary>
        /// Called when a monitor is about to process an event.
        /// </summary>
        /// <param name="senderId">The sender of the event.</param>
        /// <param name="senderStateName">The name of the state the sender is in.</param>
        /// <param name="monitorTypeName">Name of type of the monitor that will process the event.</param>
        /// <param name="id">The id of the monitor that will process the event.</param>
        /// <param name="stateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        void OnMonitorProcessEvent(ActorId senderId, string senderStateName, string monitorTypeName, ActorId id,
            string stateName, string eventName);

        /// <summary>
        /// Called when a monitor raised an event.
        /// </summary>
        /// <param name="monitorTypeName">Name of type of the monitor raising the event.</param>
        /// <param name="id">The id of the monitor raising the event.</param>
        /// <param name="stateName">The name of the state in which the event is being raised.</param>
        /// <param name="eventName">The name of the event.</param>
        void OnMonitorRaiseEvent(string monitorTypeName, ActorId id, string stateName, string eventName);

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
        void OnMonitorStateTransition(string monitorTypeName, ActorId id, string stateName,
            bool isEntry, bool? isInHotState);

        /// <summary>
        /// Called for general error reporting via pre-constructed text.
        /// </summary>
        /// <param name="text">The text of the error report.</param>
        void OnError(string text);

        /// <summary>
        /// Called for errors detected by a specific scheduling strategy.
        /// </summary>
        /// <param name="strategy">The scheduling strategy that was used.</param>
        /// <param name="strategyDescription">More information about the scheduling strategy.</param>
        void OnStrategyError(SchedulingStrategy strategy, string strategyDescription);
    }
}
