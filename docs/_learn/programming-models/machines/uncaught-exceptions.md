---
layout: reference
title: Semantics of unhandled exceptions
section: learn
permalink: /learn/programming-models/machines/uncaught-exceptions
---

## Semantics of unhandled exceptions

Coyote `Machines` can execute arbitrary C# code in their actions. This page discusses what happens if such an exception is not handled inside the action itself. As is usual in [asynchronous programming](https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/exception-handling-task-parallel-library), one should be careful with unhandled exceptions.

In test mode (i.e., when running a test with `Coyote test ...`), all unhandled exceptions are an error and the test will fail. `Coyote` tester will stop the execution at that point and report an error.

In production mode (i.e., when running a Coyote program in production), the Coyote runtime intercepts any unhandled exception in a machine action. The exception is then delivered to the `OnFailure` delegate of the runtime. At this point, it is your responsibility to take the appropriate action. For instance, you can cause the program to crash and create a dump (for debugging later) as follows:

```C#
runtime.OnFailure += delegate (Exception exception)
{
   Environment.FailFast(exception.ToString(), exception);
};
```

It is important that you induce a crash to stop the program, after perhaps taking some cleanup actions. If you don't do so, the runtime automatically enters a "failure" mode. Machines may continue running their current action, but no additional work will take place. The runtime stops all `Enqueue` and `Dequeue` operations in the program. Thus, messages sent will not be delivered, and already-received messages will not be dequeued. If you wish to suppress an exception and have the rest of the program continue normal operation, then it is best to catch the exception in the action itself using a usual `try { } catch { }` block, or override the machine's `OnException` method to handle exceptions in one place.

## Call Stack and Debugging

The runtime takes care to call the `OnFailure` delegate with the stack intact. That is, if you launch a debugger from inside the `OnFailure` method, you will see the stack (with local variables) at the point the exception was thrown. However, this is not always the case. If the exception was thrown from an `async` machine action, then the stack may have gotten unwound even before the runtime gets to know of the exception (because of the way `async` continuations get compiled).
