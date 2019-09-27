---
layout: reference
section: learn
title: What is an actor?
permalink: /learn/tutorials/building-your-first-coyote-application
---
Writing your first Coyote program
=============================
A Coyote program consists of one or more state-machines (which we simply refer to as machines). Each Coyote machine has an input queue, states, state transitions, event handlers, fields and methods. Machines run concurrently with each other, each executing an event handling loop that dequeues an event from the input queue and handles it by executing a sequence of operations. Each operation might update a field, create a new machine, or send an event to another machine. In Coyote, create machine operations and send operations are non-blocking. In the case of a send operation the message is simply enqueued into the input queue of the target machine.

We will now show how to write a program using the surface syntax of Coyote, but the same principles apply when using Coyote as a C# library (we show the example program using the Coyote library below).

The source code of a Coyote program is a collection of `event` and `machine` declarations and, optionally, other top-level C# declarations, such as `class` and `struct`. All top-level declarations must be declared inside a `namespace`, as in C#. If someone uses the surface syntax of Coyote, then events and machines must be declared inside a `.psharp` file, while C# top-level declarations must be declared in a `.cs` file. On the other hand, if someone uses Coyote as a C# library, all the code must be written inside a `.cs` file.

State machines are first-class citizens of the Coyote language and can be declared in the following way:
```
machine Server { ... }
```

The above code snippet declares a Coyote machine named `Server`. Machine declarations are similar to class declarations in C#, and thus can contain an arbitrary number of fields and methods. For example, the below code snippet declares the field `client` of type `machine`. An object of this type contains a reference to a machine instance.

```
machine Server {
  machine client;
}
```

The main difference between a class and a machine declaration is that the latter must also declare one or more _states_:
```
machine Server {
  machine client;
  start state Init { ... }
  state Active { ... }
}
```

The above declares two states in the `Server` machine: `Init` and `Active`. The Coyote developer must use the `start` modifier to declare an _initial_ state, which will be the first state that the machine will transition to upon instantiation. In this example, the `Init` state has been declared as the initial state of `Server`. Note that only a single state is allowed to be declared as an initial per machine. A `state` declaration can optionally contain a number of state-specific actions, as seen in the following code snippet:

```
state SomeState {
  entry { ... }
  exit { ... }
}
```

A code block indicated by `entry { ... }` denotes an action that will be executed when the machine transitions to the state, while a code block indicated by `exit { ... }` denotes an action that will be executed when the machine leaves the state. Actions in Coyote are essentially C# methods with no input parameters and `void` return type. Coyote actions can contain arbitrary Coyote and C# statements. However, since we want to explicitly declare all sources of asynchrony using Coyote, we only allow the use of _sequential_ C# code inside a Coyote machine, with [some exceptions](Features/ObjectSharing.md). In practice, we just _assume_ that the C# code is sequential, as it would be very challenging to impose this rule in real life programs.

An example of an `entry` action is the following:
```
entry {
  this.client = create(Client);
  send(this.client, Config, this);
  send(this.client, Ping);
  raise(Unit);
}
```

The above action contains the three most important Coyote statements. The `create` statement is used to create a new instance of the `Client` machine. A reference to this instance is stored in the `client` field. Next, the `send` statement is used to send an event (in this case the events `Config` and `Ping`) to a target machine (in this case the machine whose address is stored in the field `client`).

When an event is being sent, it is enqueued in the event queue of the target machine, which can then dequeue the received event, and handle it asynchronously from the sender machine. Finally, the `raise` statement is used to send an event to the caller machine (i.e. to itself). Like calling a `send`, when a machine raises an event, it still continues execution of the enclosing code block. However, when the current machine action finishes, instead of dequeuing from the inbox, the machine immediately handles the raised event.

In Coyote, events (e.g. `Ping`, `Unit` and `Config` in the above example) can be declared as follows:
```
event Ping;
event Unit;
event Config (target: machine);
```

A Coyote machine can send data (scalar values or references) to a target machine, as the payload of an event. Such an event must specify the type of the payload in its declaration (as in the case of the `Config` event above). A machine can also send data to itself (e.g. for processing in a later state) using `raise`.

