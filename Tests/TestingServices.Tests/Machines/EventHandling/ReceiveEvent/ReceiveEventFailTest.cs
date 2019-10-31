// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Actors;
using Microsoft.Coyote.Runtime.Exploration;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class ReceiveEventFailTest : BaseTest
    {
        public ReceiveEventFailTest(ITestOutputHelper output)
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
                this.Client = this.CreateMachine(typeof(Client));
                this.Send(this.Client, new Config(this.Id));
                this.Raise(new Unit());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            [IgnoreEvents(typeof(Pong))]
            private class Active : State
            {
            }

            private void ActiveOnEntry()
            {
                this.Send(this.Client, new Ping());
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
                this.Raise(new Unit());
            }

            [OnEntry(nameof(ActiveOnEntry))]
            private class Active : State
            {
            }

            private async Task ActiveOnEntry()
            {
                while (this.Counter < 5)
                {
                    await this.Receive(typeof(Ping));
                    this.SendPong();
                }

                this.Raise(new Halt());
            }

            private void SendPong()
            {
                this.Counter++;
                this.Send(this.Server, new Pong());
            }
        }

        [Fact(Timeout=5000)]
        public void TestOneMachineReceiveEventFailure()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(Server));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS),
            expectedError: "Deadlock detected. 'Client()' is waiting to receive an event, but no other controlled tasks are enabled.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestTwoMachinesReceiveEventFailure()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(Server));
                r.CreateMachine(typeof(Server));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS),
            expectedError: "Deadlock detected. 'Client()' and 'Client()' are waiting to " +
                "receive an event, but no other controlled tasks are enabled.",
            replay: true);
        }

        [Fact(Timeout=5000)]
        public void TestThreeMachinesReceiveEventFailure()
        {
            this.TestWithError(r =>
            {
                r.CreateMachine(typeof(Server));
                r.CreateMachine(typeof(Server));
                r.CreateMachine(typeof(Server));
            },
            configuration: GetConfiguration().WithStrategy(SchedulingStrategy.DFS),
            expectedError: "Deadlock detected. 'Client()', 'Client()' and 'Client()' are " +
                "waiting to receive an event, but no other controlled tasks are enabled.",
            replay: true);
        }
    }
}
