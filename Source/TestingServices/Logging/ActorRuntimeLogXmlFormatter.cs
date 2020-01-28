// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime.Exploration;

namespace Microsoft.Coyote.Runtime.Logging
{
    /// <summary>
    /// This class implements IActorRuntimeLog and generates log output in an XML format.
    /// </summary>
    internal class ActorRuntimeLogXmlFormatter : IActorRuntimeLog
    {
        private readonly XmlWriter Writer;

        public ActorRuntimeLogXmlFormatter(XmlWriter writer)
        {
            this.Writer = writer;
            this.Writer.WriteStartElement("Log");
        }

        /// <summary>
        /// Invoked when a log is complete (and is about to be closed).
        /// </summary>
        public void OnCompleted()
        {
            using (this.Writer)
            {
                this.Writer.WriteEndElement();
            }
        }

        public void OnAssertionFailure(string error)
        {
            this.Writer.WriteElementString("AssertionFailure", error);
        }

        public void OnCreateActor(ActorId id, ActorId creator)
        {
            this.Writer.WriteStartElement("CreateActor");
            this.Writer.WriteAttributeString("id", id.ToString());
            var creatorId = creator is null ? "external" : creator.Name;
            this.Writer.WriteAttributeString("creator", creatorId);
            this.Writer.WriteEndElement();
        }

        public void OnCreateMonitor(string monitorTypeName, ActorId id)
        {
            this.Writer.WriteStartElement("CreateMonitor");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("type", monitorTypeName);
            this.Writer.WriteEndElement();
        }

        public void OnCreateTimer(TimerInfo info)
        {
            var ownerId = info.OwnerId is null ? "external" : info.OwnerId.Name;
            this.Writer.WriteStartElement("CreateTimer");
            this.Writer.WriteAttributeString("owner", ownerId);
            this.Writer.WriteAttributeString("due", info.DueTime.ToString());
            this.Writer.WriteAttributeString("period", info.Period.ToString());
            this.Writer.WriteEndElement();
        }

        public void OnDefaultEventHandler(ActorId id, string stateName)
        {
            this.Writer.WriteStartElement("DefaultEvent");
            this.Writer.WriteAttributeString("id", id.ToString());
            if (!string.IsNullOrEmpty(stateName))
            {
                this.Writer.WriteAttributeString("state", stateName);
            }

            this.Writer.WriteEndElement();
        }

        public void OnDequeueEvent(ActorId id, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            this.Writer.WriteStartElement("DequeueEvent");
            this.Writer.WriteAttributeString("id", id.ToString());
            if (!string.IsNullOrEmpty(stateName))
            {
                this.Writer.WriteAttributeString("state", stateName);
            }

            this.Writer.WriteAttributeString("event", eventName);
            this.Writer.WriteEndElement();
        }

        public void OnEnqueueEvent(ActorId id, Event e)
        {
            string eventName = e.GetType().FullName;
            this.Writer.WriteStartElement("EnqueueEvent");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("event", eventName);
            this.Writer.WriteEndElement();
        }

        public void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
        {
            this.Writer.WriteStartElement("ExceptionHandled");
            this.Writer.WriteAttributeString("id", id.ToString());
            if (!string.IsNullOrEmpty(stateName))
            {
                this.Writer.WriteAttributeString("state", stateName);
            }

            this.Writer.WriteAttributeString("action", actionName);
            this.Writer.WriteAttributeString("type", ex.GetType().FullName);
            this.Writer.WriteString(ex.ToString());
            this.Writer.WriteEndElement();
        }

        public void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
        {
            this.Writer.WriteStartElement("ExceptionThrown");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("state", stateName);
            this.Writer.WriteAttributeString("action", actionName);
            this.Writer.WriteAttributeString("type", ex.GetType().FullName);
            this.Writer.WriteString(ex.ToString());
            this.Writer.WriteEndElement();
        }

        public void OnExecuteAction(ActorId id, string stateName, string actionName)
        {
            this.Writer.WriteStartElement("Action");
            this.Writer.WriteAttributeString("id", id.ToString());
            if (!string.IsNullOrEmpty(stateName))
            {
                this.Writer.WriteAttributeString("state", stateName);
            }

            this.Writer.WriteAttributeString("action", actionName);
            this.Writer.WriteEndElement();
        }

        public void OnGotoState(ActorId id, string currStateName, string newStateName)
        {
            this.Writer.WriteStartElement("Goto");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("currState", currStateName);
            this.Writer.WriteAttributeString("newState", newStateName);
            this.Writer.WriteEndElement();
        }

        public void OnHalt(ActorId id, int inboxSize)
        {
            this.Writer.WriteStartElement("Halt");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("inBoxSize", inboxSize.ToString());
            this.Writer.WriteEndElement();
        }

        public void OnHandleRaisedEvent(ActorId id, string stateName, Event e)
        {
        }

        public void OnMonitorExecuteAction(string monitorTypeName, ActorId id, string stateName, string actionName)
        {
            this.Writer.WriteStartElement("MonitorAction");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("monitorType", monitorTypeName);
            this.Writer.WriteAttributeString("state", stateName);
            this.Writer.WriteAttributeString("action", actionName);
            this.Writer.WriteEndElement();
        }

        public void OnMonitorProcessEvent(ActorId senderId, string senderStateName, string monitorTypeName, ActorId id, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            this.Writer.WriteStartElement("MonitorEvent");
            if (senderId != null)
            {
                this.Writer.WriteAttributeString("sender", senderId.ToString());
            }

            this.Writer.WriteAttributeString("senderState", senderStateName);
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("monitorType", monitorTypeName);
            this.Writer.WriteAttributeString("state", stateName);
            this.Writer.WriteAttributeString("event", eventName);
            this.Writer.WriteEndElement();
        }

