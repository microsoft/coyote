// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    public class DuplicateEventHandlersTests : BaseTest
    {
        public DuplicateEventHandlersTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [OnEventDoAction(typeof(UnitEvent), nameof(Check1))]
        [OnEventDoAction(typeof(UnitEvent), nameof(Check2))]
        private class A1 : Actor
        {
            private void Check1()
            {
            }

            private void Check2()
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestDuplicateDoEventHandlerInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A1));
            },
            expectedError: "A1 declared multiple handlers for event 'Actors.UnitEvent'.");
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(UnitEvent), nameof(Check1))]
            [OnEventDoAction(typeof(UnitEvent), nameof(Check2))]
            private class Init : State
            {
            }

            private void Check1()
            {
            }

            private void Check2()
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestDuplicateDoEventHandlerInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M1));
            },
            expectedError: "M1() declared multiple handlers for event 'Actors.UnitEvent' in state 'M1+Init'.");
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEventGotoState(typeof(UnitEvent), typeof(S1))]
            [OnEventGotoState(typeof(UnitEvent), typeof(S2))]
            private class Init : State
            {
            }

            private class S1 : State
            {
            }

            private class S2 : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestDuplicateGotoEventHandlerInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M2));
            },
            expectedError: "M2() declared multiple handlers for event 'Actors.UnitEvent' in state 'M2+Init'.");
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEventPushState(typeof(UnitEvent), typeof(S1))]
            [OnEventPushState(typeof(UnitEvent), typeof(S2))]
            private class Init : State
            {
            }

            private class S1 : State
            {
            }

            private class S2 : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestDuplicatePushEventHandlerInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M3));
            },
            expectedError: "M3() declared multiple handlers for event 'Actors.UnitEvent' in state 'M3+Init'.");
        }

        private class M4 : StateMachine
        {
            [Start]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(UnitEvent), nameof(Check1))]
            [OnEventDoAction(typeof(UnitEvent), nameof(Check2))]
            private class BaseState : State
            {
            }

            private void Check1()
            {
            }

            private void Check2()
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInheritedDuplicateDoEventHandlerInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M4));
            },
            expectedError: "M4() inherited multiple handlers for event 'Actors.UnitEvent' from state 'M4+BaseState' in state 'M4+Init'.");
        }

        private class M5 : StateMachine
        {
            [Start]
            private class Init : BaseState
            {
            }

            [OnEventGotoState(typeof(UnitEvent), typeof(S1))]
            [OnEventGotoState(typeof(UnitEvent), typeof(S2))]
            private class BaseState : State
            {
            }

            private class S1 : State
            {
            }

            private class S2 : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInheritedDuplicateGotoEventHandlerInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M5));
            },
            expectedError: "M5() inherited multiple handlers for event 'Actors.UnitEvent' from state 'M5+BaseState' in state 'M5+Init'.");
        }

        private class M6 : StateMachine
        {
            [Start]
            private class Init : BaseState
            {
            }

            [OnEventPushState(typeof(UnitEvent), typeof(S1))]
            [OnEventPushState(typeof(UnitEvent), typeof(S2))]
            private class BaseState : State
            {
            }

            private class S1 : State
            {
            }

            private class S2 : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInheritedDuplicatePushEventHandlerInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M6));
            },
            expectedError: "M6() inherited multiple handlers for event 'Actors.UnitEvent' from state 'M6+BaseState' in state 'M6+Init'.");
        }

        private class M7 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(UnitEvent), nameof(Check))]
            [OnEventGotoState(typeof(UnitEvent), typeof(S1))]
            [OnEventPushState(typeof(UnitEvent), typeof(S2))]
            private class Init : State
            {
            }

            private class S1 : State
            {
            }

            private class S2 : State
            {
            }

            private void Check()
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestDuplicateMixedEventHandlerInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M7));
            },
            expectedError: "M7() declared multiple handlers for event 'Actors.UnitEvent' in state 'M7+Init'.");
        }
    }
}
