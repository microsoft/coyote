// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class CreateActorIdFromNameTest : BaseTest
    {
        public CreateActorIdFromNameTest(ITestOutputHelper output)
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
        public void TestCreateActorIdFromName1()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(LivenessMonitor));
                var m1 = r.CreateMachine(typeof(M));
                var m2 = r.CreateActorIdFromName(typeof(M), "M");
                r.Assert(!m1.Equals(m2));
                r.CreateMachine(m2, typeof(M));
            });
        }

        [Fact(Timeout=5000)]
        public void TestCreateActorIdFromName2()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(LivenessMonitor));
                var m1 = r.CreateActorIdFromName(typeof(M), "M1");
                var m2 = r.CreateActorIdFromName(typeof(M), "M2");
                r.Assert(!m1.Equals(m2));
                r.CreateMachine(m1, typeof(M));
                r.CreateMachine(m2, typeof(M));
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
        public void TestCreateActorIdFromName4()
        {
            this.TestWithError(r =>
            {
                var m3 = r.CreateActorIdFromName(typeof(M3), "M3");
                r.CreateMachine(m3, typeof(M2));
            },
            expectedError: "Cannot bind actor id '' of type 'M3' to a machine of type 'M2'.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestCreateActorIdFromName5()
        {
            this.TestWithError(r =>
            {
                var m1 = r.CreateActorIdFromName(typeof(M2), "M2");
                r.CreateMachine(m1, typeof(M2));
                r.CreateMachine(m1, typeof(M2));
            },
            expectedError: "Actor id '' is used by an existing machine.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestCreateActorIdFromName6()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateActorIdFromName(typeof(M2), "M2");
                r.SendEvent(m, new E());
            },
            expectedError: "Cannot send event 'E' to actor id '' that was never previously bound to a machine of type 'M2'",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestCreateActorIdFromName7()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateActorIdFromName(typeof(M2), "M2");
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
            [OnEventDoAction(typeof(E), nameof(Process))]
            private class S : State
            {
            }

            private void Process()
            {
                this.Monitor<WaitUntilDone>(new Done());
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
                var id = (this.ReceivedEvent as E2).Mid;
                this.Send(id, new E());
            }
        }

        [Fact(Timeout=5000)]
        public void TestCreateActorIdFromName8()
        {
            var configuration = Configuration.Create();
            configuration.SchedulingIterations = 100;

            this.TestWithError(r =>
            {
                var m = r.CreateActorIdFromName(typeof(M4), "M4");
                r.CreateMachine(typeof(M5), new E2(m));
                r.CreateMachine(m, typeof(M4));
            },
            configuration,
            "Cannot send event 'E' to actor id '' that was never previously bound to a machine of type 'M4'",
            false);
        }

        [Fact(Timeout=5000)]
        public void TestCreateActorIdFromName9()
        {
            this.Test(r =>
            {
                var m1 = r.CreateActorIdFromName(typeof(M4), "M4");
                var m2 = r.CreateActorIdFromName(typeof(M4), "M4");
                r.Assert(m1.Equals(m2));
            });
        }

        private class M6 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                var m = this.Runtime.CreateActorIdFromName(typeof(M4), "M4");
                this.CreateMachine(m, typeof(M4), "friendly");
            }
        }

        [Fact(Timeout=5000)]
        public void TestCreateActorIdFromName10()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(M6));
                r.CreateMachine(typeof(M6));
            },
            expectedError: "Actor id '' is used by an existing machine.",
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
            private class S1 : State
            {
            }

            [Cold]
            private class S2 : State
            {
            }
        }

        private class M7 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                await this.Runtime.CreateMachineAndExecuteAsync(typeof(M6));
                var m = this.Runtime.CreateActorIdFromName(typeof(M4), "M4");
                this.Runtime.SendEvent(m, new E());
            }
        }

        [Fact(Timeout=5000)]
        public void TestCreateActorIdFromName11()
        {
            this.Test(r =>
            {
                r.RegisterMonitor(typeof(WaitUntilDone));
                r.CreateMachine(typeof(M7));
            });
        }
    }
}
