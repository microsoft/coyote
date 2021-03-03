## Semantics of unhandled exceptions

Coyote `Actors` can execute arbitrary C# code in their event handlers. This page discusses what
happens if such code throws an exception that is not handled inside the action itself. As is usual
in [asynchronous
programming](https://docs.microsoft.com/en-us/dotnet/standard/parallel-programming/exception-handling-task-parallel-library),
one should be careful with unhandled exceptions.

In test mode (i.e., when running a test with `coyote test ...`), all unhandled exceptions are an
error and the test will fail. The `coyote` tester will stop the execution at that point and report
an error.

In production mode (i.e., when running a Coyote program outside of `coyote test`), the Coyote
runtime intercepts any unhandled exception in an actor. The exception is then delivered to the
`OnFailure` delegate of the runtime. At this point, it is your responsibility to take the
appropriate action. For instance, you can cause the program to crash and create a dump (for
debugging later) as follows:

```csharp
runtime.OnFailure += delegate (Exception exception)
{
   Environment.FailFast(exception.ToString(), exception);
};
```

It is important that you induce a crash to stop the program, after perhaps taking some cleanup
actions. If you don't do so, the runtime automatically enters a "failure" mode. Actors may continue
running their current action, but no additional work will take place. The runtime stops all
`Enqueue` and `Dequeue` operations in the program. Thus, messages sent will not be delivered, and
already-received messages will not be dequeued. If you wish to suppress an exception and have the
rest of the program continue normal operation, then it is best to catch the exception in the action
itself using a usual `try { } catch { }` block, or override the actor's `OnException` method to
handle exceptions in one place.  This override can return one of the following outcomes:

```csharp
// The outcome when an Microsoft.Coyote.Actors.Actor throws an exception.
public enum OnExceptionOutcome
{
   // The actor throws the exception causing the runtime to fail.
   ThrowException = 0,

   // The actor handles the exception and resumes execution.
   HandledException = 1,

   // The actor handles the exception and halts.
   Halt = 2
}
```

## Call Stack and debugging

The runtime tries to call the `OnFailure` delegate with the stack intact. That is, if you launch a
debugger from inside the `OnFailure` method, you will see the stack (with local variables) at the
point the exception was thrown. However, this is not always the case. If the exception was thrown
from an `async` action, then the stack may have gotten unwound even before the runtime gets to know
of the exception (because of the way `async` continuations get compiled).
