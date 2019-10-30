// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Machines;
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
            private class S1 : MonitorState
            {
            }

            [Hot]
            [OnEventGotoState(typeof(E), typeof(S3))]
            private class S2 : MonitorState
            {
            }

            [Cold]
            private class S3 : MonitorState
            {
            }
        }

        private class M : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
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
                var m = r.CreateMachine(typeof(M));
                var mprime = r.CreateActorId(typeof(M));
                r.Assert(m != mprime);
                r.CreateMachine(mprime, typeof(M));
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
            private class S : MachineState
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
                    this.Send(this.Id, new E());
                }
                else
                {
                    this.Monitor(typeof(LivenessMonitor), new E());
                    this.Monitor(typeof(LivenessMonitor), new E());
                }
            }

            private void Terminate()
            {
                this.Send((this.ReceivedEvent as TerminateReq).Sender, new TerminateResp());
                this.Raise(new Halt());
            }
        }

        private class Harness : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class S : MachineState
            {
            }

            private void InitOnEntry()
            {
                var data = new Data();
                var m1 = this.CreateMachine(typeof(M1), new E1(data));
                var m2 = this.Id.Runtime.CreateActorId(typeof(M1));
                this.Send(m1, new TerminateReq(this.Id));
                this.Receive(typeof(TerminateResp));
                this.Id.Runtime.CreateMachine(m2, typeof(M1), new E1(data));
            }
        }

        [Fact(Timeout=5000)]
        public void TestCreateMachineWithId2()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(LivenessMonitor));
                var m = r.CreateMachine(typeof(Harness));
            });
        }

        private class M2 : StateMachine
        {
            [Start]
            private class S : MachineState
            {
            }
        }

        private class M3 : StateMachine
        {
            [Start]
            private class S : MachineState
            {
            }
        }

        [Fact(Timeout=5000)]
        public void TestCreateMachineWithId3()
        {
            this.TestWithError(r =>
            {
                ActorId id = r.CreateActorId(typeof(M3));
                r.CreateMachine(id, typeof(M2));
            },
            expectedError: "Cannot bind actor id '' of type 'M3' to a machine of type 'M2'.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestCreateMachineWithId4()
        {
            this.TestWithError(r =>
            {
                ActorId id = r.CreateMachine(typeof(M2));
                r.CreateMachine(id, typeof(M2));
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

                ActorId id = r.CreateMachine(typeof(M2));
                while (!isEventDropped)
                {
                    // Make sure the machine halts before trying to reuse its id.
                    r.SendEvent(id, new Halt());
                }

                // Trying to bring up a halted machine.
                r.CreateMachine(id, typeof(M2));
            },
            configuration: GetConfiguration(),
            expectedErrors: new string[]
            {
                // Note: because RunMachineEventHandler is async, the halted machine
                // may or may not be removed by the time we call CreateMachine.
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
            private class S : MachineState
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
            private class S : MachineState
            {
            }

            private void InitOnEntry()
            {
                ActorId id = (this.ReceivedEvent as E2).Mid;
                this.Send(id, new E());
            }
        }

        [Fact(Timeout=5000)]
        public void TestCreateMachineWithId7()
        {
            this.TestWithError(r =>
            {
                ActorId id = r.CreateActorId(typeof(M4));
                r.CreateMachine(typeof(M5), new E2(id));
                r.CreateMachine(id, typeof(M4));
            },
            configuration: Configuration.Create().WithNumberOfIterations(100),
            expectedError: "Cannot send event 'E' to actor id '' that was never previously bound to a machine of type 'M4'",
            replay: true);
        }
    }
}
