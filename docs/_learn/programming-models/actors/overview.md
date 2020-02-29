---
layout: reference
title: Machines programming model
section: learn
permalink: /learn/programming-models/actors/overview
---

## Programming model: asynchronous actors

The _asynchronous actors_ programming model of Coyote is an actor-based programming model that
encourages a message passing (or event based) programming model where all asynchronous actions
happen by sending asynchronous events from one actor to another.  This model is similar to
how real people interact asynchronously via email.

Each Coyote actor has an inbox for events, event handlers, as well as normal class fields and methods.
Actors run concurrently with respect to each other, but individually they handle their input queue
in a sequential way.  When an event arrives, the actor dequeues that event from the input queue and
handles it by executing a sequence of operations. Each operation might update a field, create a new
actor, or send an event to another actor. In Coyote, creating actors and sending events are both
non-blocking operations. In the case of a send operation the message (or event) is simply enqueued into the
input queue of the target actor and, most importantly, it does not wait for the message to be processed
at the target before returning control to the sender.

The actor model also provides a specialized type of actor called a [StateMachine](/coyote/learn/programming-models/actors/state-machines).
State machines are actors, but also have explicit states, and state transitions.
Every object oriented class that has member variables is really also just a state machine where
that state is updated as methods are called, but sometimes this gets really complicated and hard to test.
Formal state machines help you model your states more explicitly and the [coyote tester](/coyote/learn/tools/testing) can help you
find bugs by exploring different state transitions using information you provide declaring how
various types of events causes those state transitions.

### Declaring and creating actors

An actor based program in Coyote is a normal .NET program that also uses the `Actor` or `StateMachine`
base classes from the `Microsoft.Coyote.Actors` namespace, as well as the `Event` base class from the `Microsoft.Coyote` namespace.
Actors can be declared in the following way:

```c#
using Microsoft.Coyote.Actors;

class Client : Actor { ... }

class Server : Actor { ... }
```
The above code declares two actors named `Client` and `Server`.  Being a C# class you can also declare
fields, properties and methods.

An actor can create an instance of another actor and send Events using the following `Actor` methods:

```c#
  ActorId clientId = this.CreateActor(typeof(Client));
  this.SendEvent(this.ClientId, new PingEvent());
```

When an event is being sent, it is enqueued in the event queue of the target actor.  The coyote
runtime will at some point dequeue the received event, and allow the target actor to handle it
asynchronously.

All actors in Coyote have an associated unique `ActorId` which identifies a specific instance of that type.
Note that Coyote never gives you the actual object reference for an actor instance.
This ensures your code does not get too tightly coupled.

By limiting yourself to the Coyote API's for interacting with an actor, you also get all the benefits
of the `coyote tester` in terms of understanding more deeply how to test all asynchronous interactions and
ensure your specifications are maintained correctly.  There is a lot of literature on actor models
that explain in more depth the importance of this message passing programming model which is
especially popular in the world of distributed systems.  Event based programming is also popular in
User Interface development and even shows up in low level embedded systems.  It is a powerful
tool for solving the tangled web of complexity that happens with less disciplined architectures.

### Starting a Coyote actor-based program

To create the first instance of an `Actor` you need to initialize the Coyote actor runtime
inside your C# process (typically in the `Main` method). An example of this is the following:

```c#
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.TestingServices;

class Program
{
    static void Main(string[] args)
    {
        IActorRuntime runtime = ActorRuntimeFactory.Create();
        Execute(runtime);
        Console.ReadLine();
    }

    [Test]
    public static void Execute(IActorRuntime runtime)
    {
        ActorId serverId = runtime.CreateActor(typeof(Server));
    }
}
```