In the previous example, the `Server` machine sends `this` (i.e. a reference to the current machine instance) to the `client` machine. The receiver (in our case `client`) can retrieve the sent data by using the keyword `trigger` (or `ReceivedEvent` when using Coyote as a C# library), which is a handle to the received event, casting `trigger` to the expected event type (in this case `Config`), and then accessing the payload as a field of the received event.

As discussed earlier, the `create` and `send` statements are non-blocking. The Coyote runtime will take care of all the underlying asynchrony using the Task Parallel Library and, thus, the developer does not need to explicitly create and manage tasks.

Besides the `entry` and `exit` declarations, all other declarations inside a Coyote state are related to _event-handling_, which is a key feature of Coyote. An event-handler declares how a Coyote machine should _react_ to a received event. One such possible reaction is to create one or more machine instances, send one or more events, or process some local data. The two most important event-handling declarations in Coyote are the following:
```
state SomeState {
  on Unit goto AnotherState;
  on Pong do SomeAction;
}
```

The declaration `on Unit goto AnotherState` indicates that when the machine receives the `Unit` event in `SomeState`, it must handle `Unit` by exiting the state and transitioning to `AnotherState`. The declaration `on Pong do SomeAction` indicates that the `Pong` event must be handled by invoking the action `SomeAction`, and that the machine will remain in `SomeState`.

Coyote also supports _anonymous_ event-handlers. For example, the declaration `on Pong do { ... }` is an anonymous event-handler, which states that the block of statements between the braces must be executed when event `Pong` is dequeued. Each event can be associated with at most one handler in a particular state of a machine. If a Coyote machine is in a state `SomeState` and dequeues an event `SomeEvent`, but no event-handler is declared in `SomeState` for `SomeEvent`, then Coyote will throw an appropriate exception.

Besides the above event-handling declarations, Coyote also provides the capability to _defer_ and _ignore_ events in a particular state:
```
state SomeState {
  defer Ping;
  ignore Unit;
}
```

The declaration `defer Ping` indicates that the `Ping` event should not be dequeued while the machine is in the state `SomeState`. Instead, the machine should skip over `Ping` (without dropping `Ping` from the queue) and dequeue the next event that is not being deferred. The declaration `ignore Unit` indicates that whenever `Unit` is dequeued while the machine is in `SomeState`, then the machine should drop `Unit` without invoking any action.

Coyote also supports specifying invariants (i.e. assertions) on the local state of a machine. The developer can achieve this by using the `assert` statement, which accepts as input a predicate that must always hold in that specific program point, e.g. `assert(k == 0)`, which holds if the integer `k` equals to `0`.

## An example program
The following Coyote program shows a `Client` machine and a `Server` machine that communicate asynchronously by exchanging `Ping` and `Pong` events:
```
namespace PingPong {
  event Ping; // Client sends this event to the Server
  event Pong; // Server sends this event to the Client
  event Unit; // Event used for local transitions
  
  // Event used for configuration, can take a payload
  event Config (target: machine);
  
  machine Server {
    machine client;
  
    start state Init {
      entry {
        // Instantiates the Client
        this.client = create(Client);
        // Sends event to client to configure it
        send(this.client, Config, this);
        raise(Unit); // Sends an event to itself
      }
      
      on Unit goto Active; // Performs a state transition
    }
  
    state Active {
      on Ping do {
        // Sends a Pong event to the Client
        send(this.client, Pong);
      };
    }
  }
  
  machine Client {
    machine server;
  
    start state Init {
      on Config do Configure; // Handles the event
      on Unit goto Active; // Performs a state transition
    }
    
    void Configure() {
      // Receives reference to Server
      this.server = (trigger as Config).target;
      raise(Unit); // Sends an event to itself
    }
  
    state Active {
      entry {
        SendPing();
      }
      on Pong do SendPing;
    }
  
    void SendPing() {
      // Sends a Ping event to the Server
      send(this.server, Ping);
    }
  }
  
  public class HostProgram {
    static void Main(string[] args) {
      PSharpRuntime.Create().CreateMachine(typeof(Server));
      Console.ReadLine();
    }
  }
}
```

In the above example, the program starts by creating an instance of the `Server` machine. The implicit constructor of each Coyote machine initializes the internal to the Coyote runtime data of the machine, including the event queue, a set of available states, and a map from events to event-handlers per state.

After the `Server` machine has initialized, the Coyote runtime executes the `entry` action of the initial (`Init`) state of `Server`, which first creates an instance of the `Client` machine, then sends the event `Config` to the `Client` machine, with the `this` reference as a payload, and then raises the event
`Unit`. As mentioned earlier, when a machine calls `raise`, it bypasses the queue and first handles the raised event. In this case, the `Server` machine handles `Unit` by transitioning to the `Active` state.

`Client` starts executing (asynchronously) when it is created by `Server`. The `Client` machine stores the received payload (which is a reference to the `Server` machine) in the `server` field, and then raises `Unit` to transition to the `Active` state. In the new state, `Client` calls the `SendPing` method to send a `Ping` event to `Server`. In turn, the `Server` machine dequeues `Ping` and handles it by sending a `Pong` event to `Client`, which subsequently responds by sending a new `Ping` event to `Server`. This asynchronous exchange of `Ping` and `Pong` events continues indefinitely.

## Entry point to a Coyote program
Because Coyote is built on top of the C# language, the entry point of a Coyote program (i.e. the first machine that the Coyote runtime will instantiate and execute) must be explicitly declared inside a host C# program (typically in the `Main` method), as follows:
```c#
using Microsoft.PSharp;
public class HostProgram {
  static void Main(string[] args) {
    IMachineRuntime runtime = PSharpRuntime.Create();
    runtime.CreateMachine(typeof(Server));
    Console.ReadLine();
  }
}
```

The developer must first import the Coyote runtime library (`Microsoft.PSharp.dll`), then create a `runtime` instance (of type `IMachineRuntime`), and finally invoke the `CreateMachine` method of `runtime` to instantiate the first Coyote machine (`Server` in the above example).

The `CreateMachine` method accepts as a parameter the type of the machine to be instantiated, and returns an object of the `MachineId` type, which contains a reference to the created Coyote machine. Because `CreateMachine` is an asynchronous method, we call the `Console.ReadLine` method, which pauses the main thread until a console input has been given, so that the host C# program does not exit prematurely.

The `IMachineRuntime` interface also provides the `SendEvent` method for sending events to a Coyote machine from C#. This method accepts as parameters an object of type `MachineId`, an event and an optional payload. Although the developer has to use `CreateMachine` and `SendEvent` to interact with the Coyote runtime from C#, the opposite is straightforward, as it only requires accessing a C# object from inside a Coyote machine.

## Using Coyote as a C# library
The above example can be written using Coyote as a C# library as follows:
```c#
using System;
using Microsoft.PSharp;

namespace PingPong {
  class Unit : Event { }
  class Ping : Event { }
  class Pong : Event { }
  
  class Config : Event {
    public MachineId Target;
    public Config(MachineId target) : base() {
      this.Target = target;
    }
  }
  
  class Server : Machine {
    MachineId Client;
    
    [Start]
    [OnEntry(nameof(InitOnEntry))]
    [OnEventGotoState(typeof(Unit), typeof(Active))]
    class Init : MachineState { }
    
    void InitOnEntry() {
      this.Client = this.CreateMachine(typeof(Client));
      this.Send(this.Client, new Config(this.Id));
      this.Raise(new Unit());
    }
    
    [OnEntry(nameof(ServerActiveEntry))]
    [OnEventDoAction(typeof(Pong), nameof(SendPing))]
    class Active : MachineState { }

    void ServerActiveEntry()
    {
      this.SendPing();
    }
    
    void SendPing() {
      this.Send(this.Client, new Ping());
    }
  }
  
  class Client : Machine {
    MachineId Server;
    
    [Start]
    [OnEventGotoState(typeof(Unit), typeof(Active))]
    [OnEventDoAction(typeof(Config), nameof(Configure))]
    class Init : MachineState { }
    
    void Configure() {
      this.Server = (this.ReceivedEvent as Config).Target;
      this.Raise(new Unit());
    }
    
    [OnEventDoAction(typeof(Ping), nameof(SendPong))]
    class Active : MachineState { }
    
    void SendPong() {
      this.Send(this.Server, new Pong());
    }
  }
  
  public class Program {
    static void Main(string[] args) {
      Console.WriteLine("Starting machines...");
      var configuration = Configuration.Create().WithVerbosityEnabled(1);
      var runtime = PSharpRuntime.Create(configuration);
      runtime.CreateMachine(typeof(Server));
      Console.ReadLine();
    }
  }
}
```

The developer can use Coyote as a library by importing the `Microsoft.PSharp.dll` library. A Coyote machine can be declared by creating a C# `class` that inherits from the type `Machine` (provided by the Coyote library). A state can be declared by creating a `class` that inherits from the type `MachineState`. This state class must be nested inside a machine class (no other class besides a state can be nested inside a machine class). The start state can be declared using the `[Start]` attribute.

A state transition can be declared using the `[OnEventGotoState(...)]` attribute, where the first argument of the attribute is the type of the received event and the second argument is the type of the target state. An optional third argument, is a string that denotes the name of the method to be executed after exiting the state and before entering the new state. Likewise, an action handler can be declared using the `[OnEventDoAction(...)]` attribute, where the first argument of the attribute is the type of the received event and the second argument is the name of the action to be executed. All Coyote statements (e.g. `send` and `raise`) are exposed as method calls of the `Machine` and `MachineState` classes.

