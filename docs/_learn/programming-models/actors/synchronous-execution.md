---
layout: reference
title: Synchronous execution of actors
section: learn
permalink: /learn/programming-models/actors/synchronous-execution
---

## Synchronous execution of actors

Coyote offers the following APIs (and overloads) for synchronous execution of actor creation and event sending.

```c#
Task<ActorId> CreateActorAndExecuteAsync(Type type, Event e = null, Guid opGroupId = default);
Task<bool> SendEventAndExecuteAsync(ActorId target, Event e, Guid opGroupId = default, SendOptions options = null);
```

Both of these are `async` methods and must be `awaited` by the caller. The method
`CreateActorAndExecuteAsync` when awaited, returns only when the newly created actor becomes idle. That
is, it creates the actor, passes it the initial event `e`, starts executing the actor, and then
waits for the actor to become idle. A actor is idle when it is no events can be received from its inbox.
The method `SendEventAndExecuteAsync` when awaited has two possible executions. If the `target`
actor is running (i.e., it is not idle) then the method only enqueues the event and returns
immediately with the return value `false`. If the `target` actor was idle then the method enqueues
the event (which causes the `target` actor to start executing) and waits until the actor becomes
idle again. In this case, the method returns `true` indicating that the event has been processed by the
`target` actor.

Note that this is only one level deep.  If the event handler invoked by the actor creation or event handling
decodes to send more events to other actors, then the above synchronous methods do **not** wait for that
additional work to be completed, unless those events are sent to `this.Id` which does stop the actor from
becoming idle.

Another type of synchronous execution is provided by the `Actor` method `ReceiveEventAsync`.
This method allows an actor to wait for a given type of event to be received, and can even provide a predicate
that conditionally receives the event.  This means instead of declaring an event handler like this which
means you can receive this event any time and call HandlePoing:

```
    [OnEventDoAction(typeof(PongEvent), nameof(HandlePong))]
```
You can instead explicitly receive the event in a specific place in your actor like this so that
the event is not generally handled at other times:
```
   Event e = await this.ReceiveEventAsync(typeof(PongEvent));
   HandlePong(e);
```

A second overload of `ReceiveEventAsync` allows you to provide a list of event types each with their own predicates.
This version of the method receives the first matching event.

## Potential deadlocks with ReceiveEventAsync

You should be careful with the use of `ReceiveEventAsync` when using `CreateActorAndExecuteAsync` and `SendEventAndExecuteAsync`.
In the absence of `ReceiveEventAsync`, the semantics of these methods guarantee that the program
cannot deadlock. With a `ReceiveEventAsync` the following situation can occur. Let's suppose
there are two actors `A` and `B` and the latter is idle. Then actor `A` does `SendEventAndExecuteAsync` to pass an event `e` to `B`. Because `B` was idle, `A`
will wait until `B` becomes idle again. But if `B` executes a `ReceiveEventAsync` while
processing the event `e`, expecting another event from `A` then the program deadlocks.
(Blocking on a `ReceiveEventAsync` is not considered as being idle.)

## Extracting information from an actor

Suppose there is a Coyote actor `M1` that holds some information that we are interested in grabbing.
The usual way of getting this information would be to `SendEvent` a "get" message to `M1` and then
wait for its response via `ReceiveEventAsync`. However, a `ReceiveEventAsync` can only be
executed by an actor. How do you get the result outside the context of an actor, from, say,
a static method? One option is to use these `*AndExecuteAsync` methods. We define a trampoline
actor `T` that we create from our static method via `CreateActorAndExecuteAsync`.
The trampoline actor, in its `OnEntry` method of the start state (which is called immediately
when a actor is created), sends the "get" message to `M1` and waits for its response
via `ReceiveEventAsync`. Once it gets the response, it can stash the result in an object
that can be safely shared with the calling static method without any race conditions.

You can use a [SharedRegister](/coyote/learn/advanced/object-sharing), which will rule out race
conditions as well, but this still requires a separate protocol to know when the result has been made
available.

## Running an actor synchronously

Another programming pattern is to drive an actor synchronously. The program can do
`CreateActorAndExecuteAsync` to create the actor, then repeatedly do
`SendEventAndExecuteAsync` to make the actor process events one after another. Let's consider an
example. Suppose that we need to define a actor `M` that is easily decomposed into two smaller
actors `M1` and `M2`. For each incoming event, `M` decides to run one of the two actors;
there is no need to run them in parallel. In this case, you only need to code up the smaller
actors `M1` and `M2`. The actor `M` can be a simple wrapper. On instantiation, `M`
creates the two sub-state actors as follows.

```c#
ActorId m1 = this.CreateActorAndExecuteAsync(typeof(M1), ...);
ActorId m2 = this.CreateActorAndExecuteAsync(typeof(M2), ...);
```

When `M` receives an event `e`, it will choose to run the appropriate actor as follows.

```c#
if (SomeCondition(e))
{
   bool b1 = await this.SendEventAndExecuteAsync(m1, e);
   this.Assert(b1);
}
else
{
   bool b2 = await this.SendEventAndExecuteAsync(m2, e);
   this.Assert(b2);
}
```

Note that the two assertions above are guaranteed to never fail because the `m1` and `m2` actors
are always left in an idle state by `M`, provided that`M` never gives out the `ActorId` of `m1` or `m2` to
any other actors.
