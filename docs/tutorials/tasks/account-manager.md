## Bounded Buffer Example

Concurrent programming can be tricky. This tutorial shows a classic example of a deadlock and how Coyote can help you
find and understand that deadlock. More details about this program and how Coyote can find this deadlock is found in
this [blog
article](https://cloudblogs.microsoft.com/opensource/2020/07/14/extreme-programming-meets-systematic-testing-using-coyote/).

## What you will need

To run the `BoundedBuffer` example, you will need to:

- Install [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/).
- Install the [.NET Core 5.0 version of the coyote tool](../get-started/install.md).
- Clone the [Coyote Samples git repo](http://github.com/microsoft/coyote-samples).
- Be familiar with the `coyote test` tool. See [Testing](../tools/testing.md).

## Build the samples

Build the `coyote-samples` repo by running the following command:

```plain
powershell -f build.ps1
```

## Run the failover coffee machine application

Now you can run the `BoundedBuffer` application in a mode that should trigger the deadlock most of the time:

```plain
./bin/net5.0/BoundedBuffer.exe -m
```

And you can run it with a fix for the deadlock as follows:

```plain
./bin/net5.0/BoundedBuffer.exe -f
```

### Can you find the deadlock bug in BoundedBuffer class?

The BoundedBuffer is a producer consumer queue where the `Take` method blocks if the buffer is empty and the `Put`
method blocks if the buffer has reached it's maximum allowed capacity and while the code looks correct it contains a
nasty deadlock bug.

Writing concurrent code is tricky and with the popular `async/await` feature in the C# Programming Language it is now
extremely easy to write concurrent code.

But how do we test concurrency in our code? Testing tools and frameworks have not kept up with the pace of language
innovation and so this is where `Coyote` comes to the rescue. When you use Coyote to test your programs you will find
concurrency bugs that are very hard to find any other way.

Coyote rewrites your `async tasks` in a way that allows the [coyote test](../tools/testing.md) tool to control all the
concurrency and locking in your program and this allows it to find bugs using intelligent [systematic
testing](../core/systematic-testing.md).

For example, if you take the `BoundedBuffer.dll` from the above sample you can do the following:

```
coyote rewrite BoundedBuffer.dll
coyote test BoundedBuffer.dll -m TestBoundedBufferMinimalDeadlock --iterations 100
```

This will report a deadlock error because Coyote has deadlock detection during testing. You will get a log file
explaining all this, and more importantly you will also get a trace file that can be used to replay the bug in your
debugger in a way that is 100% reproducable.

Concurrency bugs tend to be the kind of bugs that keep people up late at night pulling their hair out because they are
often not easily reproduced in any sort of predictable manner.

Coyote solves this problem giving you an environment where concurrency bugs can be systematically found and reliably
reproduced in a debugger -- allowing developers to fully understand them and fix the core problem.










## Bounded Buffer Example

Welcome to the Coyote learning series.

Coyote is a very effective tool to test your applications and services for concurrency bugs. Modern services and applications are inherently concurrent in nature as they perform many different activities at the same time, across different threads, processes and machines.

Concurrency is notoriously is hard to test, and concurrency induced bugs are hard to reproduce and understand. Coyote is a very effective tool in taming this complexity and building reliable applications.

We'll introduce Coyote through a series of examples, each one building on top of another. By the end of the learning series, you should be comfortable and familiar with the tools and techniques on how to apply Coyote in your projects.

Let's begin the exercise through a simple example where we write a simple class to create, get and delete "user account" records in a backend NoSQL database. We'll design our class to be used in a concurrent setting, where methods in multiple instances of our class can be called concurrently, either within the same process or across processes and machines. This latter condition means that using locks will not help us in writing correct concurrent code.

Without further ado, let's look at the signature of the class we've to implement.

```csharp

public class UserAccountManager
{
  private IDbCollection userCollection;

  // returns true if user is created, false otherwise
  public async Task<bool> CreateUser(string userName, string userPayload) { ... }

  // returns the userPayload, null otherwise
  public async Task<string> GetUser(string userName) { ... }

  // return true if user is deleted, false otherwise
  public async Task<bool> DeleteUser(string userName) { ... }
}

```

Here are the methods available available in IDbColleciton

```csharp

public interface IDbCollection
{
  Task CreateRow(string key, string value);

  Task<bool> DoesRowExist(string key);

  Task<string> GetRow(string key);

  Task DeleteRow(string key);
}

```

The `CreateRow` method creates the row with the given key, unless it already exists in which case it returns the `RowAlreadyExists` exception. The `GetRow` method returns the content of the given key and throws `RowNotFound` exception if it doesn't exist. Similarly, the `DeleteRow` method deletes the row if it exists and throws  `RowNotFound` exception if it doesn't exist.

Before reading on, we encourage you to open your editor and attempt to write out the code. You can check out the [github repo](https://github.com) and fill out the methods above.

Here's one attempt to write out the methods.

```csharp

public class UserAccountManager
{
  private IDbCollection userCollection;

  // returns true if user is created, false otherwise
  public async Task<bool> CreateUser(string userName, string userPayload)
  {
    if (await userCollection.DoesRowExist(userName))
    {
      return false;
    }


    await userCollection.CreateRow(userName, userPayload);
    return true;
  }

  // returns the userPayload, null otherwise
  public async Task<string> GetUser(string userName)
  {
     if (!await userCollection.DoesRowExist(userName))
     {
       return null;
     }

     return userCollection.GetRow(userName);
  }

  // return true if user is deleted, false otherwise
  public async Task<bool> DeleteUser(string userName)
  {
     if (!await userCollection.DoesRowExist(userName))
     {
       return false;
     }

     return userCollection.DeleteRow(userName);
  }
}
```

Does the above implementation look reasonable to you? Can you find any bugs in the above? And how can you convince yourselves of the absence of any bugs in the above program?

We typically write unit or integration tests to test our software. The repo contains an in-memory implementation of IDbCollection so we can write a unit test. Let's write a test which ensures that the

```csharp

[Test]
public void TestUserAccounts()
{
  var dbCollection = new InMemoryDbCollection();
  var userAccountManager = new UserAccountManager(dbCollection);

  var userName = "joe";
  var userPayload = "...";

  var result = await userAccountManager.CreateUser(userName, userPayload);
  Assert.IsTrue(result);

  // create the same user again; the method should return false this time
  result = await userAccountManager.CreateUser(userName, userPayload);
  Assert.IsFalse(result);
}

```

The test above clearly tests that the same user cannot be created twice. But is the behavior still true if two requests are made _concurrently_? And how can we test it?

What if we spawn two tasks to create the user, concurrently? And then assert that only one succeeds while the other always fails? Hmm - that can possibly work. Let's write this test.

```csharp

[Test]
public void TestUserAccounts()
{
  var dbCollection = new InMemoryDbCollection();
  var userAccountManager = new UserAccountManager(dbCollection);

  var userName = "joe";
  var userPayload = "...";

  // call CreateUser twice, but do not await thus making both of them run concurrently
  var task1 = userAccountManager.CreateUser(userName, userPayload);
  var task2 = userAccountManager.CreateUser(userName, userPayload);

  await Task.WhenAll(task1, task2);

  var result1 = task1.Result;
  var result2 = task2.Result;

  // one of them must have succceded, and the other one must have failed,
  // but we do not which one as they ran concurrently so we check for both
  // possibilities.
  Assert.IsTrue(
    (result1 && !result2) ||
    (!result2 && result1));

  // alternatively, we could have asserted for an Exclusive OR of the two boolean
  // values which tests the same thing
  // Assert.IsTrue(result1 ^ result2);
}

```

When you run this test, it will most likely fail. The reason we say most likely instead of a guaranteed failure is that there are some task interleavings where it passes, and others where it fails.

```
Unhandled exception. System.AggregateException: One or more errors occurred. (Exception of type 'UserAccountManager.RowAlreadyExists' was thrown.)
 ---> UserAccountManager.RowAlreadyExists: Exception of type 'UserAccountManager.RowAlreadyExists' was thrown.
...
```

Let's dig into why this failed.

We starterd two concurrent `CreateUser` calls, the first one checked whether the user existed through the `DoesRowExist` call which returned false. The control passed to the second task which made a similar call to `DoesRowExist` which returned false. Both the tasks resumed believing the user to not exist and tried to add the user. One of them succeeded while the other threw an exception, indicating a bug in our implementation.

So writing out this test was useful and easily exposed a bug in our implementation. But why don't we write such tests a lot more often? The reason is they are often flaky and find bugs through "sheer luck" instead of a systematic exploration of the possible interleavings. The above test hits the bug fairly frequently due to the way .NET task scheduling works (on a reasonably fast machine with light CPU load).

Let's tweak the test very slightly by adding a delay of a millisecond between the two `CreateUser` calls:

```csharp
// call CreateUser twice, but do not await thus making both of them run concurrently
var task1 = userAccountManager.CreateUser(userName, userPayload);
await Task.Delay(1);
var task2 = userAccountManager.CreateUser(userName, userPayload);
```

If you run this test, chances are it will fail exceedingly rarely. We ran this test in a loop invoking it about a hundred times and it didn't fail once.

```
Iteration 0 - Passed
Iteration 1 - Passed
...
Iteration 99 - Passed
```

The concurrency bug is still there but our small test suddenly became ineffective at catching it. This explains why developers don't write such tests as they are very sensitive to timing issues. Developers often write stress tests, where the system may be bombarded with thousands of concurrent user creation requests in the hopes that an interleaving with the bug may be hit before the code is deployed in production.

The above is clearly not a satisfactory solution. What we need is a tool which can systematically explore the various task interleavings in test mode as opposed to leaving that to the operating system scheduler.

Coyote gives us _exactly_ the above.

Let's run our test, unchanged, under the control of the Coyote's runtime. Coyote rewrites assemblies during testing to take control of task schedudling from .NET's built-in task scheduler to a custom task scheduler which can systematically explore various interleavings. Coyote also runs the test multiple number of times, exploring different interleavings across different runs.

We see that the bug is caught frequently when the test is run under Coyote's control.

```
..... Iteration #1
..... Iteration #2
..... Iteration #3
Unhandled exception. NUnit.Framework.AssertionException:    Testing statistics:
 Found 1 bug.
 Scheduling statistics:
 Explored 9 schedules: 9 fair and 0 unfair.
 Found 11.11% buggy schedules.
 Number of scheduling points in fair terminating schedules: 18 (min), 22 (avg), 27 (max).
 Random Generator Seed:

Bug Report: Unhandled exception. UserAccountManager.RowAlreadyExists: Exception of type 'UserAccountManager.RowAlreadyExists' was thrown.
...
```

We are able to reliably reproduce this race condition through a race between just two calls through Coyote. This was a simple example and it's easy to imagine the many non-trivial concurrency bugs which have very low probability of being caught if a tool like Coyote doesn't systematically explore the interleavings. In the absence of such tools, these bugs go undetected and occur sporadically in production and are difficult to diagnose and debug (due to their sporadic and hard-to-reproduce nature). We are able to fairly reilably trigger such bugs through the smallest possible input (just two tasks racing with each other) as opposed to overloading the system with thousands of concurrent tasks through stress testing.

Coyote also gives us a reproducible trace file through which we can run the exact set of interleavings which lead to the bug, over and over again through the debugger, which speaking from personal experience, is extremely useful when understanding tricky bug traces discovered by Coyote.

Having learned the basics of Coyote, we'll test the `UserAccountManager` more thoroughly and write further tests in the next article.
