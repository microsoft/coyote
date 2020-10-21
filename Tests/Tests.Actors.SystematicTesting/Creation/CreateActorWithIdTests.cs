// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common.Events;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.SystematicTesting.Tests
{
    public class CreateActorWithIdTests : BaseActorSystematicTest
    {
        public CreateActorWithIdTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class LivenessMonitor : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(UnitEvent), typeof(S2))]
            private class S1 : State
            {
            }

            [Hot]
            [OnEventGotoState(typeof(UnitEvent), typeof(S3))]
            private class S2 : State
            {
            }

            [Cold]
            private class S3 : State
            {
            }
        }

        private class M : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Monitor(typeof(LivenessMonitor), UnitEvent.Instance);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestCreateActorWithId1()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<LivenessMonitor>();
                var m = r.CreateActor(typeof(M));
                var mprime = r.CreateActorId(typeof(M));
                r.Assert(m != mprime);
                r.CreateActor(mprime, typeof(M));
            });
        }

        private class Data
        {
            public int X;

            public Data()
            {
                this.X = 0;
            }
        }

        private class E1 : Event
        {
            public Data Data;

            public E1(Data data)
            {
                this.Data = data;
            }
        }

        private class TerminateReq : Event
        {
            public ActorId Sender;

            public TerminateReq(ActorId sender)
            {
                this.Sender = sender;
            }
        }

        private class TerminateResp : Event
        {
        }

        private class M1 : StateMachine
        {
            private Data Data;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(UnitEvent), nameof(Process))]
            [OnEventDoAction(typeof(TerminateReq), nameof(Terminate))]
            private class S : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.Data = (e as E1).Data;
                this.Process();
            }

            private void Process()
            {
                if (this.Data.X != 10)
                {
                    this.Data.X++;
                    this.SendEvent(this.Id, UnitEvent.Instance);
                }
                else
                {
                    this.Monitor(typeof(LivenessMonitor), UnitEvent.Instance);
                    this.Monitor(typeof(LivenessMonitor), UnitEvent.Instance);
                }
            }

            private void Terminate(Event e)
            {
                this.SendEvent((e as TerminateReq).Sender, new TerminateResp());
                this.RaiseHaltEvent();
            }
        }

        private class Harness : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class S : State
            {
            }

            private void InitOnEntry()
            {
                var data = new Data();
                var m1 = this.CreateActor(typeof(M1), new E1(data));
                var m2 = this.Id.Runtime.CreateActorId(typeof(M1));
                this.SendEvent(m1, new TerminateReq(this.Id));
                this.ReceiveEventAsync(typeof(TerminateResp));
                this.Id.Runtime.CreateActor(m2, typeof(M1), new E1(data));
            }
        }

        [Fact(Timeout = 5000)]
        public void TestCreateActorWithId2()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<LivenessMonitor>();
                var m = r.CreateActor(typeof(Harness));
            });
        }

        private class M2 : StateMachine
        {
            [Start]
            private class S : State
            {
            }
        }

        private class M3 : StateMachine
        {
            [Start]
            private class S : State
            {
            }
        }

        [Fact(Timeout = 5000)]
        public void TestCreateActorWithId3()
        {
            this.TestWithError(r =>
            {
                ActorId id = r.CreateActorId(typeof(M3));
                r.CreateActor(id, typeof(M2));
            },
            expectedError: "Cannot bind actor id '' of type 'M3' to an actor of type 'M2'.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestCreateActorWithId4()
        {
            this.TestWithError(r =>
            {
                ActorId id = r.CreateActor(typeof(M2));
                r.CreateActor(id, typeof(M2));
            },
            expectedError: "Actor id '' is used by an existing or previously halted actor.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestCreateActorWithId5()
        {
            this.TestWithError(r =>
            {
                ActorId id = r.CreateActorId(typeof(M2));
                r.SendEvent(id, UnitEvent.Instance);
            },
            expectedError: "Cannot send event 'Events.UnitEvent' to actor id '' that is not bound to an actor instance.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestCreateActorWithId6()
        {
            this.TestWithError(r =>
            {
                bool isEventDropped = false;
                r.OnEventDropped += (Event e, ActorId target) =>
                {
                    isEventDropped = true;
                };

                ActorId id = r.CreateActor(typeof(M2));
                while (!isEventDropped)
                {
                    // Make sure the actor halts before trying to reuse its id.
                    r.SendEvent(id, HaltEvent.Instance);
                }

                // Trying to bring up a halted actor.
                r.CreateActor(id, typeof(M2));
            },
            configuration: GetConfiguration(),
            expectedError: "Actor id '' is used by an existing or previously halted actor.");
        }

        private class E2 : Event
        {
            public ActorId Mid;

            public E2(ActorId id)
            {
                this.Mid = id;
            }
        }

        private class M4 : StateMachine
        {
            [Start]
            [IgnoreEvents(typeof(UnitEvent))]
            [OnEntry(nameof(InitOnEntry))]
            private class S : State
            {
            }

            private void InitOnEntry()
            {
            }
        }

        private class M5 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class S : State
            {
            }

            private void InitOnEntry(Event e)
            {
                ActorId id = (e as E2).Mid;
                this.SendEvent(id, UnitEvent.Instance);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestCreateActorWithId7()
        {
            this.TestWithError(r =>
            {
                ActorId id = r.CreateActorId(typeof(M4));
                r.CreateActor(typeof(M5), new E2(id));
                r.CreateActor(id, typeof(M4));
            },
            configuration: Configuration.Create().WithTestingIterations(100),
            expectedError: "Cannot send event 'Events.UnitEvent' to actor id '' that is not bound to an actor instance.",
            replay: true);
        }
    }
}
