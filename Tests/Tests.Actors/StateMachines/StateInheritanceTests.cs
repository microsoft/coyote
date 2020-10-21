// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tests.Common.Events;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.Tests.StateMachines
{
    public class StateInheritanceTests : BaseActorTest
    {
        public StateInheritanceTests(ITestOutputHelper output)
            : base(output)
        {
        }

        /// <summary>
        /// This class tests that a state can inherit actions from an abstract base class.
        /// </summary>
        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(UnitEvent), nameof(Check))]
            private abstract class BaseState : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            private void Check()
            {
                this.Assert(false, "Test passed.");
            }
        }

        /// <summary>
        /// Test that you can't declare more than one start states using inheritance.
        /// </summary>
        private class M2 : StateMachine
        {
            [Start]
            private class Init : BaseState
            {
            }

            [Start]
            private class BaseState : State
            {
            }
        }

        /// <summary>
        /// Test that OnEntry attribute can be inherited.
        /// </summary>
        private class M3 : StateMachine
        {
            [Start]
            private class Init : BaseState
            {
            }

            [OnEntry(nameof(BaseOnEntry))]
            private class BaseState : State
            {
            }

            private void BaseOnEntry()
            {
                this.Assert(false, "Test passed.");
            }
        }

        /// <summary>
        /// Test that you can override OnEntry in a subclass of a State.
        /// </summary>
        private class M4 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEntry(nameof(BaseOnEntry))]
            private class BaseState : State
            {
            }

            private void InitOnEntry()
            {
                this.Assert(false, "Test passed.");
            }

            private void BaseOnEntry()
            {
                this.Assert(false, "Test failed!");
            }
        }

        /// <summary>
        /// Test you can inheriting OnEventDoAction.
        /// </summary>
        private class M5 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(UnitEvent), nameof(Check))]
            private class BaseState : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            private void Check()
            {
                this.Assert(false, "Test passed.");
            }
        }

        /// <summary>
        /// Test you can overriding OnEventDoAction in a state subclass.
        /// </summary>
        private class M6 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(UnitEvent), nameof(Check))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(UnitEvent), nameof(BaseCheck))]
            private class BaseState : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            private void Check()
            {
                this.Assert(false, "Test passed.");
            }

            private void BaseCheck()
            {
                this.Assert(false, "Test failed!.");
            }
        }

        /// <summary>
        /// Test you can override OnEventDoAction inherited from 2 base classes.
        /// </summary>
        private class M7 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(UnitEvent), nameof(Check))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(UnitEvent), nameof(BaseCheck))]
            private class BaseState : BaseBaseState
            {
            }

            [OnEventDoAction(typeof(UnitEvent), nameof(BaseBaseCheck))]
            private class BaseBaseState : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            private void Check()
            {
                this.Assert(false, "Test passed.");
            }

            private void BaseCheck()
            {
                this.Assert(false, "Test failed!");
            }

            private void BaseBaseCheck()
            {
                this.Assert(false, "Test failed!");
            }
        }

        /// <summary>
        /// Test that a base state can override OnEventDoAction.
        /// </summary>
        private class M8 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(UnitEvent), nameof(BaseCheck))]
            private class BaseState : BaseBaseState
            {
            }

            [OnEventDoAction(typeof(UnitEvent), nameof(BaseBaseCheck))]
            private class BaseBaseState : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            private void BaseCheck()
            {
                this.Assert(false, "Test passed.");
            }

            private void BaseBaseCheck()
            {
                this.Assert(false, "Test failed!");
            }
        }

        /// <summary>
        /// Test inheritance of OnEventGotoState.
        /// </summary>
        private class M9 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventGotoState(typeof(UnitEvent), typeof(Done))]
            private class BaseState : State
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            private void DoneOnEntry()
            {
                this.Assert(false, "Test passed.");
            }
        }

        /// <summary>
        /// Test overriding of OnEventGotoState.
        /// </summary>
        private class M10 : StateMachine
        {
            [Start]
            [OnEventGotoState(typeof(UnitEvent), typeof(Done))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventGotoState(typeof(UnitEvent), typeof(Error))]
            private class BaseState : State
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : State
            {
            }

            [OnEntry(nameof(ErrorOnEntry))]
            private class Error : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            private void DoneOnEntry()
            {
                this.Assert(false, "Test passed.");
            }

            private void ErrorOnEntry()
            {
                this.Assert(false, "Test failed!");
            }
        }

        /// <summary>
        /// Test overriding of OnEventGotoState from 2 base classes.
        /// </summary>
        private class M11 : StateMachine
        {
            [Start]
            [OnEventGotoState(typeof(UnitEvent), typeof(Done))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventGotoState(typeof(UnitEvent), typeof(Error))]
            private class BaseState : BaseBaseState
            {
            }

            [OnEventGotoState(typeof(UnitEvent), typeof(Error))]
            private class BaseBaseState : State
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : State
            {
            }

            [OnEntry(nameof(ErrorOnEntry))]
            private class Error : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            private void DoneOnEntry()
            {
                this.Assert(false, "Test passed.");
            }

            private void ErrorOnEntry()
            {
                this.Assert(false, "Test failed!");
            }
        }

        /// <summary>
        /// Test overriding of OnEventGotoState in a State sub class.
        /// </summary>
        private class M12 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventGotoState(typeof(UnitEvent), typeof(Done))]
            private class BaseState : BaseBaseState
            {
            }

            [OnEventGotoState(typeof(UnitEvent), typeof(Error))]
            private class BaseBaseState : State
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : State
            {
            }

            [OnEntry(nameof(ErrorOnEntry))]
            private class Error : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            private void DoneOnEntry()
            {
                this.Assert(false, "Test passed.");
            }

            private void ErrorOnEntry()
            {
                this.Assert(false, "Test failed!");
            }
        }

        /// <summary>
        /// Test inheritance of OnEventPushState.
        /// </summary>
        private class M13 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventPushState(typeof(UnitEvent), typeof(Done))]
            private class BaseState : State
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            private void DoneOnEntry()
            {
                this.Assert(false, "Test passed.");
            }
        }

        /// <summary>
        /// Test overriding OnEventPushState.
        /// </summary>
        private class M14 : StateMachine
        {
            [Start]
            [OnEventPushState(typeof(UnitEvent), typeof(Done))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventPushState(typeof(UnitEvent), typeof(Error))]
            private class BaseState : State
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : State
            {
            }

            [OnEntry(nameof(ErrorOnEntry))]
            private class Error : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            private void DoneOnEntry()
            {
                this.Assert(false, "Test passed.");
            }

            private void ErrorOnEntry()
            {
                this.Assert(false, "Test failed!");
            }
        }

        /// <summary>
        /// Test overriding OnEventPushState inherited from 2 subclasses.
        /// </summary>
        private class M15 : StateMachine
        {
            [Start]
            [OnEventPushState(typeof(UnitEvent), typeof(Done))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventPushState(typeof(UnitEvent), typeof(Error))]
            private class BaseState : BaseBaseState
            {
            }

            [OnEventPushState(typeof(UnitEvent), typeof(Error))]
            private class BaseBaseState : State
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : State
            {
            }

            [OnEntry(nameof(ErrorOnEntry))]
            private class Error : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            private void DoneOnEntry()
            {
                this.Assert(false, "Test passed.");
            }

            private void ErrorOnEntry()
            {
                this.Assert(false, "Test failed!");
            }
        }

        /// <summary>
        /// Test overriding OnEventPushState in a state subclass.
        /// </summary>
        private class M16 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventPushState(typeof(UnitEvent), typeof(Done))]
            private class BaseState : BaseBaseState
            {
            }

            [OnEventPushState(typeof(UnitEvent), typeof(Error))]
            private class BaseBaseState : State
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : State
            {
            }

            [OnEntry(nameof(ErrorOnEntry))]
            private class Error : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            private void DoneOnEntry()
            {
                this.Assert(false, "Test passed.");
            }

            private void ErrorOnEntry()
            {
                this.Assert(false, "Test failed!");
            }
        }

        /// <summary>
        /// This class tests that a state can inherit actions from an the class.
        /// </summary>
        [OnEventDoAction(typeof(UnitEvent), nameof(Check))]
        private class M17 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(UnitEvent.Instance);
            }

            private void Check()
            {
                this.Assert(false, "Test passed.");
            }
        }

        /// <summary>
        /// This class tests that class level handler can cause transitions.
        /// </summary>
        [OnEventDoAction(typeof(UnitEvent), nameof(Check))]
        private class M18 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(UnitEvent.Instance);
            }

            private void Check()
            {
                this.RaiseGotoStateEvent(typeof(Done));
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : State
            {
            }

            private void DoneOnEntry()
            {
                this.Assert(false, "Test passed.");
            }
        }

        // ==================================================================================================================

        [Fact(Timeout = 5000)]
        public void TestMachineStateInheritingAbstractState()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M1));
            },
            expectedError: "Test passed.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateInheritingStateDuplicateStart()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M2));
            },
            expectedError: "M2() can not declare more than one start states.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateInheritingStateOnEntry()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M3));
            },
            expectedError: "Test passed.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateOverridingStateOnEntry()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M4));
            },
            expectedError: "Test passed.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateInheritingStateOnEventDoAction()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M5));
            },
            expectedError: "Test passed.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateOverridingStateOnEventDoAction()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M6));
            },
            expectedError: "Test passed.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateOverridingTwoStatesOnEventDoAction()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M7));
            },
            expectedError: "Test passed.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateOverridingDeepStateOnEventDoAction()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M8));
            },
            expectedError: "Test passed.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateInheritingStateOnEventGotoState()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M9));
            },
            expectedError: "Test passed.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateOverridingStateOnEventGotoState()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M10));
            },
            expectedError: "Test passed.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateOverridingTwoStatesOnEventGotoState()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M11));
            },
            expectedError: "Test passed.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateOverridingDeepStateOnEventGotoState()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M12));
            },
            expectedError: "Test passed.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateInheritingStateOnEventPushState()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M13));
            },
            expectedError: "Test passed.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateOverridingStateOnEventPushState()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M14));
            },
            expectedError: "Test passed.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateOverridingTwoStatesOnEventPushState()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M15));
            },
            expectedError: "Test passed.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateOverridingDeepStateOnEventPushState()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M16));
            },
            expectedError: "Test passed.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineInheritActionsFromClass()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M17));
            },
            expectedError: "Test passed.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineClassActionTransitions()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M18));
            },
            expectedError: "Test passed.");
        }
    }
}
