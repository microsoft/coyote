---
layout: reference
title: Synchronous execution of machines
section: learn
permalink: /learn/programming-models/machines/synchronous-execution
---

## Synchronous execution of machines

Coyote offers the following APIs (and overloads) for synchronous execution.

```c#
Task<ActorId> CreateMachineAndExecuteAsync(Type type, Event e = null, Guid opGroupId = default);
Task<bool> SendEventAndExecuteAsync(ActorId target, Event e, Guid opGroupId = default, SendOptions options = null);
```

Both of these are `async` methods and must be `awaited` by the caller. The method
`CreateMachineAndExecute` when awaited, returns only when the newly created machine becomes idle. That
is, it creates the machine, passes it the initial event `e`, starts executing the machine, and then
waits for the machine to become idle. (A machine is idle when it is blocked on its inbox for receiving
events.) The method `SendEventAndExecute` when awaited has two possible executions. If the `target`
machine is running (i.e., it is not idle) then the method only enqueues the event and returns
immediately with the return value `false`. If the `target` machine was idle then the method enqueues
the event (which causes the `target` machine to start executing) and waits until the machine becomes
idle again. In this case, the method returns `true` indicating that the event has been processed by the
`target` machine.

## Potential deadlocks with the receive API

You should be careful with the use of `Receive` when using these methods. In the absence of `Receive`,
the semantics of these methods guarantee that the program cannot deadlock. With a `Receive` the
following situation can occur. Let's suppose there are two machines `A` and `B` and the latter is idle.
Then machine `A` does `SendEventAndExecuteAsync` to pass an event `e` to `B`. Because `B` was idle, `A`
will wait until `B` becomes idle again. But if `B` executes a `Receive` while processing the event `e`,
expecting another event from `A` then the program deadlocks. (Blocking on a `Receive` is not considered
as being idle.)

## Extracting information from a machine

Suppose there is a Coyote machine `M1` that holds some information that we are interested in grabbing.
The usual way of getting this information would be to `Send` a "get" message to `M1` and then wait for
its response via `Receive`. However, a `Receive` can only be executed by a machine. How do you get the
result outside the context of a machine, from, say, a static method? One option is to use these
`AndExecuteAsync` methods. We define a trampoline machine `T` that we create from our static method via
`CreateMachineAndExecuteAsync`. The trampoline machine, in its `OnEntry` method of the start state
(which is called immediately when a machine is created), sends the "get" message to `M1` and waits for
its response via `Receive`. Once it gets the response, it can stash the result in an object that can be
safely shared with the calling static method without any race conditions. A sample demonstrating this
pattern is available in `Samples\SendAndReceive`.

You can use a [SharedRegister](/Coyote/learn/advanced/object-sharing), which will rule out race
conditions as well, but this still requires a separate protocol to know when the result has been made
available.

## Running a machine synchronously

Another programming pattern is to drive a state machine synchronously. The program can do
`CreateMachineAndExecuteAsync` to create the state machine, then repeatedly do
`SendEventAndExecuteAsync` to make the state machine process events one after another. Let's consider an
example. Suppose that we need to define a state machine `M` that is easily decomposed into two smaller
state machines `M1` and `M2`. For each incoming event, `M` decides to run of the two state machines;
there is no need to run them in parallel. In this case, you only need to code up the smaller
state machines `M1` and `M2`. The state machine `M` can be a simple wrapper. On instantiation, `M`
creates the two sub-state machines as follows.

```c#
ActorId m1 = this.CreateMachineAnsExecuteAsync(typeof(M1), ...);
ActorId m2 = this.CreateMachineAnsExecuteAsync(typeof(M2), ...);
```

When `M` receives an event, it will choose to run the appropriate state machine as follows.

```c#
Event e = this.ReceivedEvent;
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

Note that the two assertions above are guaranteed to never fail because the `m1` and `m2`state machines
are always left in an idle state by `M`, provided that`M` never sends out the handles `m1` and `m2` to
other state machines.
