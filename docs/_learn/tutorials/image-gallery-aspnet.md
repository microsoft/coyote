---
layout: reference
section: learn
title: ASP.NET Image Gallery
permalink: /learn/tutorials/image-gallery-aspnet
---

## Systematic testing of an unmodified ASP.NET service

This sample shows how to use Coyote to **systematically test** an ASP.NET service. The sample
ASP.NET service included here implements a simple **image gallery**. Please note that this sample
service is *not* fully-fledged and contains some interesting *bugs* on purpose. You can run the unit
tests with and without Coyote, but you cannot actually deploy the sample (as some production logic
is missing).

## What you will need

You will also need to:
- Install [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/).
- Install the [.NET Core 3.1 version of the `coyote` tool](/coyote/learn/get-started/install#installing-the-net-core-31-coyote-tool).
- Clone the [Coyote Samples git repo](http://github.com/microsoft/coyote-samples).
- Be familiar with the `coyote test` tool. See [Testing](/coyote/learn/tools/testing).
- Be familiar with the `coyote rewrite` tool. See [Testing](/coyote/learn/tools/rewriting).

## Sample structure

The `ImageGallery.sln` solution consists of four projects (all will be discussed below):
- `ImageGalleryService`, this is the ASP.NET service.
- `Tests`, this contains two regular unit tests that use `MSTest`.
- `Tests.Coyote`, this invokes the two tests in `Tests` wrapping them with the Coyote systematic
  testing engine.
- `TraceReplayer`, makes it easy to reproduce the bugs found by `coyote test`.

## The Image Gallery sample service

This service is based on a 3-tier architecture: (1) a client (implemented by the unit tests), (2)
two ASP.NET controllers, one for managing accounts (`AccountController`) and one for managing image
galleries (`GalleryController`) associated with these accounts, and (3) two backend storage systems
used by the controllers (Cosmos DB, which stores accounts, and Azure Blob Storage, which stores
images).

The `AccountController` has 4 APIs to `Create`, `Update`, `Get` and `Delete` an account. `Create`
first checks if the account already exists, and if not it creates it in Cosmos DB. Similarly,
`Update` and `Get` first check if the account exists and, if it does, it updates it, or returns it,
accordingly. Finally, `Delete` first checks if the account exists, and if it does it deletes it from
Cosmos DB and then also deletes the associated image container for this account from Azure Blob
Storage.

The `GalleryController` is similar. It has 3 APIs to `Store`, `Get` and `Delete` an image. `Store`
first checks if the account already exists in Cosmos DB and, if it does, it stores the image in
Azure Blob Storage. `Get` is simple, it checks if the image blob exists, if it it does, it returns
the image. Finally, `Delete` first checks if the account already exists in Cosmos DB and, if it
does, it deletes the image blob from Azure Blob Storage.

## Bugs in the service

As mentioned above, this service was designed in purpose to be contain some interesting bugs to
showcase systematic testing with Coyote. You can read the comments in the controllers for more
details about the bugs, but briefly a lot of the above APIs have race conditions. For example, the
controller checks that the account exists, and tries to update it, but another request deletes the
account after the exists check but before the update, resulting in an unhandled exception, and thus
a 500 internal server error, which is a bad bug. The service should never return a 500 to its users!

These kind of bugs involving race conditions between concurrent requests are hard to test. Unit
tests are typically tailored to test sequential programs. Async race conditions can result in flaky
tests and when you start debugging them, adding break points, and diving in, the race condition
might just disappear, something known as a [Heisenbug](https://en.wikipedia.org/wiki/Heisenbug)!

We wrote two unit tests (using `MSTest` and `Microsoft.AspNetCore.Mvc.Testing`) to exercise the
service and uncover bugs: `TestConcurrentAccountRequests` and
`TestConcurrentAccountAndImageRequests`. The former only tests the `AccountController`, while the
later tests both controllers at the same time. You can read more details in the comments for these
tests, but let us quickly summarize what these tests do and what the bugs are:

The `TestConcurrentAccountRequests` method initializes the mocks and injects them into the ASP.NET
service, then creates a client, which does a `Create` account request to create a new account for
Alice. It waits for this request to complete. It then *concurrently* invokes two requests: `Update`
account and `Delete` account, and waits for both to complete. These requests are now racing, and
because there is a race condition in the `AccountController` controller logic, the update request
can nondeterministically (not every time you run the test!) fail due to an unhandled exception (500
error code). The issue is that the controller first checks if the account exists and, if it does, it
then updates it. But after the "does account exists check", the delete request could run, deleting
the account! The update then tries to happen and BAM there is a bug! Interestingly the non-coyote
test run rarely finds this bug.

The `TestConcurrentAccountAndImageRequests` initializes the mocks and injects them into the ASP.NET
service, then creates a client, which does a `Create` account request to create a new account for
Alice. It waits for this request to complete. It then *concurrently* invokes two requests: `Store`
image and `Delete` account, and waits for both to complete. Similar to the above test, these
requests are now racing, and because there is a race condition in the controller logic, the store
request can nondeterministically store the image in an "orphan" container in Azure Storage, even if
the associated account was deleted. This is more subtle bug because the request itself is not
throwing an unhandled exception, it is just a sneaky race when trying to read/write from/to two
backend systems at the same time. This issue is also a data race and you can see the detail in the
controller logic.

Most of the bugs described in this tutorial (and in the comments in the service code) can be fixed
by catching the exceptions thrown by the (mock) storage systems (or checking their returned error
codes) and returning back an appropriate non-500 error code to the client. Some of the bugs might
also require to implement roll back logic in the case where a request updates one or more storage
accounts in a non-transactional manner, which could result in stale data being stored, even if the
the request failed.

Before going further (and into Coyote), lets build and run the tests!

## Build the samples

Build the `coyote-samples` repo by running the following command:

```
powershell -f build.ps1
```

## How to run the unit tests

Just run them from inside Visual Studio, or run the following:
```
cd ./ImageGalleryAspNet/bin/netcoreapp3.1
dotnet test ImageGalleryTests.dll
```

The tests may or may not trigger the bug! Now if you try to debug them, the bugs may or may not
manifest (as they are Heizenbugs). We surely want some better way of testing concurrent code, right?

## Coyote is here to help

You can learn more about the systematic testing capabilities of Coyote
[here](https://microsoft.github.io/coyote/learn/tools/testing), but lets give a brief summary here
to get quickly in action.

Coyote serializes the execution of a concurrent program (i.e., only executes a single task at at
time). A Coyote test executes this serialized program lots of times (we call these testing
iterations), each time exploring different interleavings in a clever manner. If a bug is found,
Coyote gives a trace that allows you to deterministically reproduce the bug. You can use the VS
Debugger to go over the trace as many times as required to fix the bug.

To be able to test this service, we use Coyote's binary rewriting capabilities to instrument
concurrency primitives (Coyote primarily supports common task-related types like `Task` and
`TaskCompletionSource`, the `lock` statement, and some
[more](https://microsoft.github.io/coyote/learn/programming-models/async/rewriting)). This is
handled by our build script above. If you end up reading our website, or see some of the production
code that uses Coyote today, you will notice they are using a custom task type
`Microsoft.Coyote.Tasks.Task`, this is being replaced by the binary rewriting described here (and in
our website), as it makes things so much easier to use Coyote.

## How to run the Coyote systematic tests

Please do not run the MSTest from inside Visual Studio, as it will currently run the un-rewritten
binaries (requires a post build task to get around this). Instead, once you build the service and
the tests, run the following from the root directory of the repo:
```
coyote rewrite ./ImageGalleryAspNet/rewrite.coyote.json
cd ./ImageGalleryAspNet/bin/coyote
dotnet test ImageGalleryTests.Coyote.dll
```

Note: this is `./ImageGalleryAspNet/bin/coyote` and not `./ImageGalleryAspNet/bin/netcoreapp3.1`.

This will run the tests inside the Coyote testing engine, up to 1000 iterations each, and report any
found bugs. The bug should be found most of the time after just a few iterations (as they are not
too deep).

Besides the error output, you should see a bug error such as:
```
X TestConcurrentAccountRequests [10s 407ms]
  Error Message:
   Assert.Fail failed. Found bug: Found unexpected error code.
   Replay trace using Coyote by running:
     TraceReplayer.exe TestConcurrentAccountRequests TestConcurrentAccountRequests.schedule
```

Which also tells you how to reliably reproduce the bug using Coyote.

## How to reproduce a buggy concurrent trace

As you can see above, the `TestConcurrentAccountRequests` failed. This bug is nondeterministic, and
if you try debug it without Coyote it might not always happen. However, Coyote gives you a reliable
repro. Right now, someone can use the replay functionality from the `coyote replay` tool or
programmatically through our replay API, but for the purposes of this sample, we put together a
simple `TraceReplayer` executable that takes the name of the test and the trace file produced by
Coyote, and replays it in the VS debugger. To do this, just invoke the command mentioned in the
error above (change the paths to the ones on your machine):
```
TraceReplayer.exe TestConcurrentAccountRequests TestConcurrentAccountRequests.schedule
```

You will also see that the trace output contains logs such as:
```
[0cad9c28-519e-434f-9c86-9095eaea0dfe] Getting container 'Accounts' from database 'ImageGalleryDB'.
[0cad9c28-519e-434f-9c86-9095eaea0dfe] Creating account with id '0' (name: 'alice', email: 'alice@coyote.com').
[0cad9c28-519e-434f-9c86-9095eaea0dfe] Checking if item with partition key '0' and id '0' exists in container 'Accounts' of database 'ImageGalleryDB'.
[0cad9c28-519e-434f-9c86-9095eaea0dfe] Creating new item with partition key '0' and id '0' in container 'Accounts' of database 'ImageGalleryDB'.
[1ca3b47e-a33c-4908-b73f-6eb9ca74d4c7] Getting container 'Accounts' from database 'ImageGalleryDB'.
[1ca3b47e-a33c-4908-b73f-6eb9ca74d4c7] Creating or updating image with name 'beach' and acccount id '0'.
[1ca3b47e-a33c-4908-b73f-6eb9ca74d4c7] Creating container 'Gallery_0' if it does not exist.
[1ca3b47e-a33c-4908-b73f-6eb9ca74d4c7] Creating blob 'beach' in container 'Gallery_0'.
[1ca3b47e-a33c-4908-b73f-6eb9ca74d4c7] Checking if item with partition key '0' and id '0' exists in container 'Accounts' of database 'ImageGalleryDB'.
[1ca3b47e-a33c-4908-b73f-6eb9ca74d4c7] Reading item with partition key '0' and id '0' in container 'Accounts' of database 'ImageGalleryDB'.
[1ca3b47e-a33c-4908-b73f-6eb9ca74d4c7] Replacing item with partition key '0' and id '0' in container 'Accounts' of database 'ImageGalleryDB'.
[e870b22a-5c31-4244-afc3-2dc25e342034] Getting container 'Accounts' from database 'ImageGalleryDB'.
[e870b22a-5c31-4244-afc3-2dc25e342034] Deleting container with account id '0'.
[09ee84d9-c44e-4e88-9e4f-14ae36bfff37] Getting container 'Accounts' from database 'ImageGalleryDB'.
[09ee84d9-c44e-4e88-9e4f-14ae36bfff37] Deleting account with id '0'.
[09ee84d9-c44e-4e88-9e4f-14ae36bfff37] Checking if item with partition key '0' and id '0' exists in container 'Accounts' of database 'ImageGalleryDB'.
[09ee84d9-c44e-4e88-9e4f-14ae36bfff37] Deleting item with partition key '0' and id '0' in container 'Accounts' of database 'ImageGalleryDB'.
[e870b22a-5c31-4244-afc3-2dc25e342034] Checking if item with partition key '0' and id '0' exists in container 'Accounts' of database 'ImageGalleryDB'.
```

In the above logs, the `GUID` is unique per request to make it easier to see how Coyote explores
various interleaving during testing, and while you are debugging it.

## Summary

In this tutorial you learned:

1. How to mock external systems like Azure Cosmos DB and Azure Storage for systematic testing.
2. How to use the `coyote rewrite` command line to rewrite an ASP.NET service with task-based
   concurrency for systematic testing with Coyote.
3. How to run a systematic test of unmodified code with Coyote inside an MSTest.
4. How to replay a buggy trace of unmodified code with Coyote.

Happy debugging!
