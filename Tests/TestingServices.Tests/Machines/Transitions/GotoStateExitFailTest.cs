// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Machines;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class GotoStateExitFailTest : BaseTest
    {
        public GotoStateExitFailTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitInit))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Goto<Done>();
            }

            private void ExitInit()
            {
                // This assertion is reachable.
                this.Assert(false, "Bug found.");
            }

            private class Done : MachineState
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestGotoStateExitFail()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M));
            },
            expectedError: "Bug found.",
            replay: true);
        }
    }
}
