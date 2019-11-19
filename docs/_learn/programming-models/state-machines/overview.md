---
layout: reference
title: Machines programming model
section: learn
permalink: /learn/programming-models/state-machines/overview
---

## Programming model: asynchronous state machines

The _asynchronous state machines_ programming model of Coyote is an actor-based programming model that
exposes the `StateMachine` type. Using this programming model allows you to create new machines and send
events from one machine to another.

A program written using this programming model consists of one or more state machines (which we simply
refer to as machines). Each Coyote machine has an input queue, states, state transitions, event
handlers, fields and methods. Machines run concurrently with each other, each executing a sequential
event handling loop that dequeues an event from the input queue and handles it by executing a sequence
of operations. Each operation might update a field, create a new machine, or send an event to another
machine. In Coyote, create machine operations and send operations are non-blocking. In the case of a
send operation the message is simply enqueued into the input queue of the target machine, that is send
does not wait for the message to be processed at the target before returning.

## Writing your first program using state machines

A state machine program in Coyote is a collection of machines (declared by subclassing the `StateMachine`
type) and events (declared by subclassing the `Event` type), as well as other regular C# types (such as
classes and structs).

Machines types can be declared in the following way:

```c#
class Server : StateMachine { ... }
```

The above code snippet declares a machine named `Server`. Since machines are defined as C# classes,
they can contain an arbitrary number of fields and methods. For example, the below code snippet
declares the field `Client` of type `ActorId`, which is a handle to a machine instance.

```c#
class Server : StateMachine {
  ActorId Client;
}
```

Machines in Coyote must also declare one or more _states_:

```c#
class Server : StateMachine {
  ActorId Client;

  [Start]
  class Init : State { }

  class Active : State { }
}
```

The above code snippet declares two states in the `Server` machine: `Init` and `Active`. You must use
the `Start` attribute to declare an _initial_ state, which will be the first state that the machine
will transition to upon instantiation. In this example, the `Init` state has been declared as the
initial state of `Server`. Note that only a single state is allowed to be declared as an initial per
machine. A state declaration can optionally be associated with a number of state-specific actions, as
seen in the following code snippet:

```c#
[OnEntry(nameof(InitOnEntry))]
[OnExit(nameof(InitOnExit))]
class SomeState : State { }

void InitOnEntry() {
  // Code executing when entering the state.
}

void InitOnExit() {
  // Code executing when exiting the state.
}
```

A method declared using the `OnEntry` attribute denotes an action that will be executed when the
machine transitions to the state, while a method indicated by the `OnExit` attribute denotes an action
that will be executed when the machine leaves the state. Actions in Coyote are essentially methods with
no input parameters and the `void` return type. Coyote actions can contain arbitrary C# code. However,
since (1) a Coyote machine is an actor and (2) we want to explicitly declare all sources of concurrency
using Coyote (so that the tester can take control and explore interleavings to find bugs), the
framework typically only allows the use of _sequential_ C# code inside a machine. In practice, we just
_assume_ that the C# code is sequential, as it would be very challenging to impose this rule in real
life programs. Using non-controlled concurrency inside a machine handler results into undefined
behavior, but the Coyote tester will try to identify such cases and report an error.

An example of an `OnEntry` action is the following:

```c#
void InitOnEntry() {
  this.Client = this.CreateStateMachine(typeof(Client));
  this.SendEvent(this.Client, new ConfigEvent(this.Id));
  this.SendEvent(this.Client, new PingEvent());
  this.RaiseEvent(new UnitEvent());
}
```

The above action contains three of the most important machine APIs. The `CreateStateMachine` method is used
to create a new instance of the `Client` machine. A handle to this instance (with type `ActorId`) is
stored in the `Client` field. Next, the `SendEvent` method is used to send an event (in this case
the events `ConfigEvent` and `PingEvent`) to a target machine (in this case the machine whose
address is stored in the field `Client`).

When an event is being sent, it is enqueued in the event queue of the target machine, which can then
dequeue the received event, and handle it asynchronously from the sender machine.
Finally, the `RaiseEvent` method is used to send an event to the caller machine (i.e. to itself).
Similar to invoking `SendEvent`, when a machine raises an event, it still continues execution
of the method that raised. However, when the current machine action finishes, instead of
dequeuing from the inbox, the machine immediately handles the raised event (i.e. has higher
priority than the queue).

