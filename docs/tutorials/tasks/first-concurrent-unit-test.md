## Write your first concurrency unit test with Coyote

Modern software systems are inherently concurrent in nature as they perform many different
activities at the same time, across different threads, processes and machines. Concurrency is
notoriously hard to test, and concurrent bugs can be hard to reproduce and understand. Coyote is a
very effective tool in taming this complexity. By giving you the ability to easily test for
concurrency bugs, Coyote helps you build more reliable applications and services.

In this tutorial, we will write a simple `AccountManager` class to create, get and delete _account_
records in a backend NoSQL database. We'll design our class to be used in a concurrent setting,
where methods in multiple instances of the class can be called concurrently, either within the same
process or across processes and machines. This latter condition means that using locks will not help
us in writing correct concurrent code.

## What you will need

To run the `AccountManager` example, you will need to:

- Install [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/).
- Install the [.NET 5.0 version of the coyote tool](../../get-started/install.md).
- Be familiar with the `coyote test` tool. See [Testing](../../tools/testing.md).
- Be familiar with the `coyote rewrite` tool. See [Rewriting](../../tools/rewriting.md).

## Walkthrough

Without further ado, let's look at the signature of the `AccountManager` class:
```c#
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

Here are the methods available available in the `IDbCollection` interface:
```c#
public interface IDbCollection
{
  Task CreateRow(string key, string value);

  Task<bool> DoesRowExist(string key);

  Task<string> GetRow(string key);

  Task DeleteRow(string key);
}
```

The `CreateRow` method creates the row with the given key, unless it already exists in which case it
returns the `RowAlreadyExistsException` exception. The `DoesRowExist` method returns `true` if the
row exists, otherwise it returns `false`. The `GetRow` method returns the content of the given key
and throws `RowNotFoundException` exception if it doesn't exist. Finally, the `DeleteRow` method
deletes the row if it exists and throws `RowNotFoundException` exception if it doesn't exist.

Before reading on, we encourage you to open your editor and attempt to write out the code.

Here's one attempt to implement the `AccountManager` methods:
```c#
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

    await this.AccountCollection.CreateRow(accountName, accountPayload);
    return true;
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

    await this.AccountCollection.DeleteRow(accountName);
    return true;
  }
}
```

Does the above implementation look reasonable to you? Can you find any bugs? And how can you
convince yourself of the absence of any bugs in the above program? Let's write a unit test to test
this code. In production, `IDbCollection` is implemented using a distributed NoSQL database. To keep
things simple during testing, we can just replace it with a mock. The following code shows such a
mock implementation:
```c#
public class InMemoryDbCollection : IDbCollection
{
  private readonly ConcurrentDictionary<string, string> Collection;

  public InMemoryDbCollection()
  {
    this.Collection = new ConcurrentDictionary<string, string>();
  }

  public Task CreateRow(string key, string value)
  {
    return Task.Run(() =>
    {
      var result = this.Collection.TryAdd(key, value);
      if (!result)
      {
        throw new RowAlreadyExistsException();
      }
    });
  }

  public Task DeleteRow(string key)
  {
    return Task.Run(() =>
    {
      var removed = this.Collection.TryRemove(key, out string value);
      if (!removed)
      {
        throw new RowNotFoundException();
      }
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
      var result = this.Collection.TryGetValue(key, out string value);
      if (!result)
      {
        throw new RowNotFoundException();
      }
      return value;
    });
  }
}
```

The `InMemoryDbCollection` mock is very simple, it just maintains an in-memory
`ConcurrentDictionary` to store the keys and values. Each method of the mock runs a new concurrent
task (via `Task.Run`) to make the call execute asynchronously, modeling async I/O in a real database
call.

Now that we have written this mock, lets write a simple test:
```c#
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
_concurrently_? How can we test this? What if we spawn two tasks to create the account,
concurrently? And then assert that only one creation succeeds, while the other always fails? Hmm -
that can possibly work. Let's write this test.

