// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;

namespace Microsoft.Coyote.Tests.Common.Runtime
{
    public class CustomActorRuntimeLog : IActorRuntimeLog
    {
        private readonly StringBuilder Log = new StringBuilder();

        public override string ToString()
        {
            return this.Log.ToString();
        }

        public void OnCreateActor(ActorId id, string creatorType, string creatorName)
        {
            this.Log.AppendLine("CreateActor");
        }

        public void OnExecuteAction(ActorId id, string handlingStateName, string currentStateName, string actionName)
        {
        }

        public void OnSendEvent(ActorId targetActorId, string senderType, string senderName, string senderStateName,
            Event e, Guid opGroupId, bool isTargetHalted)
        {
        }

        public void OnRaiseEvent(ActorId id, string stateName, Event e)
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

        public void OnRandom(ActorId id, object result)
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

        public void OnHalt(ActorId id, int inboxSize)
        {
        }

        public void OnHandleRaisedEvent(ActorId id, string stateName, Event e)
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

        public void OnCreateMonitor(string monitorTypeName)
        {
        }

        public void OnMonitorExecuteAction(string monitorTypeName, string stateName, string actionName)
        {
        }

        public void OnMonitorProcessEvent(string monitorTypeName, string stateName, string senderType,
            string senderName, string senderStateName, Event e)
        {
        }

        public void OnMonitorRaiseEvent(string monitorTypeName, string stateName, Event e)
        {
        }

        public void OnMonitorStateTransition(string monitorTypeName, string stateName, bool isEntry, bool? isInHotState)
        {
        }

        public void OnAssertionFailure(string error)
        {
        }

        public void OnStrategyDescription(string strategyName, string description)
        {
        }

        public void OnCompleted()
        {
        }
    }
}
