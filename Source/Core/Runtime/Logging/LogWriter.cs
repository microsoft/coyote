// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Coyote.IO;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Manages the installed <see cref="TextWriter"/> and all registered <see cref="IRuntimeLog"/> objects.
    /// </summary>
    internal class LogWriter
    {
        /// <summary>
        /// The set of registered log writers.
        /// </summary>
        protected readonly HashSet<IRuntimeLog> Logs;

        /// <summary>
        /// Used to log messages.
        /// </summary>
        protected internal ILogger Logger { get; protected set; }

        /// <summary>
        /// The log level to report.
        /// </summary>
        protected readonly LogSeverity LogLevel;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogWriter"/> class.
        /// </summary>
        internal LogWriter(Configuration configuration)
        {
            this.Logs = new HashSet<IRuntimeLog>();
            this.LogLevel = configuration.LogLevel;

            if (configuration.IsVerbose)
            {
                this.GetOrCreateLogTextFormatter();
            }
            else
            {
                this.Logger = new NullLogger();
            }
        }

        /// <summary>
        /// Logs that the specified monitor has been created.
        /// </summary>
        /// <param name="monitorType">The name of the type of the monitor that has been created.</param>
        public void LogCreateMonitor(string monitorType)
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
        public void LogMonitorExecuteAction(string monitorType, string stateName, string actionName)
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
        public void LogMonitorProcessEvent(string monitorType, string stateName, string senderName,
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
        public void LogMonitorRaiseEvent(string monitorType, string stateName, Event e)
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
        public void LogMonitorStateTransition(string monitorType, string stateName, bool isEntry, bool? isInHotState)
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
        public void LogMonitorError(string monitorType, string stateName, bool? isInHotState)
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
        public void LogRandom(bool result, string callerName, string callerType)
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
        public void LogRandom(int result, string callerName, string callerType)
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
        public void LogAssertionFailure(string error)
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
        /// Returns all registered logs of type <typeparamref name="TRuntimeLog"/>, if there are any.
        /// </summary>
        public IEnumerable<TRuntimeLog> GetLogsOfType<TRuntimeLog>()
            where TRuntimeLog : IRuntimeLog =>
            this.Logs.OfType<TRuntimeLog>();

        /// <summary>
        /// Use this method to override the default <see cref="ILogger"/> for logging messages.
        /// </summary>
        internal ILogger SetLogger(ILogger logger)
        {
            var prevLogger = this.Logger;
            if (logger is null)
            {
                this.Logger = new NullLogger();

                var textLog = this.GetLogsOfType<RuntimeLogTextFormatter>().FirstOrDefault();
                if (textLog != null)
                {
                    textLog.Logger = this.Logger;
                }
            }
            else
            {
                this.Logger = logger;

                // Overrides the original verbosity flag and creates a new text logger.
                var textLog = this.GetOrCreateLogTextFormatter();
                textLog.Logger = this.Logger;
            }

            return prevLogger;
        }

        /// <summary>
        /// Returns an existing or new <see cref="RuntimeLogTextFormatter"/>.
        /// </summary>
        protected virtual RuntimeLogTextFormatter GetOrCreateLogTextFormatter()
        {
            var textLog = this.GetLogsOfType<RuntimeLogTextFormatter>().FirstOrDefault();
            if (textLog is null)
            {
                if (this.Logger is null)
                {
                    this.Logger = new ConsoleLogger() { LogLevel = this.LogLevel };
                }

                textLog = new RuntimeLogTextFormatter
                {
                    Logger = this.Logger
                };

                this.Logs.Add(textLog);
            }

            return textLog;
        }

        /// <summary>
        /// Use this method to register an <see cref="IRuntimeLog"/>.
        /// </summary>
        internal void RegisterLog(IRuntimeLog log)
        {
            if (log is null)
            {
                throw new InvalidOperationException("Cannot register a null log.");
            }

            // Make sure we only have one text logger.
            if (log is RuntimeLogTextFormatter a)
            {
                var textLog = this.GetLogsOfType<RuntimeLogTextFormatter>().FirstOrDefault();
                if (textLog != null)
                {
                    this.Logs.Remove(textLog);
                }

                if (this.Logger != null)
                {
                    a.Logger = this.Logger;
                }
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
    }
}
