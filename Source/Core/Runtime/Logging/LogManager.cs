// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Logging;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Manages all registered <see cref="IRuntimeLog"/> objects.
    /// </summary>
    internal class LogManager
    {
        /// <summary>
        /// The set of registered log writers.
        /// </summary>
        protected readonly HashSet<IRuntimeLog> Logs = new HashSet<IRuntimeLog>();

        /// <summary>
        /// Logs that the specified monitor has been created.
        /// </summary>
        /// <param name="monitorType">The name of the type of the monitor that has been created.</param>
        internal void LogCreateMonitor(string monitorType)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    log.OnCreateMonitor(monitorType);
                }
            }
        }

        /// <summary>
        /// Logs that the specified monitor executes an action.
        /// </summary>
        /// <param name="monitorType">Name of type of the monitor that is executing the action.</param>
        /// <param name="stateName">The name of the state in which the action is being executed.</param>
        /// <param name="actionName">The name of the action being executed.</param>
        internal void LogMonitorExecuteAction(string monitorType, string stateName, string actionName)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    log.OnMonitorExecuteAction(monitorType, stateName, actionName);
                }
            }
        }

        /// <summary>
        /// Logs that the specified monitor is about to process an event.
        /// </summary>
        /// <param name="monitorType">Name of type of the monitor that will process the event.</param>
        /// <param name="stateName">The name of the state in which the event is being raised.</param>
        /// <param name="senderName">The name of the sender, if any.</param>
        /// <param name="senderType">The type of the sender, if any.</param>
        /// <param name="senderStateName">The name of the state the sender is in.</param>
        /// <param name="e">The event being processed.</param>
        internal void LogMonitorProcessEvent(string monitorType, string stateName, string senderName,
            string senderType, string senderStateName, Event e)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    log.OnMonitorProcessEvent(monitorType, stateName, senderName, senderType, senderStateName, e);
                }
            }
        }

        /// <summary>
        /// Logs that the specified monitor raised an event.
        /// </summary>
        /// <param name="monitorType">Name of type of the monitor raising the event.</param>
        /// <param name="stateName">The name of the state in which the event is being raised.</param>
        /// <param name="e">The event being raised.</param>
        internal void LogMonitorRaiseEvent(string monitorType, string stateName, Event e)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    log.OnMonitorRaiseEvent(monitorType, stateName, e);
                }
            }
        }

        /// <summary>
        /// Logs that the specified monitor enters or exits a state.
        /// </summary>
        /// <param name="monitorType">The name of the type of the monitor entering or exiting the state.</param>
        /// <param name="stateName">The name of the state being entered or exited; if <paramref name="isInHotState"/>
        /// is not null, then the temperature is appended to the statename in brackets, e.g. "stateName[hot]".</param>
        /// <param name="isEntry">If true, this is called for a state entry; otherwise, exit.</param>
        /// <param name="isInHotState">If true, the monitor is in a hot state; if false, the monitor is in a cold state;
        /// else no liveness state is available.</param>
        internal void LogMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    log.OnMonitorStateTransition(monitorType, stateName, isEntry, isInHotState);
                }
            }
        }

        /// <summary>
        /// Logs that the specified monitor has found an error.
        /// </summary>
        /// <param name="monitorType">The name of the type of the monitor.</param>
        /// <param name="stateName">The name of the current state.</param>
        /// <param name="isInHotState">If true, the monitor is in a hot state; if false, the monitor is in a cold state;
        /// else no liveness state is available.</param>
        internal void LogMonitorError(string monitorType, string stateName, bool? isInHotState)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    log.OnMonitorError(monitorType, stateName, isInHotState);
                }
            }
        }

        /// <summary>
        /// Logs that the specified random boolean result has been obtained.
        /// </summary>
        /// <param name="result">The random boolean result.</param>
        /// <param name="callerName">The name of the caller, if any.</param>
        /// <param name="callerType">The type of the caller, if any.</param>
        internal void LogRandom(bool result, string callerName, string callerType)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    log.OnRandom(result, callerName, callerType);
                }
            }
        }

        /// <summary>
        /// Logs that the specified random integer result has been obtained.
        /// </summary>
        /// <param name="result">The random integer result.</param>
        /// <param name="callerName">The name of the caller, if any.</param>
        /// <param name="callerType">The type of the caller, if any.</param>
        internal void LogRandom(int result, string callerName, string callerType)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    log.OnRandom(result, callerName, callerType);
                }
            }
        }

        /// <summary>
        /// Logs that the specified assertion failure has occurred.
        /// </summary>
        /// <param name="error">The text of the error.</param>
        internal void LogAssertionFailure(string error)
        {
            if (this.Logs.Count > 0)
            {
                foreach (var log in this.Logs)
                {
                    log.OnAssertionFailure(error);
                }
            }
        }

        /// <summary>
        /// Use this method to notify all logs that the test iteration is complete.
        /// </summary>
        internal void LogCompletion()
        {
            foreach (var log in this.Logs)
            {
                log.OnCompleted();
            }
        }

        /// <summary>
        /// Use this method to register an <see cref="IRuntimeLog"/>.
        /// </summary>
        internal void RegisterLog(IRuntimeLog log, LogWriter logWriter)
        {
            if (log is null)
            {
                throw new InvalidOperationException("Cannot register a null log.");
            }

            if (log is RuntimeLogTextFormatter textFormatter)
            {
                textFormatter.LogWriter = logWriter;

                // Ensure we only have one text formatter by replacing the previous one, if it exists.
                this.RemoveLog(this.GetLogsOfType<RuntimeLogTextFormatter>().FirstOrDefault());
            }

            this.Logs.Add(log);
        }

        /// <summary>
        /// Use this method to unregister a previously registered <see cref="IRuntimeLog"/>.
        /// </summary>
        internal void RemoveLog(IRuntimeLog log)
        {
            if (log != null)
            {
                this.Logs.Remove(log);
            }
        }

        /// <summary>
        /// Returns all registered logs of type <typeparamref name="TRuntimeLog"/>, if there are any.
        /// </summary>
        internal IEnumerable<TRuntimeLog> GetLogsOfType<TRuntimeLog>()
            where TRuntimeLog : IRuntimeLog =>
            this.Logs.OfType<TRuntimeLog>();
    }
}
