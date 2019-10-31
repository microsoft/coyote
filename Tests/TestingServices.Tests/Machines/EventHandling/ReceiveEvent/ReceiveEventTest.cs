// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime.Exploration;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class ReceiveEventTest : BaseTest
    {
        public ReceiveEventTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class Config : Event
        {
            public ActorId Id;

            public Config(ActorId id)
            {
                this.Id = id;
            }
        }

        private class Unit : Event
        {
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
            [OnEventGotoState(typeof(Unit), typeof(Active))]
            private class Init : State
            {
            }

            private void InitOnEntry()
            {
                this.Client = this.CreateStateMachine(typeof(Client));
                this.SendEvent(this.Client, new Config(this.Id));
                this.RaiseEvent(new Unit());
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
            [OnEventDoAction(typeof(Config), nameof(Configure))]
            [OnEventGotoState(typeof(Unit), typeof(Active))]
            private class Init : State
            {
            }

            private void Configure()
            {
                this.Server = (this.ReceivedEvent as Config).Id;
                this.Counter = 0;
                this.RaiseEvent(new Unit());
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

                this.RaiseEvent(new Halt());
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
                r.CreateStateMachine(typeof(Server));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS));
        }
    }
}
