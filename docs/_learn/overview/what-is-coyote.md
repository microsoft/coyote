---
layout: reference
title: What is Coyote?
section: learn
permalink: /learn/overview/what-is-coyote
---

## What is Coyote?

Coyote is a .NET programming framework designed to help ensure that your code is free of bugs. Too
often developers are drowning in the complexity of their own code and many hours are wasted trying
to track down impossible-to-find bugs, especially when dealing with _concurrent_ code or various
other sources of _non-determinism_ (like message ordering, failures, timeouts and so on).

Coyote provides programming models to express concurrent systems. These programming models offer
convenient ways to program at a high-level of abstraction. As mentioned below, Coyote currently
supports two programming models: a familiar tasks-based programming model (currently in-preview) as
well as a more advanced actor-based programming model. These programming models are built using
asynchronous APIs, supported by a lightweight runtime, making it easy to program efficient
non-blocking code.

Coyote helps write powerful, expressive tests for your code. You can declare sources of
non-determinism (such as timers, failures, etc.) as part of your tests. The Coyote testing tool can
_systematically_ explore a large number of interleavings of concurrent operations as well as
non-deterministic choices so that it covers a large set of behaviors in a very short time. This is
different from _stress testing_. Coyote takes control of the concurrency so that it can manipulate
every possible scheduling. With appropriate _mocking_, Coyote can also do this in "developer" mode
on a single laptop with little or no dependence on the bigger production environment.

Coyote is not a verification system. It does not use theorem proving to make correctness guarantees,
instead it uses intelligent search strategies to drive systematic testing, based on deep
understanding of concurrency primitives that you have used in your code. This approach has proven to
work well for large production teams, including many teams in Microsoft Azure because it has a small
barrier to entry with almost immediate benefits for those who adopt it.

Coyote does not require that a team starts from scratch and rebuild their system. Often it is too
expensive to start over. Instead Coyote can be adopted gradually, adding more and more structure
around your _Coyote-aware_ code. The more of this structure you add the more benefit you get from
Coyote, but it is certainly not an all or nothing proposition.

So Coyote brings together elements of design, development and testing into an integrated package
that works really well in the real world. See our [case
studies](../../case-studies/azure-batch-service) for some great customer testimonials.

<script src="/coyote/assets/js/animation.js"></script>

<svg id="animation" viewbox="0,0,1920,1080" width="75%">
    <style>
      .title { font: bold 50px sans-serif; fill:white; }
    </style>
    <defs>
        <filter id="glow-filter" x="0" y="0" width="125%">
            <feGaussianBlur stdDeviation="5" />
            <feOffset dx="0" dy="0"/>
            <feMerge>
                <feMergeNode/>
                <feMergeNode in="SourceGraphic"/>
            </feMerge>
        </filter>
        <filter id="glow-filter-2" x="-25%" y="-25%" width="150%" height="150%">
            <feGaussianBlur in="SourceGraphic" stdDeviation="10"/>
            <feOffset dx="0" dy="0"/>
            <feMerge>
                <feMergeNode/>
                <feMergeNode in="SourceGraphic"/>
            </feMerge>
        </filter>
    </defs>
    <rect fill="#151520" width="100%" height="100%"/>
</svg>

<script>
    $(document).ready(function () {
        svg = $("#animation")[0];
        hero_animation.start(svg);
    });
</script>

## Supported programming models

Coyote provides two main programming models:

- [Asynchronous tasks](../programming-models/async/overview) ![Task programming model is currently
  in-preview](https://img.shields.io/static/v1?style=flat&color=red&label=&message=preview), which
  follows the popular [task-based asynchronous
  pattern](https://docs.microsoft.com/en-us/dotnet/standard/asynchronous-programming-patterns/task-based-asynchronous-pattern-tap).
  This programming model offers a `Task` type  that serves as a drop-in-replacement type for the
  native .NET `System.Threading.Tasks.Task`. As with the native `Task`, a Coyote `Task` represents
  an asynchronous operation that the programmer can coordinate using the `async` and `await`
  keywords of [C#](https://docs.microsoft.com/en-gb/dotnet/csharp/). In production, a Coyote `Task`
  executes with the same semantics of a native `Task`. In fact, it is simply a thin wrapper around a
  native `Task` object. During testing, however, is where the magic happens. Coyote controls the
  execution of each Coyote `Task` so that it can explore various different interleavings to find
  bugs. Please note that this programming model is currently in preview.

- [Asynchronous actors](../programming-models/actors/overview) is an [actor-based programming
  model](https://en.wikipedia.org/wiki/Actor_model) that allows you to express your design and
  concurrency at a higher-level of abstraction. This programming model starts with the `Actor` type
  that represents a long-lived, interactive asynchronous object. An actor can create new actors,
  send events to other actors, and handle received events. This more advanced programming model is
  ideal for cases when asynchronous tasks get too unwieldy. This programming model also provides a
  `StateMachine` type for easy development of event-driven state-machines. A `StateMachine` is
  simply an `Actor` with explicit `States` and event-driven state transitions.

Note that you cannot use the above two programming models at the same time in the same process.
