// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime.Exploration;
using Microsoft.Coyote.Tests.Common.Actors;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Actors.StateMachines
{
    public class ReceiveEventTest : BaseTest
    {
        public ReceiveEventTest(ITestOutputHelper output)
            : base(output)
        {
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

        private class Server : StateMachine
        {
            private ActorId Client;

            [Start]
            [OnEntry(nameof(InitOnEntry))]
            [OnEventGotoState(typeof(UnitEvent), typeof(Active))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Client = this.CreateActor(typeof(Client));
                this.SendEvent(this.Client, new SetupEvent(this.Id));
                this.RaiseEvent(new UnitEvent());
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

        private class Client : StateMachine
        {
            private ActorId Server;
            private int Counter;

            [Start]
            [OnEventDoAction(typeof(SetupEvent), nameof(SetupEvent))]
            [OnEventGotoState(typeof(UnitEvent), typeof(Active))]
            private class Init : State
            {
            }

            private void SetupEvent()
            {
                this.Server = (this.ReceivedEvent as SetupEvent).Id;
                this.Counter = 0;
                this.RaiseEvent(new UnitEvent());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : State
            {
            }

            private async Task ActiveOnEntry()
            {
                while (this.Counter < 5)
                {
                    await this.ReceiveEventAsync(typeof(Ping));
                    this.SendPong();
                }

                this.RaiseEvent(new HaltEvent());
            }

            private void SendPong()
            {
                this.Counter++;
                this.SendEvent(this.Server, new Pong());
            }
        }

        /// <summary>
        /// Coyote semantics test: two machines, monitor instantiation parameter.
        /// </summary>
        [Fact(Timeout=5000)]
        public void TestReceiveEvent()
        {
            this.Test(r =>
            {
                r.CreateActor(typeof(Server));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS));
        }
    }
}
