---
layout: reference
section: learn
title: Synchronous execution of machines
permalink: /learn/core-concepts/synchronous-execution
---
Synchronous execution of machines
=================================
Coyote offers the following APIs (and overloads) for synchronous execution.
```C#
public Task<MachineId> CreateMachineAndExecute(Type type, Event e = null, Guid? operationGroupId = null);
public Task<bool> SendEventAndExecute(MachineId target, Event e, SendOptions options = null);
```

Both these are `async` methods and must be `awaited` by the caller. The method `CreateMachineAndExecute` when awaited, returns only when the newly created machine becomes idle. That is, it creates the machine, passes it the initial event `e` and then waits for the machine to become idle. (A machine is idle when it is blocked on its inbox for receiving input.) The method `SendEventAndExecute` when awaited has two possible executions. If the `target` machine is running (i.e., it is not idle) then the method only enqueues the event and returns immediately with the return value `false`. If the `target` machine was idle then the method enqueues the event (which causes the `target` machine to start executing) and blocks until the machine becomes idle again. In this case, the method returns `true` indicating that the event has been processed by the `target` machine.

## Potential Deadlocks with Receive

The user should be careful with the use of `Receive` when using these methods. In the absence of `Receive`, the semantics of these methods guarantee that the program cannot deadlock. With a `Receive` the following situation can occur. Lets suppose there are two machines `A` and `B` and the latter is idle. Then machine `A` does `SendEventAndExecute` to pass an event `e` to `B`. Because `B` was idle, `A` will wait until `B` becomes idle again. But if `B` executes a `Receive` while processing the event `e`, expecting another event from `A` then the program deadlocks. (Blocking on a `Receive` is not considered as being idle.)

## Extracting information from a Machine

Suppose there is a Coyote machine `M1` that holds some information that we are interested in grabbing. The usual way of getting this information would be to `Send` a "grab" message to `M1` and then wait for its response via `Receive`. However, a `Receive` can only be executed by a machine. How does one get the result outside the context of a machine (or, say, from a static method)? One option is to use these `AndExecute` methods. We can create a trampoline machine that sends the grab message to `M1` and waits for its response. But we create this machine by awaiting `CreateMachineAndExecute`. This will ensure that by the time this calls returns, the trampoline machine would have already grabbed the result, which it can then stash in an object that can be safely shared with the caller without any race conditions. A sample demonstrating this pattern is available in `Samples\SendAndReceive`.  

One can use a [SharedRegister](https://github.com/p-org/PSharp/blob/master/Docs/Features/ObjectSharing.md#sharing-objects-across-machines), which will rule out races but this still requires a separate protocol to know when the result has been made available.

## Running a Machine synchronously

Another programming pattern is to drive a state machine synchronously. The program can do `CreateMachineAndExecute` to create the state machine, then repeatedly do `SendEventAndExecute` to make the machine process events one after another. 
