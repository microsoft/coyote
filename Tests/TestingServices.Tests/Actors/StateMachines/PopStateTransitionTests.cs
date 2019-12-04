// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    public class PopStateTransitionTests : BaseTest
    {
        public PopStateTransitionTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(Init))]
            public class S1 : State
            {
            }

            private Transition Init() => this.PopState();
        }

        [Fact(Timeout = 5000)]
        public void TestUnbalancedPopStateTransition()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M1));
            },
            expectedError: "'M1()' popped its state with no matching push state.",
            replay: true);
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitMethod))]
            private class Init : State
            {
            }

            private Transition InitOnEntry() => this.GotoState<Done>();

            private Transition ExitMethod() => this.PopState();

            private class Done : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestPopStateTransitionOnExit()
        {
            var expectedError = "'M2()' has performed a 'PopState' transition from an OnExit action.";
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M2));
            },
            expectedError: expectedError,
            replay: true);
        }
    }
}
