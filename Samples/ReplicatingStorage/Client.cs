using System;
using Microsoft.Coyote;

namespace ReplicatingStorage
{
    internal class Client : Machine
    {
        #region events

        /// <summary>
        /// Used to configure the client.
        /// </summary>
        public class ConfigureEvent : Event
        {
            public MachineId NodeManager;

            public ConfigureEvent(MachineId manager)
                : base()
            {
                this.NodeManager = manager;
            }
        }

        /// <summary>
        /// Used for a client request.
        /// </summary>
        internal class Request : Event
        {
            public MachineId Client;
            public int Command;

            public Request(MachineId client, int cmd)
                : base()
            {
                this.Client = client;
                this.Command = cmd;
            }
        }

        private class LocalEvent : Event { }

        #endregion

        #region fields

        private MachineId NodeManager;

        private int Counter;

        #endregion

        #region states

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        [OnEventDoAction(typeof(ConfigureEvent), nameof(Configure))]
        [OnEventGotoState(typeof(LocalEvent), typeof(PumpRequest))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            this.Counter = 0;
        }

        void Configure()
        {
            this.NodeManager = (this.ReceivedEvent as ConfigureEvent).NodeManager;
            this.Raise(new LocalEvent());
        }

        [OnEntry(nameof(PumpRequestOnEntry))]
        [OnEventGotoState(typeof(LocalEvent), typeof(PumpRequest))]
        class PumpRequest : MachineState { }

        void PumpRequestOnEntry()
        {
            int command = this.RandomInteger(100) + 1;
            this.Counter++;

            this.Logger.WriteLine("\n [Client] new request {0}.\n", command);

            this.Send(this.NodeManager, new Request(this.Id, command));

            if (this.Counter == 1)
            {
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
