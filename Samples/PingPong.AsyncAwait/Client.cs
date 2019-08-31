using System;
using System.Threading.Tasks;
using Microsoft.Coyote;

namespace PingPong.AsyncAwait
{
    /// <summary>
    /// A Coyote machine that models a simple client.
    ///
    /// It sends 'Ping' events to a server, and handles received 'Pong' event.
    /// </summary>
    internal class Client : Machine
    {
        /// <summary>
        /// Event declaration of a 'Config' event that contains payload.
        /// </summary>
        internal class Config : Event
        {
            /// <summary>
            /// The payload of the event. It is a reference to the server machine
            /// (send by the 'NetworkEnvironment' machine upon creation of the client).
            /// </summary>
            public MachineId Server;

            public Config(MachineId server)
            {
                this.Server = server;
            }
        }

        /// <summary>
        /// Event declaration of a 'Unit' event that does not contain any payload.
        /// </summary>
        internal class Unit : Event { }

        /// <summary>
        /// Event declaration of a 'Ping' event that contains payload.
        /// </summary>
        internal class Ping : Event
        {
            /// <summary>
            /// The payload of the event. It is a reference to the client machine.
            /// </summary>
            public MachineId Client;

            public Ping(MachineId client)
            {
                this.Client = client;
            }
        }

        /// <summary>
        /// Reference to the server machine.
        /// </summary>
        MachineId Server;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            // Receives a reference to a server machine (as a payload of
            // the 'Config' event).
            this.Server = (this.ReceivedEvent as Config).Server;

            // Notifies the Coyote runtime that the machine must transition
            // to the 'Active' state when 'InitOnEntry' returns.
            this.Goto<Active>();
        }

        /// <summary>
        [OnEntry(nameof(ActiveOnEntry))]
        /// </summary>
        class Active : MachineState { }

        async Task ActiveOnEntry()
        {
            // A counter for ping-pong turns.
            int counter = 0;
            while (counter < 5)
            {
                // Sends (asynchronously) a 'Ping' event to the server that contains
                // a reference to this client as a payload.
                this.Send(this.Server, new Ping(this.Id));

                // Invoking 'Receive' will cause the machine to wait (asynchronously)
                // until a 'Pong' event is received. The event will then get dequeued
                // and execution will resume.
                await this.Receive(typeof(Server.Pong));

                counter++;

                Console.WriteLine("Client request: {0} / 5", counter);
            }

            // If 5 'Ping' events were sent, then raise the special event 'Halt'.
            //
            // Raising an event, notifies the Coyote runtime to execute the event handler
            // that corresponds to this event in the current state, when 'SendPing'
            // returns.
            //
            // In this case, when the machine handles the special event 'Halt', it
            // will terminate the machine and release any resources. Note that the
            // 'Halt' event is handled automatically, the user does not need to
            // declare an event handler in the state declaration.
            this.Raise(new Halt());
        }
    }
}