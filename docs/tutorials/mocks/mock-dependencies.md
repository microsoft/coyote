## Mocking dependencies for concurrency unit testing

Mocking dependencies is a common activity when writing unit tests. The code that you want to test
often depends on other more complex code, such as third-party libraries and external services (e.g.
[Cosmos DB](https://azure.microsoft.com/services/cosmos-db/)). These dependencies might be
impractical to include as part of the test, so you want to replace them with much simpler
implementations that simulate the real behavior. One popular way to replace real dependencies with
mocks is via [dependency injection](https://en.wikipedia.org/wiki/Dependency_injection).

Mocks play an even greater role when [writing concurrency unit
tests](../../concepts/concurrency-unit-testing.md). Coyote explores different interleavings during
each testing iteration, so you have to write mocks that simulate the behavior of the real dependency
by returning the correct response no matter which interleaving is explored. This means that when
testing with Coyote, you need to design mocks with concurrency in mind.

In this tutorial, you will write a simple mock for the `IDbCollection` that was introduced in [write
your first concurrency unit test](../first-concurrency-unit-test.md). You will design this mock to
be used in a concurrent setting, where methods in multiple instances of the class can be called
concurrently, either within the same process or across processes and machines. This latter condition
means that using locks in your code will not help you in writing correct concurrent code.

## What you will need

To run the code in this tutorial, you will need to:

- Install [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/).
- Install the [.NET 6.0 version of the coyote tool](../../get-started/install.md).
- Be familiar with the `coyote` tool. See [using Coyote](../../get-started/using-coyote.md).
- Clone the [Coyote git repo](http://github.com/microsoft/coyote).
- Go through the [write your first concurrency unit test](../first-concurrency-unit-test.md) tutorial.

## Walkthrough

Consider the following deliberately buggy implementation of the `AccountManager.CreateAccount`
method:

```csharp
// Returns true if the account is created, else false.
public async Task<bool> CreateAccount(string accountName, string accountPayload)
{
  if (await this.AccountCollection.DoesRowExist(accountName))
  {
    return false;
  }

  return await this.AccountCollection.CreateRow(accountName, accountPayload);
}
```

and consider this simple `InMemoryDbCollection` mock for the `IDbCollection` interface, which
implements the `CreateRow` and `DoesRowExist` methods used in the above method. You can ignore the
`GetRow` and `DeleteRow` methods for now as they aren't used in `CreateAccount`.

```csharp
public class InMemoryDbCollection : IDbCollection
{
  public Task<bool> CreateRow(string key, string value)
  {
    return Task.FromResult(true);
  }

  public Task<bool> DoesRowExist(string key)
  {
    return Task.FromResult(false);
  }

  public Task<string> GetRow(string key) { ... }
  public Task<bool> DeleteRow(string key) { ... }
}
```

Using this simple mock, you can write a unit test to exercise _sequential_ account creation in the
`AccountManager` class (see [write your first concurrency unit
test](../first-concurrency-unit-test.md) for the `AccountManager` code).

```csharp
[Microsoft.Coyote.SystematicTesting.Test]
public static async Task TestAccountCreation()
{
  // Initialize the mock DB and account manager.
  var dbCollection = new InMemoryDbCollection();
  var accountManager = new AccountManager(dbCollection);

  // Create some dummy data.
  string accountName = "MyAccount";
  string accountPayload = "...";

  // Create the account, it should complete successfully and return true.
  var result = await accountManager.CreateAccount(accountName, accountPayload);
  Assert.True(result);
}
```

After building the code, rewrite the assembly and run the test using Coyote for `10` iterations:

```plain
coyote rewrite .\AccountManager.dll
coyote test .\AccountManager.dll -m TestAccountCreation -i 10
```

The test succeeds.

```plain
. Testing .\AccountManager.dll
... Method TestAccountCreation
... Started the testing task scheduler (process:9072).
... Created '1' testing task (process:9072).
... Task 0 is using 'random' strategy (seed:2168858778).
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
... Testing statistics:
..... Found 0 bugs.
... Scheduling statistics:
..... Explored 10 schedules: 10 fair and 0 unfair.
..... Number of scheduling decisions in fair terminating schedules: 0 (min), 0 (avg), 0 (max).
... Elapsed 0.1182 sec.
```

This works, but can this same mock also be used to pass the test if it was executing concurrently?

Go ahead and try it out on the following concurrency unit test.

```csharp
[Microsoft.Coyote.SystematicTesting.Test]
public static async Task TestConcurrentAccountCreation()
{
  // Initialize the mock DB and account manager.
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

Build the code again, and then rewrite the assembly and run the test using Coyote for `10`
iterations:

```plain
coyote rewrite .\AccountManager.dll
coyote test .\AccountManager.dll -m TestConcurrentAccountCreation -i 10
```

This time the test immediately (and always) fails!

```plain
. Testing .\AccountManager.dll
... Method TestConcurrentAccountCreation
... Started the testing task scheduler (process:13328).
... Created '1' testing task (process:13328).
... Task 0 is using 'random' strategy (seed:802918651).
..... Iteration #1
... Task 0 found a bug.
... Emitting task 0 traces:
..... Writing AccountManager.dll\CoyoteOutput\AccountManager_0_0.txt
..... Writing AccountManager.dll\CoyoteOutput\AccountManager_0_0.schedule
... Elapsed 0.0798435 sec.
... Testing statistics:
..... Found 1 bug.
... Scheduling statistics:
..... Explored 1 schedule: 1 fair and 0 unfair.
..... Found 100.00% buggy schedules.
..... Number of scheduling decisions in fair terminating schedules: 5 (min), 5 (avg), 5 (max).
... Elapsed 0.1867838 sec.
```

Your test asserts that one `CreateAccount` call must succeed and the other must fail, but both calls
succeed with the current mock. This is because the `dbCollection.DoesRowExist` mock method _always_
returns `false` and the `dbCollection.CreateRow` mock method _always_ returns `true` no matter what
order the two `CreateAccount` requests execute. The `dbCollection.DoesRowExist` method should only
return `false` if the account doesn't exist and the `dbCollection.CreateRow` method should only
return `true` if a new row was created.

The mock needs to implement more of the stateful behavior of the database as follows:

```csharp
public class InMemoryDbCollection : IDbCollection
{
  private bool UserExists = false;

  public Task<bool> CreateRow(string key, string value)
  {
    if (this.UserExists)
    {
      throw new RowAlreadyExistsException();
    }

    this.UserExists = true;
    return Task.FromResult(true);
  }

  public Task<bool> DoesRowExist(string key)
  {
    return Task.FromResult(this.UserExists);
  }

  public Task<string> GetRow(string key) { ... }
  public Task<bool> DeleteRow(string key) { ... }
}
```

The above mock is a bit more complicated as it models the `DoesRowExist` and `CreateRow` behavior
more precisely for your test. Build, rewrite and run the same test once again.

```plain
. Testing .\AccountManager.dll
... Method TestConcurrentAccountCreation
... Started the testing task scheduler (process:38080).
... Created '1' testing task (process:38080).
... Task 0 is using 'random' strategy (seed:2983982407).
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
... Testing statistics:
..... Found 0 bugs.
... Scheduling statistics:
..... Explored 10 schedules: 10 fair and 0 unfair.
..... Number of scheduling decisions in fair terminating schedules: 2 (min), 3 (avg), 6 (max).
... Elapsed 0.1560682 sec.
```

The assertion will now pass, but the `CreateAccount` method is actually buggy (read the [write your
first concurrency unit test](../first-concurrency-unit-test.md) tutorial to remember why that is).
Why does the assertion not fail?!

The reason is that while the two asynchronous `CreateAccount` methods are invoked concurrently,
there is no _actual_ concurrency in the test. While your code uses async/await methods, no code path
introduces any concurrency (through `Task.Run`, `Task.Yield` etc), which means that the two methods
execute sequentially, one after another. Let's see how you can inject some concurrency which will
allow Coyote to "shake" the system and uncover the bug!

There are a few ways to make the test truly concurrent. One simple way is to tweak the mock so that
it uses `Task.Run` to [start a new
task](https://docs.microsoft.com/dotnet/api/system.threading.tasks.task.run) whenever its methods
are invoked.

```csharp
public class InMemoryDbCollection : IDbCollection
{
  private bool UserExists = false;

  public Task<bool> CreateRow(string key, string value)
  {
    return Task.Run(() =>
    {
      if (this.UserExists)
      {
        throw new RowAlreadyExistsException();
      }

      this.UserExists = true;
      return true;
    });
  }

  public Task<bool> DoesRowExist(string key)
  {
    return Task.Run(() =>
    {
      return this.UserExists;
    });
  }

  public Task<string> GetRow(string key) { ... }
  public Task<bool> DeleteRow(string key) { ... }
}
```

If you run the `TestConcurrentAccountCreation` test again using the above mock version, you will see
that the bug in `CreateAccount` is now triggered and the assertion fails!

```plain
. Testing .\AccountManager.dll
... Method TestConcurrentAccountCreation
... Started the testing task scheduler (process:17760).
... Created '1' testing task (process:17760).
... Task 0 is using 'random' strategy (seed:641979276).
..... Iteration #1
..... Iteration #2
..... Iteration #3
..... Iteration #4
..... Iteration #5
..... Iteration #6
..... Iteration #7
..... Iteration #8
... Task 0 found a bug.
... Emitting task 0 traces:
..... Writing AccountManager.dll\CoyoteOutput\AccountManager_0_0.txt
..... Writing AccountManager.dll\CoyoteOutput\AccountManager_0_0.schedule
... Elapsed 0.0902799 sec.
... Testing statistics:
..... Found 1 bug.
... Scheduling statistics:
..... Explored 8 schedules: 8 fair and 0 unfair.
..... Found 12.50% buggy schedules.
..... Number of scheduling decisions in fair terminating schedules: 10 (min), 14 (avg), 23 (max).
... Elapsed 0.198829 sec.
```

Awesome! Using `Task.Run` in the mock methods introduces concurrency in the test, which allows the
two `CreateAccount` methods to execute asynchronously and race with each other. This is similar to
how invoking the production implementation of `IDbCollection` (i.e. the actual backend NoSQL
database) typically happens asynchronously.

Can you make the above mock a little more generally applicable, so you don't have to write custom
mocks for each test case? What if you model it in a way that more closely simulates the behavior of
the actual `IDbCollection`? You could write a mock that you can use in all your concurrency unit
tests for the `AccountManager` using a `ConcurrentDictionary`:

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
      var success = this.Collection.TryAdd(key, value);
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

  public Task<string> GetRow(string key) { ... }
  public Task<bool> DeleteRow(string key) { ... }
}
```

Through a very simple change, which is to add a `ConcurrentDictionary` collection to back the
in-memory database, you have now written a simple mock that not only simulates the behavior of
_asynchronously_ adding rows and checking for their existence, but also can be used in many
different concurrency unit tests for the `AccountManager` logic. For example, this mock can be used
in the `TestConcurrentAccountCreationAndDeletion` test that exercises a race between a
`CreateAccount` and `DeleteAccount` request in this [tutorial](../test-concurrent-operations.md).

You can now complete the mock by implementing the `GetRow` and `DeleteRow` methods.

```csharp
public Task<string> GetRow(string key)
{
  return Task.Run(() =>
  {
    var success = collection.TryGetValue(key, out string value);
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
    var success = collection.TryRemove(key);
    if (!success)
    {
      throw new RowNotFoundException();
    }

    return true;
  });
}
```

The above is the complete implementation of the `InMemoryDbCollection` mock that you used in the
[write your first concurrency unit test](../first-concurrency-unit-test.md) tutorial.

Mocks that can be used in concurrency unit tests are often surprisingly easy to write and have the
benefit that they can be reused in multiple testing scenarios as they more closely model the
production behavior of the mocked dependency. Teams in Azure have
[reported](../../case-studies/azure-blockchain-service.md) that spending a little effort to write such
mocks yielded large productivity gains through better concurrency testing coverage.

The cool thing is that writing mocks for testing with Coyote can be done in a "pay-as-you-go"
fashion where the initial mock implementation can be as simple as you want, and more functionality
can be added later to cover increasingly more complex testing scenarios. Even simple mocks can help
you write interesting concurrency unit tests that can find tons of bugs in your code!

In the [next tutorial](optimistic-concurrency-control.md), you will learn how to extend the above
mock to simulate optimistic concurrency control using ETags. Adding support for ETags combined with
the [systematic testing](../../concepts/concurrency-unit-testing.md) of Coyote will allow you to
test a scenario that is fairly hard to hit in production but can lead to data loss.

## Get the sample source code

To get the complete source code for the `AccountManager` tutorial, first clone the [Coyote git
repo](http://github.com/microsoft/coyote).

You can then build the sample by following the instructions
[here](https://github.com/microsoft/coyote/tree/main/Samples/README.md).
