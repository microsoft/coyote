// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime.Exploration;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    public class EventHandlerTests : BaseTest
    {
        public EventHandlerTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SetupEvent : Event
        {
            public ActorId Id;

            public SetupEvent(ActorId id)
            {
                this.Id = id;
            }
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
            public int Value;

            public E4(int value)
            {
                this.Value = value;
            }
        }

        [OnEventDoAction(typeof(UnitEvent), nameof(HandleUnitEvent))]
        [OnEventDoAction(typeof(E1), nameof(HandleE1))]
        private class A1 : Actor
        {
            private bool Test = false;

            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
                this.SendEvent(this.Id, new E1());
                return Task.CompletedTask;
            }

            private void HandleUnitEvent(Event e)
            {
                this.Test = true;
                this.Assert(e is Event);
            }

            private void HandleE1(Event e)
            {
                this.Assert(e is Event);
                this.Assert(this.Test == false, "Reached test assertion.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInActor1()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A1));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [OnEventDoAction(typeof(E1), nameof(HandleE1))]
        private class A2 : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.SendEvent(this.Id, new E1());
                return Task.CompletedTask;
            }

            private void HandleE1()
            {
                this.Assert(false, "Reached test assertion.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInActor2()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A2));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [OnEventDoAction(typeof(UnitEvent), nameof(HandleUnitEvent))]
        private class A3 : Actor
        {
            private void HandleUnitEvent()
            {
                this.Assert(false, "Reached test assertion.");
            }
        }

        [Fact(Timeout=5000)]
        public void TestEventHandlerInActor3()
        {
            this.TestWithError(r =>
            {
                var id = r.CreateActor(typeof(A3));
                r.SendEvent(id, UnitEvent.Instance);
            },
            configuration: GetConfiguration(),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M1 : StateMachine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(UnitEvent), nameof(HandleUnitEvent))]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            private class Init : State
            {
            }

            private Transition InitOnEntry()
            {
                this.SendEvent(this.Id, new E1());
                return this.RaiseEvent(UnitEvent.Instance);
            }

            private void HandleUnitEvent()
            {
                this.Test = true;
            }

            private void HandleE1()
            {
                this.Assert(this.Test == false, "Reached test assertion.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInStateMachine1()
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
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, new E1());
            }

            private void HandleE1()
            {
                this.Assert(false, "Reached test assertion.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInStateMachine2()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M2));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M3 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(UnitEvent), typeof(Active))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            private void InitOnExit()
            {
                this.SendEvent(this.Id, new E1());
            }

            private class Active : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInStateMachine3()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M3));
            },
            expectedError: "'M3()' received event 'E1' that cannot be handled.",
            replay: true);
        }

        private class M4 : StateMachine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(UnitEvent), typeof(Active))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            private void InitOnExit()
            {
                this.SendEvent(this.Id, new E1());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
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

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInStateMachine4()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M4));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M5 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(UnitEvent), typeof(Init))]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            private void InitOnExit()
            {
                this.SendEvent(this.Id, new E1());
            }

            private void HandleE1()
            {
                this.Assert(false, "Reached test assertion.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInStateMachine5()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M5));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M6 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(UnitEvent), typeof(Init))]
            [OnEventPushState(typeof(E1), typeof(Init))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance, options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.SendEvent(this.Id, new E1());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInStateMachine6()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M6));
            },
            expectedError: "There are more than 1 instances of 'Actors.UnitEvent' in the input queue of 'M6()'.",
            replay: true);
        }

        private class M7 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(UnitEvent), typeof(Init))]
            [OnEventPushState(typeof(E1), typeof(Active))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            private void InitOnExit()
            {
                this.SendEvent(this.Id, new E1());
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

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInStateMachine7()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M7));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M8 : StateMachine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(UnitEvent), typeof(Init))]
            [OnEventPushState(typeof(E1), typeof(Active))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            private void InitOnExit()
            {
                this.SendEvent(this.Id, new E1());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.Test = true;
                this.SendEvent(this.Id, new E2());
            }

            private void HandleE2()
            {
                this.Assert(this.Test == false, "Reached test assertion.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInStateMachine8()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M8));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M9 : StateMachine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventPushState(typeof(UnitEvent), typeof(Active))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            private void InitOnExit()
            {
                this.SendEvent(this.Id, new E1());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : State
            {
            }

            private Transition ActiveOnEntry()
            {
                this.Test = true;
                this.SendEvent(this.Id, new E2());
                return this.PopState();
            }

            private void HandleE2()
            {
                this.Assert(this.Test == false, "Reached test assertion.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInStateMachine9()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M9));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M10 : StateMachine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventPushState(typeof(UnitEvent), typeof(Active))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            private void InitOnExit()
            {
                this.SendEvent(this.Id, new E1());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.Test = true;
                this.SendEvent(this.Id, new E2());
            }

            private void HandleE2()
            {
                this.Assert(this.Test == false, "Reached test assertion.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInStateMachine10()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M10));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M11 : StateMachine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(UnitEvent), typeof(Active))]
            [OnEventGotoState(typeof(E2), typeof(Checking))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnExit(nameof(ActiveOnExit))]
            [OnEventGotoState(typeof(E2), typeof(Init))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.Test = true;
                this.SendEvent(this.Id, new E2());
            }

            private void ActiveOnExit()
            {
                this.SendEvent(this.Id, new E2());
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

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInStateMachine11()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M11));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M12 : StateMachine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(UnitEvent), typeof(Init))]
            [OnEventPushState(typeof(E1), typeof(Active))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            private void InitOnExit()
            {
                this.SendEvent(this.Id, new E1());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnExit(nameof(ActiveOnExit))]
            [OnEventGotoState(typeof(E2), typeof(Init))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.Test = true;
                this.SendEvent(this.Id, new E2());
            }

            private void ActiveOnExit()
            {
                this.Assert(this.Test == false, "Reached test assertion.");
            }

            private void HandleE2()
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInStateMachine12()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M12));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M13 : StateMachine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventPushState(typeof(UnitEvent), typeof(Active))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
            }

            private void InitOnExit()
            {
                this.SendEvent(this.Id, new E1());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnExit(nameof(ActiveOnExit))]
            private class Active : State
            {
            }

            private Transition ActiveOnEntry()
            {
                this.Test = true;
                return this.PopState();
            }

            private void ActiveOnExit()
            {
                this.SendEvent(this.Id, new E2());
            }

            private void HandleE2()
            {
                this.Assert(this.Test == false, "Reached test assertion.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInStateMachine13()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M13));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M14 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(UnitEvent), typeof(Init))]
            [OnEventPushState(typeof(E1), typeof(Active))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance, options: new SendOptions(assert: 1));
            }

            private void InitOnExit()
            {
                this.SendEvent(this.Id, new E1());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : State
            {
            }

            private Transition ActiveOnEntry() => this.RaiseEvent(UnitEvent.Instance);

            private void HandleE2()
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInStateMachine14()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M14));
            },
            expectedError: "There are more than 1 instances of 'Actors.UnitEvent' in the input queue of 'M14()'.",
            replay: true);
        }

        private class M15 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventPushState(typeof(UnitEvent), typeof(Active))]
            private class Init : State
            {
            }

            private Transition InitOnEntry() => this.RaiseEvent(UnitEvent.Instance);

            [OnEntry(nameof(ActiveOnEntry))]
            [OnExit(nameof(ActiveOnExit))]
            private class Active : State
            {
            }

            private Transition ActiveOnEntry() => this.PopState();

            private void ActiveOnExit()
            {
                this.Assert(false, "Reached test assertion.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInStateMachine15()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M15));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M16 : StateMachine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventPushState(typeof(HaltEvent), typeof(Active))]
            [OnEventDoAction(typeof(UnitEvent), nameof(HandleUnitEvent))]
            private class Init : State
            {
            }

            private Transition InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
                return this.RaiseEvent(HaltEvent.Instance);
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.Test = true;
            }

            private void HandleUnitEvent()
            {
                this.Assert(this.Test == false, "Reached test assertion.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInStateMachine16()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M16));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M17 : StateMachine
        {
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(DefaultEvent), typeof(Active))]
            [OnEventDoAction(typeof(UnitEvent), nameof(HandleUnitEvent))]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            private class Init : State
            {
            }

            private Transition InitOnEntry() => this.RaiseEvent(new E1());

            private void HandleUnitEvent()
            {
                this.Test = true;
            }

            private void HandleE1()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
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

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInStateMachine17()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M17));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M18 : StateMachine
        {
            private readonly bool Test = false;

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
                this.Assert(this.Test == true, "Reached test assertion.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInStateMachine18()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M18));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M19 : StateMachine
        {
            private int Value;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventPushState(typeof(UnitEvent), typeof(Active))]
            [OnEventDoAction(typeof(DefaultEvent), nameof(DefaultAction))]
            private class Init : State
            {
            }

            private Transition InitOnEntry()
            {
                this.Value = 0;
                return this.RaiseEvent(UnitEvent.Instance);
            }

            private void DefaultAction()
            {
                this.Assert(false, "Reached test assertion.");
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [IgnoreEvents(typeof(UnitEvent))]
            private class Active : State
            {
            }

            private Transition ActiveOnEntry()
            {
                if (this.Value == 0)
                {
                    return this.RaiseEvent(UnitEvent.Instance);
                }
                else
                {
                    this.Value++;
                }

                return default;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInStateMachine19()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M19));
            },
            expectedError: "Reached test assertion.",
            replay: true);
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

            private void ActiveOnEntry(Event e)
            {
                this.Assert(e.GetType() == typeof(DefaultEvent));
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInStateMachine20()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(M20));
            });
        }

        private class M21a : StateMachine
        {
            private ActorId GhostMachine;
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitInit))]
            [OnEventGotoState(typeof(E1), typeof(S1))] // exit actions are performed before transition to S1
            [OnEventDoAction(typeof(E3), nameof(Action1))] // E3, E2 have no effect on reachability of assert(false)
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.GhostMachine = this.CreateActor(typeof(M21b));
                this.SendEvent(this.GhostMachine, new SetupEvent(this.Id));
                this.SendEvent(this.GhostMachine, UnitEvent.Instance);
            }

            private void ExitInit()
            {
                this.Test = true;
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(UnitEvent), typeof(S2))]
            private class S1 : State
            {
            }

            private Transition EntryS1()
            {
                this.Assert(this.Test == true); // Holds.
                return this.RaiseEvent(UnitEvent.Instance);
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : State
            {
            }

            private void EntryS2()
            {
                // This assert is reachable: M1A -UnitEvent-> M1B -E1-> M1A;
                // then Real_S1 (assert holds), Real_S2 (assert fails)
                this.Assert(false, "Reached test assertion.");
            }

            private void Action1()
            {
                this.SendEvent(this.GhostMachine, new E2());
            }
        }

        private class M21b : StateMachine
        {
            private ActorId RealMachine;

            [Start]
            [OnEventDoAction(typeof(SetupEvent), nameof(SetupEvent))]
            [OnEventGotoState(typeof(UnitEvent), typeof(S1))]
            private class Init : State
            {
            }

            private void SetupEvent(Event e)
            {
                this.RealMachine = (e as SetupEvent).Id;
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(E2), typeof(S2))]
            private class S1 : State
            {
            }

            private void EntryS1()
            {
                this.SendEvent(this.RealMachine, new E3());
                this.SendEvent(this.RealMachine, new E1());
            }

            private class S2 : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInStateMachine21()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M21a));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M22a : StateMachine
        {
            private ActorId GhostMachine;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E3), typeof(S2))]
            [OnEventPushState(typeof(UnitEvent), typeof(S1))]
            [OnEventDoAction(typeof(E1), nameof(Action1))]
            private class Init : State
            {
            }

            private Transition InitOnEntry()
            {
                this.GhostMachine = this.CreateActor(typeof(M22b));
                this.SendEvent(this.GhostMachine, new SetupEvent(this.Id));
                return this.RaiseEvent(UnitEvent.Instance);
            }

            [OnEntry(nameof(EntryS1))]
            private class S1 : State
            {
            }

            private void EntryS1()
            {
                this.SendEvent(this.GhostMachine, UnitEvent.Instance);

                // We wait in this state until E1 comes from M2B,
                // then handle E1 using the inherited handler Action1
                // installed by Init.
                // Then wait until E3 comes from M2B, and since
                // there's no handler for E3 in this pushed state,
                // this state is popped, and E3 goto handler from Init
                // is invoked.
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : State
            {
            }

            private void EntryS2()
            {
                // This assert is reachable.
                this.Assert(false, "Reached test assertion.");
            }

            private void Action1()
            {
                this.SendEvent(this.GhostMachine, new E2());
            }
        }

        private class M22b : StateMachine
        {
            private ActorId RealMachine;

            [Start]
            [OnEventDoAction(typeof(SetupEvent), nameof(SetupEvent))]
            [OnEventGotoState(typeof(UnitEvent), typeof(S1))]
            private class Init : State
            {
            }

            private void SetupEvent(Event e)
            {
                this.RealMachine = (e as SetupEvent).Id;
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(E2), typeof(S2))]
            private class S1 : State
            {
            }

            private void EntryS1()
            {
                this.SendEvent(this.RealMachine, new E1());
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : State
            {
            }

            private void EntryS2()
            {
                this.SendEvent(this.RealMachine, new E3());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInStateMachine22()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M22a));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M23a : StateMachine
        {
            private ActorId GhostMachine;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E3), typeof(S2))]
            [OnEventPushState(typeof(UnitEvent), typeof(S1))]
            [OnEventDoAction(typeof(E4), nameof(Action1))]
            private class Init : State
            {
            }

            private Transition InitOnEntry()
            {
                this.GhostMachine = this.CreateActor(typeof(M23b));
                this.SendEvent(this.GhostMachine, new SetupEvent(this.Id));
                return this.RaiseEvent(UnitEvent.Instance);
            }

            [OnEntry(nameof(EntryS1))]
            private class S1 : State
            {
            }

            private void EntryS1()
            {
                this.SendEvent(this.GhostMachine, UnitEvent.Instance);
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : State
            {
            }

            private void EntryS2()
            {
                // This assert is reachable.
                this.Assert(false, "Reached test assertion.");
            }

            private void Action1()
            {
                this.SendEvent(this.GhostMachine, new E2());
            }
        }

        private class M23b : StateMachine
        {
            private ActorId RealMachine;

            [Start]
            [OnEventDoAction(typeof(SetupEvent), nameof(SetupEvent))]
            [OnEventGotoState(typeof(UnitEvent), typeof(S1))]
            private class Init : State
            {
            }

            private void SetupEvent(Event e)
            {
                this.RealMachine = (e as SetupEvent).Id;
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(E2), typeof(S2))]
            private class S1 : State
            {
            }

            private void EntryS1()
            {
                this.SendEvent(this.RealMachine, new E4(100));
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : State
            {
            }

            private void EntryS2()
            {
                this.SendEvent(this.RealMachine, new E3());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventHandlerInStateMachine23()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M23a));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS),
            expectedError: "Reached test assertion.",
            replay: true);
        }
    }
}
