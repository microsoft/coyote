---
layout: reference
title: Unit-testing machines in isolation
section: learn
permalink: /learn/programming-models/machines/unit-testing
---

## Unit-testing machines in isolation

The `StateMachineTestKit` API provides the capability to _unit-test_ a single machine _sequentially_ and
in _isolation_ from other machines, or the external environment. This is orthogonal from using the
[Coyote tester](/coyote/learn/tools/testing) for end-to-end testing of a program. You will get the
most value out of the Coyote framework if you use both machine-unit-tests and Coyote tests.

Let's discuss how to use `StateMachineTestKit` by going through some simple examples.

Say that you have the following machine `M`, which waits to receive an event of type `E` (via the
`ReceiveEventAsync` statement) in the `InitOnEntry` handler of the `Start` state `Init`:

```c#
private class E : Event {}

private class M : StateMachine
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

To unit-test the above logic, first import the `Microsoft.Coyote.TestingServices` library:

```c#
using Microsoft.Coyote.TestingServices;
```

Next, create a new `StateMachineTestKit` instance for the machine `M` in your test method, as seen below.
You can pass an optional `Configuration` (e.g. if you want to enable verbosity).

```c#
public void Test()
{
   var test = new StateMachineTestKit<M>(configuration: Configuration.Create());
}
```

When `StateMachineTestKit<M>` is instantiated, it creates an instance of the machine `M`, which executes in
a special runtime that provides isolation. The internals of the machine (e.g. the queue) are properly
initialized, as if the machine was executing in production. However, if the machine is trying to create
other machines, it will get a _dummy_ `ActorId`, and if the machine tries to send an event to a
machine other than itself, that event will be dropped. Talking to external APIs (e.g. network or
storage) will require mocking (as is the case in regular unit-testing).

The `StateMachineTestKit` provides two APIs that allow someone to asynchronously (but sequentially) interact
with the machine via its inbox, and thus test how the machine transitions to different states and
handles events. These two APIs are `StartMachineAsync(Event initialEvent = null)` and
`SendEventAsync(Event e)`.

The `StartMachineAsync` method transitions the machine to its `Start` state, passes the optional
specified event (`initialEvent`) and invokes its `OnEntry` handler, if there is one available. This
method returns a task that completes when the machine reaches quiescence (typically when the event
handler finishes executing because there are not more events to dequeue, or when the machine
asynchronously waits to receive an event). This method should only be called in the beginning of the
unit-test, since a machine only transitions to its `Start` state once.

The `SendEventAsync` method sends an event to the machine and starts its event handler. Similar to
`StartMachineAsync`, this method returns a task that completes when the machine reaches quiescence
(typically when the event handler finishes executing because there are not more events to dequeue, or
when the machine asynchronously waits to receive an event).

The `StateMachineTestKit<M>` also provides `Assert`, which is a generic assertion that you can use for
checking correctness, as well as several other specialized assertions, e.g. for asserting state
transitions, or to check if the inbox is empty.

The following code snippet shows how to use these APIs to test the machine `M` of the above example:

```c#
public async Task Test()
{
   var test = new StateMachineTestKit<M>();

   await test.StartMachineAsync();
   test.AssertIsWaitingToReceiveEvent(true);

   await test.SendEventAsync(new E());
   test.AssertIsWaitingToReceiveEvent(false);
   test.AssertInboxSize(0);
}
```

The above unit-test creates a new instance of the machine `M`, then transitions the machine to its
`Start` state using `StartMachineAsync`, and then asserts that the machine is asynchronously waiting to
receive an event of type `E` by invoking the `AssertIsWaitingToReceiveEvent(true)` assertion. Next, the
test is sending the expected event `E` using `SendEventAsync`, and finally asserts that the machine is
not waiting any event, by calling `AssertIsWaitingToReceiveEvent(false)`, and that its inbox is empty,
by calling `AssertInboxSize(0)`.

Besides providing the capability to drive the execution of a machine via the `StartMachineAsync` and
`SendEventAsync` APIs, the `StateMachineTestKit` also allows someone to directly call machine methods. Let's
see how this can be done in a simple example. Say that you have the following machine `M`, which has a
method `Add(int m, int k)` that takes two integers, adds them, and returns the result:

```c#
private class M : StateMachine
{
   [Start]
   private class Init : State { }

   internal int Add(int m, int k)
   {
         return m + k;
   }
}
```

To unit-test the above `Add` machine method, the `StateMachineTestKit<M>` instance gives you access to the
machine reference through the `StateMachineTestKit.Machine` property. You can then use this reference to
directly invoke methods of the machine, as seen below.

```c#
public void Test()
{
   var test = new StateMachineTestKit<M>(configuration: Configuration.Create());
   int result = test.Machine.Add(3, 4);
   test.Assert(result == 7, $"Incorrect result '{result}'");
}
```

Note that directly calling machine methods only works if these methods are declared as `public` or
`internal`. This is typically not recommended for machines (and actors, in general), since the only way
to interact with them should be by sending messages. However, it can be very useful to unit-test
private machine methods, and for this reason the `StateMachineTestKit` provides the `Invoke` and
`InvokeAsync` APIs, which accept the name of the method (and, optionally, parameter types for
distinguishing overloaded methods), as well as the parameters to invoke the method, if any. The
following example shows how to use these APIs to invoke private machine methods:

```c#
private class M : StateMachine
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
   var test = new StateMachineTestKit<M>(configuration: Configuration.Create());

   // Use this API to unit-test a private machine method.
   int result = (int)test.Invoke("Add", 3, 4);
   test.Assert(result == 7, $"Incorrect result '{result}'");

   // Use this API to unit-test an overloaded private machine method.
   result = (int)test.Invoke("Add", new Type[] { typeof(int), typeof(int) }, 3, 4);
   test.Assert(result == 7, $"Incorrect result '{result}'");

   // Use this API to unit-test an asynchronous private machine method.
   int result = (int)await test.InvokeAsync("AddAsync", 3, 4);
   test.Assert(result == 7, $"Incorrect result '{result}'");

   // Use this API to unit-test an asynchronous overloaded private machine method.
   result = (int)await test.InvokeAsync("AddAsync", new Type[] { typeof(int), typeof(int) }, 3, 4);
   test.Assert(result == 7, $"Incorrect result '{result}'");
}
```

Note that its possible to use both the `StartMachineAsync` and `SendEventAsync`, as well as invoke
directly machine methods by accessing the `StateMachineTestKit.Machine` property, based on the testing
scenario.
