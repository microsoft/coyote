// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime.Exploration;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors.StateMachines
{
    public class StateMachineEventHandlerTest : BaseTest
    {
        public StateMachineEventHandlerTest(ITestOutputHelper output)
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
        }

        private class E5 : Event
        {
            public int Value;

            public E5(int value)
            {
                this.Value = value;
            }
        }

        private class M1A : StateMachine
        {
            private ActorId GhostMachine;
            private bool Test = false;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(ExitInit))]
            [OnEventGotoState(typeof(E2), typeof(S1))] // exit actions are performed before transition to S1
            [OnEventDoAction(typeof(E4), nameof(Action1))] // E4, E3 have no effect on reachability of assert(false)
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.GhostMachine = this.CreateActor(typeof(M1B));
                this.SendEvent(this.GhostMachine, new SetupEvent(this.Id));
                this.SendEvent(this.GhostMachine, new E1(), options: new SendOptions(assert: 1));
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

            private void EntryS1()
            {
                this.Assert(this.Test == true); // Holds.
                this.RaiseEvent(new UnitEvent());
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : State
            {
            }

            private void EntryS2()
            {
                // This assert is reachable: M1A -E1-> M1B -E2-> M1A;
                // then Real_S1 (assert holds), Real_S2 (assert fails)
                this.Assert(false);
            }

            private void Action1()
            {
                this.SendEvent(this.GhostMachine, new E3(), options: new SendOptions(assert: 1));
            }
        }

        private class M1B : StateMachine
        {
            private ActorId RealMachine;

            [Start]
            [OnEventDoAction(typeof(SetupEvent), nameof(SetupEvent))]
            [OnEventGotoState(typeof(E1), typeof(S1))]
            private class Init : State
            {
            }

            private void SetupEvent()
            {
                this.RealMachine = (this.ReceivedEvent as SetupEvent).Id;
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(E3), typeof(S2))]
            private class S1 : State
            {
            }

            private void EntryS1()
            {
                this.SendEvent(this.RealMachine, new E4(), options: new SendOptions(assert: 1));
                this.SendEvent(this.RealMachine, new E2(), options: new SendOptions(assert: 1));
            }

            private class S2 : State
            {
            }
        }

        /// <summary>
        /// Tests basic semantics of actions and goto transitions.
        /// </summary>
        [Fact(Timeout=5000)]
        public void TestEventHandler1()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M1A));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS),
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        private class M2A : StateMachine
        {
            private ActorId GhostMachine;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E4), typeof(S2))]
            [OnEventPushState(typeof(UnitEvent), typeof(S1))]
            [OnEventDoAction(typeof(E2), nameof(Action1))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.GhostMachine = this.CreateActor(typeof(M2B));
                this.SendEvent(this.GhostMachine, new SetupEvent(this.Id));
                this.RaiseEvent(new UnitEvent());
            }

            [OnEntry(nameof(EntryS1))]
            private class S1 : State
            {
            }

            private void EntryS1()
            {
                this.SendEvent(this.GhostMachine, new E1(), options: new SendOptions(assert: 1));

                // We wait in this state until E2 comes from M2B,
                // then handle E2 using the inherited handler Action1
                // installed by Init.
                // Then wait until E4 comes from M2B, and since
                // there's no handler for E4 in this pushed state,
                // this state is popped, and E4 goto handler from Init
                // is invoked.
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : State
            {
            }

            private void EntryS2()
            {
                // This assert is reachable.
                this.Assert(false);
            }

            private void Action1()
            {
                this.SendEvent(this.GhostMachine, new E3(), options: new SendOptions(assert: 1));
            }
        }

        private class M2B : StateMachine
        {
            private ActorId RealMachine;

            [Start]
            [OnEventDoAction(typeof(SetupEvent), nameof(SetupEvent))]
            [OnEventGotoState(typeof(E1), typeof(S1))]
            private class Init : State
            {
            }

            private void SetupEvent()
            {
                this.RealMachine = (this.ReceivedEvent as SetupEvent).Id;
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(E3), typeof(S2))]
            private class S1 : State
            {
            }

            private void EntryS1()
            {
                this.SendEvent(this.RealMachine, new E2(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : State
            {
            }

            private void EntryS2()
            {
                this.SendEvent(this.RealMachine, new E4(), options: new SendOptions(assert: 1));
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventHandler2()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M2A));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS),
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        private class M3A : StateMachine
        {
            private ActorId GhostMachine;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(E4), typeof(S2))]
            [OnEventPushState(typeof(UnitEvent), typeof(S1))]
            [OnEventDoAction(typeof(E5), nameof(Action1))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.GhostMachine = this.CreateActor(typeof(M3B));
                this.SendEvent(this.GhostMachine, new SetupEvent(this.Id));
                this.RaiseEvent(new UnitEvent());
            }

            [OnEntry(nameof(EntryS1))]
            private class S1 : State
            {
            }

            private void EntryS1()
            {
                this.SendEvent(this.GhostMachine, new E1(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : State
            {
            }

            private void EntryS2()
            {
                // This assert is reachable.
                this.Assert(false);
            }

            private void Action1()
            {
                this.SendEvent(this.GhostMachine, new E3(), options: new SendOptions(assert: 1));
            }
        }

        private class M3B : StateMachine
        {
            private ActorId RealMachine;

            [Start]
            [OnEventDoAction(typeof(SetupEvent), nameof(SetupEvent))]
            [OnEventGotoState(typeof(E1), typeof(S1))]
            private class Init : State
            {
            }

            private void SetupEvent()
            {
                this.RealMachine = (this.ReceivedEvent as SetupEvent).Id;
            }

            [OnEntry(nameof(EntryS1))]
            [OnEventGotoState(typeof(E3), typeof(S2))]
            private class S1 : State
            {
            }

            private void EntryS1()
            {
                this.SendEvent(this.RealMachine, new E5(100), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(EntryS2))]
            private class S2 : State
            {
            }

            private void EntryS2()
            {
                this.SendEvent(this.RealMachine, new E4(), options: new SendOptions(assert: 1));
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventHandler3()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M3A));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS),
            expectedError: "Detected an assertion failure.",
            replay: true);
        }
    }
}
