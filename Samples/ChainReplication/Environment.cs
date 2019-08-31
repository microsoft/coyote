using System.Collections.Generic;
using Microsoft.Coyote;

namespace ChainReplication
{
    internal class Environment : Machine
    {
        List<MachineId> Servers;
        List<MachineId> Clients;

        int NumOfServers;

        MachineId ChainReplicationMaster;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

		void InitOnEntry()
        {
            this.Servers = new List<MachineId>();
            this.Clients = new List<MachineId>();

            this.NumOfServers = 3;

            for (int i = 0; i < this.NumOfServers; i++)
            {
                MachineId server = null;

                if (i == 0)
                {
                    server = this.CreateMachine(typeof(ChainReplicationServer),
                        new ChainReplicationServer.Config(i, true, false));
                }
                else if (i == this.NumOfServers - 1)
                {
                    server = this.CreateMachine(typeof(ChainReplicationServer),
                        new ChainReplicationServer.Config(i, false, true));
                }
                else
                {
                    server = this.CreateMachine(typeof(ChainReplicationServer),
                        new ChainReplicationServer.Config(i, false, false));
                }

                this.Servers.Add(server);
            }

            this.Monitor<InvariantMonitor>(
                new InvariantMonitor.Config(this.Servers));
            this.Monitor<ServerResponseSeqMonitor>(
                new ServerResponseSeqMonitor.Config(this.Servers));

            for (int i = 0; i < this.NumOfServers; i++)
            {
                MachineId pred = null;
                MachineId succ = null;

                if (i > 0)
                {
                    pred = this.Servers[i-1];
                }
                else
                {
                    pred = this.Servers[0];
                }

                if (i < this.NumOfServers - 1)
                {
                    succ = this.Servers[i+1];
                }
                else
                {
                    succ = this.Servers[this.NumOfServers-1];
                }

                this.Send(this.Servers[i], new ChainReplicationServer.PredSucc(pred, succ));
            }

            this.Clients.Add(this.CreateMachine(typeof(Client),
                new Client.Config(0, this.Servers[0], this.Servers[this.NumOfServers-1], 1)));

            this.Clients.Add(this.CreateMachine(typeof(Client),
                new Client.Config(1, this.Servers[0], this.Servers[this.NumOfServers-1], 100)));

            this.ChainReplicationMaster = this.CreateMachine(typeof(ChainReplicationMaster),
                new ChainReplicationMaster.Config(this.Servers, this.Clients));

            this.Raise(new Halt());
        }
    }
}
