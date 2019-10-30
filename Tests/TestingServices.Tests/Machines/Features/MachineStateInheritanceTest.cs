// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Machines;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class MachineStateInheritanceTest : BaseTest
    {
        public MachineStateInheritanceTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(E), nameof(Check))]
            private abstract class BaseState : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
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
            private class BaseState : MachineState
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
            private class BaseState : MachineState
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
            private class BaseState : MachineState
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

            [OnEventDoAction(typeof(E), nameof(Check))]
            private class BaseState : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
            }

            private void Check()
            {
                this.Assert(false, "Error reached.");
            }
        }

        private class M6 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Check))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(E), nameof(BaseCheck))]
            private class BaseState : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
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
            [OnEventDoAction(typeof(E), nameof(Check))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(E), nameof(BaseCheck))]
            private class BaseState : BaseBaseState
            {
            }

            [OnEventDoAction(typeof(E), nameof(BaseBaseCheck))]
            private class BaseBaseState : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
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

            [OnEventDoAction(typeof(E), nameof(BaseCheck))]
            private class BaseState : BaseBaseState
            {
            }

            [OnEventDoAction(typeof(E), nameof(BaseBaseCheck))]
            private class BaseBaseState : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
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

            [OnEventGotoState(typeof(E), typeof(Done))]
            private class BaseState : MachineState
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
            }

            private void DoneOnEntry()
            {
                this.Assert(false, "Done reached.");
            }
        }

        private class M10 : StateMachine
        {
            [Start]
            [OnEventGotoState(typeof(E), typeof(Done))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventGotoState(typeof(E), typeof(Error))]
            private class BaseState : MachineState
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : MachineState
            {
            }

            [OnEntry(nameof(ErrorOnEntry))]
            private class Error : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
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
            [OnEventGotoState(typeof(E), typeof(Done))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventGotoState(typeof(E), typeof(Error))]
            private class BaseState : BaseBaseState
            {
            }

            [OnEventGotoState(typeof(E), typeof(Error))]
            private class BaseBaseState : MachineState
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : MachineState
            {
            }

            [OnEntry(nameof(ErrorOnEntry))]
            private class Error : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
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

            [OnEventGotoState(typeof(E), typeof(Done))]
            private class BaseState : BaseBaseState
            {
            }

            [OnEventGotoState(typeof(E), typeof(Error))]
            private class BaseBaseState : MachineState
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : MachineState
            {
            }

            [OnEntry(nameof(ErrorOnEntry))]
            private class Error : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
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

            [OnEventPushState(typeof(E), typeof(Done))]
            private class BaseState : MachineState
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
            }

            private void DoneOnEntry()
            {
                this.Assert(false, "Done reached.");
            }
        }

        private class M14 : StateMachine
        {
            [Start]
            [OnEventPushState(typeof(E), typeof(Done))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventPushState(typeof(E), typeof(Error))]
            private class BaseState : MachineState
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : MachineState
            {
            }

            [OnEntry(nameof(ErrorOnEntry))]
            private class Error : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
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
            [OnEventPushState(typeof(E), typeof(Done))]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : BaseState
            {
            }

            [OnEventPushState(typeof(E), typeof(Error))]
            private class BaseState : BaseBaseState
            {
            }

            [OnEventPushState(typeof(E), typeof(Error))]
            private class BaseBaseState : MachineState
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : MachineState
            {
            }

            [OnEntry(nameof(ErrorOnEntry))]
            private class Error : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
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

            [OnEventPushState(typeof(E), typeof(Done))]
            private class BaseState : BaseBaseState
            {
            }

            [OnEventPushState(typeof(E), typeof(Error))]
            private class BaseBaseState : MachineState
            {
            }

            [OnEntry(nameof(DoneOnEntry))]
            private class Done : MachineState
            {
            }

            [OnEntry(nameof(ErrorOnEntry))]
            private class Error : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E());
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

        [Fact(Timeout=5000)]
        public void TestMachineStateInheritingAbstractState()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M1));
            },
            expectedError: "Error reached.");
        }

        [Fact(Timeout=5000)]
        public void TestMachineStateInheritingStateDuplicateStart()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M2));
            },
            expectedError: "Machine 'M2()' can not declare more than one start states.");
        }

        [Fact(Timeout=5000)]
        public void TestMachineStateInheritingStateOnEntry()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M3));
            },
            expectedError: "Error reached.");
        }

        [Fact(Timeout=5000)]
        public void TestMachineStateOverridingStateOnEntry()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M4));
            });
        }

        [Fact(Timeout=5000)]
        public void TestMachineStateInheritingStateOnEventDoAction()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M5));
            },
            expectedError: "Error reached.");
        }

        [Fact(Timeout=5000)]
        public void TestMachineStateOverridingStateOnEventDoAction()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M6));
            });
        }

        [Fact(Timeout=5000)]
        public void TestMachineStateOverridingTwoStatesOnEventDoAction()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M7));
            });
        }

        [Fact(Timeout=5000)]
        public void TestMachineStateOverridingDeepStateOnEventDoAction()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M8));
            });
        }

        [Fact(Timeout=5000)]
        public void TestMachineStateInheritingStateOnEventGotoState()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M9));
            },
            expectedError: "Done reached.");
        }

        [Fact(Timeout=5000)]
        public void TestMachineStateOverridingStateOnEventGotoState()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M10));
            },
            expectedError: "Done reached.");
        }

        [Fact(Timeout=5000)]
        public void TestMachineStateOverridingTwoStatesOnEventGotoState()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M11));
            },
            expectedError: "Done reached.");
        }

        [Fact(Timeout=5000)]
        public void TestMachineStateOverridingDeepStateOnEventGotoState()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M12));
            },
            expectedError: "Done reached.");
        }

        [Fact(Timeout=5000)]
        public void TestMachineStateInheritingStateOnEventPushState()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M13));
            },
            expectedError: "Done reached.");
        }

        [Fact(Timeout=5000)]
        public void TestMachineStateOverridingStateOnEventPushState()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M14));
            },
            expectedError: "Done reached.");
        }

        [Fact(Timeout=5000)]
        public void TestMachineStateOverridingTwoStatesOnEventPushState()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M15));
            },
            expectedError: "Done reached.");
        }

        [Fact(Timeout=5000)]
        public void TestMachineStateOverridingDeepStateOnEventPushState()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M16));
            },
            expectedError: "Done reached.");
        }
    }
}
