---
layout: reference
title: State machines
section: learn
permalink: /learn/programming-models/actors/state-machines
---

## State machines

A Coyote state machine is a special type of `Actor` that inherits from the `StateMachine` class
which lives in the `Microsoft.Coyote.Actors` namespace. A state machine adds `State` semantics with
explicit information about how `Events` can trigger `State` changes in a `StateMachine`. You can
write a state machine version of the `Server` class shown in [Programming model: asynchronous
actors](overview) like this:

```c#
class ReadyEvent : Event { }

class Server : StateMachine
{
    ActorId ClientId;

    [Start]
    [OnEntry(nameof(InitOnEntry))]
    [OnEventGotoState(typeof(ReadyEvent), typeof(Active))]
    class Init : State { }

    void InitOnEntry()
    {
        Console.WriteLine("Server Creating client");
        this.ClientId = this.CreateActor(typeof(Client));
        Console.WriteLine("Server sending ping event to client");
        this.SendEvent(this.ClientId, new PingEvent(this.Id));
        this.RaiseEvent(new ReadyEvent());
    }

    [OnEventDoAction(typeof(PongEvent), nameof(HandlePong))]
    class Active : State { }

    void HandlePong()
    {
        Console.WriteLine("Server received pong event while in the {0} state", this.CurrentState.Name);
    }
}
```

The above class declares a state machine named `Server`. The `StateMachine` class itself inherits
from `Actor` so state machines are also actors and, of course, state machines are also normal C#
classes. `Actors` and `StateMachines` can talk to each other by sending events. State machines in
Coyote must also declare one or more _states_ where a state is a nested class that inherits from the
coyote `State` class which lives in the `Microsoft.Coyote.Actors` namespace. The nested state
classes can be private.

The above code snippet declares two states in the `Server` machine: `Init` and `Active`. You must
use the `Start` attribute to declare one of the states the _initial_ state, which will be the first
state that the machine will transition to upon initialization. In this example, the `Init` state has
been declared as the initial state of `Server`. A state declaration can optionally be decorated with
a number of state-specific attributes, as seen in the `[Init]` state:

```c#
[OnEntry(nameof(InitOnEntry))]
[OnEventGotoState(typeof(ReadyEvent), typeof(Active))]
```

The `OnEntry` attribute denotes an action that will be executed when the machine transitions to the
`Init` state, while the `OnExit` attribute denotes an action that will be executed when the machine
leaves the state. Actions in Coyote are C# methods that take either no input parameters or a single
input parameter of type `Event`, and return either `void`, `async Task`. `OnExit` actions cannot
receive an Event argument. Note that Coyote actions are also referred to as event handlers, however
these should not be confused with the `System.EventHandler`, which have a different prototype.

Notice that the `InitOnEntry` method declared above is similar to the original `OnInitializeAsync`
method on the `Server` Actor. The `RaiseEvent` call is used to trigger the state transition defined
in the `OnEventGotoState` custom attribute, in this case it is ready to transition to the `Active`
state:

```c#
this.RaiseEvent(new ReadyEvent());
```

The `RaiseEvent` call is used to send an event to yourself. Similar to `SendEvent`, when a machine
raises an event on itself, it is also queued so that the method can continue execution until the
`InitOnEntry` method is completed. When control returns to the coyote runtime, instead of dequeuing
the next event from the inbox (if there is one), the machine immediately handles the raised event
(so raised events are prioritized over any events in the inbox). This prioritization is important in
the above case, because it guarantees that the Server will transition to the `Active` state before
the `PongEvent` is received from the `Client`.

The attribute `OnEventGotoState` indicates that if the state machine receives the `ReadyEvent` event
while it is currently in the `Init` state, it will automatically handle the `ReadyEvent` by exiting
the `Init` state and transitioning to the `Active` state. This saves you from having to write that
trivial event handler.

All this happens as a result of the simple `RaiseEvent` call and the `OnEventGotoState` attribute.
The Coyote state machine programming model takes a lot of tedium out of managing explicit state
machinery. If you ever find yourself building your own state machinery, then you definitely should
consider using the Coyote state machine class instead. Note that on a given `State` of a state
machine, you can only define one handler for a given event type.

When you run this new `StateMachine` based `Server` you will see the same output as before, with the
addition of the state information from `HandlePong`:

