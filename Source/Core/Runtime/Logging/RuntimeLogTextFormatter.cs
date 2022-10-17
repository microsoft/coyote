// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using Microsoft.Coyote.Logging;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// This class implements <see cref="IRuntimeLog"/> and generates output in a a human readable text format.
    /// </summary>
    /// <remarks>
    /// See <see href="/coyote/concepts/actors/logging">Logging</see> for more information.
    /// </remarks>
    public class RuntimeLogTextFormatter : IRuntimeLog
    {
        /// <summary>
        /// Logs messages using the installed <see cref="ILogger"/>.
        /// </summary>
        internal LogWriter LogWriter;

        /// <summary>
        /// Used for logging runtime messages.
        /// </summary>
        protected ILogger Logger => this.LogWriter;

        /// <inheritdoc/>
        public virtual void OnCreateMonitor(string monitorType)
        {
            this.Logger.WriteLine("<CreateLog> {0} was created.", monitorType);
        }

        /// <inheritdoc/>
        public virtual void OnMonitorExecuteAction(string monitorType, string stateName, string actionName) =>
            this.Logger.WriteLine("<MonitorLog> {0} executed action '{1}' in state '{2}'.", monitorType, actionName, stateName);

        /// <inheritdoc/>
        public virtual void OnMonitorProcessEvent(string monitorType, string stateName, string senderName,
            string senderType, string senderStateName, Event e) =>
            this.Logger.WriteLine("<MonitorLog> {0} is processing event '{1}' in state '{2}'.",
                monitorType, e.GetType().FullName, stateName);

        /// <inheritdoc/>
        public virtual void OnMonitorRaiseEvent(string monitorType, string stateName, Event e) =>
            this.Logger.WriteLine("<MonitorLog> {0} raised event '{1}' in state '{2}'.",
                monitorType, e.GetType().FullName, stateName);

        /// <inheritdoc/>
        public virtual void OnMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState)
        {
            var liveness = isInHotState.HasValue ? (isInHotState.Value ? "hot " : "cold ") : string.Empty;
            var direction = isEntry ? "enters" : "exits";
            var text = $"<MonitorLog> {monitorType} {direction} {liveness}state '{stateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnMonitorError(string monitorType, string stateName, bool? isInHotState) =>
            this.Logger.WriteLine(LogSeverity.Error, "<MonitorLog> {0} found an error in state {1}.", monitorType, stateName);

        /// <inheritdoc/>
        public virtual void OnRandom(bool result, string callerName, string callerType)
        {
            var source = callerName ?? $"Thread '{Thread.CurrentThread.ManagedThreadId}'";
            this.Logger.WriteLine("<RandomLog> {0} non-deterministically chose '{1}'.", source, result);
        }

        /// <inheritdoc/>
        public virtual void OnRandom(int result, string callerName, string callerType)
        {
            var source = callerName ?? $"Thread '{Thread.CurrentThread.ManagedThreadId}'";
            this.Logger.WriteLine("<RandomLog> {0} non-deterministically chose '{1}'.", source, result);
        }

        /// <inheritdoc/>
        public virtual void OnAssertionFailure(string error) => this.Logger.WriteLine(LogSeverity.Error, error);

        /// <inheritdoc/>
        public virtual void OnCompleted()
        {
        }
    }
}
