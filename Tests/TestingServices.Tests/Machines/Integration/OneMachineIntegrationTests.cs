// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote.Machines;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class OneMachineIntegrationTests : BaseTest
    {
        public OneMachineIntegrationTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
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

        private class M1 : Machine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E2(), options: new SendOptions(assert: 1));
                this.Raise(new E1());
            }

            private void HandleE1()
            {
                this.Test = true;
            }

            private void HandleE2()
            {
                this.Assert(this.Test == false);
            }
        }

        private class M2 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E2(), options: new SendOptions(assert: 1));
            }

            private void HandleE2()
            {
                this.Assert(false);
            }
        }

        private class M3 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Active))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.Send(this.Id, new E2(), options: new SendOptions(assert: 1));
            }

            private class Active : MachineState
            {
            }
        }

        private class M4 : Machine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Active))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.Send(this.Id, new E2(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Test = true;
            }

            private void HandleE2()
            {
                this.Assert(this.Test == false);
            }
        }

        private class M5 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Init))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.Send(this.Id, new E2(), options: new SendOptions(assert: 1));
            }

            private void HandleE2()
            {
                this.Assert(false);
            }
        }

        private class M6 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Init))]
            [OnEventPushState(typeof(E2), typeof(Init))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.Send(this.Id, new E2(), options: new SendOptions(assert: 1));
            }
        }

        private class M7 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Init))]
            [OnEventPushState(typeof(E2), typeof(Active))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.Send(this.Id, new E2(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Assert(false);
            }
        }

        private class M8 : Machine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Init))]
            [OnEventPushState(typeof(E2), typeof(Active))]
            [OnEventDoAction(typeof(E3), nameof(HandleE3))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.Send(this.Id, new E2(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Test = true;
                this.Send(this.Id, new E3(), options: new SendOptions(assert: 1));
            }

            private void HandleE3()
            {
                this.Assert(this.Test == false);
            }
        }

        private class M9 : Machine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventPushState(typeof(E1), typeof(Active))]
            [OnEventDoAction(typeof(E3), nameof(HandleE3))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.Send(this.Id, new E2(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Test = true;
                this.Send(this.Id, new E3(), options: new SendOptions(assert: 1));
                this.Pop();
            }

            private void HandleE3()
            {
                this.Assert(this.Test == false);
            }
        }

        private class M10 : Machine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventPushState(typeof(E1), typeof(Active))]
            [OnEventDoAction(typeof(E3), nameof(HandleE3))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.Send(this.Id, new E2(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Test = true;
                this.Send(this.Id, new E3(), options: new SendOptions(assert: 1));
            }

            private void HandleE3()
            {
                this.Assert(this.Test == false);
            }
        }

        private class M11 : Machine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E1), typeof(Active))]
            [OnEventGotoState(typeof(E3), typeof(Checking))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnExit(nameof(ActiveOnExit))]
            [OnEventGotoState(typeof(E3), typeof(Init))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Test = true;
                this.Send(this.Id, new E3(), options: new SendOptions(assert: 1));
            }

            private void ActiveOnExit()
            {
                this.Send(this.Id, new E3(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(CheckingOnEntry))]
            private class Checking : MachineState
            {
            }

            private void CheckingOnEntry()
            {
                this.Assert(this.Test == false);
            }
        }

        private class M12 : Machine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Init))]
            [OnEventPushState(typeof(E2), typeof(Active))]
            [OnEventDoAction(typeof(E3), nameof(HandleE3))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.Send(this.Id, new E2(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnExit(nameof(ActiveOnExit))]
            [OnEventGotoState(typeof(E3), typeof(Init))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Test = true;
                this.Send(this.Id, new E3(), options: new SendOptions(assert: 1));
            }

            private void ActiveOnExit()
            {
                this.Assert(this.Test == false);
            }

            private void HandleE3()
            {
            }
        }

        private class M13 : Machine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventPushState(typeof(E1), typeof(Active))]
            [OnEventDoAction(typeof(E3), nameof(HandleE3))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.Send(this.Id, new E2(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnExit(nameof(ActiveOnExit))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Test = true;
                this.Pop();
            }

            private void ActiveOnExit()
            {
                this.Send(this.Id, new E3(), options: new SendOptions(assert: 1));
            }

            private void HandleE3()
            {
                this.Assert(this.Test == false);
            }
        }

        private class M14 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Init))]
            [OnEventPushState(typeof(E2), typeof(Active))]
            [OnEventDoAction(typeof(E3), nameof(HandleE3))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.Send(this.Id, new E2(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Raise(new E1());
            }

            private void HandleE3()
            {
            }
        }

        private class M15 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventPushState(typeof(E), typeof(Active))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new E());
            }

            private void InitOnExit()
            {
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnExit(nameof(ActiveOnExit))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Pop();
            }

            private void ActiveOnExit()
            {
                this.Assert(false);
            }
        }

        private class M16 : Machine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventPushState(typeof(Halt), typeof(Active))]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Send(this.Id, new E1(), options: new SendOptions(assert: 1));
                this.Raise(new Halt());
            }

            private void InitOnExit()
            {
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Test = true;
            }

            private void HandleE1()
            {
                this.Assert(this.Test == false);
            }
        }

        private class M17 : Machine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(Default), typeof(Active))]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Raise(new E2());
            }

            private void InitOnExit()
            {
            }

            private void HandleE1()
            {
                this.Test = true;
            }

            private void HandleE2()
            {
                this.Send(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Assert(this.Test == false);
            }
        }

        private class M18 : Machine
        {
            private readonly bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(Default), typeof(Active))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
            }

            private void InitOnExit()
            {
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Assert(this.Test == true);
            }
        }

        private class M19 : Machine
        {
            private int Value;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventPushState(typeof(E), typeof(Active))]
            [OnEventDoAction(typeof(Default), nameof(DefaultAction))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                this.Value = 0;
                this.Raise(new E());
            }

            private void InitOnExit()
            {
            }

            private void DefaultAction()
            {
                this.Assert(false);
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnExit(nameof(ActiveOnExit))]
            [IgnoreEvents(typeof(E))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                if (this.Value == 0)
                {
                    this.Raise(new E());
                }
                else
                {
                    this.Value++;
                }
            }

            private void ActiveOnExit()
            {
            }
        }

        private class M20 : Machine
        {
            [Start]
            [OnEventGotoState(typeof(Default), typeof(Active))]
            private class Init : MachineState
            {
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : MachineState
            {
            }

            private void ActiveOnEntry()
            {
                this.Assert(this.ReceivedEvent.GetType() == typeof(Default));
            }
        }

        [Fact(Timeout=5000)]
        public void TestOneMachineIntegration1()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M1));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestOneMachineIntegration2()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M2));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestOneMachineIntegration3()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M3));
            },
            expectedError: "Machine 'M3()' received event 'E2' that cannot be handled.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestOneMachineIntegration4()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M4));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestOneMachineIntegration5()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M5));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestOneMachineIntegration6()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M6));
            },
            expectedError: "There are more than 1 instances of 'E1' in the input queue of machine 'M6()'.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestOneMachineIntegration7()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M7));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestOneMachineIntegration8()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M8));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestOneMachineIntegration9()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M9));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestOneMachineIntegration10()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M10));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestOneMachineIntegration11()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M11));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestOneMachineIntegration12()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M12));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestOneMachineIntegration13()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M13));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestOneMachineIntegration14()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M14));
            },
            expectedError: "There are more than 1 instances of 'E1' in the input queue of machine 'M14()'.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestOneMachineIntegration15()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M15));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestOneMachineIntegration16()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M16));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestOneMachineIntegration17()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M17));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestOneMachineIntegration18()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M18));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestOneMachineIntegration19()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M19));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestOneMachineIntegration20()
        {
            this.Test(r =>
            {
                r.CreateMachine(typeof(M20));
            });
        }
    }
}
