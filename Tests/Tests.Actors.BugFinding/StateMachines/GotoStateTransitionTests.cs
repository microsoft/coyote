// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests
{
    public class GotoStateTransitionTests : BaseActorBugFindingTest
    {
        public GotoStateTransitionTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry() => this.RaiseGotoStateEvent<Done>();

            private class Done : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestGotoStateTransition()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M1));
            });
        }

        private class M2a : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry() => this.RaiseGotoStateEvent<M2b.Done>();

            private class Done : State
            {
            }
        }

        private class M2b : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

#pragma warning disable CA1822 // Mark members as static
            private void InitOnEntry()
#pragma warning restore CA1822 // Mark members as static
            {
            }

            internal class Done : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestGotoTransitionToNonExistingState()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M2a));
            },
            expectedError: "M2a() is trying to transition to non-existing state 'Done'.",
            replay: true);
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitInit))]
            private class Init : State
            {
            }

            private void InitOnEntry() => this.RaiseGotoStateEvent<Done>();

            private void ExitInit()
            {
                // This assertion is reachable.
                this.Assert(false, "Reached test assertion.");
            }

            private class Done : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestGotoStateTransitionWithOnExit()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M3));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M4 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitMethod))]
            private class Init : State
            {
            }

            private void InitOnEntry() => this.RaiseGotoStateEvent<Done>();

            private void ExitMethod() => this.RaiseGotoStateEvent<Done>();

            private class Done : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestGotoStateTransitionOnExit()
        {
            var expectedError = "M4() has performed a 'GotoState' transition from an OnExit action.";
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M4));
            },
            expectedError: expectedError,
            replay: true);
        }
    }
}
