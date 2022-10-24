// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.IO;
using System.Text;
using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.Tests.Logging
{
    public class ActorLoggingTests : BaseActorLoggingTests
    {
        public ActorLoggingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestDefaultActorLogger()
        {
            Configuration config = this.GetConfiguration().WithVerbosityEnabled(VerbosityLevel.Info);
            this.Test(async runtime =>
            {
                var runtimeLogger = (runtime.Logger as LogWriter).Logger;
                Assert.True(runtimeLogger is NullLogger || runtimeLogger is TestOutputLogger);

                using var stream = new MemoryStream();
                using (var interceptor = new ConsoleOutputInterceptor(stream))
                {
                    runtime.RegisterMonitor<TestMonitor>();
                    runtime.Monitor<TestMonitor>(new TestMonitor.SetupEvent());
                    runtime.CreateActor(typeof(M));
                    await (runtime as IRuntimeExtension).WaitUntilQuiescenceAsync();
                }

                string result = Encoding.UTF8.GetString(stream.ToArray()).NormalizeNewLines()
                    .RemoveNonDeterministicValues().SortLines();
                this.TestOutput.WriteLine(result);
                Assert.Equal(string.Empty, result);
            }, config);
        }

        [Fact(Timeout = 5000)]
        public void TestConsoleActorLogger()
        {
            Configuration config = this.GetConfiguration().WithVerbosityEnabled(VerbosityLevel.Info)
                .WithConsoleLoggingEnabled();
            this.Test(async runtime =>
            {
                Assert.IsType<ConsoleLogger>((runtime.Logger as LogWriter).Logger);

                using var stream = new MemoryStream();
                using (var interceptor = new ConsoleOutputInterceptor(stream))
                {
                    runtime.RegisterMonitor<TestMonitor>();
                    runtime.Monitor<TestMonitor>(new TestMonitor.SetupEvent());
                    runtime.CreateActor(typeof(M));
                    await (runtime as IRuntimeExtension).WaitUntilQuiescenceAsync();
                }

                string result = Encoding.UTF8.GetString(stream.ToArray()).NormalizeNewLines()
                    .RemoveNonDeterministicValues().SortLines();
                string expected = StringExtensions.FormatLines(
                    "<CreateLog> TestMonitor was created.",
                    "<MonitorLog> TestMonitor enters state 'Init'.",
                    "<MonitorLog> TestMonitor is processing event 'SetupEvent' in state 'Init'.",
                    "<MonitorLog> TestMonitor executed action 'OnSetup' in state 'Init'.",
                    "<CreateLog> M() was created by thread ''.",
                    "<CreateLog> N() was created by M().",
                    "<SendLog> M() sent event 'E' to N().",
                    "<EnqueueLog> N() enqueued event 'E'.",
                    "<StateLog> N() enters state 'Init'.",
                    "<ActionLog> N() invoked action 'OnInitEntry' in state 'Init'.",
                    "<DequeueLog> N() dequeued event 'E' in state 'Init'.",
                    "<GotoLog> N() is transitioning from state 'Init' to state 'N.Act'.",
                    "<StateLog> N() exits state 'Init'.",
                    "<StateLog> N() enters state 'Act'.",
                    "<ActionLog> N() invoked action 'ActOnEntry' in state 'Act'.",
                    "<SendLog> N() in state 'Act' sent event 'E' to M().",
                    "<EnqueueLog> M() enqueued event 'E'.",
                    "<DequeueLog> M() dequeued event 'E'.",
                    "<ActionLog> M() invoked action 'Act'.",
                    "<MonitorLog> TestMonitor is processing event 'CompletedEvent' in state 'Init'.",
                    "<MonitorLog> TestMonitor executed action 'OnCompleted' in state 'Init'.");
                expected = expected.NormalizeNewLines().SortLines();

                this.TestOutput.WriteLine(result);
                Assert.Equal(expected, result);
            }, config);
        }

        [Fact(Timeout = 5000)]
        public void TestCustomActorLogger()
        {
            Configuration config = this.GetConfiguration().WithVerbosityEnabled(VerbosityLevel.Info);
            using var logger = new MemoryLogger(config.VerbosityLevel);
            this.Test(async runtime =>
            {
                var runtimeLogger = (runtime.Logger as LogWriter).Logger;
                Assert.True(runtimeLogger is NullLogger || runtimeLogger is TestOutputLogger);

                runtime.Logger = logger;
                Assert.IsType<MemoryLogger>((runtime.Logger as LogWriter).Logger);

                runtime.RegisterMonitor<TestMonitor>();
                runtime.Monitor<TestMonitor>(new TestMonitor.SetupEvent());
                runtime.CreateActor(typeof(M));
                await (runtime as IRuntimeExtension).WaitUntilQuiescenceAsync();

                string result = logger.ToString().RemoveNonDeterministicValues().SortLines();
                string expected = StringExtensions.FormatLines(
                    "<CreateLog> TestMonitor was created.",
                    "<MonitorLog> TestMonitor enters state 'Init'.",
                    "<MonitorLog> TestMonitor is processing event 'SetupEvent' in state 'Init'.",
                    "<MonitorLog> TestMonitor executed action 'OnSetup' in state 'Init'.",
                    "<CreateLog> M() was created by thread ''.",
                    "<CreateLog> N() was created by M().",
                    "<SendLog> M() sent event 'E' to N().",
                    "<EnqueueLog> N() enqueued event 'E'.",
                    "<StateLog> N() enters state 'Init'.",
                    "<ActionLog> N() invoked action 'OnInitEntry' in state 'Init'.",
                    "<DequeueLog> N() dequeued event 'E' in state 'Init'.",
                    "<GotoLog> N() is transitioning from state 'Init' to state 'N.Act'.",
                    "<StateLog> N() exits state 'Init'.",
                    "<StateLog> N() enters state 'Act'.",
                    "<ActionLog> N() invoked action 'ActOnEntry' in state 'Act'.",
                    "<SendLog> N() in state 'Act' sent event 'E' to M().",
                    "<EnqueueLog> M() enqueued event 'E'.",
                    "<DequeueLog> M() dequeued event 'E'.",
                    "<ActionLog> M() invoked action 'Act'.",
                    "<MonitorLog> TestMonitor is processing event 'CompletedEvent' in state 'Init'.",
                    "<MonitorLog> TestMonitor executed action 'OnCompleted' in state 'Init'.");
                expected = expected.NormalizeNewLines().SortLines();

                this.TestOutput.WriteLine(result);
                Assert.Equal(expected, result);
            }, config);
        }
    }
}
