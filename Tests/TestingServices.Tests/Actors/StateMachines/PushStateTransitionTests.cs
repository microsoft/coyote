// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    public class PushStateTransitionTests : BaseTest
    {
        public PushStateTransitionTests(ITestOutputHelper output)
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

            private void InitOnEntry() => this.RaisePushStateEvent<Done>();

            [OnEntry(nameof(EntryDone))]
            private class Done : State
            {
            }

            private void EntryDone()
            {
                // This assert is reachable.
                this.Assert(false, "Reached test assertion.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestPushStateTransition()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M1));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M2 : StateMachine
        {
            private int cnt = 0;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [IgnoreEvents(typeof(UnitEvent))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Assert(this.cnt == 0); // called once
                this.cnt++;
                this.RaisePushStateEvent<Done>();
            }

            [OnEntry(nameof(EntryDone))]
            private class Done : State
            {
            }

            private void EntryDone() => this.RaisePopStateEvent();
        }

        [Fact(Timeout = 5000)]
        public void TestPushAndPopStateTransitions()
        {
            this.Test(r =>
            {
                var m = r.CreateActor(typeof(M2));
                r.SendEvent(m, UnitEvent.Instance);
            });
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitInit))]
            private class Init : State
            {
            }

            private void InitOnEntry() => this.RaisePushStateEvent<Done>();

            private void ExitInit()
            {
                // This assert is not reachable.
                this.Assert(false, "Reached test assertion.");
            }

            private class Done : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestPushStateTransitionWithOnExitSkipped()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M3));
            });
        }

        private class M4a : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry() => this.RaisePushStateEvent<M4b.Done>();

            private class Done : State
            {
            }
        }

        private class M4b : StateMachine
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

        [Fact(Timeout = 5000)]
        public void TestPushTransitionToNonExistingState()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M4a));
            },
            expectedError: "M4a() is trying to transition to non-existing state 'Done'.",
            replay: true);
        }

        private class M5a : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(Foo))]
            [OnEventPushState(typeof(E2), typeof(S1))]
            private class S0 : State
            {
            }

            [OnEventDoAction(typeof(E3), nameof(Bar))]
            private class S1 : State
            {
            }

            private void Foo()
            {
            }

            private void Bar() => this.RaisePopStateEvent();
        }

        private class E1 : Event
        {
        }

        private class E2 : Event
        {
        }

        private class E3 : Event
        {
        }

        private class E4 : Event
        {
        }

        private class M5b : StateMachine
        {
            [Start]
            [OnEntry(nameof(Conf))]
            private class Init : State
            {
            }

            private void Conf()
            {
                var a = this.CreateActor(typeof(M5a));
                this.SendEvent(a, new E2()); // Push(S1)
                this.SendEvent(a, new E1()); // Execute foo without popping
                this.SendEvent(a, new E3()); // Can handle it because A is still in S1
            }
        }

        [Fact(Timeout = 5000)]
        public void TestPushStateTransitionViaEvent()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M5b));
            });
        }

        private class M6 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitMethod))]
            private class Init : State
            {
            }

            private void InitOnEntry() => this.RaiseGotoStateEvent<Done>();

            private void ExitMethod() => this.RaisePushStateEvent<Done>();

            private class Done : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestPushStateTransitionOnExit()
        {
            var expectedError = "M6() has performed a 'PushState' transition from an OnExit action.";
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M6));
            },
            expectedError: expectedError,
            replay: true);
        }
    }
}
