---
layout: reference
title: State machines
section: learn
permalink: /learn/programming-models/actors/state-machines
---

## State machines

A Coyote state machine is a special type of `Actor` that inherits from the `StateMachine` class
which lives in the `Microsoft.Coyote.Actors` namespace.  A state machine adds `State` semantics
with explicit information about how `Events` can trigger `State` changes in a `StateMachine`.
You can write a state machine version of the `Server` class shown in [Programming model: asynchronous actors](/coyote/learn/programming-models/actors/overview) like this:

```c#
class ReadyEvent : Event { }

class Server : StateMachine
{
    ActorId ClientId;

    [Start]
    [OnEntry(nameof(InitOnEntry))]
    [OnEventGotoState(typeof(ReadyEvent), typeof(Active))]
    class Init : State { }

    Transition InitOnEntry()
    {
        Console.WriteLine("Server Creating client");
        this.ClientId = this.CreateActor(typeof(Client));
        Console.WriteLine("Server sending ping event to client");
        this.SendEvent(this.ClientId, new PingEvent(this.Id));
        return this.RaiseEvent(new ReadyEvent());
    }

    [OnEventDoAction(typeof(PongEvent), nameof(HandlePong))]
    class Active : State { }

    void HandlePong()
    {
        Console.WriteLine("Server received pong event while in the {0} state", this.CurrentState.Name);
    }
}

```

The above class declares a state machine named `Server`. The `StateMachine` class itself inherits from `Actor`
so state machines are also actors and, of course, state machines are also normal C# classes.
`Actors` and `StateMachines` can talk to each other by sending events.
State machines in Coyote must also declare one or more _states_ where a state is
a nested class that inherits from the coyote `State` class which lives in the `Microsoft.Coyote.Actors` namespace.

The above code snippet declares two states in the `Server` machine: `Init` and `Active`. You must use
the `Start` attribute to declare one of the states the _initial_ state, which will be the first state that the machine
will transition to upon initialization. In this example, the `Init` state has been declared as the
initial state of `Server`. A state declaration can optionally be decorated with a number of state-specific attributes, as
seen in the `[Init]` state:

```c#
    [OnEntry(nameof(InitOnEntry))]
    [OnEventGotoState(typeof(ReadyEvent), typeof(Active))]
```

The `OnEntry` attribute denotes an action that will be executed when the
machine transitions to the `Init` state, while the `OnExit` attribute denotes
an action that will be executed when the machine leaves the state. Actions in Coyote are
C# methods that take either no input parameters or a single input parameter of type `Event`,
and return either `void`, `async Task`, `Transition` or `async Task<Transition>`.

Notice that the `InitOnEntry` method declared above is similar to the original `OnInitializeAsync`
method on the `Server` Actor, except it returns a `Transition` object instead of void.
`Transition` is a special concept used by state machines to manage the changing of the
current state.  In this case a `RaiseEvent` call is used to signal that the `Init` state
is done and we are now ready to transition to the `Active` state:

```c#
return this.RaiseEvent(new ReadyEvent());
```

The `RaiseEvent` call is used to send an event to yourself.
Similar to `SendEvent`, when a machine raises an event on itself, it is also queued so that
the method can continue execution until the `Transition` result from `RaiseEvent` is returned
by the `InitOnEntry` method. When the current machine action finishes, instead of dequeuing the next event
from the inbox (if there is one), the machine immediately handles the raised event
(prioritizing it over the inbox) by performing the raise event transition declared in the `OnEventGotoState` attribute.
This prioritization is important in the above case, because it guarantees that the Server will transition to the
`Active` state before the `PongEvent` is received from the `Client`.

Since `Transition` represents a change of state is about to happen, a given method can only
return one such `Transition`, which means internally the logic must be written in a way that only
results in the creation of one `Transition` per action.  This means it is illegal to do two
`RaiseEvent` calls, or a `RaiseEvent` and a `Goto<State>` and so on.  This may seem limiting, but
these limits are designed to keep your code clean and simple where all your state transitions
are easy to understand and test.

