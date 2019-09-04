// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class CreateMachineIdFromNameTest : BaseTest
    {
        public CreateMachineIdFromNameTest(ITestOutputHelper output)
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
        public void TestCreateMachineIdFromName1()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(LivenessMonitor));
                var m1 = r.CreateMachine(typeof(M));
                var m2 = r.CreateMachineIdFromName(typeof(M), "M");
                r.Assert(!m1.Equals(m2));
                r.CreateMachine(m2, typeof(M));
            });
        }

        [Fact(Timeout=5000)]
        public void TestCreateMachineIdFromName2()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(LivenessMonitor));
                var m1 = r.CreateMachineIdFromName(typeof(M), "M1");
                var m2 = r.CreateMachineIdFromName(typeof(M), "M2");
                r.Assert(!m1.Equals(m2));
                r.CreateMachine(m1, typeof(M));
                r.CreateMachine(m2, typeof(M));
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
        public void TestCreateMachineIdFromName4()
        {
            this.TestWithError(r =>
            {
                var m3 = r.CreateMachineIdFromName(typeof(M3), "M3");
                r.CreateMachine(m3, typeof(M2));
            },
            expectedError: "Cannot bind machine id '' of type 'M3' to a machine of type 'M2'.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestCreateMachineIdFromName5()
        {
            this.TestWithError(r =>
            {
                var m1 = r.CreateMachineIdFromName(typeof(M2), "M2");
                r.CreateMachine(m1, typeof(M2));
                r.CreateMachine(m1, typeof(M2));
            },
            expectedError: "Machine id '' is used by an existing machine.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestCreateMachineIdFromName6()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateMachineIdFromName(typeof(M2), "M2");
                r.SendEvent(m, new E());
            },
            expectedError: "Cannot send event 'E' to machine id '' that was never previously bound to a machine of type 'M2'",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestCreateMachineIdFromName7()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateMachineIdFromName(typeof(M2), "M2");
                r.CreateMachine(m, typeof(M2));

                // Make sure that the machine halts.
                for (int i = 0; i < 100; i++)
                {
                    r.SendEvent(m, new Halt());
                }

                // Trying to bring up a halted machine.
                r.CreateMachine(m, typeof(M2));
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
            [OnEventDoAction(typeof(E), nameof(Process))]
            private class S : MachineState
            {
            }

            private void Process()
            {
                this.Monitor<WaitUntilDone>(new Done());
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
                var mid = (this.ReceivedEvent as E2).Mid;
                this.Send(mid, new E());
            }
        }

        [Fact(Timeout=5000)]
        public void TestCreateMachineIdFromName8()
        {
            var configuration = Configuration.Create();
            configuration.SchedulingIterations = 100;

            this.TestWithError(r =>
            {
                var m = r.CreateMachineIdFromName(typeof(M4), "M4");
                r.CreateMachine(typeof(M5), new E2(m));
                r.CreateMachine(m, typeof(M4));
            },
            configuration,
            "Cannot send event 'E' to machine id '' that was never previously bound to a machine of type 'M4'",
            false);
        }

        [Fact(Timeout=5000)]
        public void TestCreateMachineIdFromName9()
        {
            this.Test(r =>
            {
                var m1 = r.CreateMachineIdFromName(typeof(M4), "M4");
                var m2 = r.CreateMachineIdFromName(typeof(M4), "M4");
                r.Assert(m1.Equals(m2));
            });
        }

        private class M6 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private void InitOnEntry()
            {
                var m = this.Runtime.CreateMachineIdFromName(typeof(M4), "M4");
                this.CreateMachine(m, typeof(M4), "friendly");
            }
        }

        [Fact(Timeout=5000)]
        public void TestCreateMachineIdFromName10()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M6));
                r.CreateMachine(typeof(M6));
            },
            expectedError: "Machine id '' is used by an existing machine.",
            replay: true);
        }

        private class Done : Event
        {
        }

        private class WaitUntilDone : Monitor
        {
            [Start]
            [Hot]
            [OnEventGotoState(typeof(Done), typeof(S2))]
            private class S1 : MonitorState
            {
            }

            [Cold]
            private class S2 : MonitorState
            {
            }
        }

        private class M7 : Machine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : MachineState
            {
            }

            private async Task InitOnEntry()
            {
                await this.Runtime.CreateMachineAndExecute(typeof(M6));
                var m = this.Runtime.CreateMachineIdFromName(typeof(M4), "M4");
                this.Runtime.SendEvent(m, new E());
            }
        }

        [Fact(Timeout=5000)]
        public void TestCreateMachineIdFromName11()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(WaitUntilDone));
                r.CreateMachine(typeof(M7));
            });
        }
    }
}
