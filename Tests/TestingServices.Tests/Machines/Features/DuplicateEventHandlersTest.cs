// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote.Machines;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class DuplicateEventHandlersTest : BaseTest
    {
        public DuplicateEventHandlersTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
        }

        private class M1 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Check1))]
            [OnEventDoAction(typeof(E), nameof(Check2))]
            private class Init : MachineState
            {
            }

            private void Check1()
            {
            }

            private void Check2()
            {
            }
        }

        private class M2 : Machine
        {
            [Start]
            [OnEventGotoState(typeof(E), typeof(S1))]
            [OnEventGotoState(typeof(E), typeof(S2))]
            private class Init : MachineState
            {
            }

            private class S1 : MachineState
            {
            }

            private class S2 : MachineState
            {
            }
        }

        private class M3 : Machine
        {
            [Start]
            [OnEventPushState(typeof(E), typeof(S1))]
            [OnEventPushState(typeof(E), typeof(S2))]
            private class Init : MachineState
            {
            }

            private class S1 : MachineState
            {
            }

            private class S2 : MachineState
            {
            }
        }

        private class M4 : Machine
        {
            [Start]
            private class Init : BaseState
            {
            }

            [OnEventDoAction(typeof(E), nameof(Check1))]
            [OnEventDoAction(typeof(E), nameof(Check2))]
            private class BaseState : MachineState
            {
            }

            private void Check1()
            {
            }

            private void Check2()
            {
            }
        }

        private class M5 : Machine
        {
            [Start]
            private class Init : BaseState
            {
            }

            [OnEventGotoState(typeof(E), typeof(S1))]
            [OnEventGotoState(typeof(E), typeof(S2))]
            private class BaseState : MachineState
            {
            }

            private class S1 : MachineState
            {
            }

            private class S2 : MachineState
            {
            }
        }

        private class M6 : Machine
        {
            [Start]
            private class Init : BaseState
            {
            }

            [OnEventPushState(typeof(E), typeof(S1))]
            [OnEventPushState(typeof(E), typeof(S2))]
            private class BaseState : MachineState
            {
            }

            private class S1 : MachineState
            {
            }

            private class S2 : MachineState
            {
            }
        }

        private class M7 : Machine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Check))]
            [OnEventGotoState(typeof(E), typeof(S1))]
            [OnEventPushState(typeof(E), typeof(S2))]
            private class Init : MachineState
            {
            }

            private class S1 : MachineState
            {
            }

            private class S2 : MachineState
            {
            }

            private void Check()
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestMachineDuplicateEventHandlerDo()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M1));
            },
            expectedError: "Machine 'M1()' declared multiple handlers for event 'E' in state 'M1+Init'.");
        }

        [Fact(Timeout=5000)]
        public void TestMachineDuplicateEventHandlerGoto()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M2));
            },
            expectedError: "Machine 'M2()' declared multiple handlers for event 'E' in state 'M2+Init'.");
        }

        [Fact(Timeout=5000)]
        public void TestMachineDuplicateEventHandlerPush()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M3));
            },
            expectedError: "Machine 'M3()' declared multiple handlers for event 'E' in state 'M3+Init'.");
        }

        [Fact(Timeout=5000)]
        public void TestMachineDuplicateEventHandlerInheritanceDo()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M4));
            },
            expectedError: "Machine 'M4()' inherited multiple handlers for event 'E' from state 'M4+BaseState' in state 'M4+Init'.");
        }

        [Fact(Timeout=5000)]
        public void TestMachineDuplicateEventHandlerInheritanceGoto()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M5));
            },
            expectedError: "Machine 'M5()' inherited multiple handlers for event 'E' from state 'M5+BaseState' in state 'M5+Init'.");
        }

        [Fact(Timeout=5000)]
        public void TestMachineDuplicateEventHandlerInheritancePush()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M6));
            },
            expectedError: "Machine 'M6()' inherited multiple handlers for event 'E' from state 'M6+BaseState' in state 'M6+Init'.");
        }

        [Fact(Timeout=5000)]
        public void TestMachineDuplicateEventHandlerMixed()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M7));
            },
            expectedError: "Machine 'M7()' declared multiple handlers for event 'E' in state 'M7+Init'.");
        }
    }
}
