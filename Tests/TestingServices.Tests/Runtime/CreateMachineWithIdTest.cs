// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

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

        private class M : Machine
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
                var mprime = r.CreateMachineId(typeof(M));
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
            public MachineId Sender;

            public TerminateReq(MachineId sender)
            {
                this.Sender = sender;
            }
        }

        private class TerminateResp : Event
        {
        }

        private class M1 : Machine
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

        private class Harness : Machine
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
                var m2 = this.Id.Runtime.CreateMachineId(typeof(M1));
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

        private class M2 : Machine
        {
            [Start]
            private class S : MachineState
            {
            }
        }

        private class M3 : Machine
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
                MachineId mid = r.CreateMachineId(typeof(M3));
                r.CreateMachine(mid, typeof(M2));
            },
            expectedError: "Cannot bind machine id '' of type 'M3' to a machine of type 'M2'.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestCreateMachineWithId4()
        {
            this.TestWithError(r =>
            {
                MachineId mid = r.CreateMachine(typeof(M2));
                r.CreateMachine(mid, typeof(M2));
            },
            expectedError: "Machine id '' is used by an existing machine.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestCreateMachineWithId5()
        {
            this.TestWithError(r =>
            {
                MachineId mid = r.CreateMachineId(typeof(M2));
                r.SendEvent(mid, new E());
            },
            expectedError: "Cannot send event 'E' to machine id '' that was never previously bound to a machine of type 'M2'",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestCreateMachineWithId6()
        {
            this.TestWithError(r =>
            {
                bool isEventDropped = false;
                r.OnEventDropped += (Event e, MachineId target) =>
                {
                    isEventDropped = true;
                };

                MachineId mid = r.CreateMachine(typeof(M2));
                while (!isEventDropped)
                {
                    // Make sure the machine halts before trying to reuse its id.
                    r.SendEvent(mid, new Halt());
                }

                // Trying to bring up a halted machine.
                r.CreateMachine(mid, typeof(M2));
            },
            configuration: GetConfiguration(),
            expectedErrors: new string[]
            {
                // Note: because RunMachineEventHandler is async, the halted machine
                // may or may not be removed by the time we call CreateMachine.
                "Machine id '' is used by an existing machine.",
                "Machine id '' of a previously halted machine cannot be reused to create a new machine of type 'M2'"
            });
        }

        private class E2 : Event
        {
            public MachineId Mid;

            public E2(MachineId mid)
            {
                this.Mid = mid;
            }
        }

        private class M4 : Machine
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

        private class M5 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class S : MachineState
            {
            }

            private void InitOnEntry()
            {
                MachineId mid = (this.ReceivedEvent as E2).Mid;
                this.Send(mid, new E());
            }
        }

        [Fact(Timeout=5000)]
        public void TestCreateMachineWithId7()
        {
            this.TestWithError(r =>
            {
                MachineId mid = r.CreateMachineId(typeof(M4));
                r.CreateMachine(typeof(M5), new E2(mid));
                r.CreateMachine(mid, typeof(M4));
            },
            configuration: Configuration.Create().WithNumberOfIterations(100),
            expectedError: "Cannot send event 'E' to machine id '' that was never previously bound to a machine of type 'M4'",
            replay: true);
        }
    }
}
