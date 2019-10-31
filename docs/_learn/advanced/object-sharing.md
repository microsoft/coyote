---
layout: reference
title: Shared Objects
section: learn
permalink: /learn/advanced/object-sharing
---

## Sharing objects

This feature is currently only available in the [state machines programming model](/Coyote/learn/programming-models/machines/overview).

A Coyote program is expected to be free of low-level data races. This means that two different machines
should not race on access to the same object, unless both accesses are reads. Typically, the programmer
should have an ownership protocol in mind to associate a unique owner machine to an object when writes
have to be performed on the object. An exception to this rule is when using
`Microsoft.Coyote.SharedObjects`.

## Microsoft.Coyote.SharedObjects

Coyote provides multiple shared data structures that help simplify the development of a Coyote program.
Instances of these data structures can be shared freely and accessed by multiple machines, even when
performing write operations. There is a simple API for creating these shared objects. Currently three
kinds of shared objects are available: `SharedCounter`, `SharedRegister<T>` and `SharedDictionary`.

The following code snippet creates and initializes a `SharedRegister`. It then sends the register to a
different machine `m` by stashing it as part of the payload of an event.

```c#
ISharedRegister<int> register = SharedRegister.Create<int>(this.Runtime);
register.SetValue(100);
this.SendEvent(m, new MyEvent(register, ...));
var v = register.GetValue();
this.Assert(v == 100 || v == 200);
```

Further, let's suppose that the target machine `m`, when it gets this `MyEvent` message, gets the
register and does `register.SetValue(200)`. In this case, a read of the register in the source machine
can either return the original value `100` or the value `200` set by `m`. In this way, these shared
objects offer convenient ways of sharing data between machines (without going through explicit message
creation, send, receive, etc.).

Furthermore, if the assertion at the end of the code snippet shown above was `this.Assert(v == 100)`
then the tester will be able to find and report a violation of the assertion because it understands
SharedObject operations as a source of synchronization.

Internally, these data structures are written so that they use efficient thread-safe implementations in
production runs of Coyote program. For instance, `SharedCounter` uses
[`Interlocked`](https://docs.microsoft.com/en-us/dotnet/standard/threading/interlocked-operations)
operations, `SharedRegister` uses small critical sections implemented using locks and
`SharedDictionary` uses a [`ConcurrentDictionary`](https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2?view=netframework-4.7).
However, during test mode (i.e., while running tests under `coyote test`) the implementation
automatically switches to use a machine that serializes all accesses to the object. Thus, `coyote test`
sees a normal Coyote program with no synchronization operations other than machine creation and message
passing.

At the moment, the APIs used to implement `SharedObjects` are internal to Coyote.

## Important remark on using `SharedDictionary<TKey, TValue>`

Conceptually one should think of a Coyote SharedObject as a wrapper around a Coyote machine. Thus, one
needs to be careful about stashing references inside a SharedObject and treat it in the same manner as
sharing references between machines. In the case of a `SharedDictionary` both key and value objects
(which are passed by reference into the dictionary) should not be mutated unless first removed from the
dictionary because this can lead to a data race. Consider two machines that share a `SharedDictionary`
object `D`. If both machines grab the value `D[k]` at the same key `k` they will each have a reference
to the same value object, creating the potential for a data race (unless the intention is to only do
read operations on the value object).

The same note holds for `SharedRegister<T>` when `T` is a `struct` type with reference fields inside it.
