// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Coverage;
using MonitorEvent = Microsoft.Coyote.Specifications.Monitor.Event;

namespace Microsoft.Coyote.Actors.Coverage
{
    internal class ActorRuntimeLogEventCoverage : IActorRuntimeLog
    {
        private Event Dequeued;

        public ActorEventCoverage ActorEventCoverage { get; } = new ActorEventCoverage();

        public MonitorEventCoverage MonitorEventCoverage { get; } = new MonitorEventCoverage();

        public ActorRuntimeLogEventCoverage()
        {
        }

        public void OnAssertionFailure(string error)
        {
        }

        public void OnCompleted()
        {
        }

        public void OnCreateActor(ActorId id, string creatorName, string creatorType)
        {
        }

        public void OnCreateStateMachine(ActorId id, string creatorName, string creatorType)
        {
        }

        public void OnCreateMonitor(string monitorType)
        {
        }

        public void OnCreateTimer(TimerInfo info)
        {
        }

        public void OnDefaultEventHandler(ActorId id, string stateName)
        {
            this.Dequeued = DefaultEvent.Instance;
        }

        public void OnEventHandlerTerminated(ActorId id, string stateName, DequeueStatus dequeueStatus)
        {
        }

        public void OnDequeueEvent(ActorId id, string stateName, Event e)
        {
            this.Dequeued = e;
        }

        public void OnEnqueueEvent(ActorId id, Event e)
        {
        }

        public void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
        {
        }

        public void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
        {
        }

        public void OnExecuteAction(ActorId id, string handlingStateName, string currentStateName, string actionName)
        {
            this.OnActorEventHandled(id, handlingStateName);
        }

        public void OnGotoState(ActorId id, string currentStateName, string newStateName)
        {
            this.OnActorEventHandled(id, currentStateName);
        }

        public void OnHalt(ActorId id, int inboxSize)
        {
        }

        public void OnMonitorExecuteAction(string monitorType, string stateName, string actionName)
        {
        }

        public void OnMonitorProcessEvent(string monitorType, string stateName, string senderName,
            string senderType, string senderStateName, MonitorEvent e)
        {
            string eventName = e.GetType().FullName;
            this.MonitorEventCoverage.AddEventProcessed(GetStateId(monitorType, stateName), eventName);
        }

        public void OnMonitorRaiseEvent(string monitorType, string stateName, MonitorEvent e)
        {
            string eventName = e.GetType().FullName;
            this.MonitorEventCoverage.AddEventRaised(GetStateId(monitorType, stateName), eventName);
        }

        public void OnMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState)
        {
        }

        public void OnMonitorError(string monitorType, string stateName, bool? isInHotState)
        {
        }

        public void OnRandom(bool result, string callerName, string callerType)
        {
        }

        public void OnRandom(int result, string callerName, string callerType)
        {
        }

        public void OnPopState(ActorId id, string currentStateName, string restoredStateName)
        {
        }

        public void OnPopStateUnhandledEvent(ActorId id, string stateName, Event e)
        {
        }

        public void OnPushState(ActorId id, string currentStateName, string newStateName)
        {
            this.OnActorEventHandled(id, currentStateName);
        }

        public void OnRaiseEvent(ActorId id, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            this.ActorEventCoverage.AddEventSent(GetStateId(id.Type, stateName), eventName);
        }

        public void OnHandleRaisedEvent(ActorId id, string stateName, Event e)
        {
            this.Dequeued = e;
        }

        public void OnReceiveEvent(ActorId id, string stateName, Event e, bool wasBlocked)
        {
            string eventName = e.GetType().FullName;
            this.ActorEventCoverage.AddEventReceived(GetStateId(id.Type, stateName), eventName);
        }

        public void OnSendEvent(ActorId targetActorId, string senderName, string senderType, string senderStateName,
            Event e, Guid eventGroupId, bool isTargetHalted)
        {
            string eventName = e.GetType().FullName;
            this.ActorEventCoverage.AddEventSent(GetStateId(senderType, senderStateName), eventName);
        }

        public void OnStateTransition(ActorId id, string stateName, bool isEntry)
        {
        }

        public void OnStopTimer(TimerInfo info)
        {
        }

        public void OnWaitEvent(ActorId id, string stateName, Type eventType)
        {
        }

        public void OnWaitEvent(ActorId id, string stateName, params Type[] eventTypes)
        {
        }

        private void OnActorEventHandled(ActorId id, string stateName)
        {
            if (this.Dequeued != null)
            {
                this.ActorEventCoverage.AddEventReceived(GetStateId(id.Type, stateName), this.Dequeued.GetType().FullName);
                this.Dequeued = null;
            }
        }

        private static string GetStateId(string actorType, string stateName)
        {
            string id = ResolveActorTypeName(actorType);
            if (string.IsNullOrEmpty(stateName))
            {
                if (actorType is null)
                {
                    stateName = "ExternalState";
                }
                else
                {
                    stateName = GetLabel(id, null);
                }
            }

            return id += "." + stateName;
        }

        private static string ResolveActorTypeName(string actorType)
        {
            if (actorType is null)
            {
                // The sender id can be null if an event is fired from non-actor code.
                return "ExternalCode";
            }

            return actorType;
        }

        private static string GetLabel(string actorId, string fullyQualifiedName)
        {
            if (fullyQualifiedName is null)
            {
                // then this is probably an Actor, not a StateMachine.  For Actors we can invent a state
                // name equal to the short name of the class, this then looks like a Constructor which is fine.
                int pos = actorId.LastIndexOf(".");
                if (pos > 0)
                {
                    return actorId.Substring(pos + 1);
                }

                return actorId;
            }

            if (fullyQualifiedName.StartsWith(actorId))
            {
                fullyQualifiedName = fullyQualifiedName.Substring(actorId.Length + 1).Trim('+');
            }

            return fullyQualifiedName;
        }
    }
}
