---
layout: reference
title: Writing specifications
section: learn
permalink: /learn/specifications/overview
---

## Program Specifications

Coyote makes it easy to design and express system-level specifications that can be asserted during
testing. Specifications come in two forms. _Safety_ specifications assert that the system never enters
a _bad_ state. _Liveness_ specifications assert that the system eventually does something _good_, that
is, it asserts that the system is always able to make progress.

## Writing safety properties

Safety property specifications generalize the notion of source code assertions. A safety property
violation is a finite execution that leads a system to an erroneous state. Coyote supports the usual
assertions for specifying safety properties that are local to a Coyote machine or task. In the Tasks
programming model, you should use `Specification.Assert` and in the machines programming model the
corresponding API is `IActorRuntime.Assert`. In addition, Coyote also provides a way to specify
_global_ assertions that can describe the relationship across tasks or machines.

Coyote provides the notion of a _monitor_. It is a special kind of machine that can receive events but
cannot send events to other machines. It some sense, it can only observe the execution of a program but
not influence it: a desirable property when writing specifications in code. Monitors are declared as
follows:

```c#
class GlobalSpec : Monitor { ... }
```

The above code snippet declares a monitor named `GlobalSpec`. Unlike machines, monitors are not
explicitly instantiated. Instead, they need to be registered with the Coyote runtime:

```c#
IActorRuntime runtime;
runtime.RegisterMonitor<GlobalSpec>();
```

There can only be one instance of a given monitor type. Communication with this monitor happens via
events:

```c#
IActorRuntime runtime;
runtime.InvokeMonitor<GlobalSpec>(new CustomEvent(...));
```

Just like machines, monitors can have any number of fields, methods and states. The following is a
simply example of a monitor. Let's say that there are two machines `A` and `B` that maintain two
important variables called `x` and `y`, respectively. We want to assert that these two values are
always within a difference of `5` between each other. There is no one assert that we can write because
both `x` and `y` are not in scope at the same time. So we define a monitor that accepts events as soon
as a variable is updated. Then it keeps asserting their values are within the required bound.

```c#
class GlobalSpec : Monitor
{
   // Current reading of x.
   int my_x;
   // Current reading of y.
   int my_y;

   [Start]
   [OnEntry(nameof(InitOnEntry))]
   [OnEventDoAction(typeof(UpdatedXEvent), nameof(UpdatedXAction))]
   [OnEventDoAction(typeof(UpdatedYEvent), nameof(UpdatedYAction))]
   class Init : State { }

   // Initialization.
   void InitOnEntry()
   {
     my_x = my_y = 0;
   }

   void UpdatedXAction(Event e)
   {
      my_x = (e as UpdatedXEvent).value;
      this.Assert(Math.mod(my_x - my_y) <= 5);
   }


   void UpdatedYAction(Event e)
   {
      my_y = (e as UpdatedYEvent).value;
      this.Assert(Math.mod(my_x - my_y) <= 5);
   }
}

```

In general, a monitor maintains local state of its own that is modified in response to events received
from the program. This local state is used to maintain a history of the computation that is relevant to
the property being specified. An erroneous global behavior is flagged via an assertion on the private
state of the safety monitor. Thus, a monitor cleanly separates the instrumentation state required for
specification (inside the monitor) from the program state (outside the monitor).

You can use monitors with the tasks programming model as well. Simply use
`Specification.RegisterMonitor` and `Specification.InvokeMonitor` instead of the
corresponding APIs in `IActorRuntime`.

## Writing liveness properties

Liveness property specifications generalize the notion of _non-termination_. A liveness property
violation is an infinite trace that exhibits lack of progress. Let's explain this concept through an
example. Suppose that we are designing a replication protocol. The job of the protocol is to ensure
that our data is replicated on, say, three different storage nodes. When a storage node fails, the
protocol kicks in, reads the lost data from one of the alive replicas, and then creates a new replica
on a different storage node. A natural specification for this protocol is that _eventually_ it must
establish three replicas for the data. In other words, it is unavoidable (on storage-node failure) that
there are less-than-required number of replicas, but in that case the protocol must work towards
creating the desired number of replicas again. A violation of this property is an (infinite) execution
where a storage node fails but the protocol is not able to create the third replica, even when given an
infinite amount of time. We keep talking about infinite behaviors here, but let's come back to that
later. First, let us see how we can write this property as a monitor.

A liveness monitor contains two special states: the _hot_ and the _cold_ state. The hot state denotes a
point in the execution where progress is required, but has not happened yet; e.g. a node has failed,
but a new one has not been created yet. A liveness monitor transitions to the hot state when it is
notified that the system must make progress. A liveness monitor leaves the hot state and enters the
cold state when it is notified that the system has progressed enough. An infinite execution is
erroneous if the liveness monitor stays in the hot state for an infinitely long period of time.
Consider the following example.

```c#
class LivenessMonitor : Monitor
{
  // current number of replicas alive.
  int alive = 0;

  [Start]
  [Hot]
  [OnEventDoAction(typeof(UpEvent),nameof(OnUp))]
  [OnEventDoAction(typeof(DownEvent),nameof(OnDown))]
  class NotEnoughReplicas : State { }


  [Cold]
  [OnEventDoAction(typeof(UpEvent),nameof(OnUp))]
  [OnEventDoAction(typeof(DownEvent),nameof(OnDown))]
  class EnoughReplicas : State { }

  // Notification that a new replica is up.
  void OnUp()
  {
     alive ++;
     if (alive >= 3)
     {
        this.GotoState<EnoughReplicas>();
     }
     else
     {
        this.GotoState<NotEnoughReplicas>();
     }
  }

  // Notification that a replica has gone down.
  void OnDown()
  {
     alive --;
     if (alive >= 3)
     {
        this.GotoState<EnoughReplicas>();
     }
     else
     {
        this.GotoState<NotEnoughReplicas>();
     }
  }
}
```

This monitor has two states. It starts in a _hot_ state `NotEnoughReplicas` and keeps tracks of the
number of replicas alive in the system. It stays in the `NotEnoughReplicas` state as long as this
number is below three, otherwise it transitions to the _cold_ state `EnoughReplicas`. A program
execution where this monitor gets stuck in the hot state demonstrates a violation of the correctness
property of the replication protocol.

Liveness monitors offer a natural way of describing properties of progress: things that must
_eventually_ happen in the system, where we cannot necessarily say (or its too cumbersome to say)
exactly when the progress will happen. In practice, of course, we cannot generate infinite executions:
we only have a finite amount of time available to us for testing. The tester resorts to heuristics: it
considers a sufficiently long and hot execution as a proxy for a liveness violation. The user can
configure a bound beyond which executions are considered infinite.
[Keep reading](../specifications/liveness-checking.md) to learn more on how this all works.