```
Server Creating client
Server sending ping event to client
Client handling ping
Client sending pong to server
Server received pong event while in the Active state
```

Unlike Actors which declare the events they can receive at the class level, `StateMachines` can also
declare this information on the `States`. This gives `StateMachines` more fine grained control, for
example, perhaps you want your state machine to only be able to receive a certain type of event when
it is in a particular state. In an Actor you would need to check this yourself and throw an
exception, whereas in a state machine this is more declarative and is enforced by the Coyote
runtime; the Coyote runtime will report an error if an event is received on a `State` of a
`StateMachine` that was not expecting to receive that event. This reduces the amount of tedious book
keeping code you need to write, and keeps your code even cleaner.

For an example of a state machine in action see the [state machine demo](state-machine-demo).

### Only one Raise* operation per action

There is an important restriction on the use of the following. Only one of these operations can be
queued up per event handling action:

```c#
RaiseEvent
RaiseGotoStateEvent
RaisePushStateEvent
RaisePopStateEvent
RaiseHaltEvent
```

A runtime `Assert` will be raised if you accidentally try and do two of these operations in a single
action. For example, this would be an error because you are trying to do two `Raise` operations in
the `InitOnEntry` action:

```c#
void InitOnEntry()
{
    this.RaiseGotoStateEvent<Active>();
    this.RaiseEvent(new TestEvent());
}
```

### Goto, push and pop states

Besides `RaiseEvent`, state machine event handlers can request a state change in code rather than
depending on `OnEventGotoState` attributes. This allows _conditional_ goto operations as shown in
the following example:

```c#
void InitOnEntry()
{
    if (this.Random())
    {
        this.RaiseGotoStateEvent<Active>();
    }
    else
    {
        this.RaiseGotoStateEvent<Busy>();
    }
}
```

State machines can also push and pop states, effectively creating a stack of active states. Use
`[OnEventPushState(...)]` or `RaisePushStateEvent` in code to push a new state:

```c#
this.RaisePushStateEvent<Active>();
```

This will push the `Active` state on the stack, but it will also inherit some actions declared on
the `Init` state. The `Active` state pop itself off the stack, returning to the `Init` state using
a `RaisePopStateEvent` call:

```c#
void HandlePong()
{
    Console.WriteLine("Server received pong event while in the {0} state", this.CurrentState.Name);
    this.RaisePopStateEvent();  // pop the current state off the stack of active states.
}
```

Note that this does not result in the `OnEntry` method being called again, because you never
actually exited the `Init` state in this case. But if you used `RaiseGotoStateEvent` instead of
`RaisePushStateEvent` and `RaisePopStateEvent` then `InitOnEntry` will be called again, and that
would creates an infinite series of ping-pong events.

The push and pop feature is considered an advanced feature of state machines. It is designed to help
you reuse some of your event handling code, where you can put "common event handling" in lower
states and more specific event handling in pushed states. If an event handler is defined more than
once in the stack, the one closest to the top of the stack is used.

### Deferring and ignoring events

Coyote also provides the capability to _defer_ and _ignore_ events while in a particular state:

```c#
[DeferEvents(typeof(PingEvent), typeof(PongEvent))]
[IgnoreEvents(typeof(ReadyEvent))]
class SomeState : State { }
```

The attribute `DeferEvents` indicates that the `PingEvent` and `PongEvent` events should not be
dequeued while the machine is in the state `SomeState`. Instead, the machine should skip over
`PingEvent` and `PongEvent` (without dropping these events from the queue) and dequeue the next
event that is not being deferred. Note that when a state decides to defer an event a subsequent
pushed state can choose to receive that event if it wants to, but if the pushed state chooses not to
receive the event then it is not an error and it remains deferred.

The attribute `IgnoreEvents` indicates that whenever `ReadyEvent` is dequeued while the machine is
in `SomeState`, then the machine should drop `ReadyEvent` without invoking any action. Note that
when a state decides to ignore an event a subsequent pushed state can choose to receive that event
if it wants to, but if the pushed state chooses not to receive the event then it is not an error and
the event will be ignored and dropped.

### Default events

State machines support an interesting concept called _default events_. A state can request that
something be done by default when there is _nothing else_ to do.

