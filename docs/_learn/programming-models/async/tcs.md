---
layout: reference
title: Controlled TaskCompletionSource
section: learn
permalink: /learn/programming-models/async/tcs
---

## Controlled task completion source

The Coyote asynchronous tasks programming model provides a
`Microsoft.Coyote.Tasks.TaskCompletionSource<TResult>` type that is controlled by `coyote test`
during systematic testing to explore interleavings and find concurrency bugs. In production, this
type is simply a thin wrapper over the native .NET
[`System.Threading.Tasks.TaskCompletionSource<TResult>`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskcompletionsource-1)
type, providing the same operational semantics that you are familiar with.

## Why is a task completion source necessary?

In many asynchronous scenarios, it is useful to create a `Task<TResult>` and pass it to one or more
consumers, which can then `await` for this task to complete to get its result. The controlled
`TaskCompletionSource<TResult>` type of Coyote acts as the producer for such a `Task<TResult>`. The
owner of the `TaskCompletionSource<TResult>` is able to control the state of the produced
`Task<TResult>` and complete it by setting its result. You can learn more about the semantics of a
task completion source in the .NET documentation of the native [`TaskCompletionSource<T>`
type](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskcompletionsource-1).

## How do you use it?

1. First, create a new controlled `TaskCompletionSource<TResult>` using the static method
   `TaskCompletionSource.Create<TResult>()`.
2. Then, you can get the controlled `Task<TResult>` associated with this task completion source,
   using the `TaskCompletionSource.Task` property, and share it with one or more consumers.
3. Finally, you can use the `TaskCompletionSource` to complete the produced `Task<TResult>`. You can
   do this using the methods of the controlled task completion source:
   - `SetResult()`
   - `SetException()`
   - `SetCanceled()`, or their `TrySet*` variants:
   - `TrySetResult()`
   - `TrySetException()`
   - `TrySetCanceled()`

Why are there two sets of methods, namely, the `Set*` methods that return `void` and their `TrySet*`
variants that return `bool`? The reason is that the controlled `Task<TResult>` associated with the
`TaskCompletionSource<TResult>` may only be completed once, thus attempting to set this task into a
completed state when it's already in a completed state is an error, and the `Set*` methods will
throw. However, as we're dealing with concurrency here, and there are some situations where races
may be expected between multiple threads trying to resolve the completion source, the `TrySet*`
variants return `bool` indicating success rather than throwing an exception.

## Example code

The code below demonstrates a controlled `TaskCompletionSource<TResult>` in action. This represents
the most typical scenario in which programmers need and use a task completion source, namely to know
when a long-running operation, that itself is not awaitable, has finished.

```c#
using Microsoft.Coyote.Tasks;

public void StartLongOperation(TaskCompletionSource<bool> tcs)
{
    Console.WriteLine("Producer: Running a long operation...");
    // ... do something that takes a long time.
    // This may include calling a 3rd party library that is not awaitable.
    Console.WriteLine("Producer: Completed the long operation.");
    tcs.SetResult(true);
    Console.WriteLine("Producer: Completed the task completion source.");
}
```

The above `StartLongOperation` method is responsible for running some long operation. Although the
method is not awaitable, as it returns `void`, it can signal its completion by setting the result of
the controlled `TaskCompletionSource<bool>` to `true`.

A consumer of the task produced by this task completion source can await on its completion using the
following code:

```c#
using Microsoft.Coyote.Tasks;

// The input task is the task produced by the task completion source.
public async Task Consumer(Task<bool> task)
{
    Console.WriteLine("Consumer: Waiting the long operation to complete...");
    await task;
    Console.WriteLine("Consumer: The long operation has completed so we can proceed with other work.");
}
```

You can now use a `TaskCompletionSource` to coordinate this long running operation with another
async `Task` that needs to wait on the completion of that operation like this:

```c#
private async Task RunTest()
{
    var completion = TaskCompletionSource.Create<bool>();
    this.StartLongOperation(completion);
    await this.Consumer(completion.Task);

    Console.WriteLine("Main test complete!");
}
```

The output of this program will look something like this:

```
Producer: Running a long operation...
Consumer: Waiting the long operation to complete...
Producer: Completed the long operation.
Consumer: The long operation has completed so we can proceed with other work.
Main test complete!
Producer: Completed the task completion source.
```

**Note**: You must be careful to specify the namespace `Microsoft.Coyote.Tasks` to use the
controlled `TaskCompletionSource<TResult>`, otherwise you could get the native .NET
`TaskCompletionSource<TResult>` instead!

You might also be puzzled by a build error if you try and do this:

```c#
static async Task Main(string[] args)
{
    Program p = new Program();
    await p.RunTest();
}
```

saying:
```
error CS5001: Program does not contain a static 'Main' method suitable for an entry point
```

The reason is C# doesn't know about the coyote `Task`, so the fix is to fully qualify that Main entry point task like this:

```c#
static async System.Threading.Tasks.Task Main(string[] args)
{
    Program p = new Program();
    await p.RunTest();
}
```

Hopefully this is the only place in your code that you will need to explicitly use a native .NET `Task`.