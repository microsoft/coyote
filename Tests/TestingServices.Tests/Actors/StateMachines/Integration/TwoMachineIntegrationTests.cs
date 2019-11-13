// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime.Exploration;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors.StateMachines
{
    public class TwoMachineIntegrationTests : BaseTest
    {
        public TwoMachineIntegrationTests(ITestOutputHelper output)
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
            public bool Value;

            public E3(bool value)
            {
                this.Value = value;
            }
        }

        private class E4 : Event
        {
            public ActorId Id;

            public E4(ActorId id)
            {
                this.Id = id;
            }
        }

        private class SuccessE : Event
        {
        }

        private class IgnoredE : Event
        {
        }

        private class M1 : StateMachine
        {
            private bool Test = false;
            private ActorId TargetId;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(DefaultEvent), typeof(S1))]
            [OnEventDoAction(typeof(E1), nameof(Action1))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.TargetId = this.CreateActor(typeof(M2));
                this.RaiseEvent(new E1());
            }

            private void InitOnExit()
            {
                this.SendEvent(this.TargetId, new E3(this.Test), options: new SendOptions(assert: 1));
            }

            private class S1 : State
            {
            }

            private void Action1()
            {
                this.Test = true;
            }
        }

        private class M2 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E3), nameof(EntryAction))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
            }

            private void EntryAction()
            {
                if (this.ReceivedEvent.GetType() == typeof(E3))
                {
                    this.Action2();
                }
            }

            private void Action2()
            {
                this.Assert((this.ReceivedEvent as E3).Value == false);
            }
        }

        private class M3 : StateMachine
        {
            private ActorId TargetId;
            private int Count;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(Active))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.TargetId = this.CreateActor(typeof(M4));
                this.RaiseEvent(new SuccessE());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(WaitEvent))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.Count += 1;
                if (this.Count == 1)
                {
                    this.SendEvent(this.TargetId, new E4(this.Id), options: new SendOptions(assert: 1));
                }

                if (this.Count == 2)
                {
                    this.SendEvent(this.TargetId, new IgnoredE());
                }

                this.RaiseEvent(new SuccessE());
            }

            [OnEventGotoState(typeof(E1), typeof(Active))]
            private class WaitEvent : State
            {
            }

            private class Done : State
            {
            }
        }

        private class M4 : StateMachine
        {
            [Start]
            [OnEventGotoState(typeof(E4), typeof(Active))]
            [OnEventDoAction(typeof(IgnoredE), nameof(Action1))]
            private class Waiting : State
            {
            }

            private void Action1()
            {
                this.Assert(false);
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(Waiting))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.SendEvent((this.ReceivedEvent as E4).Id, new E1(), options: new SendOptions(assert: 1));
                this.RaiseEvent(new SuccessE());
            }
        }

        private class M5 : StateMachine
        {
            private ActorId TargetId;
            private int Count;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(Active))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.TargetId = this.CreateActor(typeof(M6));
                this.RaiseEvent(new SuccessE());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(WaitEvent))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.Count += 1;
                if (this.Count == 1)
                {
                    this.SendEvent(this.TargetId, new E4(this.Id), options: new SendOptions(assert: 1));
                }

                if (this.Count == 2)
                {
                    this.SendEvent(this.TargetId, new HaltEvent());
                    this.SendEvent(this.TargetId, new IgnoredE());
                }

                this.RaiseEvent(new SuccessE());
            }

            [OnEventGotoState(typeof(E1), typeof(Active))]
            private class WaitEvent : State
            {
            }

            private class Done : State
            {
            }
        }

        private class M6 : StateMachine
        {
            [Start]
            [OnEventGotoState(typeof(E4), typeof(Active))]
            [OnEventGotoState(typeof(HaltEvent), typeof(Inactive))]
            private class Waiting : State
            {
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(Waiting))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.SendEvent((this.ReceivedEvent as E4).Id, new E1(), options: new SendOptions(assert: 1));
                this.RaiseEvent(new SuccessE());
            }

            [OnEventDoAction(typeof(IgnoredE), nameof(Action1))]
            [IgnoreEvents(typeof(E4))]
            private class Inactive : State
            {
            }

            private void Action1()
            {
                this.Assert(false);
            }
        }

        private class M7 : StateMachine
        {
            private ActorId TargetId;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(Active))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.TargetId = this.CreateActor(typeof(M8));
                this.RaiseEvent(new SuccessE());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(Waiting))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.SendEvent(this.TargetId, new E4(this.Id), options: new SendOptions(assert: 1));
                this.RaiseEvent(new SuccessE());
            }

            [OnEventGotoState(typeof(E1), typeof(Active))]
            private class Waiting : State
            {
            }

            private class Done : State
            {
            }
        }

        private class M8 : StateMachine
        {
            private int Count2 = 0;

            [Start]
            [OnEntry(nameof(EntryWaitPing))]
            [OnEventGotoState(typeof(E4), typeof(Active))]
            private class Waiting : State
            {
            }

            private void EntryWaitPing()
            {
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(Waiting))]
            [OnEventDoAction(typeof(HaltEvent), nameof(Action1))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.Count2 += 1;

                if (this.Count2 == 1)
                {
                    this.SendEvent((this.ReceivedEvent as E4).Id, new E1(), options: new SendOptions(assert: 1));
                }

                if (this.Count2 == 2)
                {
                    this.SendEvent((this.ReceivedEvent as E4).Id, new E1(), options: new SendOptions(assert: 1));
                    this.RaiseEvent(new HaltEvent());
                    return;
                }

                this.RaiseEvent(new SuccessE());
            }

            private void Action1()
            {
                this.Assert(false);
            }
        }

        private class M9 : StateMachine
        {
            private ActorId TargetId;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Active))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.RaiseEvent(new E1());
            }

            private void InitOnExit()
            {
                this.TargetId = this.CreateActor(typeof(M10));
                this.SendEvent(this.TargetId, new E1(), options: new SendOptions(assert: 1));
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.SendEvent(this.TargetId, new E2(), options: new SendOptions(assert: 1));
            }
        }

        private class M10 : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E1), nameof(HandleE1))]
            [OnEventDoAction(typeof(E2), nameof(HandleE2))]
            private class Init : State
            {
            }

            private void HandleE1()
            {
            }

            private void HandleE2()
            {
                this.Assert(false);
            }
        }

        [Fact(Timeout=5000)]
        public void TestTwoMachineIntegration1()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M1));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS),
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestTwoMachineIntegration2()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M3));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS),
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestTwoMachineIntegration3()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M5));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS),
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestTwoMachineIntegration4()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M7));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS),
            expectedError: "Detected an assertion failure.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestTwoMachineIntegration5()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M9));
            },
            expectedError: "Detected an assertion failure.",
            replay: true);
        }
    }
}
