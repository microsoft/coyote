---
layout: reference
title: Coyote rewriting tool
section: learn
permalink: /learn/tools/rewriting
---

## Coyote rewriting tool

The `coyote` command line tool can be used to automatically rewrite any .NET binary to take over
concurrency that is built using `System.Threading.Tasks.Task`. For details on what kinds of
rewriting is supported see [Rewriting binaries](../programming-models/async/rewriting).

To invoke the rewriter use the following command:

```
coyote rewrite ${YOUR_PROGRAM}
```

See [Using coyote](/coyote/learn/get-started/using-coyote) for information on where to find the
`coyote` tool.

`${YOUR_PROGRAM}` is the path to your application or library to be rewritten.

Type `coyote rewrite -?` to see the full command line options. If you are using the .NET Core
version of `coyote` then you simply run `dotnet coyote.dll ...` instead.

### Example usage

The [BoundedBuffer example](https://github.com/microsoft/coyote-samples/tree/master/BoundedBuffer)
is written with `System.Threading.Tasks.Task`. It does not start with `using
Microsoft.Coyote.Tasks`. So `BoundedBuffer.cs` is pure .NET C# code that knows nothing about Coyote
types.

However, using `coyote rewrite` we can make it testable under `coyote test` tool as follows:

1. Build BoundedBuffer.sln
2. coyote rewrite bin\netcoreapp3.1\BoundedBuffer.dll
3. coyote test bin\netcoreapp3.1\BoundedBuffer.dll -m TestBoundedBufferMinimalDeadlock -i 100

Notice that the test runs, it finds a bug and the produced log file contains the error:

```
Deadlock detected. Task(0) is waiting for a task to complete,
but no other controlled tasks are enabled.
```

If this code was running in production mode it would have deadlocked and the test would not
complete. Deadlock detection and reporting in the log file is only made possible when the `coyote
test` tool can take over all concurrency, and this was made possible by the `coyote rewrite` step.

### Configuration

If you need to rewrite more than one assembly you can create a JSON file that lists all the
assemblies like this:

```json
{
  "AssembliesPath": "bin/netcoreapp3.1",
  "OutputPath": "bin/netcoreapp3.1/rewritten",
  "Assemblies": [
    "BoundedBuffer.dll",
    "MyOtherLibrary.dll",
    "FooBar123.dll"
  ]
}
```

The `AssembliesPath` is the path to the original binaries, and the `OutputPath` allows you to
specify a different location for the rewritten assemblies. The `OutputPath` can be the same as
`AssembliesPath` in which case the original assemblies will be replaced.

Then pass this config file on the command line: `coyote rewrite config.json`.

### Strong name signing

For .NET 4.7 and 4.8, you may need to resign your rewritten binaries in order for tests to run
properly. This can be done by providing the same strong name key that you provided during the
original build. This can be done using the `--strong-name-key-file` command line argument
(or `-snk` for the abbreviated option name).

For example, from your `coyote` repo:

```
bin\net48\coyote rewrite d:\git\foundry99\Coyote\Tests\BinaryRewriting.Tests\bin\net48\BinaryRewritingTests.coyote.json --strong-name-key-file Common\Key.snk
```

You can also provide this key in the JSON file using the `StrongNameKeyFile` property.