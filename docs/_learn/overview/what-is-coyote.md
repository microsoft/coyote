---
layout: reference
title: What is Coyote?
section: learn
permalink: /learn/overview/what-is-coyote
---

## Overview

Coyote is a .NET programming framework designed to help ensure that your code is free of bugs. Too often developers are drowning in the complexity of their own code and many hours are wasted trying to track down impossible-to-find bugs, especially when dealing with _concurrent_ code or various other sources of _non-determinism_ (like interleavings, failures, timeouts and so on).

Coyote provides programming models to express concurrent systems. These programming models provide convenient ways to program at a high-level of abstraction. As mentioned below, Coyote currently supports two programming models: a familiar Tasks-based programming as well as a more advanced actor-based programming model. These programming models are built using asynchronous APIs, supported by a lightweight runtime, making it easy to program efficient non-blocking code.

Coyote helps write powerful, expressive tests for your code. You can declare sources of non-determinism (such as timers, failures, etc.) as part of your tests. The Coyote testing tool can _systematically_ explore a large number of interleavings of concurrent operations as well as  non-deterministic choices so that it covers a large set of behaviors in a very short time. This is different from _stress testing_. Coyote takes control of the concurrency so that it can manipulate every possible scheduling. With appropriate _mocking_, Coyote can also do this in "developer" mode on a single laptop with little or no dependence on the bigger production environment.

Coyote is not a verification system (like TLA+, for instance). It does not use theorem proving to make correctness guarantees, instead it uses intelligent search strategies to drive systematic testing, based on deep understanding of concurrency primitives that you have used in your production code, as well as sources of non-determinism that you have explicitly declared in your test code. This approach has proven to work well for large production teams, including many teams in Azure because it has a small barrier to entry with almost immediate benefits for those who adopt it.

Coyote does not require that a team starts from scratch and rebuild their system. Often times it is too expensive to start over. Instead Coyote can be adopted gradually, adding more and more structure around your _Coyote-aware_ code. The more of this structure you add the more benefit you get from Coyote, but it is certainly not an all or nothing proposition.

So Coyote brings together elements of design, development and testing into an integrated package that works really well in the real world. See our [case studies](/coyote/case-studies/azure-batch-service) for some really great customer testimonials.

## Supported programming models

Coyote provides two main programming models:

- [Asynchronous tasks](/coyote/learn/programming-models/async/overview), which follows the popular [task-based asynchronous pattern](https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap). This programming model offers a `ControlledTask` type  that serves as a drop-in-replacement type for the native `System.Threading.Tasks.Task`. As with `Task`, a `ControlledTask` represents an asynchronous operation that the programmer can coordinate using the `async` and `await` keywords of [C#](https://docs.microsoft.com/en-gb/dotnet/csharp/). In production, a `ControlledTask` executes with the same semantics of a regular `Task`. In fact, it is simply a thin wrapper around a `Task` object. During testing, however, is where the magic happens. Coyote controls the execution of each `ControlledTask` so that it can explore various different interleavings to find bugs.

- [Asynchronous state machines](/coyote/learn/programming-models/state-machines/overview) is an [actor-based programming model](https://en.wikipedia.org/wiki/Actor_model) that allows you to express your design and concurrency at a higher-level. This programming model is based on the `StateMachine` type, which represents a long-lived, interactive asynchronous actor that can create new machines, send events to other machines, and handle received events with custom logic. This more advanced programming model is ideal for cases when asynchronous tasks get too unwieldy.

![image](/coyote/assets/images/core.png)