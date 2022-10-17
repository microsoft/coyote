// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// Manages all registered <see cref="IActorRuntimeLog"/> objects.
    /// </summary>
    internal sealed class ActorLogManager : LogManager
    {
        /// <summary>
        /// Logs that the specified actor has been created.
        /// </summary>
        /// <param name="id">The id of the actor that has been created.</param>
        /// <param name="creatorName">The name of the creator, or null.</param>
        /// <param name="creatorType">The type of the creator, or null.</param>
        internal void LogCreateActor(ActorId id, string creatorName, string creatorType)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    if (log is IActorRuntimeLog actorLog)
                    {
                        actorLog.OnCreateActor(id, creatorName, creatorType);
                    }
                }
            }
        }

        /// <summary>
        /// Logs that the specified state machine has been created.
        /// </summary>
        /// <param name="id">The id of the state machine that has been created.</param>
        /// <param name="creatorName">The name of the creator, or null.</param>
        /// <param name="creatorType">The type of the creator, or null.</param>
        internal void LogCreateStateMachine(ActorId id, string creatorName, string creatorType)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    if (log is IActorRuntimeLog actorLog)
                    {
                        actorLog.OnCreateStateMachine(id, creatorName, creatorType);
                    }
                }
            }
        }

        /// <summary>
        /// Logs that the specified actor executes an action.
        /// </summary>
        /// <param name="id">The id of the actor executing the action.</param>
        /// <param name="handlingStateName">The state that declared this action (can be different from currentStateName in the case of PushStates.</param>
        /// <param name="currentStateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        internal void LogExecuteAction(ActorId id, string handlingStateName, string currentStateName, string actionName)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    if (log is IActorRuntimeLog actorLog)
                    {
                        actorLog.OnExecuteAction(id, handlingStateName, currentStateName, actionName);
                    }
                }
            }
        }

        /// <summary>
        /// Logs that the specified event is sent to a target actor.
        /// </summary>
        /// <param name="targetActorId">The id of the target actor.</param>
        /// <param name="senderName">The name of the sender, if any.</param>
        /// <param name="senderType">The type of the sender, if any.</param>
        /// <param name="senderState">The state name, if the sender is a state machine, else null.</param>
        /// <param name="e">The event being sent.</param>
        /// <param name="eventGroupId">The id used to identify the send operation.</param>
        /// <param name="isTargetHalted">Is the target actor halted.</param>
        internal void LogSendEvent(ActorId targetActorId, string senderName, string senderType, string senderState,
            Event e, Guid eventGroupId, bool isTargetHalted)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    if (log is IActorRuntimeLog actorLog)
                    {
                        actorLog.OnSendEvent(targetActorId, senderName, senderType, senderState, e, eventGroupId, isTargetHalted);
                    }
                }
            }
        }

        /// <summary>
        /// Logs that the specified actor raises an event.
        /// </summary>
        /// <param name="id">The id of the actor raising the event.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="e">The event being raised.</param>
        internal void LogRaiseEvent(ActorId id, string stateName, Event e)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    if (log is IActorRuntimeLog actorLog)
                    {
                        actorLog.OnRaiseEvent(id, stateName, e);
                    }
                }
            }
        }

        /// <summary>
        /// Logs that the specified state machine handled a raised event.
        /// </summary>
        /// <param name="id">The id of the actor handling the event.</param>
        /// <param name="stateName">The name of the current state.</param>
        /// <param name="e">The event being handled.</param>
        internal void LogHandleRaisedEvent(ActorId id, string stateName, Event e)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    if (log is IActorRuntimeLog actorLog)
                    {
                        actorLog.OnHandleRaisedEvent(id, stateName, e);
                    }
                }
            }
        }

        /// <summary>
        /// Logs that the specified event is about to be enqueued to an actor.
        /// </summary>
        /// <param name="id">The id of the actor that the event is being enqueued to.</param>
        /// <param name="e">The event being enqueued.</param>
        internal void LogEnqueueEvent(ActorId id, Event e)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    if (log is IActorRuntimeLog actorLog)
                    {
                        actorLog.OnEnqueueEvent(id, e);
                    }
                }
            }
        }

        /// <summary>
        /// Logs that the specified event is dequeued by an actor.
        /// </summary>
        /// <param name="id">The id of the actor that the event is being dequeued by.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="e">The event being dequeued.</param>
        internal void LogDequeueEvent(ActorId id, string stateName, Event e)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    if (log is IActorRuntimeLog actorLog)
                    {
                        actorLog.OnDequeueEvent(id, stateName, e);
                    }
                }
            }
        }

        /// <summary>
        /// Logs that the specified event is received by an actor.
        /// </summary>
        /// <param name="id">The id of the actor that received the event.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="e">The event being received.</param>
        /// <param name="wasBlocked">The state machine was waiting for one or more specific events,
        /// and <paramref name="e"/> was one of them.</param>
        internal void LogReceiveEvent(ActorId id, string stateName, Event e, bool wasBlocked)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    if (log is IActorRuntimeLog actorLog)
                    {
                        actorLog.OnReceiveEvent(id, stateName, e, wasBlocked);
                    }
                }
            }
        }

        /// <summary>
        /// Logs that the specified actor waits to receive an event of a specified type.
        /// </summary>
        /// <param name="id">The id of the actor that is entering the wait state.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventType">The type of the event being waited for.</param>
        internal void LogWaitEvent(ActorId id, string stateName, Type eventType)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    if (log is IActorRuntimeLog actorLog)
                    {
                        actorLog.OnWaitEvent(id, stateName, eventType);
                    }
                }
            }
        }

        /// <summary>
        /// Logs that the specified actor waits to receive an event of one of the specified types.
        /// </summary>
        /// <param name="id">The id of the actor that is entering the wait state.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="eventTypes">The types of the events being waited for, if any.</param>
        internal void LogWaitEvent(ActorId id, string stateName, params Type[] eventTypes)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    if (log is IActorRuntimeLog actorLog)
                    {
                        actorLog.OnWaitEvent(id, stateName, eventTypes);
                    }
                }
            }
        }

        /// <summary>
        /// Logs that the specified state machine enters or exits a state.
        /// </summary>
        /// <param name="id">The id of the actor entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited.</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        internal void LogStateTransition(ActorId id, string stateName, bool isEntry)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    if (log is IActorRuntimeLog actorLog)
                    {
                        actorLog.OnStateTransition(id, stateName, isEntry);
                    }
                }
            }
        }

        /// <summary>
        /// Logs that the specified state machine performs a goto state transition.
        /// </summary>
        /// <param name="id">The id of the actor.</param>
        /// <param name="currentStateName">The name of the current state.</param>
        /// <param name="newStateName">The target state of the transition.</param>
        internal void LogGotoState(ActorId id, string currentStateName, string newStateName)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    if (log is IActorRuntimeLog actorLog)
                    {
                        actorLog.OnGotoState(id, currentStateName, newStateName);
                    }
                }
            }
        }

        /// <summary>
        /// Logs that the specified state machine performs a push state transition.
        /// </summary>
        /// <param name="id">The id of the actor being pushed to the state.</param>
        /// <param name="currentStateName">The name of the current state.</param>
        /// <param name="newStateName">The target state of the transition.</param>
        internal void LogPushState(ActorId id, string currentStateName, string newStateName)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    if (log is IActorRuntimeLog actorLog)
                    {
                        actorLog.OnPushState(id, currentStateName, newStateName);
                    }
                }
            }
        }

        /// <summary>
        /// Logs that the specified state machine performs a pop state transition.
        /// </summary>
        /// <param name="id">The id of the actor that the pop executed in.</param>
        /// <param name="currStateName">The name of the current state.</param>
        /// <param name="restoredStateName">The name of the state being re-entered, if any.</param>
        internal void LogPopState(ActorId id, string currStateName, string restoredStateName)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    if (log is IActorRuntimeLog actorLog)
                    {
                        actorLog.OnPopState(id, currStateName, restoredStateName);
                    }
                }
            }
        }

        /// <summary>
        /// Logs that the specified actor has halted.
        /// </summary>
        /// <param name="id">The id of the actor that has been halted.</param>
        /// <param name="inboxSize">Approximate size of the inbox.</param>
        internal void LogHalt(ActorId id, int inboxSize)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    if (log is IActorRuntimeLog actorLog)
                    {
                        actorLog.OnHalt(id, inboxSize);
                    }
                }
            }
        }

        /// <summary>
        /// Logs that the specified actor is idle (there is nothing to dequeue) and the default
        /// event handler is about to be executed.
        /// </summary>
        /// <param name="id">The id of the actor that the state will execute in.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        internal void LogDefaultEventHandler(ActorId id, string stateName)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    if (log is IActorRuntimeLog actorLog)
                    {
                        actorLog.OnDefaultEventHandler(id, stateName);
                    }
                }
            }
        }

        /// <summary>
        /// Logs that the event handler of the specified actor terminated.
        /// </summary>
        /// <param name="id">The id of the actor that the state will execute in.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="dequeueStatus">The status returned as the result of the last dequeue operation.</param>
        internal void LogEventHandlerTerminated(ActorId id, string stateName, DequeueStatus dequeueStatus)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    if (log is IActorRuntimeLog actorLog)
                    {
                        actorLog.OnEventHandlerTerminated(id, stateName, dequeueStatus);
                    }
                }
            }
        }

        /// <summary>
        /// Logs that the specified event cannot be handled in the current state, its exit
        /// handler is executed and then the state is popped and any previous "current state"
        /// is reentered. This handler is called when that pop has been done.
        /// </summary>
        /// <param name="id">The id of the actor that the pop executed in.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="e">The event that cannot be handled.</param>
        internal void LogPopStateUnhandledEvent(ActorId id, string stateName, Event e)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    if (log is IActorRuntimeLog actorLog)
                    {
                        actorLog.OnPopStateUnhandledEvent(id, stateName, e);
                    }
                }
            }
        }

        /// <summary>
        /// Logs that the specified actor throws an exception without handling it.
        /// </summary>
        /// <param name="id">The id of the actor that threw the exception.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        internal void LogExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    if (log is IActorRuntimeLog actorLog)
                    {
                        actorLog.OnExceptionThrown(id, stateName, actionName, ex);
                    }
                }
            }
        }

        /// <summary>
        /// Logs that the specified actor has handled a thrown exception.
        /// </summary>
        /// <param name="id">The id of the actor that handled the exception.</param>
        /// <param name="stateName">The state name, if the actor is a state machine and a state exists, else null.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        /// <param name="ex">The exception.</param>
        internal void LogExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    if (log is IActorRuntimeLog actorLog)
                    {
                        actorLog.OnExceptionHandled(id, stateName, actionName, ex);
                    }
                }
            }
        }

        /// <summary>
        /// Logs that the specified actor timer has been created.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        internal void LogCreateTimer(TimerInfo info)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    if (log is IActorRuntimeLog actorLog)
                    {
                        actorLog.OnCreateTimer(info);
                    }
                }
            }
        }

        /// <summary>
        /// Logs that the specified actor timer has been stopped.
        /// </summary>
        /// <param name="info">Handle that contains information about the timer.</param>
        internal void LogStopTimer(TimerInfo info)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    if (log is IActorRuntimeLog actorLog)
                    {
                        actorLog.OnStopTimer(info);
                    }
                }
            }
        }
    }
}
