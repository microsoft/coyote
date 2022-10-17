// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text;
using Microsoft.Coyote.Actors.Timers;

namespace Microsoft.Coyote.Actors.Tests.Logging
{
    public class CustomActorRuntimeLog : IActorRuntimeLog
    {
        private readonly StringBuilder Log = new StringBuilder();

        public override string ToString()
        {
            return this.Log.ToString();
        }

        public void OnCreateActor(ActorId id, string creatorName, string creatorType)
        {
            this.Log.AppendLine("CreateActor");
        }

        public void OnCreateStateMachine(ActorId id, string creatorName, string creatorType)
        {
            this.Log.AppendLine("CreateStateMachine");
        }

        public void OnExecuteAction(ActorId id, string handlingStateName, string currentStateName, string actionName)
        {
        }

        public void OnSendEvent(ActorId targetActorId, string senderName, string senderType, string senderStateName,
            Event e, Guid eventGroupId, bool isTargetHalted)
        {
        }

        public void OnRaiseEvent(ActorId id, string stateName, Event e)
        {
        }

        public void OnHandleRaisedEvent(ActorId id, string stateName, Event e)
        {
        }

        public void OnEnqueueEvent(ActorId id, Event e)
        {
        }

        public void OnDequeueEvent(ActorId id, string stateName, Event e)
        {
        }

        public void OnReceiveEvent(ActorId id, string stateName, Event e, bool wasBlocked)
        {
        }

        public void OnWaitEvent(ActorId id, string stateName, Type eventType)
        {
        }

        public void OnWaitEvent(ActorId id, string stateName, params Type[] eventTypes)
        {
        }

        public void OnStateTransition(ActorId id, string stateName, bool isEntry)
        {
            this.Log.AppendLine("StateTransition");
        }

        public void OnGotoState(ActorId id, string currentStateName, string newStateName)
        {
        }

        public void OnPushState(ActorId id, string currentStateName, string newStateName)
        {
        }

        public void OnPopState(ActorId id, string currentStateName, string restoredStateName)
        {
        }

        public void OnDefaultEventHandler(ActorId id, string stateName)
        {
        }

        public void OnEventHandlerTerminated(ActorId id, string stateName, DequeueStatus dequeueStatus)
        {
        }

        public void OnHalt(ActorId id, int inboxSize)
        {
        }

        public void OnPopStateUnhandledEvent(ActorId id, string stateName, Event e)
        {
        }

        public void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
        {
        }

        public void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
        {
        }

        public void OnCreateTimer(TimerInfo info)
        {
        }

        public void OnStopTimer(TimerInfo info)
        {
        }

        public void OnCreateMonitor(string monitorType)
        {
        }

        public void OnMonitorExecuteAction(string monitorType, string stateName, string actionName)
        {
        }

        public void OnMonitorProcessEvent(string monitorType, string stateName, string senderName,
            string senderType, string senderStateName, Event e)
        {
        }

        public void OnMonitorRaiseEvent(string monitorType, string stateName, Event e)
        {
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

        public void OnAssertionFailure(string error)
        {
        }

        public void OnCompleted()
        {
        }
    }
}
