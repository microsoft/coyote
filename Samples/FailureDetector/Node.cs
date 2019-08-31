using System;
using System.Collections.Generic;
using Microsoft.Coyote;

namespace FailureDetector
{
    /// <summary>
    /// Implementation of a simple node.
    ///
    /// The node responds with a 'Pong' event whenever it receives
	/// a 'Ping' event. This is used as a heartbeat to show that
	/// the node is still alive.
    /// </summary>
    internal class Node : Machine
    {
        internal class Ping : Event
        {
            public MachineId Client;

            public Ping(MachineId client)
            {
                this.Client = client;
            }
        }

        internal class Pong : Event
        {
            public MachineId Node;

            public Pong(MachineId node)
            {
                this.Node = node;
            }
        }

        [Start]
        [OnEventDoAction(typeof(Ping), nameof(SendPong))]
        class WaitPing : MachineState { }

        void SendPong()
        {
            var client = (this.ReceivedEvent as Ping).Client;
            this.Monitor<Safety>(new Safety.Pong(this.Id));
            this.Send(client, new Pong(this.Id));
        }
    }
}
