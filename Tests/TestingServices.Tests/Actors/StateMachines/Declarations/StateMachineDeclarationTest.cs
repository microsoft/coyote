// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors.StateMachines
{
    public class StateMachineDeclarationTest : BaseTest
    {
        public StateMachineDeclarationTest(ITestOutputHelper output)
            : base(output)
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

        private class M1 : StateMachine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, new E2(), options: new SendOptions(assert: 1));
                this.RaiseEvent(new E1());
            }

            private void HandleE1()
            {
                this.Test = true;
            }

            private void HandleE2()
            {
                this.Assert(this.Test == false, "Reached test assertion.");
            }
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, new E2(), options: new SendOptions(assert: 1));
            }

            private void HandleE2()
            {
                this.Assert(false, "Reached test assertion.");
            }
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Active))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.SendEvent(this.Id, new E2(), options: new SendOptions(assert: 1));
            }

            private class Active : State
            {
            }
        }

        private class M4 : StateMachine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Active))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.SendEvent(this.Id, new E2(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.Test = true;
            }

            private void HandleE2()
            {
                this.Assert(this.Test == false, "Reached test assertion.");
            }
        }

        private class M5 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Init))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.SendEvent(this.Id, new E2(), options: new SendOptions(assert: 1));
            }

            private void HandleE2()
            {
                this.Assert(false, "Reached test assertion.");
            }
        }

        private class M6 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Init))]
            [OnEventPushState(typeof(E2), typeof(Init))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.SendEvent(this.Id, new E2(), options: new SendOptions(assert: 1));
            }
        }

        private class M7 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Init))]
            [OnEventPushState(typeof(E2), typeof(Active))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.SendEvent(this.Id, new E2(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.Assert(false, "Reached test assertion.");
            }
        }

        private class M8 : StateMachine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Init))]
            [OnEventPushState(typeof(E2), typeof(Active))]
            [OnEventDoAction(typeof(E3), nameof(HandleE3))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.SendEvent(this.Id, new E2(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.Test = true;
                this.SendEvent(this.Id, new E3(), options: new SendOptions(assert: 1));
            }

            private void HandleE3()
            {
                this.Assert(this.Test == false, "Reached test assertion.");
            }
        }

        private class M9 : StateMachine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventPushState(typeof(E1), typeof(Active))]
            [OnEventDoAction(typeof(E3), nameof(HandleE3))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.SendEvent(this.Id, new E2(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.Test = true;
                this.SendEvent(this.Id, new E3(), options: new SendOptions(assert: 1));
                this.Pop();
            }

            private void HandleE3()
            {
                this.Assert(this.Test == false, "Reached test assertion.");
            }
        }

        private class M10 : StateMachine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventPushState(typeof(E1), typeof(Active))]
            [OnEventDoAction(typeof(E3), nameof(HandleE3))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.SendEvent(this.Id, new E2(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.Test = true;
                this.SendEvent(this.Id, new E3(), options: new SendOptions(assert: 1));
            }

            private void HandleE3()
            {
                this.Assert(this.Test == false, "Reached test assertion.");
            }
        }

        private class M11 : StateMachine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E1), typeof(Active))]
            [OnEventGotoState(typeof(E3), typeof(Checking))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnExit(nameof(ActiveOnExit))]
            [OnEventGotoState(typeof(E3), typeof(Init))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.Test = true;
                this.SendEvent(this.Id, new E3(), options: new SendOptions(assert: 1));
            }

            private void ActiveOnExit()
            {
                this.SendEvent(this.Id, new E3(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(CheckingOnEntry))]
            private class Checking : State
            {
            }

            private void CheckingOnEntry()
            {
                this.Assert(this.Test == false, "Reached test assertion.");
            }
        }

        private class M12 : StateMachine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Init))]
            [OnEventPushState(typeof(E2), typeof(Active))]
            [OnEventDoAction(typeof(E3), nameof(HandleE3))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.SendEvent(this.Id, new E2(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnExit(nameof(ActiveOnExit))]
            [OnEventGotoState(typeof(E3), typeof(Init))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.Test = true;
                this.SendEvent(this.Id, new E3(), options: new SendOptions(assert: 1));
            }

            private void ActiveOnExit()
            {
                this.Assert(this.Test == false, "Reached test assertion.");
            }

            private void HandleE3()
            {
            }
        }

        private class M13 : StateMachine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventPushState(typeof(E1), typeof(Active))]
            [OnEventDoAction(typeof(E3), nameof(HandleE3))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.SendEvent(this.Id, new E2(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnExit(nameof(ActiveOnExit))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.Test = true;
                this.Pop();
            }

            private void ActiveOnExit()
            {
                this.SendEvent(this.Id, new E3(), options: new SendOptions(assert: 1));
            }

            private void HandleE3()
            {
                this.Assert(this.Test == false, "Reached test assertion.");
            }
        }

        private class M14 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Init))]
            [OnEventPushState(typeof(E2), typeof(Active))]
            [OnEventDoAction(typeof(E3), nameof(HandleE3))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.SendEvent(this.Id, new E2(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.RaiseEvent(new E1());
            }

            private void HandleE3()
            {
            }
        }

        private class M15 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventPushState(typeof(UnitEvent), typeof(Active))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(new UnitEvent());
            }

            private void InitOnExit()
            {
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnExit(nameof(ActiveOnExit))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.Pop();
            }

            private void ActiveOnExit()
            {
                this.Assert(false, "Reached test assertion.");
            }
        }

        private class M16 : StateMachine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventPushState(typeof(HaltEvent), typeof(Active))]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, new E1(), options: new SendOptions(assert: 1));
                this.RaiseEvent(new HaltEvent());
            }

            private void InitOnExit()
            {
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.Test = true;
            }

            private void HandleE1()
            {
                this.Assert(this.Test == false, "Reached test assertion.");
            }
        }

        private class M17 : StateMachine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(DefaultEvent), typeof(Active))]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(new E2());
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
                this.SendEvent(this.Id, new E1(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.Assert(this.Test == false, "Reached test assertion.");
            }
        }

        private class M18 : StateMachine
        {
            private readonly bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(DefaultEvent), typeof(Active))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
            }

            private void InitOnExit()
            {
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.Assert(this.Test == true, "Reached test assertion.");
            }
        }

        private class M19 : StateMachine
        {
            private int Value;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventPushState(typeof(UnitEvent), typeof(Active))]
            [OnEventDoAction(typeof(DefaultEvent), nameof(DefaultAction))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Value = 0;
                this.RaiseEvent(new UnitEvent());
            }

            private void InitOnExit()
            {
            }

            private void DefaultAction()
            {
                this.Assert(false, "Reached test assertion.");
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnExit(nameof(ActiveOnExit))]
            [IgnoreEvents(typeof(UnitEvent))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                if (this.Value == 0)
                {
                    this.RaiseEvent(new UnitEvent());
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

        private class M20 : StateMachine
        {
            [Start]
            [OnEventGotoState(typeof(DefaultEvent), typeof(Active))]
            private class Init : State
            {
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.Assert(this.ReceivedEvent.GetType() == typeof(DefaultEvent));
            }
        }

        [Fact(Timeout=5000)]
        public void TestStateMachineDeclaration1()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M1));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestStateMachineDeclaration2()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M2));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestStateMachineDeclaration3()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M3));
            },
            expectedError: "'M3()' received event 'E2' that cannot be handled.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestStateMachineDeclaration4()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M4));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestStateMachineDeclaration5()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M5));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestStateMachineDeclaration6()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M6));
            },
            expectedError: "There are more than 1 instances of 'E1' in the input queue of 'M6()'.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestStateMachineDeclaration7()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M7));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestStateMachineDeclaration8()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M8));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestStateMachineDeclaration9()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M9));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestStateMachineDeclaration10()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M10));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestStateMachineDeclaration11()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M11));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestStateMachineDeclaration12()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M12));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestStateMachineDeclaration13()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M13));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestStateMachineDeclaration14()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M14));
            },
            expectedError: "There are more than 1 instances of 'E1' in the input queue of 'M14()'.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestStateMachineDeclaration15()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M15));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestStateMachineDeclaration16()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M16));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestStateMachineDeclaration17()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M17));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestStateMachineDeclaration18()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M18));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestStateMachineDeclaration19()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M19));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestStateMachineDeclaration20()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M20));
            });
        }
    }
}
