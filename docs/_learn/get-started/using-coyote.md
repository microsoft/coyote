---
layout: reference
section: learn
title: Using Coyote
permalink: /learn/get-started/using-coyote
---

## Using Coyote

As shown in the [overview](../overview/what-is-coyote.md), there are two main ways to use Coyote. The simplest is to use the [asynchronous tasks](../programming-models/async/overview.md) and the more advanced way is using the [asynchronous state machines](../programming-models/state-machines/overview.md).

**Note:** If you are upgrading to Coyote from P#, see [upgrading from P#](../get-started/upgrade.md).

Assuming you have [installed Coyote](../get-started/install.md) and built the samples, you are ready to use the `coyote` command line tool. In your [Coyote samples](http://github.com/microsoft/coyote-samples) local repo you should have the following compiled binaries:

```
coyote\bin\net46\coyote.exe
coyote-samples\AsyncTaskExamples\bin\net46\*.exe
coyote-samples\StateMachineExamples\bin\net46\*.exe
```

You can use the `coyote` tool to automatically test these samples and find bugs. There is a particularly hard bug to find in the `coyote-samples\StateMachineExamples\bin\net46\FailureDetector.exe` sample application. If you run this application from your command prompt it will happily write output forever. It seems perfectly happy right?  But there is a bug that happens rarely, the kind of pesky bug that would keep you up late at night scratching your head.

Ok then, let's see if Coyote can find the bug. To make it easier to use the `coyote` command line go ahead and add it to your `PATH` environment as follows:

```
set PATH=%PATH%;d:\git\Coyote\bin\net46
```

Type `coyote -?` to see the help page to make sure it is working.

```
cd coyote-samples
coyote --test StateMachineExamples\bin\net46\FailureDetector.exe --iterations 1000 --max-steps 200
```

This also runs perfectly up to 1000 iterations. So this is indeed a hard bug to find. It can be found using the `PCT` exploration strategy with a given maximum number of priority switch points `--sch-pct` (or with the default `Random` exploration strategy, but with a much larger number of iterations, typically more than 100,000 of them).

```
coyote test StateMachineExamples\bin\net46\FailureDetector.exe --iterations 1000 --max-steps 200 --sch-pct 10
```

Even then you might need to run it a few times to catch the bug. Set `--iterations` to a bigger number if necessary. You can also let `coyote` decide which exploration strategy to use. Just use `--sch-portfolio` and size `--parallel N` and Coyote will run `N` different exploration strategies for you, in parallel. `coyote` manages the portfolio to give you the best chance of revealing bugs. These strategies were developed from real-world experience on large products in Microsoft Azure. When you use the right scheduling strategy, you will see a bug report:

```
... Task 0 found a bug.
... Emitting task 0 traces:
..... Writing StateMachineExamples\bin\net46\Output\FailureDetector.exe\CoyoteOutput\FailureDetector_0_0.txt
..... Writing StateMachineExamples\bin\net46\Output\FailureDetector.exe\CoyoteOutput\FailureDetector_0_0.pstrace
..... Writing StateMachineExamples\bin\net46\Output\FailureDetector.exe\CoyoteOutput\FailureDetector_0_0.schedule
```

The `*.txt` file is the text log of the iteration that found the bug. The `*.pstrace` file is an XML version of the trace and the `*.schedule` contains the information needed to reproduce the bug.

Finding a hard to find bug is one thing, but if you can't reproduce this bug while debugging there is no point. So the `*.schedule` can be used with the `coyote replay` command as follows:

```
coyote replay StateMachineExamples\bin\net46\FailureDetector.exe StateMachineExamples\bin\net46\Output\FailureDetector.exe\CoyoteOutput\FailureDetector_0_0.schedule
. Reproducing trace in coyote-samples\StateMachineExamples\bin\net46\FailureDetector.exe
... Reproduced 1 bug.
... Elapsed 0.1724228 sec.
```
Attach a debugger during replay and you can see what exactly is going wrong.

You might be wondering what the `FailureDetector` sample app is really doing. The `coyote` command line tool can help you with that also. If you run the following command line it will produce a [DGML](https://en.wikipedia.org/wiki/DGML) visualization of the state machines that are being tested:

```
coyote test StateMachineExamples\bin\net46\FailureDetector.exe --iterations 10 --max-steps 20 --graph
```

You will see the following output:

```
... Emitting graph:
..... Writing StateMachineExamples\bin\net46\Output\FailureDetector.exe\CoyoteOutput\FailureDetector.dgml
```

Open the DGML diagram using Visual Studio 2019 and you will see the following:

![image](/coyote/assets/images/FailureDetector.png)

Download the [FailureDetector.dgml](/coyote/assets/images/FailureDetector.dgml) file to view it interactively using Visual Studio. Make sure the downloaded file keeps the file extension `.dgml`.

**Note**: See [get started with Coyote](../get-started/install.md) for information on how to install the DGML editor component of Visual Studio.

You are now ready to dive into the core concepts for using Coyote to test [async tasks](../programming-models/async/overview.md) and the more advanced [async state machines](../programming-models/state-machines/overview.md).