You must first import the Coyote runtime library (`Microsoft.Coyote.dll`), which you can get from
[Nuget](https://www.nuget.org/packages/Microsoft.Coyote/), then create a `runtime` instance (of type `IActorRuntime`)
which you pass to a `[Test]` method.

The test method named `Execute` will be the entry point that is used during testing of your Coyote program.
In this case it simply invokes the `CreateActor` method of the `runtime` to instantiate the first Coyote actor
(of type `Server` in the above example).

The `CreateActor` method accepts as a parameter the type of the actor to be instantiated, and
returns the `ActorId` representing that actor instance and this bootstraps a series of asynchronous events
that handle initialization of that actor and any operations it performs during initialization.

Because `CreateActor` is an asynchronous method, we call the `Console.ReadLine` method, which pauses
the main thread until some console input has been given, so that the host C# program does not exit
prematurely.

The `IActorRuntime` interface also provides the `SendEvent` method for sending events to a Coyote
actor. This method accepts as parameters an object of type `ActorId` and an event object.  It also
has a couple more advanced parameters which you don't need to worry about right now.

An event can be created by sub-classing from `Microsoft.Coyote.Event`:

```c#
using Microsoft.Coyote;

class PingEvent : Event
{
    public readonly ActorId Caller;

    public PingEvent(ActorId caller)
    {
        this.Caller = caller;
    }
}

class PongEvent : Event { }
```

An event can contain members of any type (including scalar values or references to object) and when an event
is sent to a target actor there is no deep-copying of those members, for performance reasons.
The target actor will be able to see the Event object and cast it to a specific type to extract the information it needs.

Now you can write a complete actor, declaring what type of events it can handle, and defining some initialization behavior
and an event handler:

```c#
[OnEventDoAction(typeof(PongEvent), nameof(HandlePong))]
class Server : Actor
{
    ActorId ClientId;

    protected override Task OnInitializeAsync(Event initialEvent)
    {
        Console.WriteLine("Server creating client");
        this.ClientId = this.CreateActor(typeof(Client));
        Console.WriteLine("Server sending ping event to client");
        this.SendEvent(this.ClientId, new PingEvent(this.Id));
        return base.OnInitializeAsync(initialEvent);
    }

    void HandlePong()
    {
        Console.WriteLine("Server received pong event");
    }
}
```

This `Server` is an `Actor` that can receive `PongEvents`.
During initialization it creates a new `Client` actor and sends a `PingEvent` to it containing `this.Id` which is the `ActorId` of the `Server`.
It handles the `PongEvent` by calling the `HandlePong` event handler method which prints a message.

An event handler controls how a machine _reacts_ to a received event.
It is clearly just a method so you can do anything there, including creating one
or more actor instances, sending one or more events, updating some private state or invoking some 3rd party
library.  Notice the `HandlePong` event handler has no parameters.  It could also choose to specify one parameter of type `Event`
but in this case it doesn't care about the specific type of event it received here.

To complete this Coyote program, you can provide the following implementation of the `Client` actor created by the above `Server`:

```c#
[OnEventDoAction(typeof(PingEvent), nameof(HandlePing))]
class Client : Actor
{
    public void HandlePing(Event e)
    {
        PingEvent pe = (PingEvent)e;
        Console.WriteLine("Client handling ping");
        Console.WriteLine("Client sending pong to server");
        this.SendEvent(pe.Caller, new PongEvent());
    }
}
```

This `Client` is an `Actor` that can receive `PingEvents`. It handles the `PingEvent` by calling the `HandlePing` method.
Notice in this case the `HandlePing` method chooses to receive the `Event` as a parameter.
It then casts the incoming `Event` to type `PingEvent` and uses the `Caller` field
to send a `PongEvent` back to the caller (which is the id of the Server in this case).

Notice also that `HandlePing` is defined as an `async Task` method.  This is shown here just for completeness.  Async handlers are allowed
so that you can call external async systems in your production code.  During testing however, you should not use `Task.Delay` or `Task.Run`
in your event handlers.  It is ok to have truly async behavior in production, but
at test time the `coyote tester` wants to know about and control all async behavior of your actor.
If it detects some uncontrolled async behavior an error will be reported.

When you now run the complete program you should see this output printed to the Console Window:
```
Server creating client
Server sending ping event to client
Client handling ping
Client sending pong to server
Server received pong event
```

The `CreateActor` and `SendEvent` methods are non-blocking. The
Coyote runtime will take care of all the underlying concurrency using the Task Parallel Library,
which means that you do not need to explicitly create and manage tasks. However, you must be
careful not to share data between actors because accessing that shared data from multiple actors at once
could lead to race conditions.

You can reduce race conditions in your code if you use events to _transfer_ data from one actor to another.
But since it is a reference model without deep copy semantics, you can actually share data between actors if you really need to.
See [sharing objects](/coyote/learn/advanced/object-sharing) for more information in this advanced topic.

### Assertions

Coyote also supports specifying invariants through assertions. You can do this by using the `Assert`
method, which accepts as input a predicate that must always hold in that specific program point, e.g.
`this.Assert(k == 0)`, which holds if the integer `k` equals to `0`. These `Assert` statement are
useful for local invariants, i.e., they are about the state of a single actor. For global
invariants it is recommended that you use [Monitors](/coyote/learn/specifications/overview).

To see a full working example of an `Actor` based program see the [Hello World Actors](/coyote/learn/tutorials/hello-world-tasks) tutorial.
