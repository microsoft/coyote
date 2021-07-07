## Using Coyote

After you have [installed Coyote](install.md), to run the `coyote` tool use the following
command:

```plain
coyote -?
```

This will list the full command line options. If you are using the .NET Core version of the tool
then you can simply run `dotnet coyote.dll` instead.

### Rewrite your binaries for testing

The `coyote` command line tool can be used to automatically rewrite any .NET binary to take over
concurrency that is built using `System.Threading.Tasks.Task`. For details on what kinds of
rewriting is supported see [Rewriting binaries](../concepts/binary-rewriting.md).

To invoke the rewriter use the following command:

```plain
coyote rewrite ${YOUR_PROGRAM}
```

`${YOUR_PROGRAM}` is the path to your application or library to be rewritten or a folder containing
all the libraries you want rewritten.

Type `coyote rewrite -?` to see the full command line options.

### Example usage

Read this [introductory tutorial](../tutorials/first-concurrency-unit-test.md) to see how Coyote can
be used to rewrite the binary of a simple application for for testing.

### Configuration

You can provide more rewriting options in a JSON file like this:

```json
{
  "AssembliesPath": "bin/net5.0",
  "OutputPath": "bin/net5.0/rewritten",
  "Assemblies": [
    "BoundedBuffer.dll",
    "MyOtherLibrary.dll",
    "FooBar123.dll"
  ]
}
```

- `AssembliesPath` is the folder containing the original binaries.  This property is required.

- `OutputPath` allows you to specify a different location for the rewritten assemblies. The
`OutputPath` can be omitted in which case it is assumed to be the same as `AssembliesPath` and in
that case the original assemblies will be replaced.

- `Assemblies` is an optional list of specific assemblies in `AssembliesPath` to be rewritten. You
can also pull in another assembly by providing a full path to something outside the
`AssembliesPath`. If this list is not provided then the tool will rewrite all assemblies found in
the specified `AssembliesPath`.

Then pass this config file on the command line: `coyote rewrite config.json`.

### Strong name signing

For .NET Framework, you may need to resign your rewritten binaries in order for tests to run
properly. This can be done by providing the same strong name key that you provided during the
original build. This can be done using the `--strong-name-key-file` command line argument (or
`-snk` for the abbreviated option name).

For example, from your `coyote` repo:

```plain
coyote rewrite d:\git\Coyote\Tests\Tests.BugFinding\bin\net462\rewrite.coyote.json
    --strong-name-key-file Common\Key.snk
```

You can also provide this key in the JSON file using the `StrongNameKeyFile` property.

### Test your binaries

The `coyote` command line tool can be used to automatically test a Coyote program to find and
deterministically reproduce bugs in your code while also enforcing your safety and liveness
[specifications](../concepts/specifications.md).

To invoke the tester use the following command:

```plain
coyote test ${YOUR_PROGRAM}
```

`${YOUR_PROGRAM}` is the path to your application or library that contains a method annotated
with the `[Microsoft.Coyote.SystematicTesting.Test]` attribute. This method is the entry point to the
test.

Type `coyote test -?` to see the full command line options.

### Controlled and reproducible testing

In its essence, the Coyote tester:

1. **serializes** the execution of an asynchronous program,
2. **takes control** of the underlying task scheduler and any declared sources of
  non-determinism in the program,
3. **explores** scheduling decisions and non-deterministic choices to trigger bugs.

Because of the above capabilities, the `coyote` tester is capable of quickly discovering bugs that
would be very hard to discover using traditional testing techniques.

During testing, the `coyote` tester executes a program from start to finish for a given number of
testing iterations. During each iteration, the tester is exploring a potentially different
serialized execution path. If a bug is discovered, the tester will terminate and dump a reproducible
trace (including a human readable version of it).

