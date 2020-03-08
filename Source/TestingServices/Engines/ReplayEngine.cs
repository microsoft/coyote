// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Coyote.IO;
using Microsoft.Coyote.Runtime.Exploration;
using Microsoft.Coyote.TestingServices.Runtime;
using Microsoft.Coyote.TestingServices.Scheduling.Strategies;

namespace Microsoft.Coyote.TestingServices
{
    /// <summary>
    /// The Coyote replay engine.
    /// </summary>
    [DebuggerStepThrough]
    internal sealed class ReplayEngine : AbstractTestingEngine
    {
        /// <summary>
        /// Text describing an internal replay error.
        /// </summary>
        internal string InternalError { get; private set; }

        /// <summary>
        /// Creates a new replaying engine.
        /// </summary>
        internal static ReplayEngine Create(Configuration configuration) =>
            Create(configuration, LoadAssembly(configuration.AssemblyToBeAnalyzed));

        /// <summary>
        /// Creates a new replaying engine.
        /// </summary>
        internal static ReplayEngine Create(Configuration configuration, Assembly assembly)
        {
            TestMethodInfo testMethodInfo = null;
            try
            {
                testMethodInfo = TestMethodInfo.GetFromAssembly(assembly, configuration.TestMethodName);
            }
            catch
            {
                Error.ReportAndExit($"Failed to get test method '{configuration.TestMethodName}' from assembly '{assembly.FullName}'");
            }

            configuration.SchedulingStrategy = SchedulingStrategy.Replay;
            return new ReplayEngine(configuration, testMethodInfo);
        }

        /// <summary>
        /// Creates a new replaying engine.
        /// </summary>
        internal static ReplayEngine Create(Configuration configuration, Delegate testMethod)
        {
            var testMethodInfo = new TestMethodInfo(testMethod);
            configuration.SchedulingStrategy = SchedulingStrategy.Replay;
            return new ReplayEngine(configuration, testMethodInfo);
        }

        /// <summary>
        /// Creates a new replaying engine.
        /// </summary>
        internal static ReplayEngine Create(Configuration configuration, Delegate testMethod, string trace)
        {
            var testMethodInfo = new TestMethodInfo(testMethod);
            configuration.SchedulingStrategy = SchedulingStrategy.Replay;
            configuration.ScheduleTrace = trace;
            return new ReplayEngine(configuration, testMethodInfo);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ReplayEngine"/> class.
        /// </summary>
        private ReplayEngine(Configuration configuration, TestMethodInfo testMethodInfo)
            : base(configuration, testMethodInfo)
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
                    // Invokes the user-specified initialization method.
                    this.TestMethodInfo.InitializeAllIterations();

                    // Creates a new instance of the systematic testing runtime.
                    runtime = new SystematicTestingRuntime(this.Configuration, this.Strategy);

                    // If verbosity is turned off, then intercept the program log, and also redirect
                    // the standard output and error streams into the runtime logger.
                    if (!this.Configuration.IsVerbose)
                    {
                        runtimeLogger = new InMemoryLogger();
                        runtime.SetLogger(runtimeLogger);

                        TextWriter writer = TextWriter.Null;
                        Console.SetOut(writer);
                        Console.SetError(writer);
                    }

                    this.InitializeCustomLogging(runtime);

                    if (this.Configuration.AttachDebugger)
                    {
                        Debugger.Launch();
                    }

                    // Runs the test and waits for it to terminate.
                    runtime.RunTest(this.TestMethodInfo.Method, this.TestMethodInfo.Name);
                    runtime.WaitAsync().Wait();

                    // Invokes the user-specified disposal methods.
                    this.TestMethodInfo.DisposeCurrentIteration();
                    this.TestMethodInfo.DisposeAllIterations();

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
                    report.CoverageInfo.Merge(runtime.GetCoverageInfo());
                    this.TestReport.Merge(report);
                }
                catch (Exception ex)
                {
                    Exception innerException = ex;
                    while (innerException is TargetInvocationException)
                    {
                        innerException = innerException.InnerException;
                    }

                    if (innerException is AggregateException)
                    {
                        innerException = innerException.InnerException;
                    }

                    if (!(innerException is TaskCanceledException))
                    {
                        ExceptionDispatchInfo.Capture(innerException).Throw();
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