```c#
[OnEventDoAction(typeof(DefaultEvent), nameof(OnIdle))]
class Idle : State { }

public void OnIdle()
{
    Console.WriteLine("OnIdle");
}
```

The Coyote runtime will invoke this action handler when `Idle` is the current active state and the
state machine has nothing else to do (the inbox has no events that can be processed). If nothing
else happens, (no other actionable events are queued on this state machine) then the `OnIdle` method
will be called over and over until something else changes. It us more efficient to use
`CreatePeriodicTimer` for low priority work.

Default events can also invoke goto, and push state transitions, which brings up an interesting case
where you can actually implement an infinite ping pong using the following:

```c#
internal class PingPongMachine : StateMachine
{
    [Start]
    [OnEntry(nameof(OnPing))]
    [OnEventGotoState(typeof(DefaultEvent), typeof(Pong))]

    public class Ping : State { }

    public void OnPing()
    {
        Console.WriteLine("OnPing");
    }

    [OnEntry(nameof(OnPong))]
    [OnEventGotoState(typeof(DefaultEvent), typeof(Ping))]
    public class Pong : State { }

    void OnPong()
    {
        Console.WriteLine("OnPong");
    }
}
```

The difference between this and a timer based ping-pong is that this will run as fast as the Coyote
runtime can go. So you have to be careful using `DefaultEvents` like this as it could use up a lot
of CPU time.

### WildCard events

State machines also support a special `WildcardEvent` which acts as a special pattern matching event
that matches all event types. This means you can create generic actions, or state transitions as a
result of receiving any event (except the `DefaultEvent`).

The following example shows how the `WildcardEvent` can be used:

```c#
internal class WildMachine : StateMachine
{
    [Start]
    [OnEntry(nameof(OnInit))]
    [OnEventGotoState(typeof(WildCardEvent), typeof(CatchAll))]

    public class Init : State { }

    public void OnInit()
    {
        Console.WriteLine("Entering state {0}", this.CurrentStateName);
    }

    [OnEntry(nameof(OnInit))]
    [OnEntry(nameof(OnCatchAll))]
    [OnEventDoAction(typeof(WildCardEvent), nameof(OnCatchAll))]
    public class CatchAll : State { }

    void OnCatchAll(Event e)
    {
        Console.WriteLine("Catch all state caught event of type {0}", e.GetType().Name);
    }
}
```

The client of this state machine can send any event it wants and it will cause a transition to the
`CatchAll` state where it will be handled by the `OnCatchAll` method. For example:

```c#
class X : Event { };
var actor = runtime.CreateActor(typeof(WildMachine));
runtime.SendEvent(actor, new X());
```

And the output of this test is:

```
Entering state Init
Entering state CatchAll
Catch all state caught event of type X
```

## Precise semantics

There is a lot of interesting combinations of things that you can do with `DeferEvents`,
`IgnoreEvents`, `OnEventDoAction`,  `OnEventGotoState` or `OnEventPushState` and  `WildcardEvent`.
The following gives the precise semantics of these operations with regards to push and pop.

First of all only one action per specific event type can be defined on a given `State`, so the
following would be an error:

```c#
[DeferEvents(typeof(E1), typeof(E2))]
[OnEventDoAction(typeof(E1), nameof(HandleE1))]
class SomeState : State { }
```

Because the `E1` has both a `DeferEvents` and `OnEventDoAction` defined on the same state.

Second, a pushed state inherits `DeferEvents`, `IgnoreEvents`, `OnEventDoAction` actions from all
previous states on the active state stack, but it **does not** inherit `OnEventGotoState` or
`OnEventPushState` actions.

If multiple states on the stack of active states define an action for a specific event type then the
action closest to the top of the stack takes precedence. For example:

```c#
[DeferEvents(typeof(E1))]
[OnEventPushState(typeof(E1), typeof(S2))]
class A : State { }

[OnEventDoAction(typeof(E1), nameof(HandleE1))]
class B : State { }
```

In state `B` the `OnEventDoAction` takes precedence over the inherited `DeferEvents` for event `E1`.

On a given state actions defined for a specific event type take precedence over actions involving
`WildcardEvent` but a pushed state can override a specific event type actions with a
`WildcardEvent`.

If an event cannot be handled by a pushed state then that state is automatically popped so handling
can be attempted again on the lower states. If this auto-popping pops all states then an unhandled
event error is raised.
