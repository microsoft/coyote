// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Machines;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class GotoStateFailTest : BaseTest
    {
        public GotoStateFailTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                // This line no longer builds after converting from Goto(typeof(T)) to Goto<T>()
                // due to the "where T: MachineState" constraint on Goto<T>().
                // this.Goto<object>();

                // Added a different failure mode here; try to Goto a state from another machine.
                this.Goto<N.Done>();
            }

            private class Done : MachineState
            {
            }
        }

        private class N : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
            }

            internal class Done : MachineState
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestGotoStateFail()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M));
            },
            expectedError: "Machine 'M()' is trying to transition to non-existing state 'Done'.",
            replay: true);
        }
    }
}
