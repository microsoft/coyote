// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Actors
{
    public class CreateActorIdFromNameTests : BaseSystematicTest
    {
        public CreateActorIdFromNameTests(ITestOutputHelper output)
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
        public void TestCreateActorIdFromName1()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<LivenessMonitor>();
                var m1 = r.CreateActor(typeof(M));
                var m2 = r.CreateActorIdFromName(typeof(M), "M");
                r.Assert(!m1.Equals(m2));
                r.CreateActor(m2, typeof(M));
            });
        }

        [Fact(Timeout = 5000)]
        public void TestCreateActorIdFromName2()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<LivenessMonitor>();
                var m1 = r.CreateActorIdFromName(typeof(M), "M1");
                var m2 = r.CreateActorIdFromName(typeof(M), "M2");
                r.Assert(!m1.Equals(m2));
                r.CreateActor(m1, typeof(M));
                r.CreateActor(m2, typeof(M));
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
        public void TestCreateActorIdFromName4()
        {
            this.TestWithError(r =>
            {
                var m3 = r.CreateActorIdFromName(typeof(M3), "M3");
                r.CreateActor(m3, typeof(M2));
            },
            expectedError: "Cannot bind actor id '' of type 'M3' to an actor of type 'M2'.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestCreateActorIdFromName5()
        {
            this.TestWithError(r =>
            {
                var m1 = r.CreateActorIdFromName(typeof(M2), "M2");
                r.CreateActor(m1, typeof(M2));
                r.CreateActor(m1, typeof(M2));
            },
            expectedError: "Actor id '' is used by an existing or previously halted actor.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestCreateActorIdFromName6()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateActorIdFromName(typeof(M2), "M2");
                r.SendEvent(m, UnitEvent.Instance);
            },
            expectedError: "Cannot send event 'Actors.UnitEvent' to actor id '' that is not bound to an actor instance.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestCreateActorIdFromName7()
        {
            this.TestWithError(r =>
            {
                var m = r.CreateActorIdFromName(typeof(M2), "M2");
                r.CreateActor(m, typeof(M2));

                // Make sure that the state machine halts.
                for (int i = 0; i < 100; i++)
                {
                    r.SendEvent(m, HaltEvent.Instance);
                }

                // Trying to bring up a halted state machine.
                r.CreateActor(m, typeof(M2));
            },
            configuration: GetConfiguration(),
            expectedError: "Actor id '' is used by an existing or previously halted actor.",
            replay: true);
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
            [OnEventDoAction(typeof(UnitEvent), nameof(Process))]
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

            private void InitOnEntry(Event e)
            {
                var id = (e as E2).Mid;
                this.SendEvent(id, UnitEvent.Instance);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestCreateActorIdFromName8()
        {
            var configuration = Configuration.Create();
            configuration.TestingIterations = 100;

            this.TestWithError(r =>
            {
                var m = r.CreateActorIdFromName(typeof(M4), "M4");
                r.CreateActor(typeof(M5), new E2(m));
                r.CreateActor(m, typeof(M4));
            },
            configuration,
            expectedError: "Cannot send event 'Actors.UnitEvent' to actor id '' that is not bound to an actor instance.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
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
                this.CreateActor(m, typeof(M4), "friendly");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestCreateActorIdFromName10()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M6));
                r.CreateActor(typeof(M6));
            },
            expectedError: "Actor id '' is used by an existing or previously halted actor.",
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
                await this.Runtime.CreateActorAndExecuteAsync(typeof(M6));
                var m = this.Runtime.CreateActorIdFromName(typeof(M4), "M4");
                this.Runtime.SendEvent(m, UnitEvent.Instance);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestCreateActorIdFromName11()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<WaitUntilDone>();
                r.CreateActor(typeof(M7));
            });
        }
    }
}
