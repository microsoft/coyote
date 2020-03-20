// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common.Events;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Actors
{
    public class StateInheritanceTests : BaseSystematicTest
    {
        public StateInheritanceTests(ITestOutputHelper output)
            : base(output)
        {
        }

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
                this.Assert(false, "Error reached.");
            }
        }

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
                this.Assert(false, "Error reached.");
            }
        }

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
            }

            private void BaseOnEntry()
            {
                this.Assert(false, "Error reached.");
            }
        }

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
                this.Assert(false, "Error reached.");
            }
        }

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
            }

            private void BaseCheck()
            {
                this.Assert(false, "Error reached.");
            }
        }

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
            }

            private void BaseCheck()
            {
                this.Assert(false, "Error reached.");
            }

            private void BaseBaseCheck()
            {
                this.Assert(false, "Error reached.");
            }
        }

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
            }

            private void BaseBaseCheck()
            {
                this.Assert(false, "Error reached.");
            }
        }

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
                this.Assert(false, "Done reached.");
            }
        }

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
                this.Assert(false, "Done reached.");
            }

            private void ErrorOnEntry()
            {
                this.Assert(false, "Error reached.");
            }
        }

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
                this.Assert(false, "Done reached.");
            }

            private void ErrorOnEntry()
            {
                this.Assert(false, "Error reached.");
            }
        }

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
                this.Assert(false, "Done reached.");
            }

            private void ErrorOnEntry()
            {
                this.Assert(false, "Error reached.");
            }
        }

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
                this.Assert(false, "Done reached.");
            }
        }

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
                this.Assert(false, "Done reached.");
            }

            private void ErrorOnEntry()
            {
                this.Assert(false, "Error reached.");
            }
        }

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
                this.Assert(false, "Done reached.");
            }

            private void ErrorOnEntry()
            {
                this.Assert(false, "Error reached.");
            }
        }

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
                this.Assert(false, "Done reached.");
            }

            private void ErrorOnEntry()
            {
                this.Assert(false, "Error reached.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateInheritingAbstractState()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M1));
            },
            expectedError: "Error reached.");
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
            expectedError: "Error reached.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateOverridingStateOnEntry()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M4));
            });
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateInheritingStateOnEventDoAction()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M5));
            },
            expectedError: "Error reached.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateOverridingStateOnEventDoAction()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M6));
            });
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateOverridingTwoStatesOnEventDoAction()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M7));
            });
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateOverridingDeepStateOnEventDoAction()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M8));
            });
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateInheritingStateOnEventGotoState()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M9));
            },
            expectedError: "Done reached.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateOverridingStateOnEventGotoState()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M10));
            },
            expectedError: "Done reached.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateOverridingTwoStatesOnEventGotoState()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M11));
            },
            expectedError: "Done reached.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateOverridingDeepStateOnEventGotoState()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M12));
            },
            expectedError: "Done reached.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateInheritingStateOnEventPushState()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M13));
            },
            expectedError: "Done reached.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateOverridingStateOnEventPushState()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M14));
            },
            expectedError: "Done reached.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateOverridingTwoStatesOnEventPushState()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M15));
            },
            expectedError: "Done reached.");
        }

        [Fact(Timeout = 5000)]
        public void TestMachineStateOverridingDeepStateOnEventPushState()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M16));
            },
            expectedError: "Done reached.");
        }
    }
}
