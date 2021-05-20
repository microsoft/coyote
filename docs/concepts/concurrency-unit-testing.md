## Concurrency unit testing

Coyote gives you the ability to write _concurrency unit tests_. These simple but very powerful tests
look similar to traditional (sequential) tests, but allow (and encourage) the use of concurrency and
[non-determinism](non-determinism.md), which you would normally avoid due to flakiness. By writing
such a test, Coyote allows you to exercise what happens, for example, when [two requests execute
concurrently](../tutorials/first-concurrency-unit-test.md) in your service, as if it was
deployed in production.

Now the cool thing is that once a bug is found, Coyote allows you to fully reproduce the exact same
trace that led to the bug 100% of the time, and as many times as you want. Coyote achieves this by
automatically replaying all nondeterministic choices in your test that led to this bug. This is a
game changer!

Let's see how this kind of testing works in practice.

## Systematic testing of unmodified programs

During testing, Coyote takes over the non-determinism in your program. Once Coyote has control over
the non-determinism, it will repeatedly run the concurrency unit test from start to completion, each
time exercising a different set of non-deterministic choices, offering much better coverage than
using traditional techniques such as stress testing (which rely on luck). This type of testing that
Coyote performs is known as _systematic testing_.

This powerful testing ability, however, has one requirement: you must declare _all_ sources of
non-determinism in your logic in a way that Coyote understands so that it is able to reproduce any
nondeterministic bug that it finds and help you easily debug the issue. Luckily, in most common
cases you do not need to do much thanks to the awesome [binary rewriting](binary-rewriting.md) that
Coyote does to enable testing of unmodified programs.

Out of the box, Coyote supports most common types and methods available in the .NET Task Parallel
Library (such as `Task`, `Task<TResult>` and `TaskCompletionSource<TResult>`), as well as the
`async`, `await` and `lock` C# keywords, and we are adding more types and APIs over time. You can
read more about binary rewriting in Coyote [here](binary-rewriting.md) and supported scenarios
[here](../get-started/using-coyote.md).

Take the simple example that was used to explain concurrency [non-determinism](non-determinism.md).
Notice that the code below is using the C# `Task` type. Coyote understands this `Task` type and is
able to control its schedule during systematic testing, as discussed above.

```csharp
using System.Threading.Tasks;

// Shared variable x.
int x = 0;

// Concurrency unit test.
int foo()
{
   // Concurrent operations on x.
   var t1 = Task.Run(() => { x = 1; });
   var t2 = Task.Run(() => { x = 2; });

   // Join all.
   Task.WaitAll(t1, t2);
}
```

When this method `foo` executes as part of a test case, the Coyote tester will understand that it is
spawning two tasks that can run concurrently. The tester will explore different ways of executing
the tasks to systematically cover all possibilities.

## Expressing nondeterminism and mocking

Coyote also offers APIs for expressing other forms of non-determinism that are not supported out of
the box using binary rewriting. The `CoyoteRuntime.Random` API, for instance, returns a
non-deterministic `bool` value. The exact value is chosen by the tester.

This simple API can be used to build more complex [mocks](https://en.wikipedia.org/wiki/Mock_object)
of external dependencies in the system. As an example, suppose that our code calls into an external
service. Either this call returns successfully and the external service does the work that we
requested, or it may timeout, or return an error code if the external service is unable to perform
the work at the time. For testing your code, you will write a mock for it as follows:

```csharp
Status CallExternalServiceMock(WorkItem work)
{
   if (CoyoteRuntime.Random())
   {
     // Perform some work.
     ...
     // Return success.
     return Status.Success;
   }
   else if (CoyoteRuntime.Random())
   {
     // Return error code.
     return Status.ErrorCode1;
   }
   else if (CoyoteRuntime.Random())
   {
     // Return error code.
     return Status.ErrorCode2;
   }
   else
   {
     // Timeout.
     return Status.Timeout;
   }
}
```

When using such a mock, the Coyote tester will control the values that `Random` returns in a way
that provides good coverage. All these techniques can be put together to write very expressive test
cases. A Coyote concurrency unit test has the power of encoding many different scenarios concisely
and leave their exploration to the automated tester.

Using Coyote involves two main activities. First, you write a concurrency unit test. Second, you
design mocks for external dependencies, capturing the sources of non-determinism that you want
tested in your system. Additionally, Coyote also offers ways of writing safety and liveness
[specifications](specifications.md) concisely.

## Testing other programming models

Besides the popular [task-based programming
model](https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap)
of C#, you can also choose to use the `Microsoft.Coyote.Actor` library that provides APIs for
expressing in-memory [asynchronous actors and state
machines](../concepts/actors/overview.md). Programs written using this more advanced
programming model can also be systematically tested with Coyote similar to unmodified task-based
programs.

See this [demo](../concepts/actors/state-machine-demo.md) which shows the systematic testing
process in action on a test application that implements the [Raft consensus
protocol](https://raft.github.io/) using Coyote state machines.
