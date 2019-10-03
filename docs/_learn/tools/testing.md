---
layout: reference
title: Testing Coyote programs
section: learn
permalink: /learn/tools/testing
---

## Testing Coyote programs end-to-end and reproducing bugs

The `Coyote` command line tool can be used to automatically test a Coyote program to find and deterministically reproduce bug in your code while also enforcing your safety and liveness property [specifications](/Coyote/learn/specifications/overview).

To invoke the tester use the following command:
```
Coyote test ${YOUR_PROGRAM}
```
Where `${YOUR_PROGRAM}` is the path to you application or library that contains a method annotated with the `[Microsoft.Coyote.Test]` attribute. This method is the entry point to the test.

Type `Coyote -?` to see the full command line options.  If you are using the .NET Core version of `Coyote` then you simply run `dotnet Coyote.dll ...` instead.

## Controlled, serialized and reproducible testing

In its essence, the Coyote tester:
 1. **serializes** the execution of an asynchronous program,
 2. **takes control** of the underlying machine/task scheduler and any declared sources of non-determinism in the program (e.g. timers and failures),
 3. **explores** scheduling decisions and non-deterministic choices to trigger bugs.

Because of the above capabilities, the `Coyote` tester is capable of quickly discovering bugs that would be very hard to discover using traditional testing techniques.

During testing, the `Coyote` tester executes a program from start to finish for a given number of testing iterations. During each iteration, the tester is exploring a potentially different serialized execution path. If a bug is discovered, the tester will terminate and dump a reproducible trace (including a human readable version of it).

See [below](#reproducing-and-debugging-traces) to learn how to reproduce traces and debug them using the Visual Studio IDE.

## Test entry point

A Coyote test method can be declared as follows:

```c#
[Microsoft.Coyote.Test]
public static void Execute(IMachineRuntime runtime)
{
  runtime.RegisterMonitor(typeof(SomeMonitor));
  runtime.CreateMachine(typeof(SomeMachine));
}
```

This method acts as the entry point to each testing iteration. Note that the `Coyote` tester will internally create a special machine, which invokes the test method and executes it. This allows us to capture and report any errors that occur outside the scope of a user machine (e.g. before the very first machine is created).

Note that similar to unit-testing, static state should be appropriately reset during testing with the `Coyote` tester, since iterations run in shared memory. However, [parallel instances of the tester](#parallel-and-portfolio-testing) run in separate processes, which provides isolation.

## Testing options

To see the **list of available command line options** use the flag `-?`.  You can optionally give the **number of testing iterations** to perform using `--iterations N` (where N is an integer > 1). If this option is not provided, the tester will perform 1 iteration by default.

You can also provide a **timeout**, by providing the flag `--timeout N` (where N > 0, specifying how many seconds before the timeout). If no iterations are specified (thus the default number of iterations is used), then the tester will perform testing iterations until the timeout is reached.

## Parallel and portfolio testing

The Coyote tester supports **parallel** and **portfolio** testing.

To enable parallel testing, you must run `Coyote`, and provide the flag `--parallel N` (where N > 0, with N specifying the number of parallel testing processes to be spawned). By default, the tester spawns the same testing process multiple times (using different random seeds).

To enable portfolio testing, you must run `Coyote` in parallel (as specified above), and also provide the flag `--sch-portfolio`. Portfolio testing currently spawns N (depending on `--parallel N`) testing processes that use a collection of randomized exploration strategies (including fuzzing with different seeds, and probabilistic prioritized exploration).

## Reproducing and debugging traces

The `Coyote` replayer can be used to deterministically reproduce and debug buggy executions (found by `Coyote` test`). To run the replayer use the following command:
```
Coyote replay ${YOUR_PROGRAM} ${SCHEDULE_TRACE}.schedule
```
Where `${SCHEDULE_TRACE}}.schedule` is the trace file dumped by `Coyote` test.

You can attach the Visual Studio debugger on this trace, to get the familiar VS debugging experience, by using `--break`. When using this flag, Coyote will automatically instrument a breakpoint when the bug is found. You can also insert your own breakpoints in the source code as usual.

See the replay options section of the help page `Coyote -?`.