In Coyote, events (e.g. `PingEvent`, `UnitEvent` and `ConfigEvent` in the above example) can be declared as follows:

```c#
class PingEvent : Event { }
class UnitEvent : Event { }
class ConfigEvent : Event
{
  public readonly ActorId Target;

  public ConfigEvent(ActorId target) {
    this.Target = target;
  }
}
```

An event can contain arbitrary data (scalar values or references) and be send to a target machine
(there is no deep-copying for performance reasons). A machine can also send data to itself (e.g. for
processing in a later state) using `RaiseEvent`.

In the previous example, the `Server` machine sends `this.Id` of type `ActorId` (i.e. a handle to the
current machine instance) to the `Client` machine. The receiver (in our case `Client`) can retrieve the
sent payload by using the property `ReceivedEvent`, which is a handle to the received event, casting
`ReceivedEvent` to the expected event type (in this case `ConfigEvent`), and then accessing the payload
as a field of the received event.

As discussed earlier, the `CreateStateMachine` and `SendEvent` methods are non-blocking. The
Coyote runtime will take care of all the underlying concurrency using the Task Parallel Library,
which means that you do not need to explicitly create and manage tasks. However, you must be
careful in not sharing data between machines and then accessing them after from both machines,
as this can lead to race conditions.

Besides the `OnEntry` and `OnExit` machine state attributes, all other declarations inside a machine
state are related to _event-handling_, which is a key feature of a Coyote machine. An event-handler
declares how a machine should _react_ to a received event. One such possible reaction is to create one
or more machine instances, send one or more events, update the machine state or invoke some 3rd party
library. Two of the most important event-handling declarations in Coyote machines are the following:

```c#
[OnEventGotoState(typeof(UnitEvent), typeof(AnotherState))]
[OnEventDoAction(typeof(PingEvent), nameof(SomeAction))]
[OnEventDoAction(typeof(PongEvent), nameof(SomeActionAsync))]
class SomeState : State { }

void SomeAction() {
  // Code executing when exiting the state.
}

async Task SomeActionAsync() {
  // Code executing when exiting the state.
  await // You can use await inside an async handler
}

class AnotherState : State { }
```

The attribute `OnEventGotoState` indicates that when the machine receives the `UnitEvent` event in
`SomeState`, it must handle `UnitEvent` by exiting the state and transitioning to `AnotherState`. The
first `OnEventDoAction` attribute indicates that the `PingEvent` event must be handled by invoking the
action `SomeAction`, and that the machine will remain in `SomeState`. The second `OnEventDoAction`
attribute shows how state machine event-handling methods can be declared using `async Task` so that
they can `await` awaitable C# methods.

You can associate each event with at most one handler in a particular state of a machine. If a Coyote
machine is in a state `SomeState` and dequeues an event `SomeEvent`, but no event-handler is declared
in `SomeState` for `SomeEvent`, then Coyote will throw an appropriate exception (which will be reported
as a bug during testing).

Besides the above event-handling attributes, Coyote also provides the capability to _defer_ and
_ignore_ events in a particular state:

```c#
[DeferEvents(typeof(PingEvent), typeof(PongEvent))]
[IgnoreEvents(typeof(UnitEvent))]
class SomeState : State { }
```

The attribute `DeferEvents` indicates that the `PingEvent` and `PongEvent` events should not be
dequeued while the machine is in the state `SomeState`. Instead, the machine should skip over
`PingEvent` and `PongEvent` (without dropping these events from the queue) and dequeue the next event
that is not being deferred. The attribute `IgnoreEvents` indicates that whenever `UnitEvent` is
dequeued while the machine is in `SomeState`, then the machine should drop `UnitEvent` without invoking
any action.

Coyote also supports specifying invariants through assertions. You can do this by using the `Assert`
method, which accepts as input a predicate that must always hold in that specific program point, e.g.
`this.Assert(k == 0)`, which holds if the integer `k` equals to `0`. These `Assert` statement are
useful for machine-local invariants, i.e., they are about the state of a single machine. For global
invariants, often times using [Monitors](../specifications/overview.md) is easier.

## An example program

The following Coyote program shows a `Client` machine and a `Server` machine that communicate
asynchronously by exchanging `PingEvent` and `PongEvent` events:

