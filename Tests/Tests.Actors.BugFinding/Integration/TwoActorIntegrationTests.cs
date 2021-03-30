// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.BugFinding.Tests
{
    public class TwoActorIntegrationTests : BaseActorBugFindingTest
    {
        public TwoActorIntegrationTests(ITestOutputHelper output)
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

        private class M1a : StateMachine
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
                this.TargetId = this.CreateActor(typeof(M1b));
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

        [OnEventDoAction(typeof(E3), nameof(EntryAction))]
        private class M1b : Actor
        {
            private void EntryAction(Event e)
            {
                if (e.GetType() == typeof(E3))
                {
                    this.Action2(e);
                }
            }

            private void Action2(Event e)
            {
                this.Assert((e as E3).Value is false, "Reached test assertion.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestTwoActorIntegration1()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M1a));
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M2a : StateMachine
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
                this.TargetId = this.CreateActor(typeof(M2b));
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
                if (this.Count is 1)
                {
                    this.SendEvent(this.TargetId, new E4(this.Id), options: new SendOptions(assert: 1));
                }

                if (this.Count is 2)
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

        private class M2b : StateMachine
        {
            [Start]
            [OnEventGotoState(typeof(E4), typeof(Active))]
            [OnEventDoAction(typeof(IgnoredE), nameof(Action1))]
            private class Waiting : State
            {
            }

            private void Action1()
            {
                this.Assert(false, "Reached test assertion.");
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(Waiting))]
            private class Active : State
            {
            }

            private void ActiveOnEntry(Event e)
            {
                this.SendEvent((e as E4).Id, new E1(), options: new SendOptions(assert: 1));
                this.RaiseEvent(new SuccessE());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestTwoActorIntegration2()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M2a));
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M3a : StateMachine
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
                this.TargetId = this.CreateActor(typeof(M3b));
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
                if (this.Count is 1)
                {
                    this.SendEvent(this.TargetId, new E4(this.Id), options: new SendOptions(assert: 1));
                }

                if (this.Count is 2)
                {
                    this.SendEvent(this.TargetId, HaltEvent.Instance);
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

        private class M3b : StateMachine
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

            private void ActiveOnEntry(Event e)
            {
                this.SendEvent((e as E4).Id, new E1(), options: new SendOptions(assert: 1));
                this.RaiseEvent(new SuccessE());
            }

            [OnEventDoAction(typeof(IgnoredE), nameof(Action1))]
            [IgnoreEvents(typeof(E4))]
            private class Inactive : State
            {
            }

            private void Action1()
            {
                this.Assert(false, "Reached test assertion.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestTwoActorIntegration3()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M3a));
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M4a : StateMachine
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
                this.TargetId = this.CreateActor(typeof(M4b));
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
        }

        private class M4b : StateMachine
        {
            private int Count = 0;

            [Start]
            [OnEventGotoState(typeof(E4), typeof(Active))]
            private class Waiting : State
            {
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventGotoState(typeof(SuccessE), typeof(Waiting))]
            private class Active : State
            {
            }

            private void ActiveOnEntry(Event e)
            {
                this.Count++;
                if (this.Count is 1)
                {
                    this.SendEvent((e as E4).Id, new E1(), options: new SendOptions(assert: 1));
                }
                else if (this.Count is 2)
                {
                    this.SendEvent((e as E4).Id, new E1(), options: new SendOptions(assert: 1));
                    this.RaiseHaltEvent();
                    return;
                }

                this.RaiseEvent(new SuccessE());
            }

            protected override Task OnHaltAsync(Event e)
            {
                this.Assert(false, "Reached test assertion.");
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestTwoActorIntegration4()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M4a));
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M5a : StateMachine
        {
            private ActorId TargetId;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnExit(nameof(InitOnExit))]
            [OnEventGotoState(typeof(E1), typeof(Active))]
            private class Init : State
            {
            }

            private void InitOnEntry() => this.RaiseEvent(new E1());

            private void InitOnExit()
            {
                this.TargetId = this.CreateActor(typeof(M5b));
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

        [OnEventDoAction(typeof(E1), nameof(HandleE1))]
        [OnEventDoAction(typeof(E2), nameof(HandleE2))]
        private class M5b : Actor
        {
#pragma warning disable CA1822 // Mark members as static
            private void HandleE1()
#pragma warning restore CA1822 // Mark members as static
            {
            }

            private void HandleE2()
            {
                this.Assert(false, "Reached test assertion.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestTwoActorIntegration5()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M5a));
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            expectedError: "Reached test assertion.",
            replay: true);
        }
    }
}
