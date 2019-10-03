---
layout: reference
title: Sharing objects
section: learn
permalink: /learn/advanced/object-sharing
---

## Sharing objects

A Coyote program is expected to be free of data races. This means that two different machines should not race on the access to the same object, unless both accesses are reads. Typically, the programmer should have an ownership protocol in mind to associate a unique owner machine to an object when writes have to be performed on the object. An exception to this rule is when using `Microsoft.Coyote.SharedObjects`.

## Microsoft.Coyote.SharedObjects

We have designed multiple shared data structures for use in Coyote programs that can considerably simplify the development of a Coyote program. Instances of these data structures can be shared freely and accessed by multiple machines, even when performing write operations. There is a simple API for creating these shared objects. Currently three kinds of shared objects are available: [`SharedCounter`](https://github.com/p-org/Coyote/blob/master/Libraries/SharedObjects/SharedCounter/SharedCounter.cs), [`SharedRegister<T>`](https://github.com/p-org/Coyote/blob/master/Libraries/SharedObjects/SharedRegister/SharedRegister.cs) and [`SharedDictionary`](https://github.com/p-org/Coyote/blob/master/Libraries/SharedObjects/SharedDictionary/SharedDictionary.cs), but more may be made available in the future (please send us a request if you need a specific data structure, and you believe is generic enough to be included in this library).

Internally, these data structures are written so that they use fast thread-safe implementations in production runs of Coyote program. For instance, `SharedCounter` uses [`Interlocked`](https://docs.microsoft.com/en-us/dotnet/standard/threading/interlocked-operations) operations, `SharedRegister` uses small critical sections implemented using locks and `SharedDictionary` uses a [`ConcurrentDictionary`](https://docs.microsoft.com/en-us/dotnet/api/system.collections.concurrent.concurrentdictionary-2?view=netframework-4.7). However, during bug-finding mode (i.e., while running tests under `Coyote test`) the implementation automatically switches to use a `Machine` that serializes all accesses to the object. Thus, `Coyote test` sees a usual Coyote program with no synchronization operations other than `Machine` creation and message passing.

At the moment, the APIs used to implement `SharedObjects` are internal to Coyote. They may be made available externally in the future so that users can create their own library of shared data structures.

**Example**: See how [this sample](https://github.com/p-org/Coyote/blob/master/Tests/SharedObjects.Tests/Mixed/MixedProductionTest.cs#L103) shares a `SharedDictionary` object and a `SharedCounter` object between two machines that access the objects concurrently.

## Important remark on using `SharedDictionary<TKey, TValue>`

Conceptually one should think of a Coyote SharedObject as a wrapper around a Coyote machine. Thus, one needs to be careful about stashing references inside a SharedObject and treat it in the same manner as sharing references between machines. In the case of a `SharedDictionary` both key and value objects (which are passed by reference into the dictionary) should not be mutated unless first removed from the dictionary because this can lead to a data race. Consider two machines that share a `SharedDictionary` object `D`. If both machines grab the value `D[k]` at the same key `k` they will each have a reference to the same value object, creating the potential for a data race (unless the intention is to only do read operations on the value object).

The same note holds for `SharedRegister<T>` when `T` is a `struct` type with reference fields inside it.
