## Programming model: asynchronous tasks

The _asynchronous tasks_ programming model of Coyote is based on the [task-based asynchronous
pattern](https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap).
Coyote provides a drop in replacement for the native .NET `Task` type that works with the `async`
and `await` C# keywords . To avoid confusion we will use the following terminology:

| Short Name        | Fully Qualified Name            |
| :-------------    | :-----------------------------: |
|  native `Task`    | `System.Threading.Tasks.Task`   |
| controlled `Task` | `Microsoft.Coyote.Tasks.Task`   |

A controlled `Task` is similar to a native `Task` type and can be used alongside the native `Task`
type to easily invoke external asynchronous APIs from your code. We will discuss this in more detail
below.

The benefit of using the controlled Task is that the Coyote runtime takes control of its execution
and scheduling during systematic testing, enabling `coyote test` to explore various interleavings
between controlled `Task` objects. In production, a controlled `Task` executes efficiently, as it a
simple wrapper over a native `Task`, with operations being pass-through (Coyote takes control only
during testing).

## Overview

The core of the Coyote asynchronous tasks programming model is the controlled `Task` and `Task<T>`
objects, which model asynchronous operations. They are supported by the `async` and `await`
keywords.

This programming model is fairly simple in most cases:
- For _I/O-bound_ code, you `await` an operation which returns a controlled `Task` or `Task<T>`
  inside of an `async` method.
- For _CPU-bound_ code, you `await` an operation which is started on a background thread with the
  `Task.Run` method.

In more detail, a controlled `Task` is a construct used to implement what is known as the [promise
model of concurrency](https://en.wikipedia.org/wiki/Futures_and_promises). A controlled `Task`
basically offers you a _promise_ that work will be completed at a later point, letting you
coordinate with this promise using `async` and `await`. A controlled `Task` represents a single
operation which does not return a value. A controlled `Task<T>` represents a single operation which
returns a value of type `T`. It is important to reason about tasks as abstractions of work happening
asynchronously, and not an abstraction over threading. By default, a controlled `Task` executes
(using a `Task`) on the current thread and delegates work to the operating system, as appropriate.
Optionally, a controlled `Task` can be explicitly requested to run on a separate thread via the
`Task.Run` API.

The `await` keyword is where the magic happens. Using `await` yields control to the caller of the
method that performed `await`, allowing your program to be responsive or a service to be elastic,
since it can now perform useful work while a controlled `Task` is running on the background. Your
code does not need to rely on callbacks or events to continue execution after the task has been
completed. The C# language does that for you. If you're using controlled `Task<T>`, the `await`
keyword will additionally _unwrap_ the value returned when the controlled `Task` is complete. The
details of how `await` works are further explained in the C#
[docs](https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap).

During testing, using `await` allows the Coyote runtime to automatically inject scheduling points
and thoroughly explore asynchronous interleavings to find concurrency bugs.

You can choose to use the `Microsoft.Coyote.Tasks.Task` directly in your programs or you can use
the automatic [rewriting](../binary-rewriting.md) feature which will rewrite your compiled binaries that use
`System.Threading.Tasks.Task` and inject the required Coyote controls so you can run `coyote test`
on the rewritten binaries.

## What happens under the covers

The C# compiler transforms an `async` method into a state machine (literally called
IAsyncStateMachine) which keeps track of things like yielding execution when an `await` is reached
and resuming execution when a background job has finished.

The controlled `Task` type uses a C# 7 feature known as `async task types` (see
[here](https://github.com/dotnet/roslyn/blob/master/docs/features/task-types.md)) that allows
framework developers to create custom task types that can be used with `async` and `await`. This is
where the magic happens. In production, controlled `Task` enables C# to build a custom asynchronous
state machine that uses regular `Task` objects. However, during testing, Coyote uses dependency
injection to supply a custom asynchronous state machine that allows controlling the scheduling of
controlled `Task` objects, and thus systematically exploring their interleavings.

## How to use asynchronous tasks

We will now show how to write a program using the Coyote asynchronous task programming model. As
mentioned before, the controlled `Task` type is a drop-in replacement for the `Task` type, and thus
any prior experience writing asynchronous code using `async` and `await` is useful and relevant. If
you are not already familiar with `async` and `await`, you can learn more in the C#
[docs](https://docs.microsoft.com/en-us/dotnet/standard/async-in-depth).

Say that you have the following simple C# program:

```csharp
// Use the Coyote controlled task type.
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Specifications;

public class Program
{
    public int Value = 0;

  public async Task WriteWithDelayAsync(int value)
  {
      await Task.Delay(100);
      this.Value = value;
  }

  public async Task RunAsync()
  {
      Task task1 = WriteWithDelayAsync(3);
      Task task2 = WriteWithDelayAsync(5);

      await Task.WhenAll(task1, task2);

      Specification.Assert(this.Value == 5, "Value is '{0}' instead of 5.", this.Value);
  }
}
```

The above program contains a `int Value` that is updated by the `WriteWithDelayAsync` method. This
is a C# `async` method that asynchronously waits for a controlled `Task` to complete after `100`ms
(created via the `Task.Delay(100)` call), and then modifies the Value field.

The asynchronous `RunAsync` method twice invokes the `WriteWithDelayAsync` method by passing the
values `3` and `5` respectively. Each method call returns a controlled `Task` object, which can be
awaited using `await`. The `RunAsync` method first invokes the two asynchronous method calls and
then calls `Task.WhenAll(...)` to `await` on the completion of both tasks.

Because `WriteWithDelayAsync` method awaits a `Task.Delay` to complete, it will yield control to
the caller of the method, which is the `RunAsync` method. However, the `RunAsync` method is not
awaiting immediately upon invoking the `WriteWithDelayAsync` method calls. This means that the two
calls can happen _asynchronously_, and thus the resulting Value can be either `3` or `5` after
`Task.WhenAll(...)` completes.

Using `Specification.Assert`, Coyote allows you to write assertions that check these kinds of safety
properties. In this case, the assertion will check if the Value is `5` or not, and if not it will
throw an exception, or report an error together with a reproducible trace during testing.

## What about System.Collections.Concurrent?

Yes, you can use the .NET thread safe collections to share information across tasks, but not the
`BlockingCollection` as this can block and Coyote will not know about that which will lead to
deadlocks during testing. The other thread safe collections do not have uncontrolled
non-determinism, either from Task.Run, or from retry loops, timers or waits.

The caveat is that Coyote has not instrumented the .NET concurrent collections, and so coyote does
not systematically explore thread switching in the middle of these operations, therefore Coyote
will not always find all data race conditions related to concurrent access on these collections.
For example, two tasks calling `TryAdd` with the same key, one task will succeed the other will
not, but Coyote will not systematically explore all possible orderings around this operation. You
can help Coyote do better by using [ExploreContextSwitch](../../ref/Microsoft.Coyote.Tasks/Task.md).

## Samples

To try out more samples that show how to use Coyote to systematically test task-based programs see
the following:

- [Account manager](../../tutorials/test-concurrent-operations.md)
- [Bounded buffer](../../samples/tasks/bounded-buffer.md)
- [Coffee machine failover](../../tutorials/test-failover.md)