```c#
using System;
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;

namespace PingPong {
  class UnitEvent : Event { }
  class PingEvent : Event { }
  class PongEvent : Event { }

  class ConfigEvent : Event {
    public ActorId Target;
    public ConfigEvent(ActorId target) {
      this.Target = target;
    }
  }

  class Server : StateMachine {
    ActorId Client;

    [Start]
    [OnEntry(nameof(InitOnEntry))]
    [OnEventGotoState(typeof(UnitEvent), typeof(Active))]
    class Init : State { }

    void InitOnEntry() {
      this.Client = this.CreateStateMachine(typeof(Client));
      this.SendEvent(this.Client, new ConfigEvent(this.Id));
      this.RaiseEvent(new UnitEvent());
    }

    [OnEntry(nameof(ServerActiveEntry))]
    [OnEventDoAction(typeof(PongEvent), nameof(SendPing))]
    class Active : State { }

    void ServerActiveEntry()
    {
      this.SendPing();
    }

    void SendPing() {
      this.SendEvent(this.Client, new PingEvent());
    }
  }

  class Client : StateMachine {
    ActorId Server;

    [Start]
    [OnEventGotoState(typeof(UnitEvent), typeof(Active))]
    [OnEventDoAction(typeof(ConfigEvent), nameof(Configure))]
    class Init : State { }

    void Configure() {
      this.Server = (this.ReceivedEvent as ConfigEvent).Target;
      this.RaiseEvent(new UnitEvent());
    }

    [OnEventDoAction(typeof(PingEvent), nameof(SendPong))]
    class Active : State { }

    void SendPong() {
      this.SendEvent(this.Server, new PongEvent());
    }
  }

  public class HostProgram {
    static void Main(string[] args) {
      IActorRuntime runtime = MachineRuntime.Create();
      runtime.CreateStateMachine(typeof(Server));
      Console.ReadLine();
    }
  }
}
```

In the above example, the program starts by creating an instance of the `Server` machine. The implicit
constructor of each machine initializes the machine internals, including the event queue, a set of
available states, and a map from events to event-handlers per state.

After the `Server` machine has initialized, the Coyote runtime executes the `OnEntry` action of the
initial (`Init`) state of `Server`, which first creates an instance of the `Client` machine, then sends
the event `ConfigEvent` to the `Client` machine, with the `this.Id` machine handle as a payload, and
then raises the event `UnitEvent`. As mentioned earlier, when a machine calls `RaiseEvent`,
it bypasses the queue and first handles the raised event. In this case, the `Server` machine
handles `UnitEvent` by transitioning to the `Active` state.

`Client` starts executing (asynchronously) when it is created by `Server`. The `Client` machine stores
the received payload (which is a handle to the `Server` machine) in the `Server` field, and then raises
`UnitEvent` to transition to the `Active` state. In the new state, `Client` calls the `SendPing` method
to send a `PingEvent` event to `Server`. In turn, the `Server` machine dequeues `PingEvent` and handles
it by sending a `PongEvent` event to `Client`, which subsequently responds by sending a new `PingEvent`
event to `Server`. This asynchronous exchange of `PingEvent` and `PongEvent` events continues
indefinitely.

## Entry point to a Coyote program

To create and start executing Coyote machines, you need to initialize the Coyote state machine runtime
inside your C# process (typically in the `Main` method). An example of this is the following:

```c#
using Microsoft.Coyote;
using Microsoft.Coyote.Actors;
public class HostProgram {
  static void Main(string[] args) {
    IActorRuntime runtime = ActorRuntimeFactory.Create();
    runtime.CreateStateMachine(typeof(Server));
    Console.ReadLine();
  }
}
```

The developer must first import the Coyote runtime library (`Microsoft.Coyote.dll`), then create a
`runtime` instance (of type `IActorRuntime`), and finally invoke the `CreateStateMachine` method of
`runtime` to instantiate the first Coyote machine (`Server` in the above example).

The `CreateStateMachine` method accepts as a parameter the type of the machine to be instantiated, and
returns an object of the `ActorId` type, which contains a handle to the created Coyote machine.
Because `CreateStateMachine` is an asynchronous method, we call the `Console.ReadLine` method, which pauses
the main thread until a console input has been given, so that the host C# program does not exit
prematurely.

The `IActorRuntime` interface also provides the `SendEvent` method for sending events to a Coyote
machine from outside a machine. This method accepts as parameters an object of type `ActorId`, an
event and an optional payload. Although you have to use `CreateStateMachine` and `SendEvent` to interact
with a machine from outside a machine, the opposite is straightforward, as it only requires reading
writing/calling an object from a machine.
