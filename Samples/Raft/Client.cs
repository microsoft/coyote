using System;
using System.Collections.Generic;
using Microsoft.Coyote;

namespace Raft
{
    internal class Client : Machine
    {
        #region events

        /// <summary>
        /// Used to configure the client.
        /// </summary>
        public class ConfigureEvent : Event
        {
            public MachineId Cluster;

            public ConfigureEvent(MachineId cluster)
                : base()
            {
                this.Cluster = cluster;
            }
        }

        /// <summary>
        /// Used for a client request.
        /// </summary>
        internal class Request : Event
        {
            public MachineId Client;
            public int Command;

            public Request(MachineId client, int command)
                : base()
            {
                this.Client = client;
                this.Command = command;
            }
        }

        internal class Response : Event { }

        private class LocalEvent : Event { }

        #endregion

        #region fields

        MachineId Cluster;

        int LatestCommand;
        int Counter;

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
        [OnEventGotoState(typeof(LocalEvent), typeof(PumpRequest))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.LatestCommand = -1;
            this.Counter = 0;
        }

        void Configure()
        {
            this.Cluster = (this.ReceivedEvent as ConfigureEvent).Cluster;
            this.Raise(new LocalEvent());
        }

        [OnEntry(nameof(PumpRequestOnEntry))]
        [OnEventDoAction(typeof(Response), nameof(ProcessResponse))]
        [OnEventGotoState(typeof(LocalEvent), typeof(PumpRequest))]
        class PumpRequest : MachineState { }

        void PumpRequestOnEntry()
        {
            this.LatestCommand = this.RandomInteger(100); //new Random().Next(100);
            this.Counter++;

            this.Logger.WriteLine("\n [Client] new request " + this.LatestCommand + "\n");

            this.Send(this.Cluster, new Request(this.Id, this.LatestCommand));
        }

        void ProcessResponse()
        {
            if (this.Counter == 3)
            {
                this.Send(this.Cluster, new ClusterManager.ShutDown());
                this.Raise(new Halt());
            }
            else
            {
                this.Raise(new LocalEvent());
            }
        }

        #endregion
    }
}
