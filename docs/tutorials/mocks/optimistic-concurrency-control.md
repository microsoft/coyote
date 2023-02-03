## Simulating optimistic concurrency control using ETags

Concurrency unit testing with Coyote often involves writing mocks that _simulate_ (a subset of) the
behavior of an external service or library. This is a "pay-as-you-go" effort, it is up to you to
decide how simple or complex you want your mocks to be depending on what kind of logic you want to
test! You can start with writing some very simple mocks and incrementally add behavior if you want
to test more advanced scenarios. The only requirement is that the mocks must work in a concurrent
setting, as Coyote [explores interleavings and other sources of
nondeterminism](../../concepts/non-determinism.md).

For example, the simple `InMemoryDbCollection` mock described in the
[previous tutorial](mock-dependencies.md) simulates asynchronous row manipulation in a backend NoSQL database
to [test the logic](../first-concurrency-unit-test.md) of an `AccountManager`. A great benefit of
designing such a mock is that it can be reused across [many different concurrency unit
tests](../test-concurrent-operations.md), comparing to the more traditional approach of writing very
simple mock methods that return fixed results (like in the [first version](mock-dependencies.md) of
the `InMemoryDbCollection` mock).

In this tutorial, you will see that it is very easy to take the `InMemoryDbCollection` mock and
extend it with [ETags](https://en.wikipedia.org/wiki/HTTP_ETag) to simulate [optimistic concurrency
control](https://en.wikipedia.org/wiki/Optimistic_concurrency_control). While the implementation of
an actual NoSQL database can be really complex, enhancing your mock with ETag semantics can be fairly
trivial.

## What you will need

To run the code in this tutorial, you will need to:

- Install [Visual Studio 2022](https://visualstudio.microsoft.com/downloads/).
- Install the [.NET 7.0 version of the coyote tool](../../get-started/install.md).
- Be familiar with the `coyote` tool. See [using Coyote](../../get-started/using-coyote.md).
- Clone the [Coyote git repo](http://github.com/microsoft/coyote).
- Go through the [mocking dependencies for testing](mock-dependencies.md) tutorial.

## Walkthrough

Let's motivate the problem by extending the `AccountManager` to support updating existing accounts.
An account can only be updated if the version of the new instance is greater than that of the
existing instance. To deal with this design requirement, the `AccountManager` must now maintain a
version per account (besides its name and payload) as follows:

```csharp
public class Account
{
  public string Name { get; set; }

  public string Payload { get; set; }

  public int Version { get; set; }
}
```

Recall that accounts are stored in a backend NoSQL database, which the `AccountManager` accesses via
the `IDbCollection` interface. To be able to update stored accounts, extend `IDbCollection` with an
`UpdateRow` method.

```csharp
public interface IDbCollection
{
    Task<bool> CreateRow(string key, string value);

    Task<bool> DoesRowExist(string key);

    Task<string> GetRow(string key);

    Task<bool> UpdateRow(string key, string value);

    Task<bool> DeleteRow(string key);
}
```

You will also need to extend the `InMemoryDbCollection` mock with `UpdateRow`. Let's write a very
simple mock implementation for this method.

```csharp
public Task<bool> UpdateRow(string key, string value)
{
  return Task.Run(() =>
  {
    lock (this.Collection)
    {
      bool success = this.Collection.ContainsKey(key);
      if (!success)
      {
        throw new RowNotFoundException();
      }

      this.Collection[key] = value;
    }

    return true;
  });
}
```

The `lock` statement ensures that checking if the key exists in the dictionary (via
`this.Collection.ContainsKey(key)`) and updating its value (via `this.Collection[key] = value`) is
done atomically (without interference from other concurrent operations to the database). One
interesting observation is that the lock is inside the `InMemoryDbCollection` mock but not inside
the `AccountManager` code. You might be wondering why it is not okay to use a lock in
`AccountManager`, but it is fine to use it in `InMemoryDbCollection`? The reason behind this choice
is that `AccountManager` instances can run in across different processes or machines in production,
and locks do not work in such an intra-process setting because usually intra-process calls are done
asynchronously and it is not safe to do async code inside a lock statement.  With Coyote, however,
you run the entire concurrency unit test in a single process, so it is perfectly fine for the mock
itself to take a lock, which makes it a lot easier to simulate the ETag functionality.

You can see how the rest of the `InMemoryDbCollection` methods are implemented in the
`AccountManager.ETags` sample, which is available in the [Coyote git
repo](https://github.com/microsoft/coyote/tree/main/Samples/AccountManager/AccountManager.ETags).

Next, let's implement the `AccountManager` logic.

```csharp
public class AccountManager
{
  private readonly IDbCollection AccountCollection;

  public AccountManager(IDbCollection dbCollection)
  {
    this.AccountCollection = dbCollection;
  }

  // Returns true if the account is created, else false.
  public async Task<bool> CreateAccount(string accountName, string accountPayload, int accountVersion)
  {
    var account = new Account()
    {
      Name = accountName,
      Payload = accountPayload,
      Version = accountVersion
    };

    try
    {
      return await this.AccountCollection.CreateRow(accountName, JsonSerializer.Serialize(account));
    }
    catch (RowAlreadyExistsException)
    {
      return false;
    }
  }

  // Returns true if the account is updated, else false.
  public async Task<bool> UpdateAccount(string accountName, string accountPayload, int accountVersion)
  {
    Account existingAccount;

    try
    {
      string value = await this.AccountCollection.GetRow(accountName);
      existingAccount = JsonSerializer.Deserialize<Account>(value);
    }
    catch (RowNotFoundException)
    {
      return false;
    }

    if (accountVersion <= existingAccount.Version)
    {
      return false;
    }

    var updatedAccount = new Account()
    {
      Name = accountName,
      Payload = accountPayload,
      Version = accountVersion
    };

    try
    {
      return await this.AccountCollection.UpdateRow(accountName, JsonSerializer.Serialize(updatedAccount));
    }
    catch (RowNotFoundException)
    {
      return false;
    }
  }

  // Returns the account if found, else null.
  public async Task<Account> GetAccount(string accountName)
  {
    try
    {
      string value = await this.AccountCollection.GetRow(accountName);
      return JsonSerializer.Deserialize<Account>(value);
    }
    catch (RowNotFoundException)
    {
      return null;
    }
  }

  // Returns true if the account is deleted, else false.
  public async Task<bool> DeleteAccount(string accountName)
  {
    try
    {
      return await this.AccountCollection.DeleteRow(accountName);
    }
    catch (RowNotFoundException)
    {
      return false;
    }
  }
}
```

This was a lot of code!

The `CreateAccount` is similar to the [previous tutorial](../first-concurrency-unit-test.md), but
with a few differences. It creates an `Account` instance using the input account data, then uses
`System.Text.Json` to serialize it to a `string` and tries to add it to the database by invoking
`CreateRow`. If this operation fails with a `RowAlreadyExistsException`, the `AccountManager`
catches the exception and returns `false`, else it returns `true`.

The `UpdateAccount` method is a bit more involved. The method first invokes the `GetRow` database
method to get the value of the account with the name that we want to update (if such an account
already exists), and uses `System.Text.Json` to deserialize the returned value to an `Account`
instance. Next, the `AccountManager` checks if the version of the existing account is greater or
equal than the new account, and if yes, the method fails with `false`. Else, it creates a new
`Account` instance, serializes it and tries to update the corresponding database entry by invoking
`UpdateRow`.

The `GetAccount` and `DeleteAccount` methods are also similar to the [previous
tutorial](../first-concurrency-unit-test.md), but now use a `try { ... } catch { ... }` block to
return `false` if the call to `IDbCollection` failed with a `RowNotFoundException`.
 
Let's first write a sequential unit test to exercise the above `UpdateAccount` logic.

```csharp
[Microsoft.Coyote.SystematicTesting.Test]
public static async Task TestAccountUpdate()
{
  // Initialize the mock in-memory DB and account manager.
  var dbCollection = new InMemoryDbCollection();
  var accountManager = new AccountManager(dbCollection);

  string accountName = "MyAccount";

  // Create the account, it should complete successfully and return true.
  var result = await accountManager.CreateAccount(accountName, "first_version", 1);
  Assert.True(result);

  result = await accountManager.UpdateAccount(accountName, "second_version", 2);
  Assert.True(result);

  result = await accountManager.UpdateAccount(accountName, "second_version_alt", 2);
  Assert.False(result);
}
```

Build the code, rewrite the assembly and run the test using Coyote for `10` iterations:

```plain
coyote rewrite .\AccountManager.ETags.dll
coyote test .\AccountManager.ETags.dll -m TestAccountUpdate -i 10
```

The test succeeds.

```plain
. Testing .\AccountManager.ETags.dll
... Method TestAccountUpdate
... Started the testing task scheduler (process:37236).
... Created '1' testing task (process:37236).
... Task 0 is using 'random' strategy (seed:2049239085).
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
... Exploration statistics:
..... Explored 10 schedules: 10 fair and 0 unfair.
..... Number of scheduling decisions in fair terminating schedules: 15 (min), 17 (avg), 25 (max).
... Elapsed 0.2354834 sec.
```

This is cool, but will a test that exercises concurrent account updates also succeed? Let's find out
by writing the following concurrency unit test.

```csharp
[Microsoft.Coyote.SystematicTesting.Test]
public static async Task TestConcurrentAccountUpdate()
{
  // Initialize the mock in-memory DB and account manager.
  var dbCollection = new InMemoryDbCollection();
  var accountManager = new AccountManager(dbCollection);

  string accountName = "MyAccount";

  // Create the account, it should complete successfully and return true.
  var result = await accountManager.CreateAccount(accountName, "first_version", 1);
  Assert.True(result);

  // Call UpdateAccount twice without awaiting, which makes both methods run
  // asynchronously with each other.
  var task1 = accountManager.UpdateAccount(accountName, "second_version", 2);
  var task2 = accountManager.UpdateAccount(accountName, "second_version_alt", 2);

  // Then wait both requests to complete.
  await Task.WhenAll(task1, task2);

  // Finally, assert that only one of the two requests succeeded and the other
  // failed. Note that we do not know which one of the two succeeded as the
  // requests ran concurrently (this is why we use an exclusive OR).
  Assert.True(task1.Result ^ task2.Result);
}
```

Build the code, rewrite the assembly and run the test using Coyote for `10` iterations.  You'll
realize it will fail quite fast as Coyote will find an execution in which _both_ `UpdateAccount`
requests succeed.

This is a bug because only one of the two requests should succeed. This race condition happens when
the two concurrently executing `UpdateAccount` methods both read the first `Version` of the row,
independently think their account `Version` is greater than what is currently stored in the database
and update the entry.

In fact, the problem is worse than that. Consider the following test that first updates the accounts
concurrently using two different versions, `2` and `3`, and then getting the account and asserting
that the account version should always be the latest, which is `3`.

```csharp
[Microsoft.Coyote.SystematicTesting.Test]
public static async Task TestGetAccountAfterConcurrentUpdate()
{
  // Initialize the mock in-memory DB and account manager.
  var dbCollection = new InMemoryDbCollection();
  var accountManager = new AccountManager(dbCollection);

  string accountName = "MyAccount";

  // Create the account, it should complete successfully and return true.
  var result = await accountManager.CreateAccount(accountName, "first_version", 1);
  Assert.True(result);

  // Call UpdateAccount twice without awaiting, which makes both methods run
  // asynchronously with each other.
  var task1 = accountManager.UpdateAccount(accountName, "second_version", 2);
  var task2 = accountManager.UpdateAccount(accountName, "third_version", 3);

  // Then wait both requests to complete.
  await Task.WhenAll(task1, task2);

  // Finally, get the account and assert that the version is always 3,
  // which is the latest updated version.
  var account = await accountManager.GetAccount(accountName);
  Assert.True(account.Version == 3);
}
```

Build the code, rewrite the assembly and run the test using Coyote for `10` iterations. This test
will fail in some iterations with account version `2` overwriting version `3`.

You can see that this is not just a benign failure! The code doesn't respect the `UpdateAccount`
semantics in the presence of concurrency, which is a serious issue.

A database system like [Cosmos DB](https://azure.microsoft.com/services/cosmos-db/) provides
[ETags](https://en.wikipedia.org/wiki/HTTP_ETag) which you can use to only update the row if the
ETags match. This ensures that `UpdateAccount` will fail if another concurrent request updates the
row after `UpdateAccount` has read it, which indicates that `UpdateAccount` operated on stale data.

Let's take a look at a correct implementation of `UpdateAccount` that uses ETags.

```csharp
// Returns true if the account is updated, else false.
public async Task<bool> UpdateAccount(string accountName, string accountPayload, int accountVersion)
{
  Account existingAccount;
  Guid existingAccountETag;

  // Naive retry if ETags mismatch. In production, you would either use a proper retry
  // policy with delays or return a response to the caller requesting them to retry.
  while (true)
  {
    try
    {
      (string value, Guid etag) = await this.AccountCollection.GetRow(accountName);
      existingAccount = JsonSerializer.Deserialize<Account>(value);
      existingAccountETag = etag;
    }
    catch (RowNotFoundException)
    {
      return false;
    }

    if (accountVersion <= existingAccount.Version)
    {
      return false;
    }

    var updatedAccount = new Account()
    {
      Name = accountName,
      Payload = accountPayload,
      Version = accountVersion
    };

    try
    {
      return await this.AccountCollection.UpdateRow(
        accountName,
        JsonSerializer.Serialize(updatedAccount),
        existingAccountETag);
    }
    catch (MismatchedETagException)
    {
      continue;
    }
    catch (RowNotFoundException)
    {
      return false;
    }
  }
}
```

Let's extend the `IDbCollection` interface and `InMemoryDbCollection` mock to support ETags so that
you can run the above test. You can also define a helper `DbRow` class in your mock to store the
database row value with its associated ETag.

```csharp
public class DbRow
{
    public string Value { get; set; }

    public Guid ETag { get; set; }
}

public interface IDbCollection
{
  Task<bool> CreateRow(string key, string value);

  Task<bool> DoesRowExist(string key);

  Task<(string value, Guid etag)> GetRow(string key);

  Task<bool> UpdateRow(string key, string value, Guid etag);

  Task<bool> DeleteRow(string key);
}

public class InMemoryDbCollection : IDbCollection
{
  private readonly ConcurrentDictionary<string, DbRow> Collection;

  public InMemoryDbCollection()
  {
    this.Collection = new ConcurrentDictionary<string, DbRow>();
  }

  public Task<bool> CreateRow(string key, string value)
  {
    return Task.Run(() =>
    {
      // Generate a new ETag when creating a brand new row.
      var dbRow = new DbRow()
      {
        Value = value,
        ETag = Guid.NewGuid()
      };

      bool success = this.Collection.TryAdd(key, dbRow);
      if (!success)
      {
        throw new RowAlreadyExistsException();
      }

      return true;
    });
  }

  public Task<(string value, Guid etag)> GetRow(string key)
  {
    return Task.Run(() =>
    {
      bool success = this.Collection.TryGetValue(key, out DbRow dbRow);
      if (!success)
      {
        throw new RowNotFoundException();
      }

      return (dbRow.Value, dbRow.ETag);
    });
  }

  public Task<bool> UpdateRow(string key, string value, Guid etag)
  {
    return Task.Run(() =>
    {
      lock (this.Collection)
      {
        bool success = this.Collection.TryGetValue(key, out DbRow existingDbRow);
        if (!success)
        {
          throw new RowNotFoundException();
        }
        else if (etag != existingDbRow.ETag)
        {
          throw new MismatchedETagException();
        }

        // Update the Etag value when updating the row.
        var dbRow = new DbRow()
        {
          Value = value,
          ETag = Guid.NewGuid()
        };

        this.Collection[key] = dbRow;
        return true;
      }
    });
  }

  /* Rest of the methods not shown for simplicity */
}
```

The above `InMemoryDbCollection` mock simulates the ETag semantics of Cosmos DB. You can see how the
rest of the `InMemoryDbCollection` methods are implemented in the `AccountManager.ETags` sample,
which is available in the [Coyote git
repo](https://github.com/microsoft/coyote/tree/main/Samples/AccountManager/AccountManager.ETags)

Build the code one last time, rewrite the assembly and run the test using Coyote for `10`
iterations.  This time the test succeeds! If you try to remove the ETag check, it will fail as expected.

As you can see, it didn't take much effort to simulate ETags in the mock, as you just simulated the
semantics _in-memory_. This is significantly easier than if you had to implement the _real_ ETags
functionality in a production distributed system, where you would have to worry about arbitrary
failures, coordination across machines and network delays. Mocks are often fairly easy to write and
help ensure that _your_ distributed service works correctly in the presence of arbitrary concurrency
across a fleet of machines.

## Get the sample source code

To get the complete source code for the `AccountManager.ETags` tutorial, first clone the [Coyote git
repo](http://github.com/microsoft/coyote).

You can then build the sample by following the instructions
[here](https://github.com/microsoft/coyote/tree/main/Samples/README.md).
