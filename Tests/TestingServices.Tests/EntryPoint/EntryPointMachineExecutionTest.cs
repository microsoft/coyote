// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Machines;
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
            private class Init : MachineState
            {
            }
        }

        private class N : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
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
                ActorId m = r.CreateMachine(typeof(M));
                ActorId n = r.CreateMachine(typeof(N));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }
    }
}
