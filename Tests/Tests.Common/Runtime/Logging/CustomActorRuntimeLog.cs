// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Runtime.Exploration;

namespace Microsoft.Coyote.Tests.Common.Runtime
{
    public class CustomActorRuntimeLog : IActorRuntimeLog
    {
        private readonly StringBuilder Log = new StringBuilder();

        public override string ToString()
        {
            return this.Log.ToString();
        }

        public void OnCreateActor(ActorId id, ActorId creator)
        {
            this.Log.AppendLine("CreateActor");
        }

        public void OnExecuteAction(ActorId id, string stateName, string actionName)
        {
        }

        public void OnSendEvent(ActorId targetActorId, ActorId senderId, string senderStateName, string eventName, Guid opGroupId, bool isTargetHalted)
        {
        }

        public void OnRaiseEvent(ActorId id, string stateName, string eventName)
        {
        }

        public void OnEnqueueEvent(ActorId id, string eventName)
        {
        }

        public void OnDequeueEvent(ActorId id, string stateName, string eventName)
        {
        }

        public void OnReceiveEvent(ActorId id, string stateName, string eventName, bool wasBlocked)
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

        public void OnGotoState(ActorId id, string currStateName, string newStateName)
        {
        }

        public void OnPushState(ActorId id, string currStateName, string newStateName)
        {
        }

        public void OnPopState(ActorId id, string currStateName, string restoredStateName)
        {
        }

        public void OnDefaultEventHandler(ActorId id, string stateName)
        {
        }

        public void OnHalt(ActorId id, int inboxSize)
        {
        }

        public void OnHandleRaisedEvent(ActorId id, string stateName, string eventName)
        {
        }

        public void OnPopUnhandledEvent(ActorId id, string stateName, string eventName)
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

        public void OnCreateMonitor(string monitorTypeName, ActorId id)
        {
        }

        public void OnMonitorExecuteAction(string monitorTypeName, ActorId id, string stateName, string actionName)
        {
        }

        public void OnMonitorProcessEvent(ActorId senderId, string senderStateName, string monitorTypeName, ActorId id, string stateName, string eventName)
        {
        }

        public void OnMonitorRaiseEvent(string monitorTypeName, ActorId id, string stateName, string eventName)
        {
        }

        public void OnMonitorStateTransition(string monitorTypeName, ActorId id, string stateName, bool isEntry, bool? isInHotState)
        {
        }

        public void OnAssertionFailure(string error)
        {
        }

        public void OnStrategyDescription(SchedulingStrategy strategy, string description)
        {
        }

        public void OnCompleted()
        {
        }
    }
}
