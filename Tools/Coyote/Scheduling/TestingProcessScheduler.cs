// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Microsoft.Coyote.SystematicTesting
{
    internal sealed class TestingProcessScheduler
    {
        /// <summary>
        /// Configuration.
        /// </summary>
        private readonly Configuration Configuration;

        /// <summary>
        /// The test reports per process.
        /// </summary>
        private readonly ConcurrentDictionary<uint, TestReport> TestReports;

        /// <summary>
        /// The global test report, which contains merged information
        /// from the test report of each testing process.
        /// </summary>
        private readonly TestReport GlobalTestReport;

        /// <summary>
        /// The testing profiler.
        /// </summary>
        private readonly Profiler Profiler;

        /// <summary>
        /// Set if ctrl-c or ctrl-break occurred.
        /// </summary>
        internal static bool IsProcessCanceled;

        /// <summary>
        /// Initializes a new instance of the <see cref="TestingProcessScheduler"/> class.
        /// </summary>
        private TestingProcessScheduler(Configuration configuration)
        {
            this.TestReports = new ConcurrentDictionary<uint, TestReport>();
            this.GlobalTestReport = new TestReport(configuration);
            this.Profiler = new Profiler();

            configuration.EnableColoredConsoleOutput = true;
            this.Configuration = configuration;
        }

        /// <summary>
        /// Creates a new testing process scheduler.
        /// </summary>
        internal static TestingProcessScheduler Create(Configuration configuration)
        {
            return new TestingProcessScheduler(configuration);
        }

        /// <summary>
        /// Runs the Coyote testing scheduler.
        /// </summary>
        internal void Run()
        {
            Console.WriteLine($"... Started the testing task scheduler (process:{Process.GetCurrentProcess().Id}).");

            this.Profiler.StartMeasuringExecutionTime();

            TestingProcess testingProcess = TestingProcess.Create(this.Configuration);

            Console.WriteLine($"... Created '1' testing task (process:{Process.GetCurrentProcess().Id}).");

            // Runs the testing process.
            testingProcess.Run();

            // Get and merge the test report.
            TestReport testReport = testingProcess.GetTestReport();
            if (testReport != null)
            {
                this.MergeTestReport(testReport, 0);
            }

            this.Profiler.StopMeasuringExecutionTime();

            if (!IsProcessCanceled)
            {
                // Merges and emits the test report.
                this.EmitTestReport();
            }
        }

        /// <summary>
        /// Merges the test report from the specified process.
        /// </summary>
        private void MergeTestReport(TestReport testReport, uint processId)
        {
            if (this.TestReports.TryAdd(processId, testReport))
            {
                // Merges the test report into the global report.
                IO.Debug.WriteLine($"... Merging task {processId} test report.");
                this.GlobalTestReport.Merge(testReport);
            }
            else
            {
                IO.Debug.WriteLine($"... Unable to merge test report from task '{processId}'. " +
                    " Report is already merged.");
            }
        }

        /// <summary>
        /// Emits the test report.
        /// </summary>
        private void EmitTestReport()
        {
            if (this.TestReports.Count is 0)
            {
                Environment.ExitCode = (int)ExitCode.InternalError;
                return;
            }

            if (this.Configuration.IsActivityCoverageReported)
            {
                Console.WriteLine($"... Emitting coverage reports:");
                Reporter.EmitTestingCoverageReport(this.GlobalTestReport);
            }

            if (this.Configuration.DebugActivityCoverage)
            {
                Console.WriteLine($"... Emitting debug coverage reports:");
                foreach (var report in this.TestReports)
                {
                    Reporter.EmitTestingCoverageReport(report.Value, report.Key, isDebug: true);
                }
            }

            Console.WriteLine(this.GlobalTestReport.GetText(this.Configuration, "..."));
            Console.WriteLine($"... Elapsed {this.Profiler.Results()} sec.");

            if (this.GlobalTestReport.InternalErrors.Count > 0)
            {
                Environment.ExitCode = (int)ExitCode.InternalError;
            }
            else if (this.GlobalTestReport.NumOfFoundBugs > 0)
            {
                Environment.ExitCode = (int)ExitCode.BugFound;
            }
            else
            {
                Environment.ExitCode = (int)ExitCode.Success;
            }
        }
    }
}
