---
layout: reference
title: Controlled asynchronous lock
section: learn
permalink: /learn/programming-models/async/async-lock
---

## Controlled asynchronous lock

The Coyote asynchronous tasks programming model provides an `AsyncLock` type that represents a
non-reentrant mutual exclusion lock that can be acquired asynchronously in a FIFO order. During
systematic testing, `AsyncLock` is controlled by `coyote test` to explore acquire/release
interleavings and find concurrency bugs.

## Why is an asynchronous lock necessary?

Although it is recommended that asynchronous code should be as lock-free as possible, in some
scenarios it is still useful to synchronize asynchronous access on a shared resource. Using the C#
[`lock`](https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/lock-statement)
statement might not be an option as C# forbids the use of `await` inside the `lock` scope. This is
because using `await` inside `lock` (which is implemented using
[`System.Threading.Monitor`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.monitor)
APIs) can be prone to deadlocks due to its reentrancy semantics.

The Coyote `AsyncLock` type does not have this limitation, and a [controlled `Task`](overview) is
free to `await` while holding an instance of this lock type. Awaiting on an `AsyncLock` also does
not block the underlying thread allowing for a more efficient execution.

Note that the C# lock statement is still supported by `coyote test`. However&mdash;besides not using
`await` in the body of `lock`&mdash;you should make sure to not call any Coyote API that can
introduce a scheduling decision during systematic testing (such as `Task.Run`, `Task.Delay` or
`Task.Yield`), while holding a `lock`, as this can lead to deadlocks.

## How does it work?

1. You create an instance of an `AsyncLock` by calling the static method `AsyncLock.Create()`.
2. The only instance-level method that `AsyncLock` provides is `AcquireAsync()`. This method tries
   to acquire the lock asynchronously, and returns a task that completes when the lock has been
   acquired. The returned task's result is of type `AsyncLock.Releaser`. This `Releaser` implements
   the `IDisposable` interface. It releases the lock when disposed. Note that `AcquireAsync()` is
   not a reentrant operation. 
3. `AsyncLock` exposes the `IsAcquired` boolean property, which is true if the lock has been
   acquired, else false.
4. Finally, when the `AsyncLock` is acquired, your code can enter the scope of this lock and perform
   synchronized operations. As the `Releaser` implements `IDisposable`, this makes it possible (and
   convenient) to put the synchronized code inside a `using` statement. This ensures the `AsyncLock`
   is released when exiting the body of `using` in a way that is also exception safe.

During systematic testing, `coyote test` injects scheduling points upon acquiring or releasing the
lock, which allows it to explore interleavings and expose bugs (including deadlocks).

## How to use?

The code below demonstrates `AsyncLock` in action.
 
```c#
using Microsoft.Coyote.Tasks;

public class LockExample
{
    private int Value = 0;
    private bool IsRunning = false;
    private readonly AsyncLock Mutex = AsyncLock.Create();

    public async Task FirstOperationAsync()
    {
        using (await this.Mutex.AcquireAsync())
        {
            if (!this.IsRunning)
            {
                this.IsRunning = true;
                this.Value = 5;
            }
        }
    }

    public async Task SecondOperationAsync(bool reenter)
    {
        using (await this.Mutex.AcquireAsync())
        {
            if (this.IsRunning)
            {
                this.IsRunning = false;
                this.Value = 0;
            }

            if (reenter)
            {
                // This causes a deadlock ...
                await this.SecondOperationAsync(reenter);
            }
        }
    }
}
```

In the above example, the `FirstOperationAsync` and `SecondOperationAsync` methods use an
`AsyncLock` to avoid a race condition while testing and updating `Value`.

You can write a test for this code as follows:

```c#
using Microsoft.Coyote.Tasks;

public static class Program
{
    public static async System.Threading.Tasks.Task Main()
    {
        await RunTestAsync();
    }

    [Microsoft.Coyote.SystematicTesting.Test]
    public static async Task RunTestAsync()
    {
        bool reenter = false;

        LockExample test = new LockExample();
        for (int i = 0; i < 100; i++)
        {
            var task1 = test.FirstOperationAsync();
            var task2 = test.SecondOperationAsync(reenter);
            await Task.WhenAll(task1, task2);
        }

        Console.WriteLine("Done testing.");
    }
}
```

When you run this test in the command line (by running the executable on its own, without `coyote
test`), then it normally finishes with the following output:

```
Done testing.
```

Next, change `bool reenter = false;` to `true`, build and run the test again (without `coyote
test`). This time execution is hanging forever and needs to be killed by pressing Ctrl-C. So, what
happened? As you see from the code above, the `SecondOperationAsync` can invoke itself while holding
`AsyncLock` when `reenter` is true. This results in a deadlock.

Now, try to run the test with `coyote test`. Instead of hanging, `coyote test` will quickly detect a
deadlock:

```
Error: Deadlock detected. Task(0) is waiting to acquire a resource that is already acquired, but no other controlled tasks are enabled.
```
