// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Logging;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.Tests.Logging
{
    public class RuntimeLogTests : BaseActorLoggingTests
    {
        public RuntimeLogTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestCustomActorRuntimeLog()
        {
            Configuration config = this.GetConfiguration().WithVerbosityEnabled(VerbosityLevel.Info);
            this.Test(async runtime =>
            {
                Assert.IsType<NullLogger>((runtime.Logger as LogWriter).Logger);

                runtime.RegisterMonitor<TestMonitor>();
                runtime.Monitor<TestMonitor>(new SetupEvent());
                runtime.RegisterMonitor<S>();

                var log = new CustomActorRuntimeLog();
                runtime.RegisterLog(log);

                runtime.CreateActor(typeof(M));
                await (runtime as ActorExecutionContext).WaitUntilQuiescenceAsync();

                string result = log.ToString().RemoveNonDeterministicValues().FormatNewLine();
                string expected = StringExtensions.FormatLines(
                    "CreateActor",
                    "CreateStateMachine",
                    "StateTransition",
                    "StateTransition",
                    "StateTransition");
                expected = expected.NormalizeNewLines();

                this.TestOutput.WriteLine(result);
                Assert.Equal(expected, result);
            }, config);
        }
    }
}
