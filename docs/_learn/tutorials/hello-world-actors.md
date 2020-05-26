---
layout: reference
section: learn
title: Hello World Actors Example
permalink: /learn/tutorials/hello-world-actors
---

## Hello world example with actors

[HelloWorldActors](http://github.com/microsoft/coyote-samples/) is a simple program to get you
started using the Coyote [actors programming
model](/coyote/learn/programming-models/actors/overview).

## What you will need

To run the Hello World Actors  example, you will need to:

- Install [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/).
- Install the [.NET Core 3.1 version of the `coyote` tool](/coyote/learn/get-started/install#installing-the-net-core-31-coyote-tool).
- Clone the [Coyote Samples git repo](http://github.com/microsoft/coyote-samples).
- Be familiar with the `coyote test` tool. See [Testing](/coyote/learn/tools/testing).

## Build the sample

Build the `coyote-samples` repo by running the following command:

```
powershell -f build.ps1
```

## Run the HelloWorldActors application

Now you can run the HelloWorldActors application:

```
"./bin/netcoreapp3.1/HelloWorldActors.exe"
```

Press the ENTER key to terminate the program when it is done. Note that a bug has been inserted into
the code intentionally and will be caught by a Coyote `Assert`. The program should display 1 to 5
greetings like `Hello World!` or `Good Morning`. The intentional bug is hard to reproduce when you
run the program manually.

The typical "normal" run will look like this:

```
press ENTER to terminate...
Requesting 5 greetings
Received greeting: Good Morning
Received greeting: Hello World!
Received greeting: Hello World!
Received greeting: Hello World!
Received greeting: Good Morning
```

When the error is caught, an extra line of output is written saying:

```
Exception 'Microsoft.Coyote.AssertionFailureException' was thrown in 0 (action '1'):
Too many greetings returned!
```

## Finding the bug using Coyote test tool

See [Using Coyote](/coyote/learn/get-started/using-coyote) for information on how to
find the `coyote` test tool and setup your environment to use it.

Enter the following from the command line:

```
coyote test ./bin/netcoreapp3.1/HelloWorldActors.dll --iterations 30
```

The result is:

```
. Testing .\bin\netcoreapp3.1\HelloWorldActors.dll
Starting TestingProcessScheduler in process 16432
... Created '1' testing task.
... Task 0 is using 'random' strategy (seed:308255541).
..... Iteration #1
..... Iteration #2
... Task 0 found a bug.
... Emitting task 0 traces:
..... Writing .\bin\netcoreapp3.1\Output\HelloWorldActors.exe\CoyoteOutput\HelloWorldActors_0_2.txt
..... Writing .\bin\netcoreapp3.1\Output\HelloWorldActors.exe\CoyoteOutput\HelloWorldActors_0_2.schedule
... Elapsed 0.0906639 sec.
... Testing statistics:
..... Found 1 bug.
... Scheduling statistics:
..... Explored 2 schedules: 2 fair and 0 unfair.
..... Found 50.00% buggy schedules.
..... Number of scheduling points in fair terminating schedules: 23 (min), 30 (avg), 37 (max).
... Elapsed 0.1819877 sec.
. Done

```

The Coyote tester has found the bug very quickly&mdash;something that takes much longer if testing
manually. When using Coyote for testing more complex, real-world systems the time savings can be
huge! This will give you the ability to find and fix even the most difficult to reproduce
concurrency bugs **before pushing to production**.

To learn more about testing with Coyote read [Testing Coyote programs end-to-end and reproducing
bugs](/coyote/learn/tools/testing).

## The Greeter

Please read [Programming model: asynchronous
actors](/coyote/learn/programming-models/actors/overview) to gain a basic understanding of the
Actors programming model.

The core of an `Actor` based Coyote program, is, well, an `Actor` class! The following `Actor` named
`Greeter` is able to receive a `RequestGreetingEvent` to which it responds with a `GreetingEvent`:

```c#
[OnEventDoAction(typeof(RequestGreetingEvent), nameof(HandleGreeting))]
public class Greeter : Actor
{
    /// This method is called when this actor receives a RequestGreetingEvent.
    private void HandleGreeting(Event e)
    {
        if (e is RequestGreetingEvent ge)
        {
            string greeting = this.RandomBoolean() ? "Hello World!" : "Good Morning";
            this.SendEvent(ge.Caller, new GreetingEvent(greeting));
            if (this.RandomBoolean(10))
            {
                // bug: a 1 in 10 chance of sending too many greetings.
                this.SendEvent(ge.Caller, new GreetingEvent(greeting));
            }
        }
    }
}
```

Notice the `OnEventDoAction` custom attribute decorating the class.  This tells the Coyote runtime
that this class expects to receive `RequestGreetingEvents`.  These events will be queued in the
inbox for this `Actor` until the right time to dequeue them and process them. The custom attribute
tells the `Coyote` runtime to call the `HandleGreeting` method.  The Coyote runtime will also report
an error if a different type of event is sent to the `Greeter`.

The `HandleGreeting` method is called with an `Event` parameter, and you know in this case the
event will be of type `RequestGreetingEvent`.  This event is defined in `Events.cs` like this:

```c#
internal class RequestGreetingEvent : Event
{
    public readonly ActorId Caller;

    public RequestGreetingEvent(ActorId caller)
    {
        this.Caller = caller;
    }
}
```

## The inbox is crucial to the Actor model

You can think of an `Event` declaration as a type of **interface** definition for `Actors`.  Events
really define the interface of an `Actor` because the caller will never get to see the `Greeter`
class, so the caller can't just call `HandleGreeting` directly. The caller only gets to see an
`ActorId`, which is like a Coyote runtime handle to the actor.

**There is a really important reason for this**.

Events are queued in an inbox managed by the `Actor` base class.  This serializes the incoming
events so that at any given time, only one event is being handled in the `Greeter`.  This greatly
simplifies concurrent programming. For example, there is no need to use `lock` to protect against
data race conditions, and therefore no need to worry about deadlocks.  Each `Actor` instance can run
in a separate thread, so all actors in the system can be hugely parallel, but at the same time the
processing inside an `Actor` is incredibly simple.  So you get the best of both worlds, code that
scales but is also easy to write.  An `Actor` receives messages, and can create other `Actor`
objects, and can send events.

Notice the `Greeter` gets an `ActorId` from the `RequestGreetingEvent` and uses that to send a
greeting back to the caller using `SendEvent`:

```c#
this.SendEvent(ge.Caller, new GreetingEvent(greeting));
```

You can also see the bug which has been injected deliberately, namely, it randomly returns more than
one `GreetingEvent`.

## A TestActor

To test this `Greeter` you will need to setup a `TestActor` which is done in `Program.cs`. The
`TestActor` creates the `Greeter` and sends between 1 and 5 `RequestGreetingEvents`. The `TestActor`
is declared in a way that tells the `Coyote` runtime it is expecting to receive `GreetingEvents` as
follows:

```c#
 [OnEventDoAction(typeof(GreetingEvent), nameof(HandleGreeting))]
 ```

 To make this a good test, the `TestActor` keeps track of how many greetings were sent and how many
 were received, and it writes an `Assert` to ensure it doesn't receive too many as follows:

 ```c#
private void HandleGreeting(Event e)
{
    // this is perfectly thread safe, because all message handling in actors is
    // serialized within the Actor class.
    this.Count--;
    string greeting = ((GreetingEvent)e).Greeting;
    Console.WriteLine("Received greeting: {0}", greeting);
    this.Assert(this.Count >= 0, "Too many greetings returned!");
}
```

Lastly, to test the `TestActor` you need to fire up the Coyote runtime which is done in the `HostProgram`
`Main` entry point as follows:

```c#
public static void Main()
{
    var config = Configuration.Create();
    IActorRuntime runtime = RuntimeFactory.Create(config);
    Execute(runtime);

    runtime.OnFailure += OnRuntimeFailure;

    Console.WriteLine("press ENTER to terminate...");
    Console.ReadLine();
}
```

Notice that Coyote actors run in parallel, so you have to stop the program from terminating
prematurely, which can be done with a `Console.ReadLine` call.  In order for this program to also be
testable using the `coyote test` tool, you need to declare a test method as follows:

```c#
[Microsoft.Coyote.SystematicTesting.Test]
public static void Execute(IActorRuntime runtime)
{
    runtime.CreateActor(typeof(TestActor));
}
```

The `coyote test` tool will call this method.  But it will **not** call your `Main` entry point. So
this `Execute` method needs to be standalone.  It cannot depend on anything except statically
initialized variables.  There is no way to get command line arguments from `coyote` test through to
this method.

This `Execute` method is very simple, it just creates the `TestActor`.  In Coyote once an actor is
created it lives forever until it is halted.  Note that Coyote programs can run forever, the `coyote test`
tool has ways of interrupting and restarting this `Execute` method based on `--iterations` and
`--max-steps` arguments provided to the test tool.

So now you know what happened when you ran the following command line:

```
coyote test ./bin/netcoreapp3.1/HelloWorldActors.exe --iterations 30
```

A special coyote `TestEngine` was created, it invoked the `Execute` method 30 times, and during
those executions the test engine took over all concurrent activity and the non-determinism (like
`RandomInteger`) to ensure the program covered lots of async non-deterministic choices, and recorded
all this in a way that when a bug was found, it is able to reproduce that bug.  See also [Using
Coyote](/coyote/learn/get-started/using-coyote) for information on how to replay a test that found a bug
using `coyote replay`.

## Summary

In this tutorial you learned:
1. How to create an `Actor` and use `SendEvent` to send messages to it.
2. How an `Actor` can handle events using `OnEventDoAction`.
3. The importance of the inbox to simplify parallel programming.
4. How to run the HelloWorldActors sample from the command-line.
5. How to use `Assert` in Coyote code.
6. How to write a `[Test]` method for use in `coyote test` runs.
7. How to run that test using the `coyote test` tool.
