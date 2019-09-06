---
layout: reference
title: Learn
section: learn
permalink: /learn/
---

## Get started with Coyote

Coyote is built on top of the .NET framework and the Roslyn compiler. It provides capability to easily write safety and liveness specifications (similar to TLA+) programmatically in C#.

Coyote has an efficient, lightweight runtime that is built on top of the Task Parallel Library. This runtime can be used to deploy a Coyote program in production. The runtime is very flexible and can work with any communication and storage layer.

A systematic testing engine controls the Coyote program schedule as well as all declared sources of nondeterminism (e.g., failures and timeouts). The engine explores the actual executable code systematically to discover bugs (e.g., crashes or specification violations). If a bug is found, the testing engine will report a deterministic reproducible trace that can be replayed using Visual Studio Debugger.

<div>
<a href="" class="btn btn-primary mt-20 mr-30">Install package</a> <a href="" class="btn btn-primary mt-20">Build from source</a>
</div>

## Ways to use Coyote

Coyote is provided as both a language extension of C#, as well as a set of library and runtime APIs that can be directly used from inside a C# program. This means that there are two main ways that someone can use Coyote to build highly-reliable systems:

- The surface syntax of Coyote (i.e. C# language extension) can be used to build an entire system from scratch (see an example here). The surface Coyote syntax directly extends C# with new language constructs, which allows for rapid prototyping. However, to use the surface syntax, a developer has to use the Coyote compiler, which is built on top of Roslyn. The main disadvantage of this approach is that Coyote does not yet fully integrate with the Visual Studio integrated development environment (IDE), although we are actively working on this (see here), and thus does not support high-productivity features such as IntelliSense (e.g, for auto-completion and automated refactoring).

- The Coyote library and runtime APIs (available for C#) can be used to build an entire system from scratch (see an example here). This approach is slightly more verbose than the above, but allows full integration with Visual Studio.

Coyote can be also used for thoroughly testing an existing message-passing system, by modeling its environment (e.g, a client) and/or components of the system. However, this approach has the disadvantage that if nondeterminism in the system is not captured by (or expressed in) Coyote, then the Coyote testing engine might be unable to discover and reproduce bugs.

## Example

The simplest example is a simple ping/pong state machine that sends messages from client to server and back:

```c#
   /// <summary>
    /// A Coyote machine that models a simple ping/pong server.
    ///
    /// It receives 'Ping' events from a client, and responds with a 'Pong' event.
    /// </summary>
    internal class Server : Machine
    {
        /// <summary>
        /// Event declaration of a 'Pong' event that does not contain any payload.
        /// </summary>
        internal class Pong : Event { }

        [Start]
        /// <summary>
        /// The 'OnEventDoAction' action declaration will execute (asynchrously)
        /// the 'SendPong' method, whenever a 'Ping' event is dequeued while the
        /// server machine is in the 'Active' state.
        [OnEventDoAction(typeof(Client.Ping), nameof(SendPong))]
        /// </summary>
        class Active : MachineState { }

        void SendPong()
        {
            // Receives a reference to a client machine (as a payload of
            // the 'Ping' event).
            var client = (this.ReceivedEvent as Client.Ping).Client;
            // Sends (asynchronously) a 'Pong' event to the client.
            this.Send(client, new Pong());
        }
    }

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

```

