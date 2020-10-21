// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
#if !DEBUG
using System.Diagnostics;
#endif
using System.Globalization;
using System.Linq;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Coverage;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting;

namespace Microsoft.Coyote.Specifications
{
    /// <summary>
    /// Provides methods for writing specifications and checking if they hold.
    /// </summary>
    internal sealed class SpecificationEngine : IDisposable
    {
        /// <summary>
        /// The configuration used by the runtime.
        /// </summary>
        private readonly Configuration Configuration;

        /// <summary>
        /// The asynchronous operation scheduler, if available.
        /// </summary>
        private readonly OperationScheduler Scheduler;

        /// <summary>
        /// List of monitors in the program.
        /// </summary>
        private readonly List<Monitor> Monitors;

        /// <summary>
        /// True if monitors are enabled, else false.
        /// </summary>
        private readonly bool IsMonitoringEnabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpecificationEngine"/> class.
        /// </summary>
        internal SpecificationEngine(Configuration configuration, OperationScheduler scheduler, List<Monitor> monitors)
        {
            this.Configuration = configuration;
            this.Scheduler = scheduler;
            this.Monitors = monitors;
            this.IsMonitoringEnabled = scheduler != null || configuration.IsMonitoringEnabledInInProduction;
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void Assert(bool predicate)
        {
            if (!predicate)
            {
                string msg = "Detected an assertion failure.";
                if (!CoyoteRuntime.IsExecutionControlled)
                {
                    throw new AssertionFailureException(msg);
                }

                this.Scheduler.NotifyAssertionFailure(msg);
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void Assert(bool predicate, string s, object arg0)
        {
            if (!predicate)
            {
                var msg = string.Format(CultureInfo.InvariantCulture, s, arg0?.ToString());
                if (!CoyoteRuntime.IsExecutionControlled)
                {
                    throw new AssertionFailureException(msg);
                }

                this.Scheduler.NotifyAssertionFailure(msg);
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void Assert(bool predicate, string s, object arg0, object arg1)
        {
            if (!predicate)
            {
                var msg = string.Format(CultureInfo.InvariantCulture, s, arg0?.ToString(), arg1?.ToString());
                if (!CoyoteRuntime.IsExecutionControlled)
                {
                    throw new AssertionFailureException(msg);
                }

                this.Scheduler.NotifyAssertionFailure(msg);
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void Assert(bool predicate, string s, object arg0, object arg1, object arg2)
        {
            if (!predicate)
            {
                var msg = string.Format(CultureInfo.InvariantCulture, s, arg0?.ToString(), arg1?.ToString(), arg2?.ToString());
                if (!CoyoteRuntime.IsExecutionControlled)
                {
                    throw new AssertionFailureException(msg);
                }

                this.Scheduler.NotifyAssertionFailure(msg);
            }
        }

        /// <summary>
        /// Checks if the assertion holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void Assert(bool predicate, string s, params object[] args)
        {
            if (!predicate)
            {
                var msg = string.Format(CultureInfo.InvariantCulture, s, args);
                if (!CoyoteRuntime.IsExecutionControlled)
                {
                    throw new AssertionFailureException(msg);
                }

                this.Scheduler.NotifyAssertionFailure(msg);
            }
        }

        /// <summary>
        /// Throws an <see cref="AssertionFailureException"/> exception containing the specified exception.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void WrapAndThrowException(Exception exception, string s, params object[] args)
        {
            string msg = string.Format(CultureInfo.InvariantCulture, s, args);
            string message = string.Format(CultureInfo.InvariantCulture,
                "Exception '{0}' was thrown in {1}: {2}\n" +
                "from location '{3}':\n" +
                "The stack trace is:\n{4}",
                exception.GetType(), msg, exception.Message, exception.Source, exception.StackTrace);

            if (!CoyoteRuntime.IsExecutionControlled)
            {
                throw new AssertionFailureException(message, exception);
            }

            this.Scheduler.NotifyAssertionFailure(message);
        }

        /// <summary>
        /// Tries to create a new <see cref="Monitor"/> of the specified <see cref="Type"/>.
        /// </summary>
        internal bool TryCreateMonitor(Type type, CoverageInfo coverageInfo, LogWriter logWriter)
        {
            if (!this.IsMonitoringEnabled)
            {
                return false;
            }

            lock (this.Monitors)
            {
                if (this.Monitors.Any(m => m.GetType() == type))
                {
                    // Idempotence: only one monitor per type can exist.
                    return false;
                }
            }

            this.Assert(type.IsSubclassOf(typeof(Monitor)), "Type '{0}' is not a subclass of Monitor.", type.FullName);

            Monitor monitor = (Monitor)Activator.CreateInstance(type);
            monitor.Initialize(this.Configuration, this, logWriter);
            monitor.InitializeStateInformation();

            lock (this.Monitors)
            {
                this.Monitors.Add(monitor);
            }

            if (CoyoteRuntime.IsExecutionControlled && this.Configuration.ReportActivityCoverage)
            {
                monitor.ReportActivityCoverage(coverageInfo);
            }

            monitor.GotoStartState();
            return true;
        }

        /// <summary>
        /// Invokes the specified <see cref="Monitor"/> with the specified <see cref="Event"/>.
        /// </summary>
        internal void InvokeMonitor(Type type, Event e, string senderName, string senderType, string senderStateName)
        {
            if (!this.IsMonitoringEnabled)
            {
                return;
            }

            Monitor monitor = null;
            lock (this.Monitors)
            {
                foreach (var m in this.Monitors)
                {
                    if (m.GetType() == type)
                    {
                        monitor = m;
                        break;
                    }
                }
            }

            if (monitor != null)
            {
                if (!CoyoteRuntime.IsExecutionControlled)
                {
                    lock (monitor)
                    {
                        monitor.MonitorEvent(e, senderName, senderType, senderStateName);
                    }
                }
                else
                {
                    // TODO: check if its safe to invoke the monitor with a lock during systematic testing.
                    monitor.MonitorEvent(e, senderName, senderType, senderStateName);
                }
            }
        }

        /// <summary>
        /// Asserts that no monitor is in a hot state at test termination.
        /// </summary>
        /// <remarks>
        /// If the test is still running, then this method returns without performing a check.
        /// </remarks>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void AssertNoMonitorInHotStateAtTermination()
        {
            if (this.Scheduler.HasFullyExploredSchedule)
            {
                foreach (var monitor in this.Monitors)
                {
                    if (monitor.IsInHotState(out string stateName))
                    {
                        string msg = string.Format(CultureInfo.InvariantCulture,
                            "{0} detected liveness bug in hot state '{1}' at the end of program execution.",
                            monitor.GetType().FullName, stateName);
                        this.Scheduler.NotifyAssertionFailure(msg, killTasks: false, cancelExecution: false);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the current hashed state of the monitors.
        /// </summary>
        /// <remarks>
        /// The hash is updated in each execution step.
        /// </remarks>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal int GetHashedMonitorState()
        {
            unchecked
            {
                int hash = 19;

                foreach (var monitor in this.Monitors)
                {
                    hash = (hash * 397) + monitor.GetHashedState();
                }

                return hash;
            }
        }

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Monitors.Clear();
            }
        }

        /// <summary>
        /// Disposes runtime resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
