// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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
        /// List of liveness monitors in the program.
        /// </summary>
        /// <remarks>
        /// This is a different type of monitor than the state-machine based <see cref="Monitor"/> one.
        /// </remarks>
        private readonly List<LivenessMonitor> LivenessMonitors;

        /// <summary>
        /// List of safety and liveness monitors in the program.
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
            this.LivenessMonitors = new List<LivenessMonitor>();
            this.Monitors = monitors;
            this.IsMonitoringEnabled = scheduler != null || configuration.IsMonitoringEnabledInInProduction;
        }

        /// <summary>
        /// Callback that is invoked on each scheduling step.
        /// </summary>
        internal void OnNextSchedulingStep()
        {
            foreach (var monitor in this.LivenessMonitors)
            {
                monitor.CheckProgress();
            }
        }

        /// <summary>
        /// Waits until the liveness property is satisfied.
        /// </summary>
#if !DEBUG
        [DebuggerStepThrough]
#endif
        internal Task WaitUntilLivenessPropertyIsSatisfied(Func<Task<bool>> predicate, Func<int> hashingFunction,
            TimeSpan delay, CancellationToken cancellationToken = default)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                var callerOp = this.Scheduler.GetExecutingOperation<TaskOperation>();
                var monitor = new LivenessMonitor(callerOp, predicate, hashingFunction);
                this.LivenessMonitors.Add(monitor);
                monitor.Wait();
                return Task.CompletedTask;
            }

            return WhenCompletedAsync(predicate, delay, cancellationToken);
        }

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when <paramref name="predicate"/> returns true.
        /// </summary>
        private static async Task WhenCompletedAsync(Func<Task<bool>> predicate, TimeSpan delay, CancellationToken cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }

                if (await predicate())
                {
                    break;
                }

                await Task.Delay(delay);
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
        /// Checks if the execution has deadlocked. This happens when there are no more enabled operations,
        /// but there is one or more blocked operations that are waiting to complete.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void CheckIfExecutionHasDeadlocked(IEnumerable<AsyncOperation> ops)
        {
            var blockedOnReceiveOperations = ops.Where(op => op.Status is AsyncOperationStatus.BlockedOnReceive).ToList();
            var blockedOnWaitOperations = ops.Where(op => op.Status is AsyncOperationStatus.BlockedOnWaitAll ||
                op.Status is AsyncOperationStatus.BlockedOnWaitAny).ToList();
            var blockedOnResources = ops.Where(op => op.Status is AsyncOperationStatus.BlockedOnResource).ToList();
            if (blockedOnReceiveOperations.Count is 0 &&
                blockedOnWaitOperations.Count is 0 &&
                blockedOnResources.Count is 0)
            {
                return;
            }

            var msg = new StringBuilder("Deadlock detected.");

            int blockedOperations = 0;
            if (blockedOnReceiveOperations.Count > 0)
            {
                for (int idx = 0; idx < blockedOnReceiveOperations.Count; idx++)
                {
                    msg.Append(string.Format(CultureInfo.InvariantCulture, " {0}", blockedOnReceiveOperations[idx].Name));
                    if (idx == blockedOnReceiveOperations.Count - 2)
                    {
                        msg.Append(" and");
                    }
                    else if (idx < blockedOnReceiveOperations.Count - 1)
                    {
                        msg.Append(",");
                    }

                    blockedOperations++;
                }

                msg.Append(blockedOnReceiveOperations.Count is 1 ? " is " : " are ");
                msg.Append("waiting to receive an event, but no other controlled tasks are enabled.");
            }

            if (blockedOnWaitOperations.Count > 0)
            {
                for (int idx = 0; idx < blockedOnWaitOperations.Count; idx++)
                {
                    msg.Append(string.Format(CultureInfo.InvariantCulture, " {0}", blockedOnWaitOperations[idx].Name));
                    if (idx == blockedOnWaitOperations.Count - 2)
                    {
                        msg.Append(" and");
                    }
                    else if (idx < blockedOnWaitOperations.Count - 1)
                    {
                        msg.Append(",");
                    }

                    blockedOperations++;
                }

                msg.Append(blockedOnWaitOperations.Count is 1 ? " is " : " are ");
                msg.Append("waiting for a task to complete, but no other controlled tasks are enabled.");
            }

            if (blockedOnResources.Count > 0)
            {
                for (int idx = 0; idx < blockedOnResources.Count; idx++)
                {
                    if (this.LivenessMonitors.Exists(m => !m.IsSatisfied && m.Operation.Id == blockedOnResources[idx].Id))
                    {
                        continue;
                    }

                    msg.Append(string.Format(CultureInfo.InvariantCulture, " {0}", blockedOnResources[idx].Name));
                    if (idx == blockedOnResources.Count - 2)
                    {
                        msg.Append(" and");
                    }
                    else if (idx < blockedOnResources.Count - 1)
                    {
                        msg.Append(",");
                    }

                    blockedOperations++;
                }

                msg.Append(blockedOnResources.Count is 1 ? " is " : " are ");
                msg.Append("waiting to acquire a resource that is already acquired, ");
                msg.Append("but no other controlled tasks are enabled.");
            }

            if (blockedOperations > 0)
            {
                this.Scheduler.NotifyAssertionFailure(msg.ToString());
            }
        }

        /// <summary>
        /// Checks for liveness errors.
        /// </summary>
#if !DEBUG
        [DebuggerHidden]
#endif
        internal void CheckLivenessErrors()
        {
            if (this.Scheduler.HasFullyExploredSchedule)
            {
                foreach (var monitor in this.LivenessMonitors)
                {
                    if (!monitor.IsSatisfied)
                    {
                        string msg = string.Format(CultureInfo.InvariantCulture,
                            "Found liveness bug at the end of program execution.\nThe stack trace is:\n{0}",
                            GetStackTrace(monitor.StackTrace));
                        this.Scheduler.NotifyAssertionFailure(msg, killTasks: false, cancelExecution: false);
                    }
                }

                // Checks if there is a monitor stuck in a hot state.
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

        private static string GetStackTrace(StackTrace trace)
        {
            StringBuilder sb = new StringBuilder();
            string[] lines = trace.ToString().Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                if ((line.Contains("at Microsoft.Coyote.Specifications") ||
                    line.Contains("at Microsoft.Coyote.Runtime")) &&
                    !line.Contains($"at {typeof(Specification).FullName}.{nameof(Specification.WhenTrue)}"))
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
                this.LivenessMonitors.Clear();
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
