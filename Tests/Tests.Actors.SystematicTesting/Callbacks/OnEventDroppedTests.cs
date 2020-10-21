// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Actors.SystematicTesting.Tests
{
    public class OnEventDroppedTests : BaseSystematicActorTest
    {
        public OnEventDroppedTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class E : Event
        {
            public ActorId Id;

            public E()
            {
            }

            public E(ActorId id)
            {
                this.Id = id;
            }
        }

        private class A1 : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.SendEvent(this.Id, HaltEvent.Instance);
                this.SendEvent(this.Id, new E());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSendEventDroppedAfterHaltInActor()
        {
            this.TestWithError(r =>
            {
                r.OnEventDropped += (e, target) =>
                {
                    r.Assert(false, "Reached test assertion.");
                };

                var m = r.CreateActor(typeof(A1));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M1 : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.SendEvent(this.Id, HaltEvent.Instance);
                this.SendEvent(this.Id, new E());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestSendEventDroppedAfterHaltInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.OnEventDropped += (e, target) =>
                {
                    r.Assert(false, "Reached test assertion.");
                };

                var m = r.CreateActor(typeof(M1));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class A2 : Actor
        {
            protected override Task OnHaltAsync(Event e)
            {
                this.SendEvent(this.Id, new E());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestRuntimeEventDroppedAfterHaltInActor()
        {
            this.TestWithError(r =>
            {
                r.OnEventDropped += (e, target) =>
                {
                    r.Assert(false, "Reached test assertion.");
                };

                var m = r.CreateActor(typeof(A2));
                r.SendEvent(m, HaltEvent.Instance);
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventDroppedParametersInActor()
        {
            this.Test(r =>
            {
                var m = r.CreateActor(typeof(A2));

                r.OnEventDropped += (e, target) =>
                {
                    r.Assert(e is E);
                    r.Assert(target == m);
                };

                r.SendEvent(m, HaltEvent.Instance);
            });
        }

        private class M2 : StateMachine
        {
            [Start]
            private class Init : State
            {
            }

            protected override Task OnHaltAsync(Event e)
            {
                this.SendEvent(this.Id, new E());
                return Task.CompletedTask;
            }
        }

        [Fact(Timeout = 5000)]
        public void TestRuntimeEventDroppedAfterHaltInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.OnEventDropped += (e, target) =>
                {
                    r.Assert(false, "Reached test assertion.");
                };

                var m = r.CreateActor(typeof(M2));
                r.SendEvent(m, HaltEvent.Instance);
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestOnEventDroppedParametersInStateMachine()
        {
            this.Test(r =>
            {
                var m = r.CreateActor(typeof(M2));

                r.OnEventDropped += (e, target) =>
                {
                    r.Assert(e is E);
                    r.Assert(target == m);
                };

                r.SendEvent(m, HaltEvent.Instance);
            });
        }

        private class EventProcessed : Event
        {
        }

        private class EventDropped : Event
        {
        }

        private class Monitor3 : Monitor
        {
            [Hot]
            [Start]
            [OnEventGotoState(typeof(EventProcessed), typeof(S2))]
            [OnEventGotoState(typeof(EventDropped), typeof(S2))]
            private class S1 : State
            {
            }

            [Cold]
            private class S2 : State
            {
            }
        }

        private class A3a : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.SendEvent((initialEvent as E).Id, HaltEvent.Instance);
                return Task.CompletedTask;
            }
        }

        private class A3b : Actor
        {
            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.SendEvent((initialEvent as E).Id, new E());
                return Task.CompletedTask;
            }
        }

        [OnEventDoAction(typeof(E), nameof(Processed))]
        private class A3c : Actor
        {
            private void Processed()
            {
                this.Monitor<Monitor3>(new EventProcessed());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventProcessedOrDroppedInActor()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<Monitor3>();
                r.OnEventDropped += (e, target) =>
                {
                    r.Monitor<Monitor3>(new EventDropped());
                };

                var m = r.CreateActor(typeof(A3c));
                r.CreateActor(typeof(A3a), new E(m));
                r.CreateActor(typeof(A3b), new E(m));
            },
            configuration: Configuration.Create().WithTestingIterations(200));
        }

        private class M3a : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.SendEvent((e as E).Id, HaltEvent.Instance);
            }
        }

        private class M3b : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private void InitOnEntry(Event e)
            {
                this.SendEvent((e as E).Id, new E());
            }
        }

        private class M3c : StateMachine
        {
            [Start]
            [OnEventDoAction(typeof(E), nameof(Processed))]
            private class Init : State
            {
            }

            private void Processed()
            {
                this.Monitor<Monitor3>(new EventProcessed());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestEventProcessedOrDroppedInStateMachine()
        {
            this.Test(r =>
            {
                r.RegisterMonitor<Monitor3>();
                r.OnEventDropped += (e, target) =>
                {
                    r.Monitor<Monitor3>(new EventDropped());
                };

                var m = r.CreateActor(typeof(M3c));
                r.CreateActor(typeof(M3a), new E(m));
                r.CreateActor(typeof(M3b), new E(m));
            },
            configuration: Configuration.Create().WithTestingIterations(200));
        }
    }
}
