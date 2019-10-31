// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class EntryPointMachineExecutionTest : BaseTest
    {
        public EntryPointMachineExecutionTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M : StateMachine
        {
            [Start]
            private class Init : State
            {
            }
        }

        private class N : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Assert(false, "Reached test assertion.");
            }
        }

        [Fact(Timeout=5000)]
        public void TestEntryPointMachineExecution()
        {
            this.TestWithError(r =>
            {
                ActorId m = r.CreateStateMachine(typeof(M));
                ActorId n = r.CreateStateMachine(typeof(N));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }
    }
}
