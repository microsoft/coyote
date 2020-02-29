---
layout: reference
section: learn
title: Hello World Actors Example
permalink: /learn/tutorials/hello-world-actors
---

## Hello world example with actors

The [HelloWorldActors](http://github.com/microsoft/coyote-samples/) is a simple program to get you started
using the Coyote [actors programming model](/coyote/learn/programming-models/actors/overview).

## What you will need

To run the Hello World Actors  example, you will need to:

- Install [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/).
- Build the [Coyote project](/coyote/learn/get-started/install).
- Clone the [Coyote Samples git repo](http://github.com/microsoft/coyote-samples).

## Build the Sample

Build the `coyote-samples` repo by running the following command:

```
powershell -f build.ps1
```

## Run the HelloWorldActors application

Now you can run the HelloWorldActors application:
- in .Net Core:

```
dotnet .\bin\netcoreapp2.2\HelloWorldActors.dll
```

- in .Net 4.6:

```
.\bin\net46\HelloWorldActors.exe
```

- in .Net 4.7:

```
.\bin\net47\HelloWorldActors.exe
```

**Note that in the code there is a bug** (put intentionally) that should be caught by a Coyote assertion.
The program should display the English greeting `Hello World!` exactly once -- as the first of several
greetings displayed in different languages, however sometimes it displays the English greeting `Hello World!`
for a second time during the execution of `HelloWorldActors`.

With good luck you will need to run the program only a few times in order to get the buggy result,
but on average this bug happens rarely and may need many manual runs, sometimes 20 - 30, in order
to be reproduced.

The typical "normal" run will look like this:

```
C:\git\CoyoteSamples\bin\net46>HelloWorldActors
Greeting in English             : Hello World!
Greeting in Belarussian         : Прывітанне Сусвет!
Greeting in German              : Hallo Welt!
Greeting in Polish              : Witaj świecie!
Greeting in Indonesian          : Halo Dunia!
Greeting in German              : Hallo Welt!
Greeting in Bulgarian           : Здравей свят!

C:\git\CoyoteSamples\bin\net46>
```

When the error is caught, the run may look like this:

```
C:\git\CoyoteSamples\bin\net46>HelloWorldActors
Greeting in English             : Hello World!
Greeting in German              : Hallo Welt!
Greeting in Finnish             : Hei maailma!
Greeting in English             : Hello World!
Exception of type Microsoft.Coyote.Runtime.AssertionFailureException { ... Exception text here }
```

## How to reproduce the bug

There are two ways to reproduce the bug:

The first way is to run `HelloWorldActors.exe` from the command prompt repeatedly, until you finally get this exception:

```
Greeting in English             : Hello World!
Greeting in Mongolian           : Сайн уу дэлхий!
Greeting in English             : Hello World!
Exception of type Microsoft.Coyote.Runtime.AssertionFailureException: Message: The starting Greeting in English was duplicated but this should never happen.
```

Although this is one of the programs you can write with Actors, you may need to perform many executions before getting this
exception. Even in this modest example, reproducing the bug may take you significant amount of time.

**This is where Coyote really shines**:

The second way to reproduce the bug is to run the code under `coyote test`.

From the command line you enter:

```
coyote test .\bin\net46\HelloWorldActors.exe --iterations 30
```

The result is:

```
C:\git\CoyoteSamples>coyote test .\bin\net46\HelloWorldActors.exe --iterations 30
. Testing .\bin\net46\HelloWorldActors.exe
Starting TestingProcessScheduler in process 16040
... Created '1' testing task.
... Task 0 is using 'Random' strategy (seed:102).
..... Iteration #1
..... Iteration #2
... Task 0 found a bug.
... Emitting task 0 traces:
..... Writing .\bin\net46\Output\HelloWorldActors.exe\CoyoteOutput\HelloWorldActors_0_1.txt
..... Writing .\bin\net46\Output\HelloWorldActors.exe\CoyoteOutput\HelloWorldActors_0_1.pstrace
..... Writing .\bin\net46\Output\HelloWorldActors.exe\CoyoteOutput\HelloWorldActors_0_1.schedule
... Elapsed 0.2155411 sec.
... Testing statistics:
..... Found 1 bug.
... Scheduling statistics:
..... Explored 2 schedules: 2 fair and 0 unfair.
..... Found 50.00% buggy schedules.
..... Number of scheduling points in fair terminating schedules: 38 (min), 38 (avg), 39 (max).
... Elapsed 0.4192499 sec.
. Done
```

Thus in less than a half-second the Coyote tester has found the bug in this simple program -- something that would take many minutes if testing manually.
So, even in this simple case Coyote finds the bug tens or even hundreds of times faster.
But when using Coyote for testing much more complex, real-world systems the time savings can be even more impressive!
This will give you the ability to find out and fix even the trickiest and almost impossible to reproduce concurrency
bugs **before pushing your code to production**.

To learn more about testing with Coyote read [Testing Coyote programs end-to-end and reproducing bugs](/coyote/learn/tools/testing).

## The Code

This section shows how to write a program using the Coyote Actors programming model. It is recommended that you read
[Programming model: asynchronous actors](/coyote/learn/programming-models/actors/overview) section to gain a basic understanding
of the Actors programming model before proceeding with the code below. Prior experience writing asynchronous code using `async`
and `await` is also useful and relevant. If you are not already familiar with `async` and `await`, you can learn more
in the C# [docs](https://docs.microsoft.com/en-us/dotnet/standard/async-in-depth).

The program consists of three code files and an additional file containing the text of the "Hello World!" greetings in 53 languages.

* `Program.cs` contains the `Main()` entry point of the application.

* `Server.cs` defines the `Server` class. This is a Coyote [`Actor`](/coyote/learn/programming-models/actors/overview).
Every `Actor` runs concurrently with respect to other Actors, but individually it handles its input queue in a sequential way.
When an event arrives, the actor dequeues that event from the input queue and handles it by executing a sequence of operations.

* `Client.cs` defines the `Client` class. This is a Coyote [`StateMachine`](/coyote/learn/programming-models/actors/state-machines).
A Coyote state machine is a special type of `Actor` that inherits from `StateMachine` and adds `State` semantics with explicit
information about how `Events` can trigger `State` changes in that `StateMachine`. State machines are actors, but also have explicit states,
and state transitions.

Let's study the code in detail. You can find the full code in the [Coyote Samples git repo](http://github.com/microsoft/coyote-samples).

Here is the code for `Server.cs`:

```c#
[OnEventDoAction(typeof(GreetMeEvent), nameof(SendGreeting))]
internal class Server : Actor
{
    internal class GreetMeEvent : Event
    {
        public ActorId Requestor;

        public GreetMeEvent(ActorId requestor)
        {
            this.Requestor = requestor;
        }
    }

    private void SendGreeting(Event e)
    {
        ActorId client = (e as GreetMeEvent).Requestor;

        var index = this.RandomInteger(Translations.LanguagesCount);
        string language = Translations.Languages[index];
        string greeting = Translations.HelloWorldTexts[language];

        this.SendEvent(client, new Client.GreetingProducedEvent() { Language = language, Greeting = greeting });
    }
}
```

The `Server` class inherits from the standard Coyote `Actor` type. Internally it defines a `GreetMeEvent` type that inherits from
the standard Coyote `Event` type. The `GreetMeEvent` event has a `Requestor` property which contains the `ActorId` of the `Actor`
that is the sender of the event.

The `[OnEventDoAction(typeof(GreetMeEvent), nameof(SendGreeting))]` attribute on the `Server` class specifies that any `GreetMeEvent`
in the `Server`'s input queue must be processed by a call to its `SendGreeting()` method.

The `SendGreeting()` method performs a sequence of operations:

1. Retrieves the `ActorId` of the sender from the `GreetMeEvent` object passed as parameter.

2. Gets a random number with which to index the list of all languages for which there are "Hello World!" translations.

3. Using this index, determines the language of the translation of the greeting that will be sent in response to the sender of the
`GreetMeEvent`.

4. Gets the greeting using the language as a key into  `Translations.HelloWorldTexts`.

5. Finally, the `Server` sends a `GreetingProducedEvent` back to the `Client` who sent the `GreetMeEvent` being processed.
The properties of the generated `GreetingProducedEvent` are set to the `language` and `greeting` determined in the
steps above.

Here is the code for `Client.cs` with details elided:

```c#
internal class Client : StateMachine
{
    private TaskCompletionSource<bool> CompletionSource;
    private ActorId Server;
    private long MaxRequests = long.MaxValue;

    private long GreetMeCounter = 0;

    private const string English = "English";
    private const string HelloWorldEnglish = "Hello World!";

    internal class ConfigEvent : Event . . .

    internal class GreetingProducedEvent : Event . . .

    private class ReadyEvent : Event { }

    [Start]
    [OnEntry(nameof(InitOnEntry))]
    [OnEventGotoState(typeof(ReadyEvent), typeof(Active))]
    private class Init : State { }

    private void InitOnEntry(Event e) . . .

    [OnEntry(nameof(ClientActiveEntry))]
    [OnEventDoAction(typeof(GreetingProducedEvent), nameof(HandleGreeting))]
    private class Active : State { }

    private void ClientActiveEntry() . . .

    private void HandleGreeting(Event e) . . .

    private void RequestGreeting() . . .

    private void TerminateMachines() . . .
}
```

The `Client` has two states: `Init` and `Active`.

It defines a number of events: `ConfigEvent`, `ReadyEvent` and `GreetingProducedEvent`. The client also uses the
`GreetMeEvent` that is defined by the `Server`

The `Client` has three members:

* `ActorId Server` -- the Id of the `Server` to which to send events.
* `long MaxRequests` -- how many requests for greetings (`GreetMeEvent` events) to send to the `Server` before finishing work.
* `TaskCompletionSource<bool> CompletionSource` -- A standard .NET [`TaskCompletionSource`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.tasks.taskcompletionsource-1?view=netframework-4.7.2)
which is used to indicate when all asynchronous work instigated by the Client has completed,
so that the external creator of the `Client` can wait for the completion of the work of the system.

Upon creation of the `Client` (by code in `Program.cs`), its first, initial, starting state is `Init`, as marked by the `[Start]` attribute,
and the first method to be executed is `InitOnEntry()` as specified on another attribute of the `Init` state:

`[OnEntry(nameof(InitOnEntry))]`

The `InitOnEntry()` method receives as payload a `ConfigEvent`, and it uses `ConfigEvent`'s members to set the members of the `Client` object:

```c#
private void InitOnEntry(Event e)
{
    ConfigEvent configEvent = e as ConfigEvent;
    this.CompletionSource = configEvent.CompletionSource;
    this.Server = configEvent.OtherParty;
    this.MaxRequests = configEvent.MaxRequests;

    this.RaiseEvent(new ReadyEvent());
}
```

The method above raises a `ReadyEvent`. As specified by this attribute of the `Init` state:

```c#
[OnEventGotoState(typeof(ReadyEvent), typeof(Active))]
```

receiving the `ReadyEvent` causes a transition to a new state -- the `Active` state:

```c#
[OnEntry(nameof(ClientActiveEntry))]
[OnEventDoAction(typeof(GreetingProducedEvent), nameof(HandleGreeting))]
private class Active : State { }
```

This code specifies that on entry to the `Active` state the method `ClientActiveEntry()` is executed. Then the only event
that is expected and acted upon in the `Active` state is the `GreetingProducedEvent`, handled by the `HandleGreeting()` method.

Here is the code of these two methods:

```c#
private void ClientActiveEntry()
{
    Console.WriteLine($"Greeting in {English, -20}: {HelloWorldEnglish}");
    this.RequestGreeting();
}
```

The only thing that the `ClientActiveEntry()` method does is to print the well-known English "Hello World!" greeting
and then call the `RequestGreeting()` method which, as its name suggests, sends a `Server.GreetMeEvent` to the Server.
The `Server` is then expected to send back to the `Client` a `GreetingProducedEvent` that contains the language and greeting
as selected from one of 53 languages. However, the `Server` must not return a greeting in English -- if it does so, this
is a bug.

Note that that the DOS Console has limited capability for displaying UTF-8 characters. Thus greetings in some non-Latin
alphabets might be displayed incorrectly. In order to see all greetings displayed correctly, you can redirect the output
to a file and then view it with your favorite editor / tool.

Here is the code for the `HandleGreeting()` method which is invoked when a `GreetingProducedEvent` is received in the `Active` state:

<a name="specification-assert"> </a>
```c#
private void HandleGreeting(Event e)
{
    try
    {
        var greeting = (GreetingProducedEvent)e;
        Console.WriteLine($"Greeting in {greeting.Language, -20}: {greeting.Greeting}");

        Specification.Assert(greeting.Language != English, $"The starting Greeting in {English} was duplicated but this should never happen.");

        this.RequestGreeting();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Exception of type {ex.GetType()}: Message: {ex.Message}");

        this.TerminateMachines();
    }
}
```

This method simply "unpacks" the `Language` and `Greeting` from the `GreetingProducedEvent` and then writes them on the `Console`.
At this point, a Coyote `Specification.Assert()` is invoked to assert that the language of the received greeting is not English.

If this is positive (no exception is thrown), then the `RequestGreeting()` method is called.

If the `Specification.Assert()` fails, which means that you have reproduced the bug, this raises an exception, which is caught
in the `catch` clause of the `try`/`catch` block. In the `catch` clause the type of the exception and its `Message` are output
to the `Console`. Then the `TerminateMachines()` method is called, which terminates both the `Server` and the `Client` and
completes the `TaskCompletionSource` so that whoever waits on its `Task` is notified.

Using `Specification.Assert`, Coyote allows you to write assertions that check various kinds of safety
properties. In this case, the assertion will check whether the value of `greeting.Language` is "English"  or not, and if it is,
in production it will throw an exception, or during testing it will report an error together with a reproducible trace.

Definitely, this Assertion will fire sometimes because calling `this.RandomInteger(Translations.LanguagesCount)` in the `Server`
must produce a random integer in the interval [0, `Translations.LanguagesCount`). A value of `0` for the index will cause English
to be selected as the language. Despite having this assertion here on purpose, imagine if in a real project the programmer
who wrote the code didn't read and understand the Assert properly and violated the specification.
This is a demonstration showing that Coyote assertions can be very useful in finding some pretty tricky bugs.

The `RequestGreeting()` method is called by the `OnInitEntry` of the `Active` state to send the first request for greeting. It is also called by the
`HandleGreeting()` method to send the next requests for greeting to the `Server`. Here is the code of `RequestGreeting()`:

```c#
private void RequestGreeting()
{
   this.GreetMeCounter++;
   if (this.GreetMeCounter >= this.MaxRequests)
    {
        // terminate the client and the server
        this.TerminateMachines();
    }
    else
    {
        this.SendEvent(this.Server, new Server.GreetMeEvent(this.Id));
    }
}
```

This simply increments the `GreetMeCounter` and checks if the specified maximum number of greetings have already been received.
If there are still some greetings to be received, a new `GreetMeEvent` is sent to the `Server`. Notice the `GreetMeEvent`
is created with the `ActorId` of the sender which is the `Client` in this case.
Thus the `Server` doesn't actually care what Actor the request comes from, all it needs is any ActorId
so it can correctly return the expected `GreetingProducedEvent`.

In case the specified maximum number of greeting requests have already been made, both the `Client` and the `Server` are terminated
by calling the `TerminateMachines()` method:

<a name="terminate-actors"> </a>

```c#
private void TerminateMachines()
{
    this.SendEvent(this.Server, HaltEvent.Instance);
    this.SendEvent(this.Id, HaltEvent.Instance);
    this.CompletionSource.SetResult(true);
}
```

This method sends a `HaltEvent.Instance` event both to the `Server` and to the `Client` itself. Sending an instance of the `HaltEvent` to an actor
(including one that is a state machine) puts them in the `Halted` state. You can read more about the ways to terminate an actor in the
[Explicit termination of an actor](/coyote/learn/programming-models/actors/termination) documentation.

The `TerminateMachines()` method above finally sets the result (completes) the `TaskCompletionSource` that has been provided to the
`Client` by its creator, thus awakening the `HostProgram` that is waiting on this TaskCompletionSource.

The last code file in this sample is `Program.cs`. It contains the `Main()` entry point to the sample and an `Execute()`.
This separate `Execute()` method makes Coyote testing possible:

<a name="coyote-testable"> </a>
```c#
public static class HostProgram
{
    private static long MaxGreetings = 7;

    private static readonly TaskCompletionSource<bool> CompletionSource = new TaskCompletionSource<bool>();

    public static async Task Main(string[] args)
    {
        if (args != null && args.Length > 0)
        {
            MaxGreetings = long.Parse(args[0]);
        }

        IActorRuntime runtime = ActorRuntimeFactory.Create();
        Execute(runtime);

        await CompletionSource.Task;
    }

    [Microsoft.Coyote.TestingServices.Test]
    public static void Execute(IActorRuntime runtime)
    {
        ActorId serverId = runtime.CreateActor(typeof(Server));
        runtime.CreateActor(typeof(Client), new Client.ConfigEvent(CompletionSource, serverId, MaxGreetings));
    }
}
```


The `Main()` entry point of the sample obtains from the command prompt the value for `MaxGreetings`, creates
an `ActorRuntime` instance and then calls the `Execute()` method passing this runtime to it. Then it awaits on the
`Task` of the (`TaskCompletionSource`) `CompletionSource` static member, which must be completed when the sample
finishes work or if any exception is caught.


The `Execute()` method creates both the `Server` actor and the `Client` state machine, then passes to the created `Client` a `ConfigEvent`
with payload containing the `Server` Id, the value for `MaxGreetings`, and the `TaskCompletionSource`
on whose `Task`'s property to wait for completion.

The intent is that the code must be testable with the Coyote tester. This is done by including the
`Execute()` method and the only thing the `Main()` method does is to call this `async Execute()` method and await its
execution.

It is a rule in Coyote that for any executable to be testable with the Tester, it must contain a static
method which must have a specific, predefined signature, such as one of these:

```c#
[Microsoft.Coyote.TestingServices.TestAttribute]
public static void Execute() { ... }

[Microsoft.Coyote.TestingServices.TestAttribute]
public static void Execute(IActorRuntime runtime) { ... }

[Microsoft.Coyote.TestingServices.TestAttribute]
public static async ControlledTask Execute() { ... await ... }

[Microsoft.Coyote.TestingServices.TestAttribute]
public static async ControlledTask Execute(IActorRuntime runtime) { ... await ... }
```

In this code one of these allowed signatures for the `Execute()` method is specified.

Do note the `[Microsoft.Coyote.TestingServices.Test]` attribute of the `Execute()` method. When this is specified,
then the Coyote tester can find the `Execute()` method in the executable assembly
and can control its execution.

Also note that **when running under the Coyote tester the `Main()` method is not executed**!
This means in our example, the test does not run with any command line arguments specifying `MaxGreetings`.
This is why we statically initialize `MaxGreetings` to an interesting number, 7 in this example.
This is a similar restriction that other unit testing frameworks have where test parameters are either
specified declaratively in custom attributes or in a config file that the test loads.


## Summary
In this tutorial you learned:
1. [How to install and build Coyote](#what-you-will-need)
2. [How to build the samples used in the tutorials](#build-the-sample)
3. [How to run the HelloWorldActors sample from the command-line](#run-the-helloworldtasks-application)
4. [How Actors behave: run concurrently, but handle their input queues in a sequential way](#the-code)
5. [What are State Machines: actors that also have explicit states and state transitions](/coyote/learn/programming-models/actors/state-machines)
6. [How Specification.Assert helps find violations of safety properties by Coyote in an automated way](#specification-assert)
7. [What is one way to terminate an actor or a state machine: send a HaltEvent to it](#terminate-actors)
8. [How to write code that is Coyote-testable](#coyote-testable)
9. [How testing with Coyote results in finding even the trickiest concurrency bugs quickly, even before pushing code to production.](#how-to-reproduce-the-bug)