        public void OnMonitorRaiseEvent(string monitorTypeName, ActorId id, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            this.Writer.WriteStartElement("MonitorRaise");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("monitorType", monitorTypeName);
            this.Writer.WriteAttributeString("state", stateName);
            this.Writer.WriteAttributeString("event", eventName);
            this.Writer.WriteEndElement();
        }

        public void OnMonitorStateTransition(string monitorTypeName, ActorId id, string stateName, bool isEntry, bool? isInHotState)
        {
            this.Writer.WriteStartElement("MonitorState");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("monitorType", monitorTypeName);
            this.Writer.WriteAttributeString("state", stateName);
            this.Writer.WriteAttributeString("isEntry", isEntry.ToString());
            bool hot = isInHotState == true;
            this.Writer.WriteAttributeString("isInHotState", hot.ToString());
            this.Writer.WriteEndElement();
        }

        public void OnPopState(ActorId id, string currStateName, string restoredStateName)
        {
            this.Writer.WriteStartElement("Pop");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("currState", currStateName);
            this.Writer.WriteAttributeString("restoredState", restoredStateName);
            this.Writer.WriteEndElement();
        }

        public void OnPopUnhandledEvent(ActorId id, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            this.Writer.WriteStartElement("PopUnhandled");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("state", stateName);
            this.Writer.WriteAttributeString("event", eventName);
            this.Writer.WriteEndElement();
        }

        public void OnPushState(ActorId id, string currStateName, string newStateName)
        {
            this.Writer.WriteStartElement("Push");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("currState", currStateName);
            this.Writer.WriteAttributeString("newState", newStateName);
            this.Writer.WriteEndElement();
        }

        public void OnRaiseEvent(ActorId id, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            this.Writer.WriteStartElement("Raise");
            this.Writer.WriteAttributeString("id", id.ToString());
            if (!string.IsNullOrEmpty(stateName))
            {
                this.Writer.WriteAttributeString("state", stateName);
            }

            this.Writer.WriteAttributeString("event", eventName);
            this.Writer.WriteEndElement();
        }

        public void OnRandom(ActorId id, object result)
        {
            this.Writer.WriteStartElement("Random");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("result", result.ToString());
            this.Writer.WriteEndElement();
        }

        public void OnReceiveEvent(ActorId id, string stateName, Event e, bool wasBlocked)
        {
            var eventName = e.GetType().FullName;
            this.Writer.WriteStartElement("Receive");
            this.Writer.WriteAttributeString("id", id.ToString());
            if (!string.IsNullOrEmpty(stateName))
            {
                this.Writer.WriteAttributeString("state", stateName);
            }

            this.Writer.WriteAttributeString("event", eventName);
            this.Writer.WriteAttributeString("wasBlocked", wasBlocked.ToString());
            this.Writer.WriteEndElement();
        }

        public void OnSendEvent(ActorId targetActorId, ActorId senderId, string senderStateName, Event e, Guid opGroupId, bool isTargetHalted)
        {
            var eventName = e.GetType().FullName;
            this.Writer.WriteStartElement("Send");
            this.Writer.WriteAttributeString("target", targetActorId.ToString());
            if (senderId != null)
            {
                this.Writer.WriteAttributeString("sender", senderId.ToString());
            }

            this.Writer.WriteAttributeString("senderState", senderStateName);
            this.Writer.WriteAttributeString("event", eventName);
            if (opGroupId != Guid.Empty)
            {
                this.Writer.WriteAttributeString("event", opGroupId.ToString());
            }

            this.Writer.WriteAttributeString("isTargetHalted", isTargetHalted.ToString());
            this.Writer.WriteEndElement();
        }

        public void OnStateTransition(ActorId id, string stateName, bool isEntry)
        {
            this.Writer.WriteStartElement("State");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("state", stateName);
            this.Writer.WriteAttributeString("isEntry", isEntry.ToString());
            this.Writer.WriteEndElement();
        }

        public void OnStopTimer(TimerInfo info)
        {
            var ownerId = info.OwnerId is null ? "external" : info.OwnerId.Name;
            this.Writer.WriteStartElement("StopTimer");
            this.Writer.WriteAttributeString("owner", ownerId);
            this.Writer.WriteEndElement();
        }

        public void OnStrategyDescription(SchedulingStrategy strategy, string description)
        {
            this.Writer.WriteStartElement("Strategy");
            this.Writer.WriteAttributeString("strategy", strategy.ToString());
            if (!string.IsNullOrEmpty(description))
            {
                this.Writer.WriteString(description);
            }

            this.Writer.WriteEndElement();
        }

        public void OnWaitEvent(ActorId id, string stateName, Type eventType)
        {
            this.Writer.WriteStartElement("WaitEvent");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("event", eventType.FullName);
            if (!string.IsNullOrEmpty(stateName))
            {
                this.Writer.WriteAttributeString("state", stateName);
            }

            this.Writer.WriteEndElement();
        }

        public void OnWaitEvent(ActorId id, string stateName, params Type[] eventTypes)
        {
            this.Writer.WriteStartElement("WaitEvent");
            this.Writer.WriteAttributeString("id", id.ToString());
            StringBuilder sb = new StringBuilder();
            foreach (var t in eventTypes)
            {
                if (sb.Length > 0)
                {
                    sb.Append(", ");
                }

                sb.Append(t.FullName);
            }

            this.Writer.WriteAttributeString("event", sb.ToString());
            if (!string.IsNullOrEmpty(stateName))
            {
                this.Writer.WriteAttributeString("state", stateName);
            }

            this.Writer.WriteEndElement();
        }
    }
}
