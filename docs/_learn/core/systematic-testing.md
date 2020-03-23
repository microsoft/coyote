---
layout: reference
section: learn
title: Core Concepts
permalink: /learn/core/systematic-testing
---

## Systematic testing

Coyote testing works by taking over the [non-determinism](../core/non-determinism) in a program.
Once it has control over the non-determinism, the Coyote tester will repeatedly run a test case,
each time exercising a different set of non-deterministic choices, offering much better coverage
than using other techniques. This powerful testing ability, however, does require help: you must
mark all sources of non-determinism in a way that Coyote understands. Let's see how this is done.

The first requirement is to use one of Coyote's supported programming models to express the
concurrency. This can be fairly easy to do, but very important because Coyote tester is going to
take over the scheduling of the program. In fact, the tester will complain if it detects concurrency
that is outside its control. Take the simple example that was used to explain concurrency
[non-determinism](../core/non-determinism). Notice the code below has replaced the .NET
`System.Threading.Tasks.Task` type with Coyote's `Task` type, which is part of Coyote's
[asynchronous Task programming model](../programming-models/async/overview), and is controlled
during systematic testing.

```c#
// Use the Coyote controlled task type.
using Microsoft.Coyote.Tasks;

// Shared variable x.
int x = 0;

int foo()
{
   // Concurrent operations on x.
   var t1 = Task.Run(() => { x = 1; });
   var t2 = Task.Run(() => { x = 2; });

   // Join all.
   Task.WaitAll(t1, t2);
}
```

When this method `foo` now executes as part of a test case, the Coyote tester will understand that
it is spawning two tasks that can run concurrently. The tester will explore different ways of
executing the tasks to systematically cover all possibilities. Coyote provides [multiple programming
models](../overview/what-is-coyote). Just remember that whichever programming model you choose,
you must express all the concurrency in that model itself.

Coyote also offers APIs for expressing other forms of non-determinism. The `CoyoteRuntime.Random`
API, for instance, returns a non-deterministic Boolean value. The exact value is chosen by the
tester. This simple API can be used to build more complex
[mocks](https://en.wikipedia.org/wiki/Mock_object) of external dependencies in the system. As an
example, suppose that our code calls into an external service. Either this call returns successfully
and the external service does the work that we requested, or it may timeout, or return an error code
if the external service is unable to perform the work at the time. For testing your code, you will
write a mock for it as follows:

```c#
Status CallExternalServiceMock(WorkItem work)
{
   if (CoyoteRuntime.Random())
   {
     // Perform work.
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
cases. A Coyote test has the power of encoding many different scenarios concisely and leave their
exploration to the automated tester.

Using Coyote has two main components. First, using one of Coyote's programming models to write the
code. Second, designing mocks for external dependencies, capturing the sources of non-determinism
that you want tested in your system. Additionally, Coyote also offers ways of writing
[specifications](specifications) concisely.

See [animating state machine demo](/coyote/learn/programming-models/actors/state-machine-demo) which
shows the systematic testing process in action on a test application that implements the [Raft
Consensus Algorithm](https://raft.github.io/).
