// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Actors.Coverage;
using Microsoft.Coyote.Runtime;

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
        /// Responsible for controlling the program execution during systematic testing.
        /// </summary>
        private readonly CoyoteRuntime Runtime;

        /// <summary>
        /// List of liveness monitors in the program.
        /// </summary>
        private readonly List<TaskLivenessMonitor> LivenessMonitors;

        /// <summary>
        /// List of safety and liveness state-machine monitors in the program.
        /// </summary>
        private readonly List<Monitor> StateMachineMonitors;

        /// <summary>
        /// True if monitors are enabled, else false.
        /// </summary>
        private readonly bool IsMonitoringEnabled;

        /// <summary>
        /// Initializes a new instance of the <see cref="SpecificationEngine"/> class.
        /// </summary>
        internal SpecificationEngine(Configuration configuration, CoyoteRuntime runtime)
        {
            this.Configuration = configuration;
            this.Runtime = runtime;
            this.LivenessMonitors = new List<TaskLivenessMonitor>();
            this.StateMachineMonitors = new List<Monitor>();
            this.IsMonitoringEnabled = runtime.SchedulingPolicy != SchedulingPolicy.None ||
                configuration.IsMonitoringEnabledInInProduction;
        }

        /// <summary>
        /// Creates a liveness monitor that checks if the specified task eventually completes execution successfully.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal void MonitorTaskCompletion(Task task)
        {
            if (this.Runtime.SchedulingPolicy is SchedulingPolicy.Interleaving &&
                task.Status != TaskStatus.RanToCompletion)
            {
                var monitor = new TaskLivenessMonitor(task);
                this.LivenessMonitors.Add(monitor);
            }
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

            lock (this.StateMachineMonitors)
            {
                if (this.StateMachineMonitors.Any(m => m.GetType() == type))
                {
                    // Idempotence: only one monitor per type can exist.
                    return false;
                }
            }

            this.Assert(type.IsSubclassOf(typeof(Monitor)), "Type '{0}' is not a subclass of Monitor.", type.FullName);

            Monitor monitor = (Monitor)Activator.CreateInstance(type);
            monitor.Initialize(this.Configuration, this, logWriter);
            monitor.InitializeStateInformation();

            lock (this.StateMachineMonitors)
            {
                this.StateMachineMonitors.Add(monitor);
            }

            if (this.Runtime.SchedulingPolicy is SchedulingPolicy.Interleaving
                && this.Configuration.IsActivityCoverageReported)
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
            lock (this.StateMachineMonitors)
            {
                foreach (var m in this.StateMachineMonitors)
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
                if (this.Runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    // TODO: check if its safe to invoke the monitor with a lock during systematic testing.
                    monitor.MonitorEvent(e, senderName, senderType, senderStateName);
                }
                else
                {
                    lock (monitor)
                    {
                        monitor.MonitorEvent(e, senderName, senderType, senderStateName);
                    }
                }
            }
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
                if (this.Runtime.SchedulingPolicy is SchedulingPolicy.None)
                {
                    throw new AssertionFailureException(msg);
                }

                this.Runtime.NotifyAssertionFailure(msg);
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
                if (this.Runtime.SchedulingPolicy is SchedulingPolicy.None)
                {
                    throw new AssertionFailureException(msg);
                }

                this.Runtime.NotifyAssertionFailure(msg);
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
                if (this.Runtime.SchedulingPolicy is SchedulingPolicy.None)
                {
                    throw new AssertionFailureException(msg);
                }

                this.Runtime.NotifyAssertionFailure(msg);
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
                if (this.Runtime.SchedulingPolicy is SchedulingPolicy.None)
                {
                    throw new AssertionFailureException(msg);
                }

                this.Runtime.NotifyAssertionFailure(msg);
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
                if (this.Runtime.SchedulingPolicy is SchedulingPolicy.None)
                {
                    throw new AssertionFailureException(msg);
                }

                this.Runtime.NotifyAssertionFailure(msg);
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

            if (this.Runtime.SchedulingPolicy is SchedulingPolicy.None)
            {
                throw new AssertionFailureException(message, exception);
            }

            this.Runtime.NotifyUnhandledException(exception, message);
        }

        /// <summary>
        /// Checks for liveness errors.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void CheckLivenessErrors()
        {
            foreach (var monitor in this.LivenessMonitors)
            {
                if (!monitor.IsSatisfied)
                {
                    string msg = string.Format(CultureInfo.InvariantCulture,
                        "Found liveness bug at the end of program execution.\nThe stack trace is:\n{0}",
                        GetStackTrace(monitor.StackTrace));
                    this.Runtime.NotifyAssertionFailure(msg);
                }
            }

            // Checks if there is a state-machine monitor stuck in a hot state.
            foreach (var monitor in this.StateMachineMonitors)
            {
                if (monitor.IsInHotState(out string stateName))
                {
                    string msg = string.Format(CultureInfo.InvariantCulture,
                        "{0} detected liveness bug in hot state '{1}' at the end of program execution.",
                        monitor.GetType().FullName, stateName);
                    this.Runtime.NotifyAssertionFailure(msg);
                }
            }
        }

        /// <summary>
        /// Checks if a liveness monitor exceeded its threshold and, if yes, it reports an error.
        /// </summary>
        internal void CheckLivenessThresholdExceeded()
        {
            foreach (var monitor in this.LivenessMonitors)
            {
                if (monitor.IsLivenessThresholdExceeded(this.Configuration.LivenessTemperatureThreshold))
                {
                    string msg = string.Format(CultureInfo.InvariantCulture,
                        "Found potential liveness bug at the end of program execution.\nThe stack trace is:\n{0}",
                        GetStackTrace(monitor.StackTrace));
                    this.Runtime.NotifyAssertionFailure(msg);
                }
            }

            foreach (var monitor in this.StateMachineMonitors)
            {
                if (monitor.IsLivenessThresholdExceeded(this.Configuration.LivenessTemperatureThreshold))
                {
                    string msg = $"{monitor.Name} detected potential liveness bug in hot state '{monitor.CurrentStateName}'.";
                    this.Runtime.NotifyAssertionFailure(msg);
                }
            }
        }

        private static string GetStackTrace(StackTrace trace)
        {
            StringBuilder sb = new StringBuilder();
            string[] lines = trace.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                if ((line.Contains("at Microsoft.Coyote.Specifications") ||
                    line.Contains("at Microsoft.Coyote.Runtime")) &&
                    !line.Contains($"at {typeof(Specification).FullName}.{nameof(Specification.Monitor)}"))
                {
                    continue;
                }

                sb.AppendLine(line);
            }

            return sb.ToString();
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

                foreach (var monitor in this.StateMachineMonitors)
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
                this.LivenessMonitors.Clear();
                this.StateMachineMonitors.Clear();
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
