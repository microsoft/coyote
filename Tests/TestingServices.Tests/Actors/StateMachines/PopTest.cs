// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    public class PopTest : BaseTest
    {
        public PopTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M : StateMachine
        {
            [Start]
            [OnEntry(nameof(Init))]
            public class S1 : State
            {
            }

            private void Init()
            {
                this.PopState();
            }
        }

        private class N : StateMachine
        {
            [Start]
            [OnEntry(nameof(Init))]
            [OnExit(nameof(Exit))]
            public class S1 : State
            {
            }

            private void Init()
            {
                this.GotoState<S2>();
            }

            private void Exit()
            {
                this.PopState();
            }

            public class S2 : State
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestUnbalancedPop()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M), "M");
            },
            expectedError: "'M()' popped with no matching push.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestPopDuringOnExit()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(N), "N");
            },
            expectedError: "'N()' has called raise, goto, push or pop inside an OnExit method.",
            replay: true);
        }
    }
}
