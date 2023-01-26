// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Text;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Runtime.Tests.Logging
{
    public class TestingEngineLoggingTests : BaseRuntimeTest
    {
        public TestingEngineLoggingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private static void WriteAllSeverityMessages(LogWriter logWriter)
        {
            logWriter.LogDebug(VerbosityMessages.DebugMessage);
            logWriter.LogInfo(VerbosityMessages.InfoMessage);
            logWriter.LogWarning(VerbosityMessages.WarningMessage);
            logWriter.LogError(VerbosityMessages.ErrorMessage);
            logWriter.LogImportant(VerbosityMessages.ImportantMessage);
        }

        [Fact(Timeout = 5000)]
        public void TestDefaultTestingEngineLogger()
        {
            string observed = string.Empty;
            using var stream = new MemoryStream();
            using (var interceptor = new ConsoleOutputInterceptor(stream))
            {
                var config = this.GetConfiguration().WithRandomStrategy().WithTestingIterations(2).WithRandomGeneratorSeed(0)
                    .WithTestIterationsRunToCompletion().WithVerbosityEnabled(VerbosityLevel.Info);
                using TestingEngine engine = TestingEngine.Create(config, () =>
                {
                    var runtime = CoyoteRuntime.Current;
                    Assert.IsType<NullLogger>(runtime.LogWriter.Logger);
                    WriteAllSeverityMessages(runtime.LogWriter);
                    runtime.Assert(false);
                });

                engine.Run();

                observed = engine.ReadableTrace.RemoveNonDeterministicValues().NormalizeNewLines().FormatNewLine();
                Assert.Equal(2, engine.TestReport.NumOfFoundBugs);
            }

            string logged = Encoding.UTF8.GetString(stream.ToArray()).NormalizeNewLines();
            string expectedLogged = StringExtensions.FormatLines(
                $"... Assembly is not rewritten for testing, see {Documentation.LearnAboutRewritingUrl}.",
                "... Setting up the test:",
                "..... Using the random[seed:0] exploration strategy.",
                "... Running test iterations:",
                "..... Iteration #1",
                "..... Iteration #1 found bug #1",
                "Detected an assertion failure.",
                "..... Iteration #2",
                "..... Iteration #2 found bug #2",
                "Detected an assertion failure.");

            logged = logged.RemoveNonDeterministicValues().RemoveDebugLines();
            expectedLogged = expectedLogged.RemoveNonDeterministicValues();
            this.TestOutput.WriteLine($"Logged (length: {logged.Length}):");
            this.TestOutput.WriteLine(logged);
            Assert.Equal(expectedLogged, logged);

            string expectedObserved = StringExtensions.FormatLines(
                "[coyote::test] Runtime '' started the test on thread '' using the 'random' strategy.",
                VerbosityMessages.InfoMessage,
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage,
                "[coyote::error] Detected an assertion failure.",
                "[coyote::test] Exploration finished in runtime '' [found a bug using the 'random' strategy].",
                "[coyote::report] Testing statistics:",
                "[coyote::report] Found 2 bugs.",
                "[coyote::report] Scheduling statistics:",
                "[coyote::report] Explored 2 execution paths: 2 fair, 0 unfair, 1 unique.",
                "[coyote::report] Found 100.00% buggy execution paths.",
                "[coyote::report] Controlled 2 operations: 1 (), 1 (), 1 (), 1 ().",
                "[coyote::report] Degree of operation grouping: 1 (), 1 (), 1 ().",
                "[coyote::report] Number of scheduling decisions in fair terminating execution paths: 0 (), 0 (), 0 ().");
            this.TestOutput.WriteLine($"Observed (length: {observed.Length}):");
            this.TestOutput.WriteLine(observed);
            Assert.Equal(expectedObserved, observed);
        }

        [Fact(Timeout = 5000)]
        public void TestConsoleTestingEngineLogger()
        {
            string observed = string.Empty;
            using var stream = new MemoryStream();
            using (var interceptor = new ConsoleOutputInterceptor(stream))
            {
                var config = this.GetConfiguration().WithRandomStrategy().WithTestingIterations(2).WithRandomGeneratorSeed(0)
                    .WithTestIterationsRunToCompletion().WithVerbosityEnabled(VerbosityLevel.Info)
                    .WithConsoleLoggingEnabled();
                using TestingEngine engine = TestingEngine.Create(config, () =>
                {
                    var runtime = CoyoteRuntime.Current;
                    Assert.IsType<ConsoleLogger>(runtime.LogWriter.Logger);
                    WriteAllSeverityMessages(runtime.LogWriter);
                    runtime.Assert(false);
                });

                engine.Run();

                observed = engine.ReadableTrace.RemoveNonDeterministicValues().NormalizeNewLines().FormatNewLine();
                Assert.Equal(2, engine.TestReport.NumOfFoundBugs);
            }

            string logged = Encoding.UTF8.GetString(stream.ToArray()).NormalizeNewLines();
            string expectedLogged = StringExtensions.FormatLines(
                $"... Assembly is not rewritten for testing, see {Documentation.LearnAboutRewritingUrl}.",
                "... Setting up the test:",
                "..... Using the random[seed:0] exploration strategy.",
                "... Running test iterations:",
                "..... Iteration #1",
                "[coyote::test] Runtime '' started the test on thread '' using the 'random' strategy.",
                VerbosityMessages.InfoMessage,
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage,
                "[coyote::error] Detected an assertion failure.",
                "[coyote::test] Exploration finished in runtime '' [found a bug using the 'random' strategy].",
                "..... Iteration #1 found bug #1",
                "Detected an assertion failure.",
                "..... Iteration #2",
                "[coyote::test] Runtime '' started the test on thread '' using the 'random' strategy.",
                VerbosityMessages.InfoMessage,
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage,
                "[coyote::error] Detected an assertion failure.",
                "[coyote::test] Exploration finished in runtime '' [found a bug using the 'random' strategy].",
                "..... Iteration #2 found bug #2",
                "Detected an assertion failure.");

            logged = logged.RemoveNonDeterministicValues().RemoveDebugLines();
            expectedLogged = expectedLogged.RemoveNonDeterministicValues();
            this.TestOutput.WriteLine($"Logged (length: {logged.Length}):");
            this.TestOutput.WriteLine(logged);
            Assert.Equal(expectedLogged, logged);

            string expectedObserved = StringExtensions.FormatLines(
                "[coyote::test] Runtime '' started the test on thread '' using the 'random' strategy.",
                VerbosityMessages.InfoMessage,
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage,
                "[coyote::error] Detected an assertion failure.",
                "[coyote::test] Exploration finished in runtime '' [found a bug using the 'random' strategy].",
                "[coyote::report] Testing statistics:",
                "[coyote::report] Found 2 bugs.",
                "[coyote::report] Scheduling statistics:",
                "[coyote::report] Explored 2 execution paths: 2 fair, 0 unfair, 1 unique.",
                "[coyote::report] Found 100.00% buggy execution paths.",
                "[coyote::report] Controlled 2 operations: 1 (), 1 (), 1 (), 1 ().",
                "[coyote::report] Degree of operation grouping: 1 (), 1 (), 1 ().",
                "[coyote::report] Number of scheduling decisions in fair terminating execution paths: 0 (), 0 (), 0 ().");
            this.TestOutput.WriteLine($"Observed (length: {observed.Length}):");
            this.TestOutput.WriteLine(observed);
            Assert.Equal(expectedObserved, observed);
        }

        [Fact(Timeout = 5000)]
        public void TestCustomTestingEngineLogger()
        {
            var config = this.GetConfiguration().WithRandomStrategy().WithTestingIterations(2).WithRandomGeneratorSeed(0)
                .WithTestIterationsRunToCompletion().WithVerbosityEnabled(VerbosityLevel.Info);
            using TestingEngine engine = TestingEngine.Create(config, () =>
            {
                var runtime = CoyoteRuntime.Current;
                Assert.IsType<MemoryLogger>(runtime.LogWriter.Logger);
                WriteAllSeverityMessages(runtime.LogWriter);
                runtime.Assert(false);
            });

            using var logger = new MemoryLogger(config.VerbosityLevel);
            engine.SetLogger(logger);
            engine.Run();

            string observed = engine.ReadableTrace.RemoveNonDeterministicValues().NormalizeNewLines().FormatNewLine();
            Assert.Equal(2, engine.TestReport.NumOfFoundBugs);

            string logged = logger.ToString().NormalizeNewLines();
            string expectedLogged = StringExtensions.FormatLines(
                $"... Assembly is not rewritten for testing, see {Documentation.LearnAboutRewritingUrl}.",
                "... Setting up the test:",
                "..... Using the random[seed:0] exploration strategy.",
                "... Running test iterations:",
                "..... Iteration #1",
                "[coyote::test] Runtime '' started the test on thread '' using the 'random' strategy.",
                VerbosityMessages.InfoMessage,
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage,
                "[coyote::error] Detected an assertion failure.",
                "[coyote::test] Exploration finished in runtime '' [found a bug using the 'random' strategy].",
                "..... Iteration #1 found bug #1",
                "Detected an assertion failure.",
                "..... Iteration #2",
                "[coyote::test] Runtime '' started the test on thread '' using the 'random' strategy.",
                VerbosityMessages.InfoMessage,
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage,
                "[coyote::error] Detected an assertion failure.",
                "[coyote::test] Exploration finished in runtime '' [found a bug using the 'random' strategy].",
                "..... Iteration #2 found bug #2",
                "Detected an assertion failure.");

            logged = logged.RemoveNonDeterministicValues().RemoveDebugLines();
            expectedLogged = expectedLogged.RemoveNonDeterministicValues();
            this.TestOutput.WriteLine($"Logged (length: {logged.Length}):");
            this.TestOutput.WriteLine(logged);
            Assert.Equal(expectedLogged, logged);

            string expectedObserved = StringExtensions.FormatLines(
                "[coyote::test] Runtime '' started the test on thread '' using the 'random' strategy.",
                VerbosityMessages.InfoMessage,
                VerbosityMessages.WarningMessage,
                VerbosityMessages.ErrorMessage,
                VerbosityMessages.ImportantMessage,
                "[coyote::error] Detected an assertion failure.",
                "[coyote::test] Exploration finished in runtime '' [found a bug using the 'random' strategy].",
                "[coyote::report] Testing statistics:",
                "[coyote::report] Found 2 bugs.",
                "[coyote::report] Scheduling statistics:",
                "[coyote::report] Explored 2 execution paths: 2 fair, 0 unfair, 1 unique.",
                "[coyote::report] Found 100.00% buggy execution paths.",
                "[coyote::report] Controlled 2 operations: 1 (), 1 (), 1 (), 1 ().",
                "[coyote::report] Degree of operation grouping: 1 (), 1 (), 1 ().",
                "[coyote::report] Number of scheduling decisions in fair terminating execution paths: 0 (), 0 (), 0 ().");
            this.TestOutput.WriteLine($"Observed (length: {observed.Length}):");
            this.TestOutput.WriteLine(observed);
            Assert.Equal(expectedObserved, observed);
        }
    }
}
