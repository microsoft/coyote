using System;
using System.Collections.Generic;
using Microsoft.Coyote;

namespace MultiPaxos
{
    internal class Client : Machine
    {
        internal class Config : Event
        {
            public List<MachineId> Servers;

            public Config(List<MachineId> servers)
            {
                this.Servers = servers;
            }
        }

        List<MachineId> Servers;

        [Start]
        [OnEventGotoState(typeof(local), typeof(PumpRequestOne))]
        [OnEventDoAction(typeof(Client.Config), nameof(Configure))]
        class Init : MachineState { }

        void Configure()
        {
            this.Servers = (this.ReceivedEvent as Config).Servers;
            this.Raise(new local());
        }

        [OnEntry(nameof(PumpRequestOneOnEntry))]
        [OnEventGotoState(typeof(response), typeof(PumpRequestTwo))]
        class PumpRequestOne : MachineState { }

        void PumpRequestOneOnEntry()
        {
            this.Monitor<ValidityCheck>(new ValidityCheck.monitor_client_sent(1));

            if (this.Random())
            {
                this.Send(this.Servers[0], new PaxosNode.Update(0, 1));
            }
            else
            {
                this.Send(this.Servers[this.Servers.Count - 1], new PaxosNode.Update(0, 1));
            }

            this.Raise(new response());
        }

        [OnEntry(nameof(PumpRequestTwoOnEntry))]
        [OnEventGotoState(typeof(response), typeof(Done))]
        class PumpRequestTwo : MachineState { }

        void PumpRequestTwoOnEntry()
        {
            this.Monitor<ValidityCheck>(new ValidityCheck.monitor_client_sent(2));

            if (this.Random())
            {
                this.Send(this.Servers[0], new PaxosNode.Update(0, 2));
            }
            else
            {
                this.Send(this.Servers[this.Servers.Count - 1], new PaxosNode.Update(0, 2));
            }

            this.Raise(new response());
        }

        [OnEntry(nameof(DoneOnEntry))]
        class Done : MachineState { }

        void DoneOnEntry()
        {
            this.Raise(new Halt());
        }
    }
}
