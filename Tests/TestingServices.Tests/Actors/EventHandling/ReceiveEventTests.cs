// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors
{
    public class ReceiveEventTests : BaseTest
    {
        public ReceiveEventTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class A : Actor
        {
            protected override async Task OnInitializeAsync(Event initialEvent)
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
                await this.ReceiveEventAsync(typeof(UnitEvent));
                this.Assert(false, "Reached test assertion.");
            }
        }

        [Fact(Timeout=5000)]
        public void TestReceiveEventInActor()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(A));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class M : StateMachine
        {
            [Start]
            [OnEntry(nameof(InitOnEntry))]
            private class Init : State
            {
            }

            private async Task InitOnEntry()
            {
                this.SendEvent(this.Id, UnitEvent.Instance);
                await this.ReceiveEventAsync(typeof(UnitEvent));
                this.Assert(false, "Reached test assertion.");
            }
        }

        [Fact(Timeout = 5000)]
        public void TestReceiveEventInStateMachine()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(M));
            },
            expectedError: "Reached test assertion.",
            replay: true);
        }

        private class SetupEvent : Event
        {
            public ActorId Id;

            public SetupEvent(ActorId id)
            {
                this.Id = id;
            }
        }

        private class Ping : Event
        {
        }

        private class Pong : Event
        {
        }

        [OnEventDoAction(typeof(SetupEvent), nameof(SetupEvent))]
        private class ClientActor : Actor
        {
            private ActorId Server;
            private int Counter;

            private async Task SetupEvent(Event e)
            {
                this.Server = (e as SetupEvent).Id;
                this.Counter = 0;
                while (this.Counter < 5)
                {
                    await this.ReceiveEventAsync(typeof(Ping));
                    this.SendPong();
                }

                this.Halt();
            }

            private void SendPong()
            {
                this.Counter++;
                this.SendEvent(this.Server, new Pong());
            }
        }

        [OnEventDoAction(typeof(Pong), nameof(SendPing))]
        private class ServerActor1 : Actor
        {
            private ActorId Client;

            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.Client = this.CreateActor(typeof(ClientActor));
                this.SendEvent(this.Client, new SetupEvent(this.Id));
                this.SendPing();
                return Task.CompletedTask;
            }

            private void SendPing()
            {
                this.SendEvent(this.Client, new Ping());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExchangedReceiveEventInActor()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(ServerActor1));
            },
            configuration: GetConfiguration().WithNumberOfIterations(100));
        }

        private class ServerActor2 : Actor
        {
            private ActorId Client;

            protected override Task OnInitializeAsync(Event initialEvent)
            {
                this.Client = this.CreateActor(typeof(ClientActor));
                this.SendEvent(this.Client, new SetupEvent(this.Id));
                this.SendPing();
                this.IgnoreEvent(typeof(Pong));
                return Task.CompletedTask;
            }

            private void SendPing()
            {
                this.SendEvent(this.Client, new Ping());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOneActorReceiveEventFailure()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(ServerActor2));
            },
            configuration: GetConfiguration().WithNumberOfIterations(100),
            expectedError: "Deadlock detected. 'ClientActor()' is waiting to " +
                "receive an event, but no other controlled tasks are enabled.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestTwoActorsReceiveEventFailure()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(ServerActor2));
                r.CreateActor(typeof(ServerActor2));
            },
            configuration: GetConfiguration().WithNumberOfIterations(100),
            expectedError: "Deadlock detected. 'ClientActor()' and 'ClientActor()' are " +
                "waiting to receive an event, but no other controlled tasks are enabled.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestThreeActorsReceiveEventFailure()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(ServerActor2));
                r.CreateActor(typeof(ServerActor2));
                r.CreateActor(typeof(ServerActor2));
            },
            configuration: GetConfiguration().WithNumberOfIterations(100),
            expectedError: "Deadlock detected. 'ClientActor()', 'ClientActor()' and " +
                "'ClientActor()' are waiting to receive an event, but no other " +
                "controlled tasks are enabled.",
            replay: true);
        }

        private class ClientStateMachine : StateMachine
        {
            private ActorId Server;
            private int Counter;

            [Start]
            [OnEventDoAction(typeof(SetupEvent), nameof(SetupEvent))]
            [OnEventGotoState(typeof(UnitEvent), typeof(Active))]
            private class Init : State
            {
            }

            private Transition SetupEvent(Event e)
            {
                this.Server = (e as SetupEvent).Id;
                this.Counter = 0;
                return this.RaiseEvent(UnitEvent.Instance);
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : State
            {
            }

            private async Task<Transition> ActiveOnEntry()
            {
                while (this.Counter < 5)
                {
                    await this.ReceiveEventAsync(typeof(Ping));
                    this.SendPong();
                }

                return this.Halt();
            }

            private void SendPong()
            {
                this.Counter++;
                this.SendEvent(this.Server, new Pong());
            }
        }

        private class ServerStateMachine1 : StateMachine
        {
            private ActorId Client;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(UnitEvent), typeof(Active))]
            private class Init : State
            {
            }

            private Transition InitOnEntry()
            {
                this.Client = this.CreateActor(typeof(ClientStateMachine));
                this.SendEvent(this.Client, new SetupEvent(this.Id));
                return this.RaiseEvent(UnitEvent.Instance);
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [OnEventDoAction(typeof(Pong), nameof(SendPing))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.SendPing();
            }

            private void SendPing()
            {
                this.SendEvent(this.Client, new Ping());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestExchangedReceiveEventInStateMachine()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(ServerStateMachine1));
            },
            configuration: GetConfiguration().WithNumberOfIterations(100));
        }

        private class ServerStateMachine2 : StateMachine
        {
            private ActorId Client;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(UnitEvent), typeof(Active))]
            private class Init : State
            {
            }

            private Transition InitOnEntry()
            {
                this.Client = this.CreateActor(typeof(ClientStateMachine));
                this.SendEvent(this.Client, new SetupEvent(this.Id));
                return this.RaiseEvent(UnitEvent.Instance);
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [IgnoreEvents(typeof(Pong))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.SendEvent(this.Client, new Ping());
            }
        }

        [Fact(Timeout = 5000)]
        public void TestOneStateMachineReceiveEventFailure()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(ServerStateMachine2));
            },
            configuration: GetConfiguration().WithNumberOfIterations(100),
            expectedError: "Deadlock detected. 'ClientStateMachine()' is waiting " +
                "to receive an event, but no other controlled tasks are enabled.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestTwoStateMachinesReceiveEventFailure()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(ServerStateMachine2));
                r.CreateActor(typeof(ServerStateMachine2));
            },
            configuration: GetConfiguration().WithNumberOfIterations(100),
            expectedError: "Deadlock detected. 'ClientStateMachine()' and 'ClientStateMachine()' " +
                "are waiting to receive an event, but no other controlled tasks are enabled.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestThreeStateMachinesReceiveEventFailure()
        {
            this.TestWithError(r =>
            {
                r.CreateActor(typeof(ServerStateMachine2));
                r.CreateActor(typeof(ServerStateMachine2));
                r.CreateActor(typeof(ServerStateMachine2));
            },
            configuration: GetConfiguration().WithNumberOfIterations(100),
            expectedError: "Deadlock detected. 'ClientStateMachine()', 'ClientStateMachine()' " +
                "and 'ClientStateMachine()' are waiting to receive an event, but no other " +
                "controlled tasks are enabled.",
            replay: true);
        }
    }
}
