
## Controlled Semaphore

The Coyote asynchronous tasks programming model provides a `Semaphore` type that limits the number
of tasks that can access a resource. During testing, the semaphore is automatically replaced with
a controlled mocked version so that Coyote can perform any desired scheduling and interleaving
of asynchronous operations.

## Why is a semaphore necessary?

A semaphore is used in scenarios where you have to limit the number of tasks that can use a resource
simultaneously. There are two semaphore types provided in C#:
[`Semaphore`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.semaphore)
and [`SemaphoreSlim`](https://docs.microsoft.com/en-us/dotnet/api/system.threading.semaphoreslim).

However, you should not use these .NET semaphore types in a Coyote program because synchronization
must be done via Coyote APIs.

Using the controlled `Semaphore` type of Coyote allows `coyote test` to fully control the scheduling
of asynchronous operations.

## How do you use it?

1. You create an instance of a `Semaphore` by calling the static method
`Semaphore.Create(initialCount, maxCount)`. This method requires you to specify two `Int32` arguments:
   * `initialCount`. The initial number of requests for the semaphore that can be granted concurrently.
   * `maxCount`. The maximum number of requests for the semaphore that can be granted concurrently.
   * The following relation should hold between the values of these arguments:
 ` 0 <= initialCount <= maxCount && maxCount > 0`
 If this is violated, `Semaphore.Create(initialCount, maxCount)` throws an exception in production
 mode or reports a test bug when run under coyote test.

2. You can enter the so created semaphore by executing either of its `void Wait()` or
`Task WaitAsync()` methods:
   * The `Wait()` method blocks the task until it can enter the `Semaphore`.
   * The `WaitAsync()` method returns a task that will complete when the semaphore has been entered.

3. The `CurrentCount` property has as its value the current count of requests that can be granted.

4. The `Release()` method. When you are done with handling the resource, you must exit the semaphore
by invoking this method. Not releasing the granted access and staying inside the semaphore
forever is a bug in your code that limits the capacity of the semaphore and if this bug happens
repeatedly with other tasks, can lead to the semaphore getting full and permanently unavailable
for use.
Another possible error is to perform a `Release()` more times than the `maxCount` of a semaphore.
This results in an exception (in production mode) and in a test bug when testing with Coyote.

5. The `Dispose()` method. A `Semaphore` implements the `IDisposable` interface. Finally, when your
code no longer needs the semaphore object you should dispose it using this method.
Then it is a good practice to dispose of this `Semaphore` object, invoking this method.

## Example usage

The code below demonstrates `Semaphore` in action:

 ```csharp
using Microsoft.Coyote.Tasks;

public class SemaphoreExample
{
    private readonly Semaphore TheSemaphore;
    private int ProtectedValue = 0;
    private readonly int MaxCount;

    public SemaphoreExample(int initialCurrentCount, int maxCount)
    {
        this.MaxCount = maxCount;
        this.TheSemaphore = Semaphore.Create(initialCurrentCount, maxCount);
    }

    public async Task RunTask(string taskName, int sleepMilliseconds, int newValue)
    {
        this.WriteLine($"{taskName}: About to enter the semaphore. TheSemaphore.CurrentCount: {this.TheSemaphore.CurrentCount}");
        await this.TheSemaphore.WaitAsync();
        this.WriteLine($"{taskName}: Entered the semaphore. TheSemaphore.CurrentCount: {this.TheSemaphore.CurrentCount}");
        await Task.Delay(sleepMilliseconds);
        this.ProtectedValue = newValue;
        this.WriteLine($"{taskName}: Waited {sleepMilliseconds}ms. Now exiting the semaphore. TheSemaphore.CurrentCount: {this.TheSemaphore.CurrentCount}");
        this.TheSemaphore.Release();
        this.WriteLine($"{taskName}: Exited the semaphore. TheSemaphore.CurrentCount: {this.TheSemaphore.CurrentCount}");
        this.WriteLine($"{taskName}: this.ProtectedValue: {this.ProtectedValue}");
        this.WriteLine("=====================================================================\n");
    }

    public async Task RunTask2(string taskName)
    {
        var semaphoreCount = this.TheSemaphore.CurrentCount;
        Console.WriteLine($"{taskName} starting. TheSemaphore.CurrentCount: {semaphoreCount}");

        for (var i = 1; i <= semaphoreCount + 1; i++)
        {
            Console.WriteLine($"{taskName}: ({i}) Entering the semaphore. TheSemaphore.CurrentCount: {this.TheSemaphore.CurrentCount}");
            await this.TheSemaphore.WaitAsync();
        }
    }

    private void WriteLine(string msg)
    {
        string indent = new string(' ', (this.MaxCount - this.TheSemaphore.CurrentCount) * 2);
        Console.Write(indent);
        Console.WriteLine(msg);
    }
}
 ```

In the above example, the `RunTask` and  `RunTask2` methods use a `Semaphore` that allows only
`maxCount` tasks to perform simultaneously inside the semaphore. The code below implements
2 test scenarios, and in the first of them invokes `RunTask()` to create 3 different tasks using
the semaphore, specifying for the semaphore `initialCount` = 2, and `maxCount` = 2.

```csharp
using Microsoft.Coyote.Tasks;
public static class Program
{
    public static int Scenario = 1;

    public static void Main()
    {
        RunTest();
    }

    [Microsoft.Coyote.SystematicTesting.Test]
    public static void RunTest()
    {
        switch (Scenario)
        {
            case 1:
                SemaphoreExample test1 = new SemaphoreExample(2, 2);
                var task1 = test1.RunTask("Task1", 500, 1);
                var task2 = test1.RunTask("Task2", 1000, 2);
                var task3 = test1.RunTask("Task3", 1000, 3);
                Task.WaitAll(task1, task2, task3);
                Console.WriteLine("test complete");
                break;

            case 2:
                SemaphoreExample test2 = new SemaphoreExample(2, 2);
                var task4 = test2.RunTask2("Task4");
                Task.WaitAny(task4);
                break;
        }
    }
}
```

In the first scenario (`Scenario == 1`) the method `RunTask()` is invoked 3 times and creates 3 tasks
that run asynchronously, then the code awaits the completion of all three tasks.
The first two tasks enter the semaphore and fill its capacity completely as its `maxCount` is 2.
Inside the semaphore, `task1` awaits for 500 milliseconds, then sets `this.ProtectedValue = 1`
and releases (exits) the semaphore. `task2` is also inside the semaphore with `task1` but it
waits for 1000 milliseconds, thus it has to wait about 500 milliseconds more after `task1`
exits the semaphore. Exactly when `task1` exits the semaphore, its current count is increased
from 0 to 1 and this allows the third task, `task3` waiting for semaphore to be granted access.
Having entered the semaphore, `task3` waits for 1000 milliseconds. The other occupant of the semaphore
(`task2`) needs to wait only about 500ms more, then it sets `this.ProtectedValue = 2` and releases
(exits) the semaphore. Thus for the remaining about 500 milliseconds `task3` is waiting as the
sole occupant of the semaphore. Then it finally sets `this.ProtectedValue = 3` and releases (exits)
the semaphore.

When you build and run the sample, the expected output is produced:

```plain
Task1: About to enter the semaphore. TheSemaphore.CurrentCount: 2
  Task1: Entered the semaphore. TheSemaphore.CurrentCount: 1
  Task2: About to enter the semaphore. TheSemaphore.CurrentCount: 1
    Task2: Entered the semaphore. TheSemaphore.CurrentCount: 0
    Task3: About to enter the semaphore. TheSemaphore.CurrentCount: 0
    Task1: Waited 500ms. Now exiting the semaphore. TheSemaphore.CurrentCount: 0
    Task1: Exited the semaphore. TheSemaphore.CurrentCount: 0
    Task1: this.ProtectedValue: 1
    Task3: Entered the semaphore. TheSemaphore.CurrentCount: 0
    =====================================================================

    Task2: Waited 1000ms. Now exiting the semaphore. TheSemaphore.CurrentCount: 0
  Task2: Exited the semaphore. TheSemaphore.CurrentCount: 1
  Task2: this.ProtectedValue: 2
  =====================================================================

  Task3: Waited 1000ms. Now exiting the semaphore. TheSemaphore.CurrentCount: 1
Task3: Exited the semaphore. TheSemaphore.CurrentCount: 2
Task3: this.ProtectedValue: 3
=====================================================================
```

Now, run the example with `coyote test`. The testing completes normally, without finding any test bugs.
You will get output from the coyote test tool, similar to this:

```plain
... Testing statistics:
..... Found 0 bugs.
... Scheduling statistics:
..... Explored 2000 schedules: 2000 fair and 0 unfair.
```

In the second scenario (`Scenario == 2`) a new `SemaphoreExample` instance is created with `initialCurrentCount`
and `maxCount` having the same value 2. Then a single method (`RunTask2`) is invoked and the task
it returns is awaited upon. Remarkably, the code of this task tries to enter the semaphore more times
than its `CurrentCount` and this must end abnormally.

In the sample, change this line:

```plain
public static int Scenario = 1;
```

to:

```plain
public static int Scenario = 2;
```

Then build and run. The execution hangs, you have to kill the program with Ctrl-C, and you see this output:

```plain
Task4 starting. TheSemaphore.CurrentCount: 2
Task4: (1) Entering the semaphore. TheSemaphore.CurrentCount: 2
Task4: (2) Entering the semaphore. TheSemaphore.CurrentCount: 1
Task4: (3) Entering the semaphore. TheSemaphore.CurrentCount: 0
```

Now, run the sample with `coyote test`. This time there is no hanging and Coyote quickly finds
and reports a test bug:

```plain
... Task 0 found a bug.
... Emitting task 0 traces:
..... Writing ...\Semaphores_0_2.txt
..... Writing ...\Semaphores_0_2.schedule
... Elapsed 0.1125501 sec.
```

Looking into the error file `Semaphores_0_2.txt` reveals this error message:

```plain
<ErrorLog> Deadlock detected. Task(0) is waiting to acquire a resource that is already acquired, but no other controlled tasks are enabled.
```
