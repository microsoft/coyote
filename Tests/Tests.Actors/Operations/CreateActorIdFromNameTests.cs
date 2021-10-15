// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common.Events;
using Xunit;
using Xunit.Abstractions;
using SystemTask = System.Threading.Tasks.Task;

namespace Microsoft.Coyote.Actors.Tests
{
    public class CreateActorIdFromNameTests : BaseActorTest
    {
        public CreateActorIdFromNameTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SetupEvent : Event
        {
            internal TaskCompletionSource<bool> Completed = new TaskCompletionSource<bool>();
            internal int Count;

            public SetupEvent(int count = 1)
            {
                this.Count = count;
            }
        }

        private class CompletedEvent : Event
        {
        }

        private class TestMonitor : Monitor
        {
            private SetupEvent Setup;

            [Start]
            [OnEventDoAction(typeof(SetupEvent), nameof(OnSetup))]
            [OnEventDoAction(typeof(CompletedEvent), nameof(OnCompleted))]
            private class S1 : State
            {
            }

            private void OnSetup(Event e)
            {
                this.Setup = (SetupEvent)e;
            }

            private void OnCompleted()
            {
                this.Setup.Count--;
                if (this.Setup.Count is 0)
                {
                    this.Setup.Completed.SetResult(true);
                }
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
                this.Monitor<TestMonitor>(new CompletedEvent());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestCreateActorIdFromName1()
        {
            this.Test(async r =>
            {
                var setup = new SetupEvent(2);
                r.RegisterMonitor<TestMonitor>();
                r.Monitor<TestMonitor>(setup);
                var m1 = r.CreateActor(typeof(M));
                var m2 = r.CreateActorIdFromName(typeof(M), "M");
                r.Assert(!m1.Equals(m2));
                r.CreateActor(m2, typeof(M));
                await setup.Completed.Task;
            },
            Configuration.Create().WithProductionMonitorEnabled());
        }

        [Fact(Timeout = 5000)]
        public void TestCreateActorIdFromName2()
        {
            this.Test(async r =>
            {
                var setup = new SetupEvent(2);
                r.RegisterMonitor<TestMonitor>();
                r.Monitor<TestMonitor>(setup);
                var m1 = r.CreateActorIdFromName(typeof(M), "M1");
                var m2 = r.CreateActorIdFromName(typeof(M), "M2");
                r.Assert(!m1.Equals(m2));
                r.CreateActor(m1, typeof(M));
                r.CreateActor(m2, typeof(M));
                await setup.Completed.Task;
            },
            Configuration.Create().WithProductionMonitorEnabled());
        }

        private class M2 : StateMachine
        {
            [Start]
            private class S : State
            {
            }

            protected override SystemTask OnHaltAsync(Event e)
            {
                this.Monitor<TestMonitor>(new CompletedEvent());
                return base.OnHaltAsync(e);
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
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                // Production runtime just drops the event, no errors are raised.
                return;
            }

            this.TestWithError(r =>
            {
                var m = r.CreateActorIdFromName(typeof(M2), "M2");
                r.SendEvent(m, UnitEvent.Instance);
            },
            expectedError: "Cannot send event 'Events.UnitEvent' to actor id '' that is not bound to an actor instance.",
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
                this.Monitor<TestMonitor>(new CompletedEvent());
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
        public void TestCreateActorIdFromName6()
        {
            if (this.SchedulingPolicy is SchedulingPolicy.None)
            {
                // Production runtime just drops the event, no errors are raised.
                return;
            }

            var configuration = Configuration.Create();
            configuration.TestingIterations = 100;

            this.TestWithError(r =>
            {
                var m = r.CreateActorIdFromName(typeof(M4), "M4");
                r.CreateActor(typeof(M5), new E2(m));
            },
            configuration,
            expectedError: "Cannot send event 'Events.UnitEvent' to actor id '' that is not bound to an actor instance.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestCreateActorIdFromName7()
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
                var m = this.Context.CreateActorIdFromName(typeof(M4), "M4");
                this.CreateActor(m, typeof(M4), "friendly");
            }
        }

        private class M7 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async SystemTask InitOnEntry()
            {
                await this.Context.CreateActorAndExecuteAsync(typeof(M6));
                var m = this.Context.CreateActorIdFromName(typeof(M4), "M4");
                this.Context.SendEvent(m, UnitEvent.Instance);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestCreateActorIdFromName8()
        {
            this.Test(async r =>
            {
                var setup = new SetupEvent();
                r.RegisterMonitor<TestMonitor>();
                r.Monitor<TestMonitor>(setup);
                r.CreateActor(typeof(M7));
                await setup.Completed.Task;
            }, Configuration.Create().WithProductionMonitorEnabled());
        }
    }
}