```c#
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

Try run this concurrent test. The assertion will _most likely_ fail. The reason we say most likely
instead of a guaranteed failure is that there are some task interleavings where it passes, and
others where it fails with the following exception:
```plain
Unhandled exception. RowAlreadyExistsException: Exception of type 'RowAlreadyExistsException' was thrown.
...
```

Let's dig into why the concurrent test failed.

We started two asynchronous `CreateAccount` calls, the first one checked whether the user existed
through the `DoesRowExist` method which returned `false`. Due to the underlying concurrency, control
passed to the second task which made a similar call to `DoesRowExist` which also returned `false`.
Both tasks then resumed believing that the account does not exist and tried to add the account. One
of them succeeded while the other threw an exception, indicating a bug in our implementation.

So writing out this test was useful and easily exposed this race condition. But why don't we
write such tests a lot more often? The reason is they are often flaky and find bugs through _sheer
luck_ instead of a _systematic_ exploration of the possible interleavings. The above test hits the bug
fairly frequently due to the way .NET task scheduling works (on a reasonably fast machine with light
CPU load).

Let's tweak the test very slightly by adding a delay of a millisecond between the two `CreateAccount`
calls:
```c#
var task1 = accountManager.CreateAccount(accountName, accountPayload);
await Task.Delay(1); // Artificial delay.
var task2 = accountManager.CreateAccount(accountName, accountPayload);
```

If you run this test, chances are it will fail exceedingly rarely. We ran this test in a loop
invoking it about a hundred times and it didn't fail once.

```plain
Iteration 0 - Passed
Iteration 1 - Passed
...
Iteration 99 - Passed
```

The race condition is still there but our concurrency unit test suddenly became ineffective at
catching it. This explains why developers don't write such tests as they are very sensitive to
timing issues. Instead, developers often write _stress_ tests, where the system is bombarded with
thousands of concurrent requests in the hopes that some rare interleaving would expose these kind of
nondeterministic bugs (known as [Heizenbugs](https://en.wikipedia.org/wiki/Heisenbug)) before the
code is deployed in production. But stress testing can be complex to setup and it doesn't always
find the most tricky bugs. Even if it did, it might produce such long traces (or logs) that might
make it very time consuming to debug and fix.

The above is clearly not a satisfactory solution. What we need is a tool which can systematically
explore the various task interleavings in test mode as opposed to leaving that to luck (i.e. the
operating system scheduler). Coyote gives you _exactly_ this.

To use Coyote on your task-based program is very easy in most cases. All you need to do is to invoke
the `coyote rewrite` tool which [rewrites](../../tools/rewriting.md) your assembly (for testing only)
so that Coyote can inject logic that allows it to take control of the schedule of C# tasks. Then,
you can invoke the `coyote test` tool which [systematically explores](../../core/systematic-testing.md)
task interleavings to uncover bug. If a bug is uncovered, Coyote allows you to deterministically
reproduce it every single time.

Let's now run our test, without changing a single line of code, under the control of Coyote. First use
Coyote to rewrite the assembly:
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

The above command tells Coyote to execute the test method `TestConcurrentAccountCreation` for 100
iterations. Each iteration will try explore different interleavings to try unearth the bug. You can
read more about other Coyote tool options [here](../../tools/testing.md).

Let's see if Coyote finds the bug now that the concurrent program execution is under its control.
Indeed after just 4 iterations and 0.22 seconds:
```plain
. Testing .\AccountManager.dll
... Method TestConcurrentAccountCreation
... Started the testing task scheduler (process:61212).
... Created '1' testing task (process:61212).
... Task 0 is using 'random' strategy (seed:2183365473).
..... Iteration #1
..... Iteration #2
..... Iteration #3
..... Iteration #4
... Task 0 found a bug.
... Emitting task 0 traces:
..... Writing AccountManager_0_0.txt
..... Writing AccountManager_0_0.schedule
... Elapsed 0.092809 sec.
... Testing statistics:
..... Found 1 bug.
... Scheduling statistics:
..... Explored 4 schedules: 4 fair and 0 unfair.
..... Found 25.00% buggy schedules.
..... Number of scheduling points in fair terminating schedules: 8 (min), 11 (avg), 14 (max).
... Elapsed 0.2256287 sec.
```

Cool, we found the bug! Let's see now how Coyote can help us reproduce it. You can simple run
`coyote replay` giving the `.schedule` file that Coyote dumps upon finding a bug:
```plain
coyote replay .\AccountManager.dll -schedule AccountManager_0_0.schedule -m TestConcurrentAccountCreation
. Replaying .\Output\AccountManager.dll\CoyoteOutput\AccountManager_0_1.schedule
... Task 0 is using 'replay' strategy.
... Reproduced 1 bug (use --break to attach the debugger).
... Elapsed 0.0671654 sec.
```

Nice, the bug was reproduced. You can even use the `--break` option to attach the VS debugger and
happily debug the deterministic trace to figure out what is causing the bug. You can repeat this as
many times as you want!

In this tutorial, we saw that we were able to use Coyote to reliably reproduce the race condition in
`AccountManager`. We did this with a tiny test (just two `CreateAccount` calls racing with each
other), as opposed to overloading the system with thousands of concurrent tasks through stress
testing.

This of course was a simple example, but it's easy to imagine the many non-trivial concurrency bugs
in a much more complex codebase. Such bugs have very low probability of being caught during test
time, if you don't use a tool like Coyote to systematically explore the interleavings. In the
absence of such tools, these bugs can go undetected and occur sporadically in production, making
them difficult to diagnose and debug. And who wants to stay awake at night debugging a live site!

## Get the sample source code

To get the source code for the above tutorial, clone the
[Coyote Samples git repo](http://github.com/microsoft/coyote-samples).

Build the sample by running the following command:

```plain
powershell -f build.ps1
```

You can now run the tests (without Coyote) like this:
```plain
cd .\bin\net5.0
.\AccountManager.exe
```

We have added some command line arguments to make it easy select which test to run:
```plain
Usage: AccountManager [option]
Options:
  -s    Run sequential test without Coyote
  -c    Run concurrent test without Coyote
```

To test the sample with Coyote you can use the following commands (as explained in the tutorial
above):
```plain
coyote rewrite .\AccountManager.dll
coyote test .\AccountManager.dll -m TestConcurrentAccountCreation -i 100
coyote replay .\AccountManager.dll -schedule AccountManager_0_0.schedule -m TestConcurrentAccountCreation
```

Enjoy!
