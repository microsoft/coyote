---
layout: reference
title: How does Coyote work?
section: learn
permalink: /learn/overview/how
---

## How does it work?

There are two parts to how Coyote works.  First you need to write some code that uses the Coyote
programming model.  This code then provides the metadata needed to enable the `coyote test` tool to
do it's magic.

The `coyote test` tool takes over the scheduling of a Coyote program. It is able to do this reliably
because it deeply understands the required [Coyote programming
models](/coyote/learn/overview/what-is-coyote). Therefore, the tool knows all concurrent operations
in a test as well as sources of synchronization between them. By controlling the scheduling, the
tool can reliably explore different interleavings of operations. The tool will repeatedly run a
test, each time exercising different scheduling choices. This methodology has proven to be very
effective at providing high coverage, especially _concurrency_ coverage, in a short amount of time.
In a similar way, the tool can also take over other sources of non-determinism, such as the delivery
of timeout messages or injection of failures.

If a bug is found, the `coyote test` tool reports a _reproducible_ bug trace that provides the
global order of all scheduling decisions and nondeterministic choices made during the execution of a
test. The trace can be replayed reliably over and over again, until the bug is identified. This
makes a bug reported by the tool significantly easier to debug than regular unit-/integration-tests
and logs from production or stress tests, which are typically nondeterministic. No more
[Heisenbugs](https://en.wikipedia.org/wiki/Heisenbug)!

The exact mechanism Coyote uses to do this depends on the programming model you have chosen. For the
`Task` model, the `coyote test` tool is able to intercept all task creation operations by
overriding various .NET Task methods.  For the `Actor` model the `coyote test` tool can control when
messages are dequeued by each Actor and therefore control the overall message scheduling.

The next step is to pick a search strategy that explores all possible interleavings of asynchronous
operations. The `coyote test` tool uses several state-of-the-art exploration strategies that have
been known to find very deep bugs easily. The tool can run a _portfolio_ of available strategies in
parallel, maximizing chances of revealing bugs. Coyote refers to these exploration strategies as
_scheduling strategies_ and makes it easy to incorporate new strategies, as they come out of
research. New scheduling strategies are being developed in Microsoft Research based on a wealth of
experience gathered from the Microsoft product groups that are using Coyote today. See [case
studies](/coyote/case-studies/azure-batch-service).

See [animating state machine demo](/coyote/learn/programming-models/actors/state-machine-demo) for a
visual explanation of what Coyote does when it is looking for bugs.

For more information see [Systematic Testing](/coyote/learn/core/systematic-testing) and dealing with
[Nondeterminism](/coyote/learn/core/non-determinism).