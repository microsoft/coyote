// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Coyote.Actors.Timers;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// This class implements <see cref="IActorRuntimeLog"/> and generates log output in an XML format.
    /// </summary>
    internal class ActorRuntimeLogXmlFormatter : RuntimeLogXmlFormatter, IActorRuntimeLog
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ActorRuntimeLogXmlFormatter"/> class.
        /// </summary>
        public ActorRuntimeLogXmlFormatter(XmlWriter writer)
            : base(writer)
        {
        }

        /// <inheritdoc/>
        public void OnCreateActor(ActorId id, string creatorName, string creatorType)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("CreateActor");
            this.Writer.WriteAttributeString("id", id.ToString());

            if (creatorName != null && creatorType != null)
            {
                this.Writer.WriteAttributeString("creatorName", creatorName);
                this.Writer.WriteAttributeString("creatorType", creatorType);
            }
            else
            {
                this.Writer.WriteAttributeString("creatorName", Task.CurrentId.ToString());
                this.Writer.WriteAttributeString("creatorType", "task");
            }

            this.Writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public void OnCreateStateMachine(ActorId id, string creatorName, string creatorType)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("CreateStateMachine");
            this.Writer.WriteAttributeString("id", id.ToString());

            if (creatorName != null && creatorType != null)
            {
                this.Writer.WriteAttributeString("creatorName", creatorName);
                this.Writer.WriteAttributeString("creatorType", creatorType);
            }
            else
            {
                this.Writer.WriteAttributeString("creatorName", Task.CurrentId.ToString());
                this.Writer.WriteAttributeString("creatorType", "task");
            }

            this.Writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public void OnCreateTimer(TimerInfo info)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("CreateTimer");
            this.Writer.WriteAttributeString("owner", info.OwnerId?.Name ?? Task.CurrentId.ToString());
            this.Writer.WriteAttributeString("due", info.DueTime.ToString());
            this.Writer.WriteAttributeString("period", info.Period.ToString());
            this.Writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public void OnDefaultEventHandler(ActorId id, string stateName)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("DefaultEvent");
            this.Writer.WriteAttributeString("id", id.ToString());
            if (!string.IsNullOrEmpty(stateName))
            {
                this.Writer.WriteAttributeString("state", stateName);
            }

            this.Writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public void OnEventHandlerTerminated(ActorId id, string stateName, DequeueStatus dequeueStatus)
        {
        }

        /// <inheritdoc/>
        public void OnDequeueEvent(ActorId id, string stateName, Event e)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("DequeueEvent");
            this.Writer.WriteAttributeString("id", id.ToString());
            if (!string.IsNullOrEmpty(stateName))
            {
                this.Writer.WriteAttributeString("state", stateName);
            }

            this.Writer.WriteAttributeString("event", e.GetType().FullName);
            this.Writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public void OnEnqueueEvent(ActorId id, Event e)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("EnqueueEvent");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("event", e.GetType().FullName);
            this.Writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public void OnExceptionHandled(ActorId id, string stateName, string actionName, Exception ex)
        {
            if (this.IsClosed)
            {
                return;
            }

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

        /// <inheritdoc/>
        public void OnExceptionThrown(ActorId id, string stateName, string actionName, Exception ex)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("ExceptionThrown");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("state", stateName);
            this.Writer.WriteAttributeString("action", actionName);
            this.Writer.WriteAttributeString("type", ex.GetType().FullName);
            this.Writer.WriteString(ex.ToString());
            this.Writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public void OnExecuteAction(ActorId id, string handlingStateName, string currentStateName, string actionName)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("Action");
            this.Writer.WriteAttributeString("id", id.ToString());
            if (!string.IsNullOrEmpty(currentStateName))
            {
                this.Writer.WriteAttributeString("state", currentStateName);
                if (currentStateName != handlingStateName)
                {
                    this.Writer.WriteAttributeString("handledBy", handlingStateName);
                }
            }

            this.Writer.WriteAttributeString("action", actionName);
            this.Writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public void OnGotoState(ActorId id, string currentStateName, string newStateName)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("Goto");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("currState", currentStateName);
            this.Writer.WriteAttributeString("newState", newStateName);
            this.Writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public void OnHalt(ActorId id, int inboxSize)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("Halt");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("inboxSize", inboxSize.ToString());
            this.Writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public void OnPopState(ActorId id, string currentStateName, string restoredStateName)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("Pop");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("currState", currentStateName);
            this.Writer.WriteAttributeString("restoredState", restoredStateName);
            this.Writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public void OnPopStateUnhandledEvent(ActorId id, string stateName, Event e)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("PopUnhandled");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("state", stateName);
            this.Writer.WriteAttributeString("event", e.GetType().FullName);
            this.Writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public void OnPushState(ActorId id, string currentStateName, string newStateName)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("Push");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("currState", currentStateName);
            this.Writer.WriteAttributeString("newState", newStateName);
            this.Writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public void OnRaiseEvent(ActorId id, string stateName, Event e)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("Raise");
            this.Writer.WriteAttributeString("id", id.ToString());
            if (!string.IsNullOrEmpty(stateName))
            {
                this.Writer.WriteAttributeString("state", stateName);
            }

            this.Writer.WriteAttributeString("event", e.GetType().FullName);
            this.Writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public void OnHandleRaisedEvent(ActorId id, string stateName, Event e)
        {
            if (this.IsClosed)
            {
                return;
            }
        }

        /// <inheritdoc/>
        public void OnReceiveEvent(ActorId id, string stateName, Event e, bool wasBlocked)
        {
            if (this.IsClosed)
            {
                return;
            }

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

        /// <inheritdoc/>
        public void OnSendEvent(ActorId targetActorId, string senderName, string senderType, string senderStateName,
            Event e, Guid eventGroupId, bool isTargetHalted)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("Send");
            this.Writer.WriteAttributeString("target", targetActorId.ToString());

            if (senderName != null && senderType != null)
            {
                this.Writer.WriteAttributeString("senderName", senderName);
                this.Writer.WriteAttributeString("senderType", senderType);
            }

            // TODO: should this be guarded as above?
            this.Writer.WriteAttributeString("senderState", senderStateName);

            this.Writer.WriteAttributeString("event", e.GetType().FullName);
            if (eventGroupId != Guid.Empty)
            {
                this.Writer.WriteAttributeString("group", eventGroupId.ToString());
            }

            this.Writer.WriteAttributeString("isTargetHalted", isTargetHalted.ToString());
            this.Writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public void OnStateTransition(ActorId id, string stateName, bool isEntry)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("State");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("state", stateName);
            this.Writer.WriteAttributeString("isEntry", isEntry.ToString());
            this.Writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public void OnStopTimer(TimerInfo info)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("StopTimer");
            this.Writer.WriteAttributeString("owner", info.OwnerId?.Name ?? Task.CurrentId.ToString());
            this.Writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public void OnWaitEvent(ActorId id, string stateName, Type eventType)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("WaitEvent");
            this.Writer.WriteAttributeString("id", id.ToString());
            this.Writer.WriteAttributeString("event", eventType.FullName);
            if (!string.IsNullOrEmpty(stateName))
            {
                this.Writer.WriteAttributeString("state", stateName);
            }

            this.Writer.WriteEndElement();
        }

        /// <inheritdoc/>
        public void OnWaitEvent(ActorId id, string stateName, params Type[] eventTypes)
        {
            if (this.IsClosed)
            {
                return;
            }

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
