// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Coyote;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ImageGallery.Tests
{
    [TestClass]
    public class SystematicTests
    {
        [TestMethod]
        public void TestConcurrentAccountRequests()
        {
            var tests = new UnitTests();
            RunSystematicTest(tests.TestConcurrentAccountRequestsAsync, "TestConcurrentAccountRequests"); 
        }

        [TestMethod]
        public void TestConcurrentAccountAndImageRequests()
        {
            var tests = new UnitTests();
            RunSystematicTest(tests.TestConcurrentAccountAndImageRequestsAsync, "TestConcurrentAccountAndImageRequests");
        }

        /// <summary>
        /// Invoke the Coyote systematic testing engine to run the specified test multiple iterations,
        /// each iteration exploring potentially different interleavings using some underlying program
        /// exploration strategy (by default a uniform probabilistic strategy).
        /// </summary>
        /// <remarks>
        /// Learn more in our documentation: https://microsoft.github.io/coyote/how-to/unit-testing
        /// </remarks>
        private static void RunSystematicTest(Func<Task> test, string testName)
        {
            Console.WriteLine($"Starting systematic test...");
            var config = Configuration.Create();

            // --------------------------------------------START [PCT BOUND]-------------------------------------------------------------------------------------------------------------------------------------------
            string envMaxPCTSwitchPoints = Environment.GetEnvironmentVariable("OLP_TEST_PCT_SWITCHES"); // NOTE: OLP_TEST_PCT_SWITCHES muse be a positive integer.
            uint envMaxPCTSwitchPointsInt = 10;
            if (envMaxPCTSwitchPoints != null)
            {
#pragma warning disable CA1305 // Specify IFormatProvider
                envMaxPCTSwitchPointsInt = uint.Parse(envMaxPCTSwitchPoints);
#pragma warning restore CA1305 // Specify IFormatProvider
            }

            // --------------------------------------------END [PCT BOUND]---------------------------------------------------------------------------------------------------------------------------------------------

            // --------------------------------------------START [STRATEGY]--------------------------------------------------------------------------------------------------------------------------------------------
            string envScheduler = Environment.GetEnvironmentVariable("OLP_TEST_SCHEDULER"); // NOTE: OLP_TEST_SCHEDULER muse be a string, either "PCT", "FAIRPCT" or "RANDOM".
            if (envScheduler != null)
            {
                if (envScheduler == "PCT")
                {
                    // config = config.WithPCTStrategy(false, envMaxPCTSwitchPointsInt);
                    config = config.WithPrioritizationStrategy(false, envMaxPCTSwitchPointsInt);
                }
                else if (envScheduler == "FAIRPCT")
                {
                    // config = config.WithPCTStrategy(true, (uint)envMaxPCTSwitchPointsInt);
                    config = config.WithPrioritizationStrategy(true, envMaxPCTSwitchPointsInt);
                }

                // else if (envScheduler == "TASKPCT")
                // {
                //     config = config.WithTaskPCTStrategy(false, (uint)envMaxPCTSwitchPointsInt);
                // }
                // else if (envScheduler == "FAIRTASKPCT")
                // {
                //     config = config.WithTaskPCTStrategy(true, (uint)envMaxPCTSwitchPointsInt);
                // }
                else if (envScheduler == "RL")
                {
                    config = config.WithRLStrategy();
                }
                else if (envScheduler == "PROBABILISTIC")
                {
                    config = config.WithProbabilisticStrategy();
                }

                // else if (envScheduler == "DFS")
                // {
                //     config = config.WithDFSStrategy();
                // }
                else
                {
                    envScheduler = "RANDOM";
                }
            }
            else
            {
                envScheduler = "RANDOM";
            }

            // --------------------------------------------END [STRATEGY]---------------------------------------------------------------------------------------------------------------------------------------------

            // --------------------------------------------START [DEBUGGER]-------------------------------------------------------------------------------------------------------------------------------------------
            string envDebugger = Environment.GetEnvironmentVariable("OLP_TEST_DEBUGGER");
            if (envDebugger != null)
            {
                bool envDebuggerBool = bool.Parse(envDebugger);
                if (envDebuggerBool)
                {
                    System.Diagnostics.Debugger.Launch();
                }
            }

            // --------------------------------------------END [DEBUGGER]---------------------------------------------------------------------------------------------------------------------------------------------

            // --------------------------------------------START [ITERATION]------------------------------------------------------------------------------------------------------------------------------------------
            string envIterations = Environment.GetEnvironmentVariable("OLP_TEST_ITERATIONS");
            uint envIterationsInt = 100;
            if (envIterations != null)
            {
#pragma warning disable CA1305 // Specify IFormatProvider
                envIterationsInt = uint.Parse(envIterations);
#pragma warning restore CA1305 // Specify IFormatProvider
            }

            // --------------------------------------------END [ITERATION]---------------------------------------------------------------------------------------------------------------------------------------------

            // --------------------------------------------START [VERBOSITY]---------------------------------------------------------------------------------------------------------------------------------------------
            string envVerbosity = Environment.GetEnvironmentVariable("OLP_TEST_VERBOSITY");
            bool envVerbosityBool = false;
            if (envVerbosity != null)
            {
                envVerbosityBool = bool.Parse(envVerbosity);
            }

            // --------------------------------------------END [VERBOSITY]---------------------------------------------------------------------------------------------------------------------------------------------

            // --------------------------------------------START [EXPLORE]---------------------------------------------------------------------------------------------------------------------------------------------
            string envExplore = Environment.GetEnvironmentVariable("OLP_TEST_EXPLORE");
            bool envExploreBool = false;
            if (envExplore != null)
            {
                #pragma warning disable CA1305 // Specify IFormatProvider
                envExploreBool = bool.Parse(envExplore);
                #pragma warning restore CA1305 // Specify IFormatProvider
            }

            // --------------------------------------------END [EXPLORE]---------------------------------------------------------------------------------------------------------------------------------------------

            // --------------------------------------------START [SEED]---------------------------------------------------------------------------------------------------------------------------------------------
            string envSeed = Environment.GetEnvironmentVariable("OLP_TEST_SEED");
            if (envSeed != null)
            {
                #pragma warning disable CA1305 // Specify IFormatProvider
                uint envSeedInt = uint.Parse(envSeed);
                #pragma warning restore CA1305 // Specify IFormatProvider
                config = config.WithRandomGeneratorSeed(envSeedInt);
            }

            // --------------------------------------------END [SEED]---------------------------------------------------------------------------------------------------------------------------------------------

            // =============================================================START [NON IMP env vars]=======================================================================================================================================
            string envMaxSteps = Environment.GetEnvironmentVariable("OLP_TEST_MAXSTEPS");
            if (envMaxSteps != null)
            {
                #pragma warning disable CA1305 // Specify IFormatProvider
                uint envMaxStepsInt = uint.Parse(envMaxSteps);
                #pragma warning restore CA1305 // Specify IFormatProvider
                config = config.WithMaxSchedulingSteps(envMaxStepsInt);
            }

            string envTimeout = Environment.GetEnvironmentVariable("OLP_TEST_TIMEOUT");
            if (envTimeout != null)
            {
                #pragma warning disable CA1305 // Specify IFormatProvider
                int envTimeoutInt = int.Parse(envTimeout);
                #pragma warning restore CA1305 // Specify IFormatProvider
                config = config.WithTestingTimeout(envTimeoutInt);
            }

            string envXmlLog = Environment.GetEnvironmentVariable("OLP_TEST_XMLLOG");
            if (envXmlLog != null)
            {
                #pragma warning disable CA1305 // Specify IFormatProvider
                bool envXmlLogBool = bool.Parse(envXmlLog);
                #pragma warning restore CA1305 // Specify IFormatProvider
                config = config.WithXmlLogEnabled(envXmlLogBool);
            }

            // =============================================================END [NON IMP env vars]=======================================================================================================================================

            Console.WriteLine($"--------------------COYOTE TESTING STARTING, ENV VAR VALUE: envMaxPCTSwitchPoints: {envMaxPCTSwitchPoints}");
            Console.WriteLine($"--------------------COYOTE TESTING STARTING, ENV VAR VALUE: envScheduler: {envScheduler}");
            Console.WriteLine($"--------------------COYOTE TESTING STARTING, ENV VARS VALUE: envDebugger: {envDebugger}");
            Console.WriteLine($"--------------------COYOTE TESTING STARTING, ENV VARS VALUE: envIterations: {envIterations}");
            Console.WriteLine($"--------------------COYOTE TESTING STARTING, ENV VARS VALUE: envVerbosity: {envVerbosity}");
            Console.WriteLine($"--------------------COYOTE TESTING STARTING, ENV VARS VALUE: envSeed: {envSeed}");
            Console.WriteLine($"--------------------COYOTE TESTING STARTING, ENV VARS VALUE: envMaxSteps: {envMaxSteps}");
            Console.WriteLine($"--------------------COYOTE TESTING STARTING, ENV VARS VALUE: envTimeout: {envTimeout}");
            Console.WriteLine($"--------------------COYOTE TESTING STARTING, ENV VARS VALUE: envXmlLog: {envXmlLog}");
            Console.WriteLine($"--------------------COYOTE TESTING STARTING, ENV VARS VALUE: envExploreBool: {envExploreBool}");

            config = config.WithTestingIterations(envIterationsInt);
            config = config.WithVerbosityEnabled(envVerbosityBool);
            config = config.WithTestIterationsRunToCompletion(envExploreBool);

            // maybe from coyote version 1.4.1
            // config = config.WithConcurrencyFuzzingFallbackEnabled(false);
            // config = config.WithConcurrencyFuzzingEnabled(false);
            // config = config.WithPartiallyControlledConcurrencyEnabled(false);

            config = config.WithPartiallyControlledConcurrencyAllowed(false);
            config = config.WithSystematicFuzzingEnabled(false);
            config = config.WithSystematicFuzzingFallbackEnabled(false);
            config = config.WithSharedStateReductionEnabled(false);
            config = config.WithActivityCoverageReported(false);
            config = config.WithPotentialDeadlocksReportedAsBugs(false);

            // Added to ignore the error: task not intercepted and controlled during testing, so it can interfere with the ability to reproduce bug traces.
            config = config.WithNoBugTraceRepro();

            // FN_DOUBT:
            // config = config.WithLivenessTemperatureThreshold()// doubt.
            // config = config.WithReplayStrategy() // doubt: how to do this schedule trace business.
            // config = config.WithDebugLoggingEnabled(true); // in older versions of coyote only
            // config = config.WithDeadlockTimeout();
            // config = config.WithTraceVisualizationEnabled();
            // config = config.WithPotentialDeadlocksReportedAsBugs();
            // config = config.WithUncontrolledConcurrencyResolutionTimeout();
            // config = config.WithTimeoutDelay(); // doubt.
            // config = config.WithIncrementalSeedGenerationEnabled();
            // config = config.WithProductionMonitorEnabled();

            var testingEngine = TestingEngine.Create(config, test);
            testingEngine.Run();
            // Console.WriteLine($"--------------------COYOTE TESTING DONE: <TASKPCT_WORK_RUNTIME_LOG> numSpawnTasks: {testingEngine.TestReport.NumSpawnTasks}: (Number of Spawn Tasks observed).");
            // Console.WriteLine($"--------------------COYOTE TESTING DONE: <TASKPCT_WORK_RUNTIME_LOG> numContinuationTasks: {testingEngine.TestReport.NumContinuationTasks}: (Number of Continuation Tasks observed).");
            // Console.WriteLine($"--------------------COYOTE TESTING DONE: <TASKPCT_WORK_RUNTIME_LOG> numDelayTasks: {testingEngine.TestReport.NumDelayTasks}: (Number of Delay Tasks observed).");
            // Console.WriteLine($"--------------------COYOTE TESTING DONE: <TASKPCT_WORK_RUNTIME_LOG> numOfAsyncStateMachineStart: {testingEngine.TestReport.NumOfAsyncStateMachineStart}: (Number of times Start method is called by AsyncStateMachines).");
            // Console.WriteLine($"--------------------COYOTE TESTING DONE: <TASKPCT_WORK_RUNTIME_LOG> numOfAsyncStateMachineStartMissed: {testingEngine.TestReport.NumOfAsyncStateMachineStartMissed}: (Number of Start method calls by AsyncStateMachines in which correct owner operation was not set).");
            // Console.WriteLine($"--------------------COYOTE TESTING DONE: <TASKPCT_WORK_RUNTIME_LOG> numOfMoveNext: {testingEngine.TestReport.NumOfMoveNext}: (Number of times MoveNext method is called by AsyncStateMachines).");
            // Console.WriteLine($"--------------------COYOTE TESTING DONE: <TASKPCT_WORK_RUNTIME_LOG> numOfMoveNextMissed: {testingEngine.TestReport.NumOfMoveNextMissed}: (Number of times setting correct parent or priority on a MoveNext method call is missed).");
            // Console.WriteLine($"Done testing. Found {testingEngine.TestReport.NumOfFoundBugs} bugs.");
            if (testingEngine.TestReport.NumOfFoundBugs > 0)
            {
                var error = testingEngine.TestReport.BugReports.First();
                var traceFile = WriteReproducibleTrace(testingEngine.ReproducibleTrace, testName);

                Assert.Fail("Found bug: {0}\n   Replay trace using Coyote by running:\n     TraceReplayer.exe {1} {2}",
                    error, testName, traceFile);
            }
        }

        private static string WriteReproducibleTrace(string trace, string testName)
        {
            string assemblyPath = Assembly.GetAssembly(typeof(SystematicTests)).Location;
            string directory = Path.GetDirectoryName(assemblyPath);
            string traceFile = Path.Combine(directory, $"{testName}.schedule");
            File.WriteAllText(traceFile, trace);
            return traceFile;
        }
    }
}
