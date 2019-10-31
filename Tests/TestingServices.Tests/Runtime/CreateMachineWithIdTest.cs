// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class CreateMachineWithIdTest : BaseTest
    {
        public CreateMachineWithIdTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
        }

        private class LivenessMonitor : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(E), typeof(S2))]
            private class S1 : State
            {
            }

            [Hot]
            [OnEventGotoState(typeof(E), typeof(S3))]
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
                this.Monitor(typeof(LivenessMonitor), new E());
            }
        }

        [Fact(Timeout=5000)]
        public void TestCreateMachineWithId1()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(LivenessMonitor));
                var m = r.CreateStateMachine(typeof(M));
                var mprime = r.CreateActorId(typeof(M));
                r.Assert(m != mprime);
                r.CreateStateMachine(mprime, typeof(M));
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
            private Data data;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventDoAction(typeof(E), nameof(Process))]
            [OnEventDoAction(typeof(TerminateReq), nameof(Terminate))]
            private class S : State
            {
            }

            private void InitOnEntry()
            {
                this.data = (this.ReceivedEvent as E1).Data;
                this.Process();
            }

            private void Process()
            {
                if (this.data.X != 10)
                {
                    this.data.X++;
                    this.SendEvent(this.Id, new E());
                }
                else
                {
                    this.Monitor(typeof(LivenessMonitor), new E());
                    this.Monitor(typeof(LivenessMonitor), new E());
                }
            }

            private void Terminate()
            {
                this.SendEvent((this.ReceivedEvent as TerminateReq).Sender, new TerminateResp());
                this.RaiseEvent(new Halt());
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
                var m1 = this.CreateStateMachine(typeof(M1), new E1(data));
                var m2 = this.Id.Runtime.CreateActorId(typeof(M1));
                this.SendEvent(m1, new TerminateReq(this.Id));
                this.ReceiveEventAsync(typeof(TerminateResp));
                this.Id.Runtime.CreateStateMachine(m2, typeof(M1), new E1(data));
            }
        }

        [Fact(Timeout=5000)]
        public void TestCreateMachineWithId2()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(LivenessMonitor));
                var m = r.CreateStateMachine(typeof(Harness));
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

        [Fact(Timeout=5000)]
        public void TestCreateMachineWithId3()
        {
            this.TestWithError(r =>
            {
                ActorId id = r.CreateActorId(typeof(M3));
                r.CreateStateMachine(id, typeof(M2));
            },
            expectedError: "Cannot bind actor id '' of type 'M3' to a machine of type 'M2'.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestCreateMachineWithId4()
        {
            this.TestWithError(r =>
            {
                ActorId id = r.CreateStateMachine(typeof(M2));
                r.CreateStateMachine(id, typeof(M2));
            },
            expectedError: "Actor id '' is used by an existing machine.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestCreateMachineWithId5()
        {
            this.TestWithError(r =>
            {
                ActorId id = r.CreateActorId(typeof(M2));
                r.SendEvent(id, new E());
            },
            expectedError: "Cannot send event 'E' to actor id '' that was never previously bound to a machine of type 'M2'",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestCreateMachineWithId6()
        {
            this.TestWithError(r =>
            {
                bool isEventDropped = false;
                r.OnEventDropped += (Event e, ActorId target) =>
                {
                    isEventDropped = true;
                };

                ActorId id = r.CreateStateMachine(typeof(M2));
                while (!isEventDropped)
                {
                    // Make sure the machine halts before trying to reuse its id.
                    r.SendEvent(id, new Halt());
                }

                // Trying to bring up a halted machine.
                r.CreateStateMachine(id, typeof(M2));
            },
            configuration: GetConfiguration(),
            expectedErrors: new string[]
            {
                // Note: because RunMachineEventHandler is async, the halted machine
                // may or may not be removed by the time we call CreateStateMachine.
                "Actor id '' is used by an existing machine.",
                "Actor id '' of a previously halted machine cannot be reused to create a new machine of type 'M2'"
            });
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
            [IgnoreEvents(typeof(E))]
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

            private void InitOnEntry()
            {
                ActorId id = (this.ReceivedEvent as E2).Mid;
                this.SendEvent(id, new E());
            }
        }

        [Fact(Timeout=5000)]
        public void TestCreateMachineWithId7()
        {
            this.TestWithError(r =>
            {
                ActorId id = r.CreateActorId(typeof(M4));
                r.CreateStateMachine(typeof(M5), new E2(id));
                r.CreateStateMachine(id, typeof(M4));
            },
            configuration: Configuration.Create().WithNumberOfIterations(100),
            expectedError: "Cannot send event 'E' to actor id '' that was never previously bound to a machine of type 'M4'",
            replay: true);
        }
    }
}
