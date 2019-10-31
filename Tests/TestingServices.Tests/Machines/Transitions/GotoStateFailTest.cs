// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
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

        private class M : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                // This line no longer builds after converting from Goto(typeof(T)) to Goto<T>()
                // due to the "where T: State" constraint on Goto<T>().
                // this.Goto<object>();

                // Added a different failure mode here; try to Goto a state from another machine.
                this.Goto<N.Done>();
            }

            private class Done : State
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
            }

            internal class Done : State
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestGotoStateFail()
        {
            this.TestWithError(r =>
            {
                r.CreateStateMachine(typeof(M));
            },
            expectedError: "Machine 'M()' is trying to transition to non-existing state 'Done'.",
            replay: true);
        }
    }
}
