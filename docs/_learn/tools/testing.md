---
layout: reference
title: Testing Coyote programs
section: learn
permalink: /learn/tools/testing
---

## Testing Coyote programs end-to-end and reproducing bugs

The `coyote` command line tool can be used to automatically test a Coyote program to find and
deterministically reproduce bugs in your code while also enforcing your safety and liveness
[specifications](../core/specifications).

To invoke the tester use the following command:

```
coyote test ${YOUR_PROGRAM}
```

See [Using coyote](/coyote/learn/get-started/using-coyote) for information on where to find the `coyote` tool.

`${YOUR_PROGRAM}` is the path to your application or library that contains a method annotated
with the `[Microsoft.Coyote.SystematicTesting.Test]` attribute. This method is the entry point to the
test.

Type `coyote -?` to see the full command line options. If you are using the .NET Core version of
`coyote` then you simply run `dotnet coyote.dll ...` instead.

## Controlled, serialized and reproducible testing

In its essence, the Coyote tester:
 1. **serializes** the execution of an asynchronous program,
 2. **takes control** of the underlying actor/task scheduler and any declared sources of
    non-determinism in the program,
 3. **explores** scheduling decisions and non-deterministic choices to trigger bugs.

Because of the above capabilities, the `coyote` tester is capable of quickly discovering bugs that
would be very hard to discover using traditional testing techniques.

During testing, the `coyote` tester executes a program from start to finish for a given number of
testing iterations. During each iteration, the tester is exploring a potentially different
serialized execution path. If a bug is discovered, the tester will terminate and dump a reproducible
trace (including a human readable version of it). If you get unexpected errors, please check that
your test code is adhering to the [Tester requirements](./tester-requirements).

See [below](#reproducing-and-debugging-traces) to learn how to reproduce traces and debug them using
a supported version of Visual Studio.

## Test entry point

A Coyote test method can be declared as follows:

```c#
[Microsoft.Coyote.SystematicTesting.Test]
public static void Execute(IActorRuntime runtime)
{
  runtime.RegisterMonitor(typeof(SomeMonitor));
  runtime.CreateActor(typeof(SomeMachine));
}
```

This method acts as the entry point to each testing iteration. Note that the `coyote` test tool will
internally create a `TestingEngine`, which invokes the test method and executes it. This allows it
to capture and report any errors that occur outside the scope of an actor (e.g. before the very
first actor is created).

The `coyote` test tool supports calling the following test method signatures:

```c#
[Microsoft.Coyote.SystematicTesting.Test]
public static void Execute();

[Microsoft.Coyote.SystematicTesting.Test]
public static void Execute(IActorRuntime runtime);

[Microsoft.Coyote.SystematicTesting.Test]
public static async Task Execute();

[Microsoft.Coyote.SystematicTesting.Test]
public static async Task Execute(IActorRuntime runtime);
```

Note that similar to unit-testing, static state should be appropriately reset before each test
iteration because the iterations run in shared memory. However, [parallel instances of the
tester](#parallel-and-portfolio-testing) run in separate processes, which provides isolation.

## Testing options

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
iteration forever. This is related to [liveness checking](../core/liveness-checking).

## Parallel and portfolio testing

The Coyote tester supports parallel testing, often used with a portfolio of different schedulers.

To enable parallel testing, you must run `coyote`, and provide the flag `--parallel N` (where N > 0),
with N specifying the number of parallel testing processes to be spawned. By default, the tester
spawns the same testing process multiple times (using different random seeds).

When doing parallel testing it is often useful to provide the `--sch-portfolio` flag. This option
allocates different exploration strategies to each spawned test process which increases the chance
that one of the test processes will find a particularly difficult bug.

## Reproducing and debugging traces

The `coyote` replayer can be used to deterministically reproduce and debug buggy executions (found
by `coyote test`). To run the replayer use the following command:

```
coyote replay ${YOUR_PROGRAM} ${SCHEDULE_TRACE}.schedule
```

Where `${SCHEDULE_TRACE}}.schedule` is the trace file dumped by `coyote test`.

You can attach the Visual Studio debugger on this trace by using `--break`. When using this flag,
Coyote will automatically instrument a breakpoint when the bug is found. You can also insert your
own breakpoints in the source code as usual.

See the replay options section of the help output from invoking `coyote -?`.

## Graphing the results

The `--graph` command line option produces a [DGML diagram](/coyote/learn/tools/dgml) containing a
graphical trace of all the state transitions that happened in all test iterations. The `--graph-bug`
command line option is similar but produces a graph only of the last test iteration leading up to a
bug. These graphs are different from the graph generated by `--coverage activity` because the
coverage graph collapses all machine instances into one group, so you can more easily see the whole
coverage of that machine type. The `--graph` options are handy when you need to see the difference
in state that happened in different machine instances.

See [animating state machine demo](/coyote/learn/programming-models/actors/state-machine-demo) for a
visual explanation of what `coyote test` does when it is looking for bugs. In the animation you will
see why the test failed, two of the server nodes have taken on the `Leader` role, which is not
allowed.

See also the [DGML diagram](/coyote/assets/images/raft.dgml) which you can open in Visual Studio.
Here the Server state machines are colored in green, and the Leader states in red to
highlight the bug found.

## Unit Testing

To use Coyote tester in a unit test environment see [Unit Testing](unit-testing.md).
