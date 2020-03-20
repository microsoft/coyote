---
layout: reference
title: Testing async/await code using Coyote machines
section: learn
permalink: /learn/programming-models/actors/async-await-machines
---

## Testing async/await code using Coyote machines

The Coyote language was designed to ease the development of event-driven programs. Coyote forces the
programmer to think of their design in terms of state machines driven by events passed
back-and-forth between them. This style lends itself naturally to the asynchronous world. Tasks are
kept short and their continuations are stitched together through events that arrive asynchronously.
The complexity of testing and exploration is managed by the `coyote` tester that systematically
enumerates different interleavings and provides high "concurrency" coverage. While the intended
use-case of Coyote is for the development of new systems, we do acknowledge that the more common
need is going to be for testing of existing code. Can Coyote help there? We think so, especially for
code that is primarily `async`-`await` C#. But we first need to understand how `coyote` tester
works.

## Coyote tester requirements

In Coyote, a `StateMachine` is the unit the concurrency. It represents a _state machine_, but for
the purpose of this article, we can ignore this aspect of a `StateMachine` and simply treat it as a
building block for concurrency, similar to a `Task` or a `Thread`. A `StateMachine` is internally
sequential but different `Machines` all execute concurrently. A `StateMachine` is always in an
event-driven loop until it halts; it waits for an event to arrive in its `Inbox` and fires an
`action` in response. The `action`, in addition to calling Coyote APIs for sending and receiving
events, can execute _arbitrary_ `C#` code to mutate the state of the program. It is this usage of
_arbitrary_ that we must now understand for using `coyote` on existing code.

In the simplest form, the `C#` code should be sequential so that all concurrency is delegated to the
Coyote runtime (at least while testing). In other words, the code executed by a machine action must
not spawn `Tasks` or `Threads` neither must it do any synchronization operation other than the
Coyote `SendEvent` or `ReceiveEventAsync` (i.e., no use of `locks`, `mutexes` or similar
constructs). This goes along with the recommendation that different `Machines` must not share object
references (use [Shared Objects](sharing-objects) if you must).

