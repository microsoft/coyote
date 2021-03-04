## Write your first concurrency unit test with Coyote

Modern software systems are inherently concurrent in nature as they perform many different
activities at the same time, across different threads, processes and machines. Concurrency is
notoriously hard to test, and concurrent bugs can be hard to reproduce and understand. Coyote is a
very effective tool in taming this complexity. By giving you the ability to easily test for
concurrency bugs, Coyote helps you build more reliable applications and services.

In this tutorial, you will write a simple `AccountManager` class to create, get and delete _account_
records in a backend NoSQL database. We'll design our class to be used in a concurrent setting,
where methods in multiple instances of the class can be called concurrently, either within the same
process or across processes and machines. This latter condition means that using locks will not help
you in writing correct concurrent code.

## What you will need

To run the `AccountManager` example, you will need to:

- Install [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/).
- Install the [.NET 5.0 version of the coyote tool](../get-started/install.md).
- Be familiar with the `coyote` tool. See [Testing](../get-started/using-coyote.md).

## Watch this tutorial

Optionally, you can watch this tutorial on YouTube:

[![image](../assets/images/coyote_tutorial_intro.png)](https://youtu.be/wuKo-9iRm6o)

## Walkthrough

Without further ado, let's look at the signature of the `AccountManager` class:

```csharp
public class AccountManager
{
  private IDbCollection AccountCollection;

  // Returns true if the account is created, else false.
  public async Task<bool> CreateAccount(string accountName, string accountPayload) { ... }

  // Returns the accountPayload, else null.
  public async Task<string> GetAccount(string accountName) { ... }

  // Returns true if the account is deleted, else false.
  public async Task<bool> DeleteAccount(string accountName) { ... }
}
```

Here are the methods available in the `IDbCollection` interface:

```csharp
public interface IDbCollection
{
  Task<bool> CreateRow(string key, string value);

  Task<bool> DoesRowExist(string key);

  Task<string> GetRow(string key);

  Task<bool> DeleteRow(string key);
}
```

The `CreateRow` method creates the row with the given key, unless it already exists in which case it
returns the `RowAlreadyExistsException` exception. The `DoesRowExist` method returns `true` if the
row exists, otherwise it returns `false`. The `GetRow` method returns the content of the given key
and throws `RowNotFoundException` exception if it doesn't exist. Finally, the `DeleteRow` method
deletes the row if it exists and throws `RowNotFoundException` exception if it doesn't exist.

Before reading on, please open your editor and attempt to write an implementation of the
`AccountManager` class.  You might write something like this:

```csharp
public class AccountManager
{
  private readonly IDbCollection AccountCollection;

  public AccountManager(IDbCollection dbCollection)
  {
    this.AccountCollection = dbCollection;
  }

  // Returns true if the account is created, else false.
  public async Task<bool> CreateAccount(string accountName, string accountPayload)
  {
    if (await this.AccountCollection.DoesRowExist(accountName))
    {
      return false;
    }

    return await this.AccountCollection.CreateRow(accountName, accountPayload);
  }

  // Returns the accountPayload, else null.
  public async Task<string> GetAccount(string accountName)
  {
    if (!await this.AccountCollection.DoesRowExist(accountName))
    {
      return null;
    }

    return await this.AccountCollection.GetRow(accountName);
  }

  // Returns true if the account is deleted, else false.
  public async Task<bool> DeleteAccount(string accountName)
  {
    if (!await this.AccountCollection.DoesRowExist(accountName))
    {
      return false;
    }

    return await this.AccountCollection.DeleteRow(accountName);
  }
}
```

Does the above implementation look reasonable to you? Can you find any bugs? And how can you
convince yourself of the absence of any bugs in the above program?

Let's write a unit test to test the `AccountManager` code. In production, `IDbCollection` is
implemented using a distributed NoSQL database. To keep things simple during testing, you can just
replace it with a mock. The following code shows such a mock implementation:

```csharp
public class InMemoryDbCollection : IDbCollection
{
  private readonly ConcurrentDictionary<string, string> Collection;

  public InMemoryDbCollection()
  {
    this.Collection = new ConcurrentDictionary<string, string>();
  }

  public Task<bool> CreateRow(string key, string value)
  {
    return Task.Run(() =>
    {
      bool success = this.Collection.TryAdd(key, value);
      if (!success)
      {
        throw new RowAlreadyExistsException();
      }

      return true;
    });
  }

  public Task<bool> DoesRowExist(string key)
  {
    return Task.Run(() =>
    {
      return this.Collection.ContainsKey(key);
    });
  }

  public Task<string> GetRow(string key)
  {
    return Task.Run(() =>
    {
      bool success = this.Collection.TryGetValue(key, out string value);
      if (!success)
      {
        throw new RowNotFoundException();
      }
      return value;
    });
  }

  public Task<bool> DeleteRow(string key)
  {
    return Task.Run(() =>
    {
      bool success = this.Collection.TryRemove(key, out string value);
      if (!success)
      {
        throw new RowNotFoundException();
      }

      return true;
    });
  }
}
```

The `InMemoryDbCollection` mock is very simple, it just maintains an in-memory
`ConcurrentDictionary` to store the keys and values. Each method of the mock runs a new concurrent
task (via `Task.Run`) to make the call execute asynchronously, modeling async I/O in a real database
call. You can read later this [follow-up tutorial](mock-dependencies.md) to delve into mock design
for concurrency unit testing.

Now that you have written this mock, you can write a simple test:

```csharp
[Test]
public static async Task TestAccountCreation()
{
  // Initialize the mock in-memory DB and account manager.
  var dbCollection = new InMemoryDbCollection();
  var accountManager = new AccountManager(dbCollection);

  // Create some dummy data.
  string accountName = "MyAccount";
  string accountPayload = "...";

  // Create the account, it should complete successfully and return true.
  var result = await accountManager.CreateAccount(accountName, accountPayload);
  Assert.True(result);

  // Create the same account again. The method should return false this time.
  result = await accountManager.CreateAccount(accountName, accountPayload);
  Assert.False(result);
}
```

The above unit test clearly tests that the same account cannot be created twice. Try run it (check
below for instructions on how to build and run this tutorial from our samples repository) and you
will see that it always passes. But is the behavior still true if two requests happen
_concurrently_? How can you test this?

What happens if you spawn two tasks that create the same account concurrently? What if you assert
that only one creation succeeds, while the other always fails? That should work because the
`InMemoryDbCollection` uses a `ConcurrentDictionary` right? 

```csharp
[Test]
public static async Task TestConcurrentAccountCreation()
{
  // Initialize the mock in-memory DB and account manager.
  var dbCollection = new InMemoryDbCollection();
  var accountManager = new AccountManager(dbCollection);

  // Create some dummy data.
  string accountName = "MyAccount";
  string accountPayload = "...";

  // Call CreateAccount twice without awaiting, which makes both methods run
  // asynchronously with each other.
  var task1 = accountManager.CreateAccount(accountName, accountPayload);
  var task2 = accountManager.CreateAccount(accountName, accountPayload);

  // Then wait both requests to complete.
  await Task.WhenAll(task1, task2);

  // Finally, assert that only one of the two requests succeeded and the other
  // failed. Note that we do not know which one of the two succeeded as the
  // requests ran concurrently (this is why we use an exclusive OR).
  Assert.True(task1.Result ^ task2.Result);
}
```

Try run this concurrent test. The assertion will _most likely_ fail. The reason it is not a
guaranteed failure is that there are some task interleavings where it passes, and others where it
fails with the following exception:

```plain
Unhandled exception. RowAlreadyExistsException: Exception of type 'RowAlreadyExistsException' was thrown.
...
```

Let's dig into why the concurrent test failed.

The test started two asynchronous `CreateAccount` calls, the first one checked whether the account
existed through the `DoesRowExist` method which returned `false`. Due to the underlying concurrency,
control passed to the second task which made a similar call to `DoesRowExist` which also returned
`false`. Both tasks then resumed believing that the account does not exist and tried to add the
account. One of them succeeded while the other threw an exception, indicating a bug in your
`AccountManager` implementation.

So writing out this test was useful and easily exposed this race condition. But why don't we
write such tests a lot more often? The reason is they are often flaky and find bugs through _sheer
luck_ instead of a _systematic_ exploration of the possible interleavings. The above test hits the bug
fairly frequently due to the way .NET task scheduling works (on a reasonably fast machine with light
CPU load).

Let's tweak the test very slightly by adding a delay of a millisecond between the two `CreateAccount`
calls:

```csharp
var task1 = accountManager.CreateAccount(accountName, accountPayload);
await Task.Delay(1); // Artificial delay.
var task2 = accountManager.CreateAccount(accountName, accountPayload);
```

If you run this test, chances are it will fail very rarely. If you run this test in a loop
invoking it a hundred times it probably won't fail once.

The race condition is still there but our concurrency unit test suddenly became ineffective at
catching it. This explains why developers don't write such tests as they are very sensitive to
timing issues. Instead, developers often write _stress_ tests, where the system is bombarded with
thousands of concurrent requests in the hopes that some rare interleaving would expose these kind of
nondeterministic bugs (known as [Heizenbugs](https://en.wikipedia.org/wiki/Heisenbug)) before the
code is deployed in production. But stress testing can be complex to setup and it doesn't always
find the most tricky bugs. Even if it does find a bug, it usually produces such long traces (or
logs) that understanding the bug and fixing it becomes a very time consuming and frustrating task.

Flakey tests is clearly not a satisfactory situation. What we need is a tool which can
systematically explore the various task interleavings in test mode as opposed to leaving that to
luck (i.e. the operating system scheduler). Coyote gives you _exactly_ this.

To use Coyote on your task-based program is very easy in most cases. All you need to do is to invoke
the `coyote` tool to [rewrite](../get-started/using-coyote.md) your assembly (for testing only)
so that Coyote can inject logic that allows it to take control of the schedule of C# tasks. Then,
you can invoke the `coyote test` tool which [systematically
explores](../concepts/concurrency-unit-testing.md) task interleavings to uncover bug. What is even
better is that if a bug is uncovered, Coyote allows you to deterministically reproduce it every
single time.

Now run your test under the control of Coyote. First use Coyote to rewrite the assembly:

```plain
coyote rewrite .\AccountManager.dll
. Rewriting AccountManager.dll
... Rewriting the 'AccountManager.dll' assembly
... Writing the modified 'AccountManager.dll' assembly to AccountManager.dll
. Done rewriting in 0.6425808 sec
```

Awesome, now lets try use Coyote on the above concurrent test:

```plain
coyote test .\AccountManager.dll -m TestConcurrentAccountCreation -i 100
```

Note: for this to work the unit test method needs to use the
`[Microsoft.Coyote.SystematicTesting.Test]` custom attribute to declare the test method which 
is what you will see if you have already downloaded the [Coyote Samples git
repo](http://github.com/microsoft/coyote-samples).

The above command tells Coyote to execute the test method `TestConcurrentAccountCreation` for 100
iterations. Each iteration will try explore different interleavings to try unearth the bug. You can
read more about other Coyote tool options [here](../get-started/using-coyote.md).

Indeed after 20 iterations and 0.15 seconds Coyote finds a bug:

```plain
. Testing .\AccountManager.dll
... Method TestConcurrentAccountCreation
... Started the testing task scheduler (process:17368).
... Created '1' testing task (process:17368).
... Task 0 is using 'random' strategy (seed:1046544966).
..... Iteration #1
..... Iteration #2
..... Iteration #3
..... Iteration #4
..... Iteration #5
..... Iteration #6
..... Iteration #7
..... Iteration #8
..... Iteration #9
..... Iteration #10
..... Iteration #20
... Task 0 found a bug.
... Emitting task 0 traces:
..... Writing AccountManager.dll\CoyoteOutput\AccountManager_0_0.txt
..... Writing AccountManager.dll\CoyoteOutput\AccountManager_0_0.schedule
... Elapsed 0.0743756 sec.
... Testing statistics:
..... Found 1 bug.
... Scheduling statistics:
..... Explored 26 schedules: 26 fair and 0 unfair.
..... Found 3.85% buggy schedules.
..... Number of scheduling points in fair terminating schedules: 17 (min), 23 (avg), 31 (max).
... Elapsed 0.1574494 sec.
```

Cool, the flakey test is no longer flakey! Coyote can also help you reproduce and debug it. You can
simply run `coyote replay` giving the `.schedule` file that Coyote outputs upon finding a bug:

```plain
coyote replay .\AccountManager.dll -schedule AccountManager_0_0.schedule -m TestConcurrentAccountCreation
. Replaying .\Output\AccountManager.dll\CoyoteOutput\AccountManager_0_1.schedule
... Task 0 is using 'replay' strategy.
... Reproduced 1 bug (use --break to attach the debugger).
... Elapsed 0.0671654 sec.
```

Nice, the bug was reproduced. You can use the `--break` option to attach the VS debugger and happily
debug the deterministic trace to figure out what is causing the bug and take as long as you want,
stepping through the code in the debug, and that will not change any timing conditions, the same bug
will still happen. You can repeat this as many times as you want!

In this tutorial, you saw that you were able to use Coyote to reliably reproduce the race condition
in `AccountManager`. You did this with a tiny test (just two `CreateAccount` calls racing with each
other), as opposed to overloading the system with thousands of concurrent tasks through stress
testing.

This of course was a simple example, but it's easy to imagine how Coyote can find many non-trivial
concurrency bugs in a much more complex codebase. Such bugs have very low probability of being
caught during test time if you don't use a tool like Coyote. In the absence of such tools, these
bugs can go undetected and occur sporadically in production, making them difficult to diagnose and
debug. No more late nights debugging a live site!

In the [next tutorial](test-concurrent-operations.md), you will write a few more concurrency unit
tests for the `AccountManager` to increase our familiarity with Coyote.

## Get the sample source code

To get the complete source code for the `AccountManager` tutorial, clone the [Coyote Samples git
repo](http://github.com/microsoft/coyote-samples). Note that the repo also contains the code from
the [next tutorial](test-concurrent-operations.md) which builds upon this `AccountManager` sample.

You can build the samples by running the following command:

```plain
powershell -f build.ps1
```

You can now run the tests (without Coyote) like this:

```plain
cd .\bin\net5.0
.\AccountManager.exe
```

This version has some command line arguments to make it easy select which test to run:

```plain
Usage: AccountManager [option]
Options:
  -s    Run sequential test without Coyote
  -c    Run concurrent test without Coyote
```

To rewrite and test the sample with Coyote you can use the following commands (as discussed above):

```plain
coyote rewrite .\AccountManager.dll
coyote test .\AccountManager.dll -m TestConcurrentAccountCreation -i 100
```

If you find a bug you can replay with the following command:
```plain
coyote replay .\AccountManager.dll -schedule AccountManager_0_0.schedule -m TestConcurrentAccountCreation
```

Enjoy!
