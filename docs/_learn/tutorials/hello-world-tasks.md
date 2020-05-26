---
layout: reference
section: learn
title: Hello World Tasks Example
permalink: /learn/tutorials/hello-world-tasks
---

## Hello world example with tasks

The [HelloWorldTasks](http://github.com/microsoft/coyote-samples/) is a simple program to get you
started using the [asynchronous tasks programming
model](/coyote/learn/programming-models/async/overview) with Coyote.

## What you will need

To run the Hello World Tasks  example, you will need to:

- Install [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/).
- Install the [.NET Core 3.1 version of the `coyote` tool](/coyote/learn/get-started/install#installing-the-net-core-31-coyote-tool).
- Clone the [Coyote Samples git repo](http://github.com/microsoft/coyote-samples).
- Be familiar with the `coyote test` tool. See [Testing](/coyote/learn/tools/testing).

## Build the sample

Build the `coyote-samples` repo by running the following command:

```
powershell -f build.ps1
```

## Run the HelloWorldTasks application

Now you can run the HelloWorldTasks application:

```
"./bin/netcoreapp3.1/HelloWorldTasks.exe"
```

Note that in the code there is a bug (put intentionally) that should be caught by a Coyote
assertion. The program should display exactly the string `Hello World!` however sometimes it
displays `Good Morning`. With good luck you will need to run the program only a few times in order
to get the buggy result, but on average this bug happens rarely and may need many manual runs,
sometimes 20 - 30, in order to be reproduced.

The typical "normal" run will look like this:

```
Hello World!
```

When the error is caught, the run may look like this:

```
Good Morning

Unhandled Exception: Microsoft.Coyote.AssertionFailureException: { ... Exception text here }
```

## How to reproduce the bug

There are two ways to reproduce the bug:

The first way is to run `HelloWorldTasks.exe` from the command prompt repeatedly, until you finally
get this exception:

```
Good Morning

Unhandled Exception: Microsoft.Coyote.AssertionFailureException: Value is 'Good Morning' instead of 'Hello World!'.
   at Microsoft.Coyote.Runtime.CoyoteRuntime.Assert(Boolean predicate, String s, Object[] args)
   at Microsoft.Coyote.Samples.HelloWorld.Greeter.<RunAsync>d__4.MoveNext() in C:\git\CoyoteSamples\HelloWorldTasks\Greeter.cs:line 38
```

Although this is one of the simplest Async Tasks programs, you may need to perform many executions
before getting this exception. Even in this simplest example, reproducing the bug may take you a
significant amount of time.

**Here is where Coyote really shines**.

The second way to reproduce the bug is to run the code under `coyote test`.  From the command line you enter:

```
coyote test ./bin/netcoreapp3.1/HelloWorldTasks.dll --iterations 100
```

You will see output like this:

```
. Testing .\bin\netcoreapp3.1\HelloWorldTasks.dll
Starting TestingProcessScheduler in process 36352
... Created '1' testing task.
... Task 0 is using 'random' strategy (seed:914718657).
..... Iteration #1
..... Iteration #2
..... Iteration #3
..... Iteration #4
..... Iteration #5
..... Iteration #6
..... Iteration #7
..... Iteration #8
..... Iteration #9
... Task 0 found a bug.
... Emitting task 0 traces:
..... Writing bin\netcoreapp3.1\Output\HelloWorldTasks.exe\CoyoteOutput\HelloWorldTasks_0_0.txt
..... Writing bin\netcoreapp3.1\Output\HelloWorldTasks.exe\CoyoteOutput\HelloWorldTasks_0_0.schedule
... Elapsed 0.07038 sec.
... Testing statistics:
..... Found 1 bug.
... Scheduling statistics:
..... Explored 9 schedules: 9 fair and 0 unfair.
..... Found 11.11% buggy schedules.
..... Number of scheduling points in fair terminating schedules: 12 (min), 15 (avg), 19 (max).
... Elapsed 0.3880718 sec.
. Done
```

So in just 0.38 seconds `coyote test` has found the bug in this simple program&mdash;something that
would take much longer if testing manually. So, even in this simple case Coyote finds the bug tens
or even hundreds of times quicker. When using Coyote for testing much more complex, real-world
systems the time savings can be even more impressive! This will give you the ability to find and fix
even the most difficult to reproduce concurrency bugs **before pushing to production**.

To learn more about testing with Coyote read [Testing Coyote programs end-to-end and reproducing
bugs](/coyote/learn/tools/testing).

## The code

This section shows how to write a program using the Coyote [asynchronous task programming
model](/coyote/learn/programming-models/async/overview). The Coyote controlled `Task` type is a
drop-in replacement for the `System.Threading.Tasks.Task` type, and thus any prior experience
writing asynchronous code using `async` and `await` is useful and relevant. If you are not already
familiar with `async` and `await`, you can learn more in the C#
[docs](https://docs.microsoft.com/en-us/dotnet/standard/async-in-depth).

The program consists of two code files:
1. `Program.cs` contains the `Main()` entry point of the application.
2. `Greeter.cs` defines the `Greeter` class. Its `RunAsync()` method is where "all the work" is done.

Here is the code for `Greeter.cs`:

```c#
using System;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tasks;

namespace Microsoft.Coyote.Samples.HelloWorldTasks
{
    internal class Greeter
    {
        private const string HelloWorld = "Hello World!";
        private const string GoodMorning = "Good Morning";

        private string Value;

        private async Task WriteWithDelayAsync(string value)
        {
            await Task.Delay(100);
            this.Value = value;
        }

        public async Task RunAsync()
        {
            Task task1 = this.WriteWithDelayAsync(GoodMorning);
            Task task2 = this.WriteWithDelayAsync(HelloWorld);
            Task task3 = this.WriteWithDelayAsync(HelloWorld);
            Task task4 = this.WriteWithDelayAsync(HelloWorld);
            Task task5 = this.WriteWithDelayAsync(HelloWorld);

            await Task.WhenAll(task1, task2, task3, task4, task5);

            Console.WriteLine(this.Value);

            Specification.Assert(this.Value == HelloWorld, $"Value is '{this.Value}' instead of '{HelloWorld}'.");
        }
    }
}
```

`WriteWithDelayAsync()` is a C# `async` method that asynchronously waits for a 100ms delay (created
via the `Task.Delay(100)` call), and then modifies the value of the private `string Value`.

The `RunAsync()` asynchronous method is invoking the `WriteWithDelayAsync()` method five times,
first passing the value `"Good Morning"` and the value `"Hello World!"` for the others. Each method
call returns a `Task` object, which can be awaited upon using `await`. After invoking these
asynchronous method calls The `RunAsync` method calls `Task.WhenAll(...)` to `await` on the
completion of all the async tasks.

Because the `WriteWithDelayAsync()` method awaits a `Task.Delay` to complete, it will yield control
to the caller of the method, which is the `RunAsync()` method. However, the `RunAsync()` method is
not doing any awaiting immediately after invoking the `WriteWithDelayAsync()` method calls. This
means that the five calls will be executed in _parallel_, and thus depending entirely on the thread
scheduling the `Value` member will be set to either `"Good Morning"` or `"Hello World!"` after
`Task.WhenAll(...)` completes.

Using `Specification.Assert`, Coyote allows you to write assertions that check these kinds of safety
properties. In this case, the assertion will check if the value is `"Hello World!"` or not, and if
not, it will throw an exception, or during testing will report an error together with a reproducible
trace.

Definitely, this `Assert` will fire sometimes because the asynchronous tasks do not have a
guaranteed order. Despite having this assertion here on purpose, imagine if in a real project the
programmer who wrote the Assert didn't realize how asynchronous the code was, or perhaps the
programmer who wrote the code didn't read and understand the Assert properly and has violated the
specification in their overly asynchronous implementation. Either way this is a demonstration
showing that assertions like this can be very useful in finding some pretty tricky bugs.

The code for `Program.cs` is much simpler:

```c#
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Samples.HelloWorld;
using Microsoft.Coyote.Tasks;

namespace Microsoft.Coyote.Samples.HelloWorldTasks
{
    public static class Program
    {
        public static async System.Threading.Tasks.Task Main()
        {
            await Execute();
        }

        [Microsoft.Coyote.SystematicTesting.Test]
        public static async Task Execute()
        {
            var greeter = new Greeter();
            await greeter.RunAsync();
        }
    }
}
```

The `static async Task Main()` method is the entry point to the program, as usual for a .NET
executable. If you didn't want the program to be testable with `coyote test`, you could omit the
second method above, and simply write`Main()` as:

```c#
public static async Task Main()
{
    var greeter = new Greeter();
    await greeter.RunAsync();
}
```

However, here the intent is that the code must be testable with `coyote test`. This is done by
including the `Execute()` method and the only thing the `Main()` method does is to call this `async
Execute()` method and await its execution.

It is a rule in Coyote that for any executable to be testable with `coyote test`, it must contain a
static `Execute()` method which must have a specific, signature. In this code one of these allowed
signatures for the method is specified.

Do note the `[Microsoft.Coyote.SystematicTesting.Test]` attribute of the `Execute()` method, and its
return type: `Microsoft.Coyote.Tasks.Task`. When these are specified, then `coyote test` can find
the `Execute()` method in the executable assembly and can control its execution.

The Coyote `Task` type uses a C# 7 feature known as `async task types` (see
[here](https://github.com/dotnet/roslyn/blob/master/docs/features/task-types.md)) that allows framework
developers to create custom task types that can be used with `async` and `await`. This is where the
magic happens. In production, `Task` enables C# to build a custom asynchronous state machine that
uses regular `Task` objects. However, during testing, Coyote uses dependency injection to supply
another custom implementation of an asynchronous state machine that allows controlling the
scheduling of `Task` objects, and thus systematically explore their interleavings.

Also note that with `coyote test` the `Main()` method is not executed! This is why it is recommended
that the `Main()` method should not contain any other code except awaiting the `Execute()` method.
If it does contain other actions, those actions would not be executed by `coyote test`.  If you need
to pass information from `Main` to `Execute` you can use a static variable with a default value. This
way the `Execute` will run with the default value during `coyote test`.

## Summary

In this tutorial you learned:
1. [How to install and build Coyote](#build-the-sample)
2. [How to build the samples used in the tutorials](#build-the-sample)
3. [How to run the HelloWorldTasks sample from the
   command-line](#run-the-helloworldtasks-application)
4. [Using the controlled `Task` type to enable Coyote to control the scheduling of the
   program](/coyote/learn/programming-models/async/overview)
5. [How to write code that is Coyote-testable](#coyote-testable)
6. [How testing with Coyote results in finding even the trickiest concurrency bugs quickly even
   before pushing to production.](#how-to-reproduce-the-bug)