The reason for all these restrictions is that `coyote` tester needs to be aware of all concurrency
in the program in order to control it. The `coyote` tester keeps track of all live state machines in
the program and takes over the scheduling. At any point during the execution of the program, it will
determine the next `StateMachine` to schedule and give it a chance to execute. The machine will
execute its action without interference from other machines until it finishes its current action or
it enters the Coyote runtime again via a `SendEvent` or `ReceiveEventAsync` (the only available
synchronization primitives). At this point, the `coyote` tester scheduler takes control, suspends
the currently-scheduled machine and then decides on the next one to schedule. The `coyote` tester
essentially serializes the entire execution to a single thread. By controlling the scheduling
decisions during an execution, `coyote` can explore different interleavings for a program. The exact
choice of which `StateMachine` to schedule is determined by a `SchedulingStrategy`. The `coyote`
tester has several strategies and we recommend using a portfolio of them. The strategies have been
crafted from over a decade of research on finding concurrency bugs efficiently in practice. (See,
for example, [this paper](http://dl.acm.org/citation.cfm?id=2786861).)

Despite serializing the execution on a single thread, the restrictions that we had outlined above
guarantee that `coyote` will cover all behaviors of the program in the limit. Getting there will of
course, take infinite time because there may be infinitely many executions of the program,
nonetheless, _completeness-in-the-limit_ is an important guarantee for a testing solution to have.
(Testing concurrent programs natively on the hardware without `coyote` does not offer this
guarantee.)

Relaxing the restrictions on the `C#` code can either cause `coyote` to lose completeness or it may
start to deadlock or crash. The former outcome is the acceptable one: we still dramatically gain
testing coverage by using the `coyote` tester over naïve testing. (It also makes way for a
pay-as-you-go-model: as more code is made Coyote-compliant, the coverage keeps improving and
fully-Coyote-compliant code offers completeness.) It is the latter (deadlocks and crashes) that we
must avoid.

## Async-await code

To make the discussion meaningful, we restrict our attention to mostly `async`-`await` code. By this
we mean a software component that asynchronously handles client requests that may arrive at any
time, and sends back a response when it's done servicing them. Internally, it may use other such
components: it delegates work to them asynchronously and waits for their response. Such components
are typical in web services, where for example, users pump in requests at any time and the service
must process them asynchronously; it cannot afford to block subsequent requests before it finishes
the first. Further, the service might use a backing store for persistence and fault tolerance. Let's
take an example. Suppose that our component offers the following methods for processing client
requests:

```C#
async Task<Response> HandleRequest1(...);
async Task<Response> HandleRequest2(...);
```

And these procedures call external methods for interacting with a persistent store:

```C#
async Task<Data> Read(int index);
async Task Write(int index, Data data);
```

For unit-testing such code, one might write a test method that invokes these methods in parallel and
use mocks of the storage that relies on locking to be thread-safe.

```C#
void Test()
{
   Task.Run(async () => await HandleRequest1(...));
   Task.Run(async () => await HandleRequest2(...));
}

// mock
async Task<Data> Read(int index)
{
   lock(lck)
   {
      return Task.FromResult(...);
   }
}
```

Note: Similar scenarios also exist very commonly inside the operating systems such as
[drivers](https://blogs.msdn.microsoft.com/b8/2011/08/22/building-robust-usb-3-0-support/), but the
language of choice there is `C` or `C++`, not a managed language like `C#`. Use
[P](https://github.com/p-org/P) if you operate in that world.

## Testing strategy

To use the `coyote` tester we must tame the `C#` code and work towards exposing the concurrency to
Coyote. First and foremost, _the code must not spawn `Tasks` (same applies to `Threads`)_. This is
the most important rule to follow. Creation of `Tasks` will surely make `coyote` unusable. To
eliminate `Task` creation, try replacing them with `StateMachine` creation instead, which should
work for the most part. For our running example, we modify our `Test` method to instead create
machines:

```C#
[Microsoft.Coyote.SystematicTesting.Test]
void Test(IActorRuntime runtime)
{
   runtime.CreateActor(typeof(RunTask), new TaskPayload(async () => await HandleRequest1(...)));
   runtime.CreateActor(typeof(RunTask), new TaskPayload(async () => await HandleRequest2(...)));
}
```

Here, `RunTask` is a special machine that simply invokes the payload method given to it. Look at the
sample
[here](https://github.com/p-org/CoyoteLab/tree/master/Samples/Experimental/SingleTaskMachine). Or
one may create their own special machine for invoking `HandleRequest1` or `HandleRequest2`. Any way
of replacing `Task` creation with `StateMachine` creation is fine.

Another example: your code may be using a `Timer` to register a periodic callback. Instead, create a
`TimerMachine` that either invokes the callback periodically (or non-deterministically using
Coyote's `Random`) or sends an event to the `Task` (now a `StateMachine`) that created the `Timer`.
Sample code is
[here](https://github.com/p-org/coyote/tree/master/Samples/Raft/Raft.CoyoteLibrary/Timers).

Once the `Task` creation is eliminated, the next item of focus is the use of synchronization. When
multiple `Tasks` can share a reference to the same object, they will use synchronization in the form
of `locks` to guard access to that object. When `Tasks` get converted to `Machines`, this implies
actions of different machines might share objects and invoke non-Coyote synchronization. For
`coyote` scheduling to work without causing deadlocks, one must be careful with such
synchronization. _A simple rule of thumb is that a Coyote API should not be invoked while holding a
lock_. Short synchronization blocks that guard access to a flag or a simple container should be
likely be fine, except that `coyote` loses completeness (practically, there is a loss in coverage).
To regain more coverage, _consider lifting the synchronization blocks to be hosted in their own
`StateMachine`_. For our running example, we can write a machine `StorageMachine` to mock calls to
`Read` and `Write` and do something like the following instead of locking:

```C#
async Task<Data> Read(int index)
{
   SendEvent(typeof(StorageMachine), new ReadEvent(index));
   var r = await ReceiveEventAsync(typeof(ReadResponse));
   return r.Data;
}
```

The `StorageMachine` can perform `Read` and `Write` functionality atomically (without needing to
grab a lock as `coyote` guarantees methods of a single machine are not executed in parallel). This
mocking will ensure the `coyote` tester considers different orders of execution of the `Read/Write`
critical section.

There are further standard guidelines to writing a Coyote test. _The test must be idempotent and set
up for repeated execution_. In simple terms, it must reset its state before starting the test. This
is required because the `coyote` tester execute the test method repeatedly.

If you have code that mostly uses `async`-`await` constructs then it would be an easy porting
exercise, which may just be confined to your test code. A detailed account of how we applied this
strategy to test the `ExtentManager` of Azure Storage vNext is given in our
[paper](https://www.microsoft.com/en-us/research/wp-content/uploads/2016/04/paper-1.pdf) (see
section 3).

## Controlled non-determinism

There is an additional requirement for the `coyote` tester. The C# code must be deterministic once
the concurrent interleaving between `Machines` is fixed. This means, for instance, the code should
not make branching decisions based on the current time. In order to simulate timeout, you can
instead rely on the Coyote runtime `Random` API, which in turn will provide higher coverage during
testing (by exploring both the non-timeout as well as timeout scenarios). This requirement is
necessary to reproduce a trace reported by the `coyote` tester but can also help in achieving higher
coverage. Some of the `coyote` tester strategies rely on controlled non-determinism to
systematically explore different interleavings.
