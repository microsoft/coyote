
## How does it work?

First, you need to write a [concurrency unit test](../concepts/concurrency-unit-testing.md). These
tests allow (and encourage) the use of concurrency and [non-determinism](non-determinism.md), which
you would normally avoid in traditional unit tests due to flakiness. By writing such a test, Coyote
allows you to exercise what happens, for example, when [two requests execute
concurrently](../tutorials/first-concurrency-unit-test.md) in your service, as if it was deployed in
production.

To use Coyote, you do not need to change a single line in your production code! Using [binary
rewriting](../concepts/binary-rewriting.md), the `coyote rewrite` tool will instrument your
program-under-test with hooks and stubs that allow Coyote to take control of `Task` objects and
related concurrency types from the .NET Task Parallel Library. This is where the magic happens.
Coyote controls the execution of each `Task` so that it can explore various different interleavings
to find and deterministically reproduce concurrency bugs.

Although Coyote supports testing unmodified task-based programs, it also gives you the option to use
the Coyote in-memory [actor and state machine types](advanced-topics/actors/overview.md) from the
`Microsoft.Coyote.Actors` library. This is a more advanced asynchronous reactive programming model.
This approach requires you to change the design of your application, but gives you powerful (and
battle-tested inside Azure) constructs for building highly-reliable applications.

Regardless if you use Coyote binary rewriting on your unmodified program or wrote your code using
Coyote actors, the next step is to use Coyote to systematically test your code! The `coyote test`
tool takes over the scheduling of a Coyote program. It is able to do this reliably because it deeply
understands the concurrency in .NET applications. The tool knows all concurrent operations in a test
as well as sources of synchronization between them. By controlling the program schedule, Coyote can
reliably explore different interleavings of operations.

The `coyote test` tool uses several state-of-the-art exploration strategies that have been known to
find very deep bugs easily. The tool can run a _portfolio_ of available strategies in parallel,
maximizing chances of revealing bugs. Coyote refers to these exploration strategies as _scheduling
strategies_ and makes it easy to incorporate new strategies, as they come out of
[research](../overview/publications.md). New scheduling strategies are being developed in Microsoft
Research based on a wealth of experience gathered from the Microsoft product groups that are using
Coyote today.

During testing, Coyote will repeatedly run a test from start to completion, each time exercising
different scheduling choices. This methodology has proven to be very effective at providing high
coverage, especially _concurrency_ coverage, in a short amount of time. In a similar way, the tool
can also take over other sources of non-determinism, such as the delivery of timeouts or injection
of failures.

If a bug is found, the `coyote test` tool reports a _reproducible_ bug trace that provides the
global order of all scheduling decisions and nondeterministic choices made during the execution of a
test. The trace can be replayed reliably over and over again, until the bug is identified. This
makes a bug reported by the tool significantly easier to debug than regular unit-/integration-tests
and logs from production or stress tests, which are typically nondeterministic. No more
[Heisenbugs](https://en.wikipedia.org/wiki/Heisenbug)!

See [animating state machine demo](../advanced-topics/actors/state-machine-demo.md) for a visual
explanation of what Coyote does when it is looking for bugs.

Follow this [tutorial](../tutorials/first-concurrency-unit-test.md) to to write your first
concurrency unit test with Coyote. For more information see dealing with
[nondeterminism](../concepts/non-determinism.md) and [concurrency unit
Testing](../concepts/concurrency-unit-testing.md).
