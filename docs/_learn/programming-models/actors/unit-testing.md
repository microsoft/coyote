---
layout: reference
title: Unit-testing actors in isolation
section: learn
permalink: /learn/programming-models/actors/unit-testing
---

## Unit-testing actors in isolation

The `ActorTestKit` API provides the capability to _unit-test_ a single actor _sequentially_ and in
_isolation_ from other actors, or the external environment. This is orthogonal from using the
[Coyote tester](../../tools/testing) for end-to-end testing of a program. You will get the most
value out of the Coyote framework if you use both actor-unit-tests and Coyote tests.

Let's discuss how to use `ActorTestKit` by going through some simple examples.

Say that you have the following actor `M`, which waits to receive an event of type `E` (via the
`ReceiveEventAsync` statement) in the `InitOnEntry` handler of the `Start` state `Init`:

```c#
class E : Event {}

class M : StateMachine
{
   [Start]
   [OnEntry(nameof(InitOnEntry))]
   private class Init : State { }

   private async Task InitOnEntry()
   {
         await this.ReceiveEventAsync(typeof(E));
   }
}
```

To unit-test the above logic, first import the `Microsoft.Coyote.Actors.UnitTesting` library:

```c#
using Microsoft.Coyote.Actors.UnitTesting;
```

Next, create a new `ActorTestKit` instance for the actor `M` in your test method, as seen below. You
can pass an optional `Configuration` (e.g. if you want to enable verbosity).

```c#
public void Test()
{
   var test = new ActorTestKit<M>(Configuration.Create());
}
```

When `ActorTestKit<M>` is instantiated, it creates an instance of the actor `M`, which executes in a
special runtime that provides isolation. The internals of the actor (e.g. the queue) are properly
initialized, as if the actor were executing in production. However, if the actor is trying to create
other actors, it will get a _dummy_ `ActorId`, and if the actor tries to send an event to an actor
other than itself, that event will be dropped. Talking to external APIs (e.g. network or storage)
will require mocking (as is the case in regular unit-testing).

The `ActorTestKit` provides two APIs that allow someone to asynchronously (but sequentially)
interact with the actor via its inbox, and thus test how the actor transitions to different states
and handles events. These two APIs are `StartActorAsync(Event initialEvent = null)` and
`SendEventAsync(Event e)`.

The `StartActorAsync` method initializes the Actor, passing the optional specified event
(`initialEvent`) and since it is a `StateMachine` in this example it also transitions the machine to
its `Start` state, and invokes its `OnEntry` handler, if there is one available. This method returns
a task that completes when the actor reaches quiescence (typically when the event handler finishes
executing because there are not more events to dequeue, or when the actor asynchronously waits to
receive an event). This method should only be called in the beginning of the unit-test, since an
actor can only be initialized once.

The `SendEventAsync` method sends an event to the actor and starts its event handler. Similar to
`StartActorAsync`, this method returns a task that completes when the actor reaches quiescence
(typically when the event handler finishes executing because there are not more events to dequeue,
or when the actor asynchronously waits to receive an event).

The `ActorTestKit<M>` also provides `Assert`, which is a generic assertion that you can use for
checking correctness, as well as several other specialized assertions, e.g. for asserting state
transitions, or to check if the inbox is empty.

The following code snippet shows how to use these APIs to test the machine `M` of the above example:

```c#
public async Task Test()
{
   var test = new ActorTestKit<M>(Configuration.Create());

   await test.StartActorAsync();
   test.AssertIsWaitingToReceiveEvent(true);

   await test.SendEventAsync(new E());
   test.AssertIsWaitingToReceiveEvent(false);
   test.AssertInboxSize(0);
}
```

The above unit-test creates a new instance of the machine `M`, then transitions the machine to its
`Start` state using `StartActorAsync`, and then asserts that the machine is asynchronously waiting
to receive an event of type `E` by invoking the `AssertIsWaitingToReceiveEvent(true)` assertion.
Next, the test is sending the expected event `E` using `SendEventAsync`, and finally asserts that
the machine is not waiting any event, by calling `AssertIsWaitingToReceiveEvent(false)`, and that
its inbox is empty, by calling `AssertInboxSize(0)`.

Besides providing the capability to drive the execution of an actor via the `StartActorAsync` and
`SendEventAsync` APIs, the `ActorTestKit` also allows someone to directly call actor methods. Let's
see how this can be done in a simple example. Say that you have the following state machine `M`,
which has a method `Add(int m, int k)` that takes two integers, adds them, and returns the result:

```c#
class M : StateMachine
{
   [Start]
   private class Init : State { }

   internal int Add(int m, int k)
   {
         return m + k;
   }
}
```

To unit-test the above `Add` method, the `ActorTestKit<M>` instance gives you access to the actor
reference through the `ActorTestKit.ActorInstance` property. You can then use this reference to
directly invoke methods of the actor, as seen below.

```c#
public void Test()
{
   var test = new ActorTestKit<M>(Configuration.Create());
   int result = test.Machine.Add(3, 4);
   test.Assert(result == 7, $"Incorrect result '{result}'");
}
```

Note that directly calling methods only works if these methods are declared as `public` or
`internal`. This is typically not recommended for actors, since the only way to interact with them
should be by sending messages. However, it can be very useful to unit-test private methods, and for
this reason the `ActorTestKit` provides the `Invoke` and `InvokeAsync` APIs, which accept the name
of the method (and, optionally, parameter types for distinguishing overloaded methods), as well as
the parameters to invoke the method, if any. The following example shows how to use these APIs to
invoke private methods:

```c#
class M : StateMachine
{
   [Start]
   private class Init : State { }

   private int Add(int m, int k)
   {
         return m + k;
   }

   private async Task<int> AddAsync(int m, int k)
   {
         await Task.CompletedTask;
         return m + k;
   }
}

public async Task TestAsync()
{
   var test = new ActorTestKit<M>(Configuration.Create());

   // Use this API to unit-test a private machine method.
   int result = (int)test.Invoke("Add", 3, 4);
   test.Assert(result == 7, $"Incorrect result '{result}'");

   // Use this API to unit-test an overloaded private machine method.
   result = (int)test.Invoke("Add", new Type[] { typeof(int), typeof(int) }, 3, 4);
   test.Assert(result == 7, $"Incorrect result '{result}'");

   // Use this API to unit-test an asynchronous private machine method.
   result = (int)await test.InvokeAsync("AddAsync", 3, 4);
   test.Assert(result == 7, $"Incorrect result '{result}'");

   // Use this API to unit-test an asynchronous overloaded private machine method.
   result = (int)await test.InvokeAsync("AddAsync", new Type[] { typeof(int), typeof(int) }, 3, 4);
   test.Assert(result == 7, $"Incorrect result '{result}'");
}
```
