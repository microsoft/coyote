// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// This class implements <see cref="IRuntimeLog"/> and generates log output in an XML format.
    /// </summary>
    internal class RuntimeLogXmlFormatter : IRuntimeLog
    {
        /// <summary>
        /// Writes to XML format.
        /// </summary>
        protected readonly XmlWriter Writer;

        /// <summary>
        /// True if the formatter is closed, else false.
        /// </summary>
        protected bool IsClosed { get; private set; }

        public RuntimeLogXmlFormatter(XmlWriter writer)
        {
            this.Writer = writer;
            this.Writer.WriteStartElement("Log");
        }

        public void OnCreateMonitor(string monitorType)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("CreateMonitor");
            this.Writer.WriteAttributeString("type", monitorType);
            this.Writer.WriteEndElement();
        }

        public void OnMonitorExecuteAction(string monitorType, string stateName, string actionName)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("MonitorAction");
            this.Writer.WriteAttributeString("monitorType", monitorType);
            this.Writer.WriteAttributeString("state", stateName);
            this.Writer.WriteAttributeString("action", actionName);
            this.Writer.WriteEndElement();
        }

        public void OnMonitorProcessEvent(string monitorType, string stateName, string senderName,
            string senderType, string senderStateName, Event e)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("MonitorEvent");
            this.Writer.WriteAttributeString("monitorType", monitorType);
            this.Writer.WriteAttributeString("state", stateName);

            if (senderName != null && senderType != null)
            {
                this.Writer.WriteAttributeString("senderName", senderName);
                this.Writer.WriteAttributeString("senderType", senderType);
            }
            else
            {
                this.Writer.WriteAttributeString("senderName", Task.CurrentId.ToString());
                this.Writer.WriteAttributeString("senderType", "task");
            }

            // TODO: should this be guarded as above?
            this.Writer.WriteAttributeString("senderState", senderStateName);

            this.Writer.WriteAttributeString("event", e.GetType().FullName);
            this.Writer.WriteEndElement();
        }

        public void OnMonitorRaiseEvent(string monitorType, string stateName, Event e)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("MonitorRaise");
            this.Writer.WriteAttributeString("monitorType", monitorType);
            this.Writer.WriteAttributeString("state", stateName);
            this.Writer.WriteAttributeString("event", e.GetType().FullName);
            this.Writer.WriteEndElement();
        }

        public void OnMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("MonitorState");
            this.Writer.WriteAttributeString("monitorType", monitorType);
            this.Writer.WriteAttributeString("state", stateName);
            this.Writer.WriteAttributeString("isEntry", isEntry.ToString());
            bool hot = isInHotState is true;
            this.Writer.WriteAttributeString("isInHotState", hot.ToString());
            this.Writer.WriteEndElement();
        }

        public void OnMonitorError(string monitorType, string stateName, bool? isInHotState)
        {
        }

        public void OnRandom(bool result, string callerName, string callerType)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("Random");
            this.Writer.WriteAttributeString("result", result.ToString());

            if (callerName != null && callerType != null)
            {
                this.Writer.WriteAttributeString("creatorName", callerName);
                this.Writer.WriteAttributeString("creatorType", callerType);
            }
            else
            {
                this.Writer.WriteAttributeString("creatorName", Task.CurrentId.ToString());
                this.Writer.WriteAttributeString("creatorType", "task");
            }

            this.Writer.WriteEndElement();
        }

        public void OnRandom(int result, string callerName, string callerType)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteStartElement("Random");
            this.Writer.WriteAttributeString("result", result.ToString());

            if (callerName != null && callerType != null)
            {
                this.Writer.WriteAttributeString("creatorName", callerName);
                this.Writer.WriteAttributeString("creatorType", callerType);
            }
            else
            {
                this.Writer.WriteAttributeString("creatorName", Task.CurrentId.ToString());
                this.Writer.WriteAttributeString("creatorType", "task");
            }

            this.Writer.WriteEndElement();
        }

        public void OnAssertionFailure(string error)
        {
            if (this.IsClosed)
            {
                return;
            }

            this.Writer.WriteElementString("AssertionFailure", error);
        }

        /// <summary>
        /// Invoked when a log is complete (and is about to be closed).
        /// </summary>
        public void OnCompleted()
        {
            this.IsClosed = true;
            using (this.Writer)
            {
                this.Writer.WriteEndElement();
            }
        }
    }
}
