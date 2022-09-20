// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Text;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.Tests.Logging
{
    public class RuntimeLogTextFormattingTests : BaseActorLoggingTests
    {
        public RuntimeLogTextFormattingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestDefaultActorLoggerWithCustomFormatter()
        {
            Configuration config = this.GetConfiguration().WithVerbosityEnabled(VerbosityLevel.Info);
            this.Test(async runtime =>
            {
                Assert.IsType<NullLogger>((runtime.Logger as LogWriter).Logger);

                using var stream = new MemoryStream();
                using (var interceptor = new ConsoleOutputInterceptor(stream))
                {
                    var log = new CustomActorRuntimeLogTextFormatter();
                    runtime.RegisterLog(log);

                    runtime.RegisterMonitor<TestMonitor>();
                    runtime.Monitor<TestMonitor>(new SetupEvent());
                    runtime.CreateActor(typeof(M));
                    await (runtime as ActorExecutionContext).WaitUntilQuiescenceAsync();
                }

                string result = Encoding.UTF8.GetString(stream.ToArray()).NormalizeNewLines()
                    .RemoveNonDeterministicValues().SortLines();
                this.TestOutput.WriteLine(result);
                Assert.Equal(string.Empty, result);
            }, config);
        }

        [Fact(Timeout = 5000)]
        public void TestConsoleActorLoggerWithCustomFormatter()
        {
            Configuration config = this.GetConfiguration().WithVerbosityEnabled(VerbosityLevel.Info)
                .WithConsoleLoggingEnabled();
            this.Test(async runtime =>
            {
                Assert.IsType<ConsoleLogger>((runtime.Logger as LogWriter).Logger);

                using var stream = new MemoryStream();
                using (var interceptor = new ConsoleOutputInterceptor(stream))
                {
                    var log = new CustomActorRuntimeLogTextFormatter();
                    runtime.RegisterLog(log);

                    runtime.RegisterMonitor<TestMonitor>();
                    runtime.Monitor<TestMonitor>(new SetupEvent());
                    runtime.CreateActor(typeof(M));
                    await (runtime as ActorExecutionContext).WaitUntilQuiescenceAsync();
                }

                string result = Encoding.UTF8.GetString(stream.ToArray()).NormalizeNewLines()
                    .RemoveNonDeterministicValues().SortLines();
                string expected = StringExtensions.FormatLines(
                    "<CreateMonitorLog>",
                    "<MonitorStateLog>",
                    "<MonitorProcessLog>",
                    "<MonitorActionLog>",
                    "<CreateActorLog>",
                    "<CreateStateMachineLog>",
                    "<SendLog>",
                    "<EnqueueLog>",
                    "<StateLog>",
                    "<ActionLog>",
                    "<DequeueLog>",
                    "<GotoLog>",
                    "<StateLog>",
                    "<StateLog>",
                    "<ActionLog>",
                    "<SendLog>",
                    "<EnqueueLog>",
                    "<DequeueLog>",
                    "<ActionLog>",
                    "<MonitorProcessLog>",
                    "<MonitorActionLog>");
                expected = expected.NormalizeNewLines().SortLines();

                this.TestOutput.WriteLine(result);
                Assert.Equal(expected, result);
            }, config);
        }

        [Fact(Timeout = 5000)]
        public void TestCustomActorLoggerWithCustomFormatter()
        {
            Configuration config = this.GetConfiguration().WithVerbosityEnabled(VerbosityLevel.Info);
            using var logger = new MemoryLogger(config.VerbosityLevel);
            this.Test(async runtime =>
            {
                Assert.IsType<NullLogger>((runtime.Logger as LogWriter).Logger);
                runtime.Logger = logger;
                Assert.IsType<MemoryLogger>((runtime.Logger as LogWriter).Logger);

                var log = new CustomActorRuntimeLogTextFormatter();
                runtime.RegisterLog(log);

                runtime.RegisterMonitor<TestMonitor>();
                runtime.Monitor<TestMonitor>(new SetupEvent());
                runtime.CreateActor(typeof(M));
                await (runtime as ActorExecutionContext).WaitUntilQuiescenceAsync();

                string result = logger.ToString().RemoveNonDeterministicValues().SortLines();
                string expected = StringExtensions.FormatLines(
                    "<CreateMonitorLog>",
                    "<MonitorStateLog>",
                    "<MonitorProcessLog>",
                    "<MonitorActionLog>",
                    "<CreateActorLog>",
                    "<CreateStateMachineLog>",
                    "<SendLog>",
                    "<EnqueueLog>",
                    "<StateLog>",
                    "<ActionLog>",
                    "<DequeueLog>",
                    "<GotoLog>",
                    "<StateLog>",
                    "<StateLog>",
                    "<ActionLog>",
                    "<SendLog>",
                    "<EnqueueLog>",
                    "<DequeueLog>",
                    "<ActionLog>",
                    "<MonitorProcessLog>",
                    "<MonitorActionLog>");
                expected = expected.NormalizeNewLines().SortLines();

                this.TestOutput.WriteLine(result);
                Assert.Equal(expected, result);
            }, config);
        }
    }
}
