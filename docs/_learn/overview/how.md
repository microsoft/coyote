---
layout: reference
title: How does Coyote work?
section: learn
permalink: /learn/overview/how
---

## How does it work?

The `coyote test` tool takes over the scheduling of a Coyote program. It is able to do this reliably
because it deeply understands the required [Coyote programming
models](/coyote/learn/overview/what-is-coyote). Therefore, `coyote test` knows all concurrent
operations in a test as well as sources of synchronization between them. By controlling the
scheduling, `coyote test` can reliably explore different interleavings of operations. The `coyote
test` tool will repeatedly run a Coyote test, each time exercising different scheduling choices.
This methodology has proven to be very effective at providing high coverage, especially
_concurrency_ coverage, in a short amount of time. In a similar way, `coyote test` can also take
over other sources of non-determinism, such as the delivery of timeout messages or injection of
failures.

If a bug is found, `coyote test` reports a _reproducible_ bug trace that provides the global order
of all scheduling decisions and nondeterministic choices made during the execution of a test. The
trace can be replayed reliably over and over again, until the bug is identified. This makes a bug
reported by `coyote test` significantly easier to debug than regular unit-/integration-tests and
logs from production or stress tests, which are typically nondeterministic. No more
[Heisenbugs](https://en.wikipedia.org/wiki/Heisenbug)!

The exact mechanism Coyote uses to do this depends on the programming model you have chosen. The
`coyote test` tool uses several state-of-the-art exploration strategies that have been known to find
very deep bugs easily. The tool runs a _portfolio_ of available strategies, maximizing chances of
revealing bugs. Coyote refers to these exploration strategies as _scheduling strategies_ and makes
it easy to incorporate new strategies, as they come out of research. New scheduling strategies are
being developed in Microsoft Research based on a wealth of experience gathered from the Microsoft
product groups that are using Coyote today. See [case
studies](/coyote/case-studies/azure-batch-service).

See [animating state machine demo](/coyote/learn/programming-models/actors/state-machine-demo) for a
visual explanation of what Coyote does when it is looking for bugs.
