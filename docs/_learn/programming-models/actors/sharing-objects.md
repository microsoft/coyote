---
layout: reference
title: Sharing objects
section: learn
permalink: /learn/programming-models/actors/sharing-objects
---

## Safely sharing objects between actors

An actor program in Coyote is expected to be free of low-level data races. This means that two
different actors should not race on access to the same object, unless both accesses are reads.
Typically, you should build your application with an ownership protocol in mind to associate a
unique owner actor to an object when writes have to be performed on that object. An exception to
this rule is when using the thread-safe in-memory data structures provided by the
`Microsoft.Coyote.Actors.SharedObjects` namespace.

## Microsoft.Coyote.Actors.SharedObjects

Coyote provides multiple thread-safe in-memory data structures that help simplify writing actor
programs. Instances of these data structures can be shared freely and accessed by multiple actors,
even when performing write operations. There is a simple API for creating these shared data
structures. Currently three kinds of shared data structures are available: `SharedCounter`,
`SharedRegister<T>` and `SharedDictionary`.

The following code snippet creates and initializes a `SharedRegister`. It then sends the register to
a different actor `m` by stashing it as part of the payload of an event.

```c#
SharedRegister<int> register = SharedRegister.Create<int>(this.Id.Runtime);
register.SetValue(100);
this.SendEvent(m, new MyEvent(register, ...));
var v = register.GetValue();
this.Assert(v == 100 || v == 200);
```

Let's suppose that the target actors `m`, when it gets this `MyEvent` message, gets the register and
does `register.SetValue(200)`. In this case, a read of the register in the source actor can either
return the original value `100` or the value `200` set by `m`. In this way, these shared objects
offer convenient ways of sharing data between actors (without going through explicit message
creation, send, receive, etc.).

Furthermore, if the assertion at the end of the code snippet shown above was `this.Assert(v == 100)`
then the tester will be able to find and report a violation of the assertion because it understands
`SharedRegister` operations as a source of synchronization.

Internally, these data structures are written so that they use efficient thread-safe implementations
in production runs of a Coyote program. For instance, `SharedCounter` uses
[`Interlocked`](https://docs.microsoft.com/en-us/dotnet/standard/threading/interlocked-operations)
operations, `SharedRegister` uses small critical sections implemented using locks and
`SharedDictionary` uses a
[`ConcurrentDictionary`](https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2?view=netframework-4.7).
However, during test mode (i.e., while running tests under `coyote test`) the implementation
automatically switches to use an actor that serializes all accesses to the object. Thus, `coyote
test` sees a normal Coyote program with no synchronization operations other than actor creation and
message passing.

At the moment, the APIs used to implement `Microsoft.Coyote.Actors.SharedObjects` are internal to
Coyote.

## Important remark on using `SharedDictionary<TKey, TValue>`

Conceptually you should think of a Coyote SharedObject as a wrapper around a Coyote actor. Thus, you
need to be careful about stashing references inside a SharedObject and treat it in the same manner
as sharing references between actors. In the case of a `SharedDictionary` both the key and the value
(which are passed by reference into the dictionary) should not be mutated unless first removed from
the dictionary because this can lead to a data race. Consider two actors that share a
`SharedDictionary` object `D`. If both actors grab the value `D[k]` at the same key `k` they will
each have a reference to the same object, creating the potential for a data race (unless the
intention is to only do read operations on that object).

The same note holds for `SharedRegister<T>` when `T` is a `struct` type with reference fields inside
it.

## What about System.Collections.Concurrent?

Yes, you can use the .NET thread safe collections to share information across actors but not the
`BlockingCollection` as this can block and Coyote will not know about that which will lead to
deadlocks during testing. The other thread safe collections do not have uncontrolled
non-determinism, either from Task.Run, or from retry loops, timers or waits.

The caveat is that Coyote has not instrumented the .NET concurrent collections, and so coyote does
not systematically explore thread switching in the middle of these operations, therefore Coyote
will not always find all data race conditions related to concurrent access on these collections.