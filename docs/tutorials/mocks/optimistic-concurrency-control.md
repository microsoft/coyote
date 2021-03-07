## Simulating optimistic concurrency control using ETags

Concurrency unit testing with Coyote often involves writing mocks that _simulate_ (a subset of) the
behavior of an external service or library. This is a "pay-as-you-go" effort, it is up to you to
decide how simple or complex you want your mocks to be depending on what kind of logic you want to
test! You can start with writing some very simple mocks and incrementally add behavior if you want
to test more advanced scenarios. The only requirement is that the mocks must work on a concurrent
setting, as Coyote [explores interleavings and other sources of
nondeterminism](../../concepts/non-determinism.md).

For example, the simple `InMemoryDbCollection` mock described in this
[tutorial](mock-dependencies.md) simulates asynchronous row manipulation in a backend NoSQL database
to [test the logic](../first-concurrency-unit-test.md) of an `AccountManager` controller. A great
benefit of designing such a mock is that it can be reused across [many different concurrency unit
tests](../test-concurrent-operations.md), comparing comparing to the more traditional approach of
writing very simple mock methods that return fixed results (like in the [first
version](mock-dependencies.md) of the `InMemoryDbCollection` mock).

In this tutorial, you will see that it is very easy to take this `InMemoryDbCollection` mock and
extend it with [ETags](https://en.wikipedia.org/wiki/HTTP_ETag) to simulate [optimistic concurrency
control](https://en.wikipedia.org/wiki/Optimistic_concurrency_control). While the implementation of
an actual NoSQL database can be really complex, enhancing our mock with ETag semantics can be fairly
trivial.

## What you will need

To run the code in this tutorial, you will need to:

- Install [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/).
- Install the [.NET 5.0 version of the coyote tool](../../get-started/install.md).
- Be familiar with the `coyote` tool. See [using Coyote](../../get-started/using-coyote.md).
- Go through the [mocking dependencies for testing](mock-dependencies.md) tutorial.

## Walkthrough

Let's motivate the problem by extending our `AccountManager` controller to support updating existing
accounts. An account can only be updated if the version of the new instance is greater than that of
the existing instance. To deal with this design requirement, the `AccountManager` must now maintain
a version per account (besides its name and payload), so let's make our life easier and define the
following `Account` class, which includes a `Version` property.

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
public class InMemoryDbCollection : IDbCollection
{
  private readonly ConcurrentDictionary<string, string> Collection;

  public InMemoryDbCollection()
  {
    this.Collection = new ConcurrentDictionary<string, string>();
  }

  public Task<bool> CreateRow(string key, string value) { ... }

  public Task<bool> DoesRowExist(string key) { ... }

  public Task<string> GetRow(string key) { ... }

  public Task<bool> UpdateRow(string key, string value)
  {
    return Task.Run(() =>
    {
      bool success = this.Collection.ContainsKey(key);
      if (!success)
      {
        throw new RowNotFoundException();
      }

      this.Collection[key] = value;
      return true;
    });
  }

  public Task<bool> DeleteRow(string key) { ... }
}
```

Let's implement next the `AccountManager` logic for updating accounts. For simplicity, let's only
focus on the `CreateAccount` and `UpdateAccount` methods, which can be implemented like this:

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
}
```

This was a lot of code. The `CreateAccount` is similar to the [previous
tutorial](../first-concurrency-unit-test.md), but with a few differences. It creates an `Account`
instance using the input account data, then uses `System.Text.Json` to serialize it to a `string`
and tries to add it to the database by invoking `CreateRow`. If this operation fails with a
`RowAlreadyExistsException`, the `AccountManager` catches the exception and returns `false`, else it
returns `true`.

The `UpdateAccount` method is a bit more involved. The method first invokes the `GetRow` database
method to get the value of the account with the name that we want to update (if such an account
already exists), and uses `System.Text.Json` to deserialize the returned value to an `Account`
instance. Next, the `AccountManager` checks if the version of the existing account is greater or
equal than the new account, and if yes, the method fails with `false`. Else, it creates a new
`Account` instance, serializes it and tries to update the corresponding database entry by invoking
`UpdateRow`.

Let's first write a sequential unit test to exercise the above account updating logic.

```csharp
public async Task Test()
{
   var dbCollection = new MockDbCollection();
   var accountManager = new UserAccountManager(dbCollection);

   var result = await accountManager.CreateUser("joe", "...", 1);
   Assert.IsTrue(result);

   result = await accountManager.UpdateUser("joe", "secondVersion", 2);
   Assert.IsTrue(result);

   result = await accountManager.UpdateUser("joe", "secondVersionAlternate", 2);
   Assert.IsFalse(result);
}
```

The above test passes. Let's write a test where we update the user concurrently.

```csharp

public async Task Test()
{
   var dbCollection = new MockDbCollection();
   var accountManager = new UserAccountManager(dbCollection);

   var result = await accountManager.CreateUser("joe", "...", 1);
   Assert.IsTrue(result);

   var updateTask1 = accountManager.UpdateUser("joe", "secondVersion", 2);
   var updateTask2 = accountManager.UpdateUser("joe", "secondVersionAlternate", 2);
   await Task.WhenAll(updateTask1, updateTask2);

   // Assert that only one of the updates above succeed and not both
   Assert.IsTrue(updateTask1.Result ^ updateTask2.Result);
}

```

When you run the test above, you'll realize it will fail in one of the iterations as Coyote will
find an interleaving in which both calls succeed. That race condition happens when both the calls
read the first version of the row, both independently think their version is greater than what is
currently in the database and update the entry.

The problem in fact is worse than that. Consider the following snippet.

```csharp

var updateTask1 = accountManager.UpdateUser("joe", "secondVersion", 2);
var updateTask2 = accountManager.UpdateUser("joe", "thirdVresion", 3);
await Task.WhenAll(updateTask1, updateTask2);

var user =  await accountManager.GetUser("joe");
Assert.IsTrue(user.Version == 3);

```

The above test will also fail in some iterations with version 2 overwriting version 3! So we find
out that this not just a benign failure but our code doesn't respect the semantics at all in the
presence of concurrency.

Cosmos DB provides ETags which we can use and only update the row if the ETags match. This ensures
that we fail if another writer updates the row after we have read it, thus indicating that we made
our decision operating on stale data.

Let's take a look at the correct implementation of `UpdateUser`.

```csharp

public async Task<bool> UpdateUser(string username, string details, int version)
{
   string existingUserPayload;

   ...

   var existingUser = Deserialize(existingUserPayload);
   if (version <= existingUser.Version)
   {
     return false;
   }

   var updatedUser = new User() { Details = details, version = version };

   try
   {
     // This call will fail if the ETags mismatch
     await dbCollection.ReplaceRow(username, existingUser.ETag);
   }
   catch (Exception ex) when (ex is RowNotFound || ex is ETagMismatch)
   {
     return false;
   }

   return true;
}

```

The above requires us to implement the ETag functionality in our simulator.

```csharp

public class Payload
{
  string Value;
  string ETag;
}

public class MockDbCollection : IDbCollection
{
   private ConcurrentDictionary<string, Payload> collection;

   public Task<bool> DoesRowExist(string key)
   {
     return Task.Run(() =>
     {
       return collection.ContainsKey(key);
     });
   }

   public Task<bool> CreateRow(string key, string value)
   {
     return Task.Run(() =>
     {
        var payload = new Payload() { Value = value, ETag = Guid.NewGuid().ToString() }
        var success = collection.TryAdd(key, payload);
        if (!success)
        {
          throw new RowAlreadyExists();
        }

        return true;
     });
   }

   public Task<bool> UpdateRow(string key, string value, string etag)
   {
     return Task.Run(() =>
     {
        lock (collection)
        {
          var getSuccess = collection.TryGet(key, out Payload existingPayload);
          if (getSuccess && etag != existingPayload.ETag)
          {
             throw new ETagMismatchException();
          }

          var success = collection.TryAdd(key, payload);
          if (!success)
          {
            throw new RowAlreadyExists();
          }

          return true;
        }
     });
   }
}

```

We take a `lock` during `UpateRow` to ensure no other task races with us while we checking the ETag
for mismatch. We don't need to take a `lock` during operations which don't check the ETag as we're
using a thread-safe concurrency dictionary.

If we run our test again, we'll see it passes! If we remove the ETag check, it will fail as
expected.

You can find the complete implementation of the simulator using ETags over
[here](https://github.com).

One interesting observation above is that we took a lock in our simulator when simulating the ETag
functionality but we didn't (and couldn't) take a lock in the `UserAccountManager`. This is because
`UserAccountManager` can run across different processes and different machines and locks clearly
don't work in that setting. We run the concurrency test in one process however so its perfectly fine
for our _simulator_ to take a lock to simplify the simulation of the ETag functionality.

As you can see above, it didn't take a lot of effort to simulate ETags in our simulator, as we are
just simulating the semantics in-memory as opposed to implementing the _actual_ code which must
function correctly when run in a distributed system context and has to worry about arbitrary faults
and failures.