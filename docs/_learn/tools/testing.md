---
layout: reference
title: Testing Coyote programs
section: learn
permalink: /learn/tools/testing
---

## Testing Coyote programs end-to-end and reproducing bugs

The `coyote` command line tool can be used to automatically test a Coyote program to find and
deterministically reproduce bugs in your code while also enforcing your safety and liveness property
[specifications](/Coyote/learn/specifications/overview).

To invoke the tester use the following command:

```
coyote test ${YOUR_PROGRAM}
```

Where `${YOUR_PROGRAM}` is the path to you application or library that contains a method annotated with
the `[Microsoft.Coyote.Test]` attribute. This method is the entry point to the test.

Type `coyote -?` to see the full command line options. If you are using the .NET Core version of
`coyote` then you simply run `dotnet coyote.dll ...` instead.

## Controlled, serialized and reproducible testing

In its essence, the Coyote tester:
 1. **serializes** the execution of an asynchronous program,
 2. **takes control** of the underlying machine/task scheduler and any declared sources of non-determinism in the program,
 3. **explores** scheduling decisions and non-deterministic choices to trigger bugs.

Because of the above capabilities, the `coyote` tester is capable of quickly discovering bugs that
would be very hard to discover using traditional testing techniques.

During testing, the `coyote` tester executes a program from start to finish for a given number of
testing iterations. During each iteration, the tester is exploring a potentially different serialized
execution path. If a bug is discovered, the tester will terminate and dump a reproducible trace
(including a human readable version of it).

See [below](#reproducing-and-debugging-traces) to learn how to reproduce traces and debug them using
the Visual Studio IDE.

## Test entry point

A Coyote test method can be declared as follows: (**todo**: what about tasks?)

```c#
[Microsoft.Coyote.Test]
public static void Execute(IActorRuntime runtime)
{
  runtime.RegisterMonitor(typeof(SomeMonitor));
  runtime.CreateStateMachine(typeof(SomeMachine));
}
```

This method acts as the entry point to each testing iteration. Note that the `coyote` tester will
internally create a special machine, which invokes the test method and executes it. This allows us to
capture and report any errors that occur outside the scope of a user machine (e.g. before the very
first machine is created).

Note that similar to unit-testing, static state should be appropriately reset before each test
iteration because the iterations run in shared memory. However,
[parallel instances of the tester](#parallel-and-portfolio-testing) run in separate processes,
which provides isolation.

## Testing options

To see the **list of available command line options** use the flag `-?`. You can optionally give the
**number of testing iterations** to perform using `--iterations N` (where N is an integer > 1). If this
option is not provided, the tester will perform 1 iteration by default.

You can also provide a **timeout**, by providing the flag `--timeout N` (where N > 0, specifying how
many seconds before the timeout). If no iterations are specified (thus the default number of iterations
is used), then the tester will perform testing iterations until the timeout is reached.

Another important flag is `--max-steps N`. This limits each test iteration to be `N` steps, after which
the iteration is killed and a new one is started. Here, steps are counted as the number of times a
synchronization point is hit in the program. The tester provides output at the end of a test run on the
iteration lengths that it saw. Use this output for calibrating on what `N` makes most sense for your
case. This flag is usually only needed when your test has the potential of generating non-terminating
executions. In such cases, if you do not provide `max-steps` then the tester can appear to hang because
it can get stuck running one iteration forever. This is related to
[liveness checking](/Coyote/learn/specifications/liveness-checking).

## Parallel and portfolio testing

The Coyote tester supports **parallel** and **portfolio** testing.

To enable parallel testing, you must run `coyote`, and provide the flag `--parallel N` (where N > 0,
with N specifying the number of parallel testing processes to be spawned). By default, the tester
spawns the same testing process multiple times (using different random seeds).

To enable portfolio testing, you must run `coyote` in parallel mode (as specified above), and also
provide the flag `--sch-portfolio`. Portfolio testing currently spawns N (depending on `--parallel N`)
testing processes that use a collection of different exploration strategies (including fuzzing with
different seeds, and probabilistic prioritized exploration).

## Reproducing and debugging traces

The `coyote` replayer can be used to deterministically reproduce and debug buggy executions
(found by `coyote test`). To run the replayer use the following command:

```
coyote replay ${YOUR_PROGRAM} ${SCHEDULE_TRACE}.schedule
```

Where `${SCHEDULE_TRACE}}.schedule` is the trace file dumped by `coyote test`.

You can attach the Visual Studio debugger on this trace, to get the familiar VS debugging experience,
by using `--break`. When using this flag, Coyote will automatically instrument a breakpoint when the
bug is found. You can also insert your own breakpoints in the source code as usual.

See the replay options section of the help page `Coyote -?`.

## Graphing the results

The `--graph` command line option produces a directed graph in the
[DGML file format](https://en.wikipedia.org/wiki/DGML) containing a graphical trace of the state transitions that
happened in the test iteration leading up to a bug.  This is different from
the graph generated by `--coverage activity` because the coverage graph collapses
all machine instances into one group, so you can more easily see the all up
coverage of that machine type.  The `--graph` option is handy when you need to
see the difference in state that happened in different machine instances.

The following picture shows the DGML output from the command:

```
coyote test ..\CoyoteSamples\MachineExamples\bin\net46\Raft.exe -i 1000 -ms 200 -sch-pct 10 --graph
```
This looks for bugs in the sample implementation of the [Raft Concensus Algorithm](https://raft.github.io/).  In the picture you can see why the test failed, two
of the server nodes have taken on the `Leader` role, which is not allowed.
See also the [DGML diagram](/Coyote/assets/images/raft.dgml) which you can open
in Visual Studio.

![raft](/Coyote/assets/images/raft.png)