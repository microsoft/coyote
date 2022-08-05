// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using Microsoft.Coyote.IO;

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
        /// Get or set the <see cref="ILogger"/> interface to the logger.
        /// </summary>
        /// <remarks>
        /// If you want Coyote to log to an existing TextWriter, then use the <see cref="TextWriterLogger"/> object
        /// but that will have a minor performance overhead, so it is better to use <see cref="ILogger"/> directly.
        /// </remarks>
        public ILogger Logger { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RuntimeLogTextFormatter"/> class.
        /// </summary>
        public RuntimeLogTextFormatter()
        {
            this.Logger = new ConsoleLogger();
        }

        /// <inheritdoc/>
        public virtual void OnCreateMonitor(string monitorType)
        {
            var text = $"<CreateLog> {monitorType} was created.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnMonitorExecuteAction(string monitorType, string stateName, string actionName)
        {
            string text = $"<MonitorLog> {monitorType} executed action '{actionName}' in state '{stateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnMonitorProcessEvent(string monitorType, string stateName, string senderName,
            string senderType, string senderStateName, Event e)
        {
            string eventName = e.GetType().FullName;
            string text = $"<MonitorLog> {monitorType} is processing event '{eventName}' in state '{stateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnMonitorRaiseEvent(string monitorType, string stateName, Event e)
        {
            string eventName = e.GetType().FullName;
            string text = $"<MonitorLog> {monitorType} raised event '{eventName}' in state '{stateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState)
        {
            var liveness = isInHotState.HasValue ? (isInHotState.Value ? "hot " : "cold ") : string.Empty;
            var direction = isEntry ? "enters" : "exits";
            var text = $"<MonitorLog> {monitorType} {direction} {liveness}state '{stateName}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnMonitorError(string monitorType, string stateName, bool? isInHotState)
        {
            this.Logger.WriteLine(LogSeverity.Error, $"<MonitorLog> {monitorType} found an error in state {stateName}.");
        }

        /// <inheritdoc/>
        public virtual void OnRandom(bool result, string callerName, string callerType)
        {
            var source = callerName ?? $"Thread '{Thread.CurrentThread.ManagedThreadId}'";
            var text = $"<RandomLog> {source} nondeterministically chose '{result}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnRandom(int result, string callerName, string callerType)
        {
            var source = callerName ?? $"Thread '{Thread.CurrentThread.ManagedThreadId}'";
            var text = $"<RandomLog> {source} nondeterministically chose '{result}'.";
            this.Logger.WriteLine(text);
        }

        /// <inheritdoc/>
        public virtual void OnAssertionFailure(string error)
        {
            this.Logger.WriteLine(LogSeverity.Error, error);
        }

        /// <inheritdoc/>
        public virtual void OnCompleted()
        {
        }
    }
}
