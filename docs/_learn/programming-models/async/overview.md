---
layout: reference
title: Tasks programming model
section: learn
permalink: /learn/programming-models/async/overview
---

## Programming model: asynchronous tasks

The _asynchronous tasks_ programming model of Coyote is based on the [task-based asynchronous pattern](https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap).
This programming model exposes the `ControlledTask` type, which can be used with the `async` and
`await` C# keywords, similar to the native C# `System.Threading.Tasks.Task` type (which we simply refer
as `Task` from now on). The `ControlledTask` type can be used as a drop-in replacement for the `Task`
type. However,`ControlledTask` can also be used alongside `Task` to easily invoke external asynchronous
APIs from your code. We will discuss this in more detail below.

The benefit of using `ControlledTask` is that during testing, the Coyote runtime will control the
scheduling of each `ControlledTask` to explore various interleavings. In production, `ControlledTask`
executes efficiently, as it is implemented using a `Task`.

## Overview

The core of the Coyote asynchronous tasks programming model is the `ControlledTask` and
`ControlledTask<T>` objects, which model asynchronous operations. They are supported by the
`async` and `await` keywords.

This programming model is fairly simple in most cases:
- For _I/O-bound_ code, you `await` an operation which returns a `ControlledTask` or
`ControlledTask<T>` inside of an `async` method.
- For _CPU-bound_ code, you `await` an operation which is started on a background thread with the
`ControlledTask.Run` method.

In more detail, a `ControlledTask` is a construct used to implement what is known as the
[promise model of concurrency](https://en.wikipedia.org/wiki/Futures_and_promises). A `ControlledTask`
basically offers you a _promise_ that work will be completed at a later point, letting you coordinate
with this promise using `async` and `await`. A `ControlledTask` represents a single operation which
does not return a value. A `ControlledTask<T>` represents a single operation which returns a value of
type `T`. It is important to reason about tasks as abstractions of work happening asynchronously, and
not an abstraction over threading. By default, a `ControlledTask` executes (using a `Task`) on the
current thread and delegates work to the operating system, as appropriate. Optionally, a
`ControlledTask` can be explicitly requested to run on a separate thread via the
`ControlledTask.Run` API.

The `await` keyword is where the magic happens. Using `await` yields control to the caller of the
method that performed `await`, allowing your program to be responsive or a service to be elastic, since
it can now perform useful work while a `ControlledTask` is running on the background. Your code does
not need to rely on callbacks or events to continue execution after the task has been completed. The C#
language does that for you. If you’re using `ControlledTask<T>`, the `await` keyword will additionally
_unwrap_ the value returned when the `ControlledTask` is complete. The details of how `await` works are
further explained in the C# [docs](https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap).

During testing, using `await` allows the Coyote runtime to automatically inject scheduling points and
thoroughly explore asynchronous interleavings to find concurrency bugs.

## What happens under the covers

The C# compiler transforms an `async` method into a state machine (literally called IAsyncStateMachine)
which keeps track of things like yielding execution when an `await` is reached and resuming execution
when a background job has finished.

The `ControlledTask` type uses a C# 7 feature known as `async task types`
(see [here](https://github.com/dotnet/roslyn/blob/master/docs/features/task-types.md)) that allows
framework developers to create custom task types that can be used with `async` and `await`. This is
where the magic happens. In production, `ControlledTask` enables C# to build a custom asynchronous
state machine that uses regular `Task` objects. However, during testing, Coyote uses dependency
injection to supply a custom asynchronous state machine that allows controlling the scheduling of
`ControlledTask` objects, and thus systematically exploring their interleavings.

## How to use asynchronous tasks

We will now show how to write a program using the Coyote asynchronous task programming model. As
mentioned before, the `ControlledTask` type is a drop-in replacement for the `Task` type, and thus any
prior experience writing asynchronous code using `async` and `await` is useful and relevant. If you are
not already familiar with `async` and `await`, you can learn more in the C#
[docs](https://docs.microsoft.com/en-us/dotnet/standard/async-in-depth).

Say that you have the following simple C# program:

```c#
private class SharedEntry
{
    public int Value = 0;
}

public async ControlledTask WriteWithDelayAsync(SharedEntry entry, int value)
{
    await ControlledTask.Delay(100);
    entry.Value = value;
}

public async ControlledTask RunAsync()
{
    SharedEntry entry = new SharedEntry();

    ControlledTask task1 = WriteWithDelayAsync(entry, 3);
    ControlledTask task2 = WriteWithDelayAsync(entry, 5);

    await ControlledTask.WhenAll(task1, task2);

    Specification.Assert(entry.Value == 5, "Value is '{0}' instead of 5.", entry.Value);
}
```

The above program contains a `SharedEntry` type that implements a shared container for an `int` value.
The `WriteWithDelayAsync` is a C# `async` method that asynchronously waits for a `ControlledTask` to
complete after `100`ms (created via the `ControlledTask.Delay(100)` call), and then modifies the value
of the `SharedEntry` object.

The `RunAsync` asynchronous method is creating a new `SharedEntry` object, and then twice invokes the
`WriteWithDelayAsync` method by passing the values `3` and `5` respectively. Each method call returns a
`ControlledTask` object, which can be awaited using `await`. The `RunAsync` method first invokes the
two asynchronous method calls and then calls `ControlledTask.WhenAll(...)` to `await` on the completion
of both tasks.

Because `WriteWithDelayAsync` method awaits a `ControlledTask.Delay` to complete, it will yield control
to the caller of the method, which is the `RunAsync` method. However, the `RunAsync` method is not
awaiting immediately upon invoking the `WriteWithDelayAsync` method calls. This means that the two
calls can happen _asynchronously_, and thus the value in the `SharedEntry` object can be either `3` or
`5` after `ControlledTask.WhenAll(...)` completes.

Using `Specification.Assert`, Coyote allows you to write assertions that check these kinds of safety
properties. In this case, the assertion will check if the value is `5` or not, and if not it will throw
an exception, or report an error together with a reproducible trace during testing.
