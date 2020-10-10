---
layout: reference
title: Synchronized Block
section: learn
permalink: /learn/programming-models/async/synchronized-block
---

## Controlled synchronized block

The Coyote asynchronous tasks programming model provides a
`Microsoft.Coyote.Tasks.SynchronizedBlock` type that can be used to synchronize multiple tasks
around access to a shared resource. In production this construct is a thin wrapper on
`System.Threading.Monitor`, and during testing, the `SynchronizedBlock` is automatically replaced
with a controlled mocked version so that Coyote can explore the scheduling and interleaving of
asynchronous operations that are synchronized this way.

## Why is a synchronized block necessary?

C# provides a built in `lock` keyword that uses `System.Threading.Monitor` under the covers. This
keyword is often used to protect local state from data race conditions like this:

```csharp
class Foo
{
    int count;
    object syncObject = new object();

    public int Increment()
    {
        lock(syncObject)
        {
            return ++count;
        }
    }
}
```

The `++operator` is not atomic, and therefore it is not thread safe, so the lock statement ensures
that it becomes an atomic operation and so now the `Increment` method can be safely used in parallel
situations. For added convenience, this `lock` statement is also re-entrant, so your class can call
another method that also enters the lock and this is fine.  This makes it easier to manage code
reuse within your class.

The `Monitor` class also has a very useful feature in the `Wait` and `Pulse` and `PulseAll` methods
that is hard to replace with anything else.  In this case one task can `Wait` while it is inside the
`lock` for another task to signal it using `Pulse`.  The `Wait` call releases the lock so another
task can get the lock and call `Pulse`, when the lock is released, the waiting task can proceed
as it now has the lock.  These primitives form the core of very useful "synchronization" logic.

Note that it is very important to also point out that C# does not allow `await` keyword to be used
inside a `lock` statement as this can lead to deadlocks.

## How do you use it?

Coyote provides a drop in replacement for `System.Threading.Monitor` called
`Microsoft.Coyote.Tasks.SynchronizedBlock`. To get Coyote test control over your synchronized
blocks simply replace:

```csharp
lock (this.syncObject)
```

with:

```c#
using (var monitor = SynchronizedBlock.Lock(this.syncObject))
```

and replace calls to `System.Threading.Monitor.Wait` and `System.Threading.Monitor.Pulse` with
calls to the new variable `monitor.Wait` and `monitor.Pulse` instead. Then when you run your
`coyote test` on this code you will get systematic testing of all interleavings around this lock.

Note: you must also avoid using `await` on asynchronous tasks inside a `SynchronizedBlock.Lock`. If
Coyote detects any task switching inside a synchronized block you will see an error message saying
_"Object synchronization method was called from a task that did not create this
SynchronizedBlock"_.

Bugs involving `System.Threading.Monitor` can be extremely tricky to debug due to their
[heisenbug](https://en.wikipedia.org/wiki/Heisenbug) nature. Coyote testing eliminates this problem
and can given you clear 100% reproducible bug schedules that you can replay as slowly as you need
to see what is really going on leading up to such bugs.

## Sample

For a complete example see the [Extreme Programming meets systematic testing using
Coyote](https://cloudblogs.microsoft.com/opensource/2020/07/14/extreme-programming-meets-systematic-testing-using-coyote/)
and the associated [BoundedBuffer example source code](https://github.com/microsoft/coyote-samples/tree/main/BoundedBuffer).
