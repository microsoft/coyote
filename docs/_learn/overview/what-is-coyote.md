---
layout: reference
title: What is Coyote?
section: learn
permalink: /learn/overview/what-is-coyote
---

## Overview

Coyote is a framework and set of programming models designed to help ensure that your code is free of bugs. Too often developers are drowning in the complexity of their own code and many hours are wasted trying to track down impossible-to-find bugs, especially when dealing with _asynchronous_ code and various other sources of _non-determinism_ (like failures and timeouts).

Coyote is not a software verification system (like TLA+). It does not use theorem proving to make these guarantees, instead it uses intelligent systematic testing built on a deep understanding of concurrency primitives that you have used in your production code, as well as sources of non-determinism that you have explicitly declared in your test code. This approach has proven to work well for large production teams, including many teams in Azure because it has a small barrier to entry with almost immediate benefits for those who adopt it.

Coyote does not require that a team starts from scratch and rebuilds their system. Often times it is too expensive to start over. Instead Coyote can be adopted gradually, adding more and more structure around your _Coyote-aware_ code. The more of this structure you add the more benefit you get from Coyote, but it is certainly not an all or nothing proposition.

Coyote also comes with tools to help you do the systematic testing. The main tool is called `Coyote` and can run your code in a special test mode that deliberately explores all asynchronous interleavings and sources of non-determinism, making it very efficient at finding bugs. This is different from _stress testing_ systems that can also find bugs, but not all of them. Coyote takes control of your asynchrony so that it can manipulate every possible timing, and source of non-determinism to ensure a huge number of different code paths are tested in as short a time as possible. With appropriate _mocking_ Coyote can also do this in "developer" mode on a single laptop with little or no dependence on the bigger production environment.

So Coyote brings together elements of design, development and testing into an integrated package that works really well in the real world. See [Case studies](/Coyote/learn/overview/what-is-coyote) for some really great customer testimonials.

## Supported programming models

Coyote provides two main programming models:

- [Asynchronous tasks](/Coyote/learn/programming-models/async/overview), which follows the popular [task-based asynchronous pattern](https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap). This programming model is based on the `ControlledTask` type (a drop-in-replacement type for `System.Threading.Tasks.Task`), which represents an asynchronous operation that you can coordinate using the `async` and `await` keywords of [C#](https://docs.microsoft.com/en-gb/dotnet/csharp/). In production, a `ControlledTask` executes with the same semantics of a regular `Task` (in fact it is a thin wrapper around a `Task` object). In testing, however, Coyote controls the execution of each `ControlledTask` and is able to explore interleavings to find bugs.
- [Asynchronous state-machines](/Coyote/learn/overview/machines), an [actor-based programming model](https://en.wikipedia.org/wiki/Actor_model) that allows you to express your design and concurrency at a higher-level. This programming model is based on the `Machine` type, which represents a long-lived, interactive asynchronous entity that can create new machines, send events to other machines, and handle received events with custom logic. This more advanced programming model is ideal for cases where asynchronous tasks are getting too unwieldy.
