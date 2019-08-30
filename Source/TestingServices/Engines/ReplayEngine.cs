// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;

using Microsoft.Coyote.IO;
using Microsoft.Coyote.TestingServices.Runtime;
using Microsoft.Coyote.TestingServices.Scheduling.Strategies;
using Microsoft.Coyote.Utilities;

namespace Microsoft.Coyote.TestingServices
{
    /// <summary>
    /// The Coyote replay engine.
    /// </summary>
    internal sealed class ReplayEngine : AbstractTestingEngine
    {
        /// <summary>
        /// Text describing an internal replay error.
        /// </summary>
        internal string InternalError { get; private set; }

        /// <summary>
        /// Creates a new replaying engine.
        /// </summary>
        public static ReplayEngine Create(Configuration configuration)
        {
            configuration.SchedulingStrategy = SchedulingStrategy.Replay;
            return new ReplayEngine(configuration);
        }

        /// <summary>
        /// Creates a new replaying engine.
        /// </summary>
        public static ReplayEngine Create(Configuration configuration, Assembly assembly)
        {
            configuration.SchedulingStrategy = SchedulingStrategy.Replay;
            return new ReplayEngine(configuration, assembly);
        }

        /// <summary>
        /// Creates a new replaying engine.
        /// </summary>
        public static ReplayEngine Create(Configuration configuration, Action<ICoyoteRuntime> action)
        {
            configuration.SchedulingStrategy = SchedulingStrategy.Replay;
            return new ReplayEngine(configuration, action);
        }

        /// <summary>
        /// Creates a new replaying engine.
        /// </summary>
        public static ReplayEngine Create(Configuration configuration, Action<ICoyoteRuntime> action, string trace)
        {
            configuration.SchedulingStrategy = SchedulingStrategy.Replay;
            configuration.ScheduleTrace = trace;
            return new ReplayEngine(configuration, action);
        }

        /// <summary>
        /// Creates a new replaying engine.
        /// </summary>
        public static ReplayEngine Create(Configuration configuration, Func<ICoyoteRuntime, Task> function)
        {
            configuration.SchedulingStrategy = SchedulingStrategy.Replay;
            return new ReplayEngine(configuration, function);
        }

        /// <summary>
        /// Creates a new replaying engine.
        /// </summary>
        public static ReplayEngine Create(Configuration configuration, Func<ICoyoteRuntime, Task> function, string trace)
        {
            configuration.SchedulingStrategy = SchedulingStrategy.Replay;
            configuration.ScheduleTrace = trace;
            return new ReplayEngine(configuration, function);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayEngine"/> class.
        /// </summary>
        private ReplayEngine(Configuration configuration)
            : base(configuration)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayEngine"/> class.
        /// </summary>
        private ReplayEngine(Configuration configuration, Assembly assembly)
            : base(configuration, assembly)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayEngine"/> class.
        /// </summary>
        private ReplayEngine(Configuration configuration, Action<ICoyoteRuntime> action)
            : base(configuration, action)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayEngine"/> class.
        /// </summary>
        private ReplayEngine(Configuration configuration, Func<ICoyoteRuntime, Task> function)
            : base(configuration, function)
        {
        }

        /// <summary>
        /// Creates a new testing task.
        /// </summary>
        protected override Task CreateTestingTask()
        {
            return new Task(() =>
            {
                // Runtime used to serialize and test the program.
                SystematicTestingRuntime runtime = null;

                // Logger used to intercept the program output if no custom logger
                // is installed and if verbosity is turned off.
                InMemoryLogger runtimeLogger = null;

                // Gets a handle to the standard output and error streams.
                var stdOut = Console.Out;
                var stdErr = Console.Error;

                try
                {
                    if (this.TestInitMethod != null)
                    {
                        // Initializes the test state.
                        this.TestInitMethod.Invoke(null, Array.Empty<object>());
                    }

                    // Creates a new instance of the testing runtime.
                    if (this.TestRuntimeFactoryMethod != null)
                    {
                        runtime = (SystematicTestingRuntime)this.TestRuntimeFactoryMethod.Invoke(
                            null,
                            new object[] { this.Configuration, this.Strategy });
                    }
                    else
                    {
                        runtime = new SystematicTestingRuntime(this.Configuration, this.Strategy);
                    }

                    // If verbosity is turned off, then intercept the program log, and also redirect
                    // the standard output and error streams into the runtime logger.
                    if (!this.Configuration.IsVerbose)
                    {
                        runtimeLogger = new InMemoryLogger();
                        runtime.SetLogger(runtimeLogger);

                        var writer = new LogWriter(new NulLogger());
                        Console.SetOut(writer);
                        Console.SetError(writer);
                    }

                    // Runs the test inside the test-harness machine.
                    if (this.TestAction != null)
                    {
                        runtime.RunTestHarness(this.TestAction, this.TestName);
                    }
                    else
                    {
                        runtime.RunTestHarness(this.TestFunction, this.TestName);
                    }

                    // Wait for the test to terminate.
                    runtime.WaitAsync().Wait();

                    // Invokes user-provided cleanup for this iteration.
                    if (this.TestIterationDisposeMethod != null)
                    {
                        // Disposes the test state.
                        this.TestIterationDisposeMethod.Invoke(null, Array.Empty<object>());
                    }

                    // Invokes user-provided cleanup for all iterations.
                    if (this.TestDisposeMethod != null)
                    {
                        // Disposes the test state.
                        this.TestDisposeMethod.Invoke(null, Array.Empty<object>());
                    }

                    this.InternalError = (this.Strategy as ReplayStrategy).ErrorText;

                    // Checks that no monitor is in a hot state at termination. Only
                    // checked if no safety property violations have been found.
                    if (!runtime.Scheduler.BugFound && this.InternalError.Length == 0)
                    {
                        runtime.CheckNoMonitorInHotStateAtTermination();
                    }

                    if (runtime.Scheduler.BugFound && this.InternalError.Length == 0)
                    {
                        this.ErrorReporter.WriteErrorLine(runtime.Scheduler.BugReport);
                    }

                    TestReport report = runtime.Scheduler.GetReport();
                    report.CoverageInfo.Merge(runtime.CoverageInfo);
                    this.TestReport.Merge(report);
                }
                catch (TargetInvocationException ex)
                {
                    if (!(ex.InnerException is TaskCanceledException))
                    {
                        ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                    }
                }
                finally
                {
                    if (!this.Configuration.IsVerbose)
                    {
                        // Restores the standard output and error streams.
                        Console.SetOut(stdOut);
                        Console.SetError(stdErr);
                    }

                    // Cleans up the runtime.
                    runtimeLogger?.Dispose();
                    runtime?.Dispose();
                }
            }, this.CancellationTokenSource.Token);
        }

        /// <summary>
        /// Returns a report with the testing results.
        /// </summary>
        public override string GetReport()
        {
            StringBuilder report = new StringBuilder();

            report.AppendFormat("... Reproduced {0} bug{1}.", this.TestReport.NumOfFoundBugs,
                this.TestReport.NumOfFoundBugs == 1 ? string.Empty : "s");
            report.AppendLine();

            report.Append($"... Elapsed {this.Profiler.Results()} sec.");

            return report.ToString();
        }
    }
}
