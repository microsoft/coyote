---
layout: reference
title: What is Coyote?
section: learn
permalink: /learn/overview/what-is-coyote
---

## Overview

The key value of Coyote is that it allows you to express your system design at a higher level and specify properties (both safety and liveness) programmatically in your source code. During testing, Coyote serializes your program, captures and controls all (implicit as well as specified) nondeterminism, and thoroughly explores the executable code (in your local dev machine) to automatically discover deep concurrency bugs. If a bug is found, Coyote reports a reproducible bug trace that provides a global order of all concurrent events and nondeterministic choices in the system, and thus is significantly easier to debug than regular unit-/integration-tests and logs from production or stress tests, which are typically nondeterministic.

Besides testing, Coyote can be directly used in production as it offers fast, efficient and scalable execution. As a testament of this, Coyote is being used by several teams in Azure to build mission-critical services.

<div>
<a href="" class="btn btn-primary mt-20 mr-30">Install package</a> <a href="" class="btn btn-primary mt-20">Build from source</a>
</div>

## Supported programming models

For designing and implementing reliable asynchronous software, Coyote provides the following two programming models:
- [Asynchronous tasks](/Coyote/learn/overview/tasks), which follows the popular [task-based asynchronous pattern](https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap). This programming model is based on the `ControlledTask` type (a drop-in-replacement type for `System.Threading.Tasks.Task`), which represents an asynchronous operation that you can coordinate using the `async` and `await` keywords of [C#](https://docs.microsoft.com/en-gb/dotnet/csharp/). In production, a `ControlledTask` executes with the same semantics of a regular `Task` (in fact it is a thin wrapper around a `Task` object). In testing, however, Coyote controls the execution of each `ControlledTask` and is able to explore interleavings to find bugs.
- [Asynchronous state-machines](/Coyote/learn/overview/machines), an [actor-based programming model](https://en.wikipedia.org/wiki/Actor_model) that allows you to express your design and concurrency at a higher-level. This programming model is based on the `Machine` type, which represents a long-lived, interactive asynchronous entity that can create new machines, send events to other machines, and handle received events with custom logic. This more advanced programming model is ideal for cases where asynchronous tasks are getting too unwieldy.

## Publications

Coyote is built on leading edge research:
- [Asynchronous programming, analysis and testing with state machines](https://dl.acm.org/citation.cfm?id=2737996). Pantazis Deligiannis, Alastair F. Donaldson, Jeroen Ketema, Akash Lal and Paul Thomson. In the *36th Annual ACM SIGPLAN Conference on Programming Language Design and Implementation* (PLDI), 2015.
- [Uncovering bugs in distributed storage systems during testing (not in production!)](https://www.usenix.org/node/194442). Pantazis Deligiannis, Matt McCutchen, Paul Thomson, Shuo Chen, Alastair F. Donaldson, John Erickson, Cheng Huang, Akash Lal, Rashmi Mudduluru, Shaz Qadeer and Wolfram Schulte. In the *14th USENIX Conference on File and Storage Technologies* (FAST), 2016.
- [Lasso detection using partial-state caching](https://www.microsoft.com/en-us/research/publication/lasso-detection-using-partial-state-caching-2/). Rashmi Mudduluru, Pantazis Deligiannis, Ankush Desai, Akash Lal and Shaz Qadeer. In the *17th International Conference on Formal Methods in Computer-Aided Design* (FMCAD), 2017.
- [Reliable State Machines: a framework for programming reliable cloud services](http://drops.dagstuhl.de/opus/volltexte/2019/10810/pdf/LIPIcs-ECOOP-2019-18.pdf). Suvam Mukherjee, Nitin John Raj, Krishnan Govindraj, Pantazis Deligiannis, Chandramouleswaran Ravichandran, Akash Lal, Aseem Rastogi and Raja Krishnaswamy. In the *33rd European Conference on Object-Oriented Programming* (ECOOP), 2019.