See [below](#reproducing-and-debugging-traces) to learn how to reproduce traces and debug them using
a supported version of Visual Studio.

### Test entry point

A Coyote test method can be declared as follows:

```csharp
[Microsoft.Coyote.SystematicTesting.Test]
public static void Execute()
{
  ...
}
```

The above method acts as the entry point to each testing iteration.

The `coyote` test tool supports calling the following test method signatures:

```csharp
[Microsoft.Coyote.SystematicTesting.Test]
public static void Execute();

[Microsoft.Coyote.SystematicTesting.Test]
public static async Task Execute();

[Microsoft.Coyote.SystematicTesting.Test]
public static void Execute(IActorRuntime runtime);

[Microsoft.Coyote.SystematicTesting.Test]
public static async Task Execute(IActorRuntime runtime);
```

Note that similar to unit-testing, static state should be appropriately reset before each test
iteration because the iterations run in shared memory. However, [parallel instances of the
tester](#parallel-and-portfolio-testing) run in separate processes, which provides isolation.

### Testing options

To see the list of available command line options use the flag `-?`. You can optionally give the
number of testing iterations to perform using `--iterations N` (where N is an integer > 1). If
this option is not provided, the tester will perform 1 iteration by default.

You can also provide a timeout, by providing the flag `--timeout N` (where N > 0), specifying how
many seconds before the timeout. If no iterations are specified (thus the default number of
iterations is used), then the tester will perform testing iterations until the timeout is reached.

Another important flag is `--max-steps N`. This limits each test iteration to be `N` steps, after
which the iteration is killed and a new one is started. Here, steps are counted as the number of
times a synchronization point is reached in the program. The tester provides output at the end of a
test run on the iteration lengths that it saw. Use this output for calibrating the best value of `N`
for your case. This flag is usually only needed when your test has the potential of generating
non-terminating executions (like an infinite series of ping pong events, for example). In such
cases, if you do not provide `max-steps` then the tester can appear to can get stuck running one
iteration forever. This is related to [liveness checking](../how-to/liveness-checking.md).

### Parallel and portfolio testing

The Coyote tester supports parallel testing, often used with a portfolio of different schedulers.

To enable parallel testing, you must run `coyote`, and provide the flag `--parallel N` (where N > 0),
with N specifying the number of parallel testing processes to be spawned. By default, the tester
spawns the same testing process multiple times (using different random seeds).

When doing parallel testing it is often useful to provide the `--sch-portfolio` flag. This option
allocates different exploration strategies to each spawned test process which increases the chance
that one of the test processes will find a particularly difficult bug.

### Reproducing and debugging traces

The `coyote` replayer can be used to deterministically reproduce and debug buggy executions (found
by `coyote test`). To run the replayer use the following command:

```plain
coyote replay ${YOUR_PROGRAM} ${SCHEDULE_TRACE}.schedule
```

Where `${SCHEDULE_TRACE}}.schedule` is the trace file dumped by `coyote test`.

You can attach the Visual Studio debugger on this trace by using `--break`. When using this flag,
Coyote will automatically instrument a breakpoint when the bug is found. You can also insert your
own breakpoints in the source code as usual.

`--break` uses `System.Diagnostics.Debugger.Launch` which does not seem to work on MacOS or Linux,
see troubleshooting below for an alternate way of debugging replay schedules.

See the replay options section of the help output from invoking `coyote -?`.

### Supported scenarios

Out of the box, Coyote supports finding and reproducing bugs in programs written using:

- The `async`, `await` and `lock` C# keywords.
- The most common `System.Threading.Tasks` types in the .NET Task Parallel Library:
  - Including the `Task`, `Task<TResult>` and `TaskCompletionSource<TResult>` types.
- The `Monitor` type in `System.Threading`.

Coyote will let you know with an informative error if it detects a type that it does not support, or
if the test invokes an external concurrent API that you have not mocked or rewritten its assembly.
For these scenarios, Coyote provides the `--no-repro` command line option, which allows you to
ignore these errors by *disabling the ability to reproduce* bug traces (i.e., no `.schedule` file
will be produced when a bug is found). Alternatively, if you are using the Coyote test runner from
inside another [unit testing framework](../how-to/unit-testing.md), you can run Coyote in this mode
by enabling the `Configuration.WithNoBugTraceRepro()` option.

In the `--no-repro` mode, you can continue using Coyote to expose tricky concurrency (and other
nondeterministic) bugs. As Coyote adds supports for more .NET APIs, you will be able to reproduce
bugs in increasingly more scenarios. We are adding support for more APIs over time, but if something
you need is missing please open an issue on [GitHub](https://github.com/microsoft/coyote/issues) or
contribute a [PR](https://github.com/microsoft/coyote/compare)!

### Graphing the results

The `--graph` command line option produces a [DGML diagram](../how-to/generate-dgml.md) containing a
graphical trace of all the state transitions that happened in all test iterations. The `--graph-bug`
command line option is similar but produces a graph only of the last test iteration leading up to a
bug. These graphs are different from the graph generated by `--coverage activity` because the
coverage graph collapses all machine instances into one group, so you can more easily see the whole
coverage of that machine type. The `--graph` options are handy when you need to see the difference
in state that happened in different machine instances.

See [animating state machine demo](../concepts/actors/state-machine-demo.md) for a
visual explanation of what `coyote test` does when it is looking for bugs. In the animation you will
see why the test failed, two of the server nodes have taken on the `Leader` role, which is not
allowed.

See also the [DGML diagram](../assets/images/raft.dgml) which you can open in Visual Studio.
Here the Server state machines are colored in green, and the Leader states in red to
highlight the bug found.

**Note:** this feature is currently only available for programs written using the advanced
[actor](../concepts/actors/overview.md) programming model of Coyote.

### Unit Testing

To use Coyote tester in a unit test environment (e.g. with MSTest or xUnit) see [Unit
Testing](../how-to/unit-testing.md).

### Next steps

Now that you have [installed](install.md) and learned how to use the `coyote` tool, you can jump
right into action by checking out [this tutorial](../tutorials/first-concurrency-unit-test.md) to
learn how to write your first concurrency unit test, which is also available as a [video on
YouTube](https://youtu.be/wuKo-9iRm6o). You can also read about the core
[concepts](../concepts/non-determinism.md) behind Coyote. There are many more
[tutorials](../tutorials/overview.md) and [samples](../samples/overview.md) available for you to
explore Coyote further!

### Troubleshooting

**Format of the executable (.exe) or library (.dll) is invalid.**

If you are using a .NET Core target platform then on Windows you will get executable program with
`.exe` file extension, like `coyote-samples\bin\net5.0\BoundedBuffer.exe` These are not
rewritable assemblies. You must instead rewrite and test the associated library, in this case
`BoundedBuffer.dll`.

**--break does not seem to work on MacOS or Linux**

`--break` depends on `System.Diagnostics.Debugger.Launch` and it seems that this is not supported
on MacOS or Linux. So another way to debug a replay is to follow these steps:

1. Create a new Run Configuration

2. Execute the coyote binary, which you can find in ~/.dotnet/tools/coyote if you installed the
`dotnet tool` called `Microsoft.Coyote.CLI`.

3. Add the arguments ${YOUR_PROGRAM} ${SCHEDULE_TRACE}.schedule

4. Insert the breakpoints where needed and start debugging.
