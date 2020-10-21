// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.SystematicTesting.Tests
{
    public class PopStateTransitionTests : BaseActorSystematicTest
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

            private void Init() => this.RaisePopStateEvent();
        }

        [Fact(Timeout = 5000)]
        public void TestUnbalancedPopStateTransition()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M1));
            },
            expectedError: "M1() popped its state with no matching push state.",
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

            private void InitOnEntry() => this.RaiseGotoStateEvent<Done>();

            private void ExitMethod() => this.RaisePopStateEvent();

            private class Done : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestPopStateTransitionOnExit()
        {
            var expectedError = "M2() has performed a 'PopState' transition from an OnExit action.";
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M2));
            },
            expectedError: expectedError,
            replay: true);
        }
    }
}
