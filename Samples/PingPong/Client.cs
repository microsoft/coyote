using Microsoft.Coyote;

namespace PingPong
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

        /// <summary>
        /// A counter for ping-pong turns.
        /// </summary>
        int Counter;

        [Start]
        [OnEntry(nameof(InitOnEntry))]
        class Init : MachineState { }

        void InitOnEntry()
        {
            // Receives a reference to a server machine (as a payload of
            // the 'Config' event).
            this.Server = (this.ReceivedEvent as Config).Server;
            this.Counter = 0;

            // Notifies the Coyote runtime that the machine must transition
            // to the 'Active' state when 'InitOnEntry' returns.
            this.Goto<Active>();
        }

        /// <summary>
        [OnEntry(nameof(ActiveOnEntry))]
        /// The 'OnEventDoAction' action declaration will execute (asynchrously)
        /// the 'SendPing' method, whenever a 'Pong' event is dequeued while the
        /// client machine is in the 'Active' state.
        [OnEventDoAction(typeof(Server.Pong), nameof(SendPing))]
        /// </summary>
        class Active : MachineState { }

        void ActiveOnEntry()
        {
            SendPing();
        }

        void SendPing()
        {
            this.Counter++;

            // Sends (asynchronously) a 'Ping' event to the server that contains
            // a reference to this client as a payload.
            this.Send(this.Server, new Ping(this.Id));

            this.Logger.WriteLine("Client request: {0} / 5", this.Counter);

            if (this.Counter == 5)
            {
                // If 5 'Ping' events where sent, then raise the special event 'Halt'.
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
}