The attribute `OnEventGotoState` indicates that if the state machine receives the `ReadyEvent` event
while it is currently in the `Init` state, it will automatically handle the `ReadyEvent` by exiting
the `Init` state and transitioning to the `Active` state.
All this happens as a result of the simple `RaiseEvent` call and the `OnEventGotoState` attribute.
The Coyote state machine programming model takes a lot of tedium out of managing explicit state machinery.
If you ever find yourself building your own state machinery, then you definitely should consider using the
Coyote state machine class instead.
Note that on a given `State` of a state machine, you can only define one handler for a given event type.

When you run this new `StateMachine` based `Server` you will see the same output as before,
with the addition of the state information from `HandlePong`:
```
Server Creating client
Server sending ping event to client
Client handling ping
Client sending pong to server
Server received pong event while in the Active state
```

Unlike Actors which declare the events they can receive at the class level, `StateMachines` must declare
this information on the `States`.  This gives `StateMachines` more fine grained control, for example,
perhaps you want your state machine to only be able to receive a certain type of event when it is in a particular state.
In an Actor you would need to check this yourself and throw an exception, whereas in a state machine this
is more declarative and is enforced by the Coyote runtime; the Coyote runtime will report an error if an
event is received on a `State` of a `StateMachine` that was not expecting to receive that event.
This reduces the amount of tedious book keeping code you need to write, and keeps your code even cleaner.

### Goto, Push and Pop states

Besides `RaiseEvent`, state machine event handlers can request an explicit state change rather than
depending on `OnEventGotoState` attributes.  The following example shows how Goto can be used:

```c#
    Transition InitOnEntry()
    {
        Console.WriteLine("Server Creating client");
        this.ClientId = this.CreateActor(typeof(Client));
        Console.WriteLine("Server sending ping event to client");
        this.SendEvent(this.ClientId, new PingEvent(this.Id));
        return this.GotoState<Active>();
    }
```

State machines can also `push` and `pop` states, effectively creating a stack of active states.
Use `PushState` to push a new state:

```c#
    return this.PushState<Active>();
```

This will push the `Active` state on the stack, but it will also allow any events declared on the `Init`
state to also be received and handled.  The Active state could then choose to return to the `Init`
state using a `PopState` call:

```c#
    Transition HandlePong()
    {
        Console.WriteLine("Server received pong event while in the {0} state", this.CurrentState.Name);
        return this.PopState();
    }
```

Note that this does not result in `InitOnEntry` being called again, because with `push` it never
actually exited the `Init` state.  But if you use `GotoState` instead, and add `GotoState<Init>` to the
`HandlePong` method then `InitOnEntry` will be called which then creates an infinite loop of ping pong events.

The `push` and `pop` feature is considered an advanced feature of state machines.
It is designed to help you reuse some of your event handling code, and is a analogous to how
virtual methods with overrides work in object oriented programming where the override can reuse
the base implementation.  Similarly a pushed state is reusing all the event handlers of the previous states
that are on the stack of active states.

### Deferring and ignoring events

Coyote also provides the capability to _defer_ and _ignore_ events while in a particular state:

```c#
[DeferEvents(typeof(PingEvent), typeof(PongEvent))]
[IgnoreEvents(typeof(ReadyEvent))]
class SomeState : State { }
```

The attribute `DeferEvents` indicates that the `PingEvent` and `PongEvent` events should not be
dequeued while the machine is in the state `SomeState`. Instead, the machine should skip over
`PingEvent` and `PongEvent` (without dropping these events from the queue) and dequeue the next event
that is not being deferred.  Note that when a state decides to defer an event a subsequent pushed state can choose to
receive that event if it wants to, but if the pushed state chooses not to receive the event then it is not an error.

The attribute `IgnoreEvents` indicates that whenever `ReadyEvent` is dequeued while the machine is
in `SomeState`, then the machine should drop `ReadyEvent` without invoking any action.
Note that when a state decides to ignore an event a subsequent pushed state can choose to
receive that event if it wants to, but if the pushed state chooses not to receive the event then it is not an error
and the event will be ignored.
