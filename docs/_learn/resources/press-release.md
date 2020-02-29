---
layout: reference
section: learn
title: Press Release
permalink: /learn/resources/press-release
---

# Announcing Coyote, taking the uncertainty out of asynchronous systems

Software developers face the difficult balancing act of optimizing two metrics that are fundamentally at odds with each
other: on the one hand new software must be created quickly, and on the other, it must also be free of bugs, it must not
crash or hang, or lose customer trust. So developers need a way to keep reliability in check without imposing undue
development costs.

This is getting increasingly more difficult in todays world of cloud services where developers must
integrate multiple back end services in order to deliver scalable solution to their users.  Cloud services are
inherently distributed programs that contain multiple dimensions of complexity.  Developers must be able to optimize the
concurrency of their service so they can fully exploit multiple back end resources without bottlenecks, and they must
design failover logic so that the service can survive failures without losing data or availability. Furthermore, all
this must be done while being reactive to incoming web requests and asynchronous responses from backend cloud services.

What all this boils down to is developers are struggling with the complexity of asynchronous non-deterministic systems,
in all software domains, not just cloud services.  Non-determinism shows up in many places.  First there is the
non-deterministic scheduling of concurrent operations where the operating system is scheduling threads and processes.
Next there is non-determinism from the order in which messages are received from remote systems.  Then there are random
failures that can happen in all levels of the system.  Lastly there are often timers firing at random times either for
retry logic or timeouts from other services that have become unresponsive.

Designing, implementing and testing non-deterministic systems is challenging to say the least. Current practices, such
as stress-testing, or using failure-injection tools can be useful, but developers need more. Failure-injection tools
often require complex setup, and stress-testing requires a large amount of time to find bugs, and those bugs are almost
impossible to reproduce.

Take the following example which shows 5 servers (in green) that are acting as a fault tolerant cluster so they can provide
a highly reliable service to their clients.  These services implement the raft consensus protocol to elect a leader and
in doing so hundreds of messages are flying back and forth between these machines, resulting in a large amount of
non-determinism.  This system might work under stress tests with no bugs, but can you ever be really confident it is
ready to ship?


<div style="width:400">

{% include Raft.svg %}

<script language="javascript" src="/coyote/assets/js/animate_trace.js"></script>
<script language="javascript" src="/coyote/assets/js/trace_model.js"></script>

<script language="javascript">

fetchTrace('/coyote/assets/data/Raft.xml', convertTrace);

</script>
</div>


### Coyote enables fearless development of reliable asynchronous software

Coyote is a .NET framework built on the Task Parallel Library that guides you towards designing, implementing and
testing your code in a way that embraces [non-determinism](/coyote/learn/core/non-determinism) and asynchrony.  Instead
of trying to hide non-determinism, Coyote helps you explicitly model where non-determinism is coming from in your
system.  Coyote uses this information to provide a state-of-the-art testing tool that can execute your Coyote code in a
special “test” mode.  This advanced test engine can control every source of non-determinism that you have defined,
including the exact order of every asynchronous operation, which allows the test engine to systematically explore all
the possibilities.  This tool runs very quickly and reaches unheard of levels of coverage of all non-deterministic
choices in your code, and as a result it finds most of the tricky bugs in your code in a way that is also trivial to
reproduce and debug.

One customer said, _“features developed in Coyote test mode worked perfectly in production first time”_.  Another
customer said, _“a feature that took 6 months without Coyote was developed in one month using Coyote”_.  As a result:
_“developers gained a significant confidence boost”_ and _“Coyote provided us with the confidence and capability to
churn code much faster than before”_.

Coyote is the result of years of investment from [Microsoft Research](https://www.microsoft.com/en-us/research/) in the
space of program verification and testing. Coyote draws on ideas from a previous project called P#.   Coyote has now
graduated from Research and is now a full battled-hardened component used by Azure.  Multiple Azure infrastructure
services have been written using Coyote from teams like Azure Batch, Azure Compute, and Azure Blockchain.

Coyote is a combination of a programming model, a lightweight runtime and a testing infrastructure all packaged as a
portable .NET library, with minimal dependencies. Coyote supports two main programming models. If you are happy
developing your code using C# async/await construct for asynchronous Tasks, then Coyote can add value on top of that. By
switching to the [Coyote task library](/coyote/learn/programming-models/async/overview) the Coyote tester will be able
to find many bugs in your code. The C# async/await feature is a wonderful thing, but sometimes even that can result code
that is too parallel which results in a lot of complexity, for example, when performing two or more concurrent tasks
you may need to guard private state with locks and then you have to worry about deadlocks and so on.

Coyote solves this with a more advanced programming model called the [Actor programming
model](/coyote/learn/programming-models/actors/overview). Actors constrain your parallelism so that a given instance of
an Actor receives messages in a serialized order via an inbox.  [Actor
models](https://en.wikipedia.org/wiki/Actor_model) have gained a lot of popularity especially in the area of distributed
systems, precisely because they help you manage the complexity of your system.  Actors essentially embrace asynchrony by
making every message between actors an async operation.  The Coyote test engine fully understands the semantics of
Actors and can do a world class job of testing them and finding even the most subtle bugs.  Coyote goes one step
further providing a type of Actor called a State Machine and the Coyote test engine knows how to fully test your
state machines ensuring every state is covered and every state transition is tested.

### Building blocks of Coyote applications

The Coyote programming models are easy to use, and so with minimal investment on your part you get huge upside
value from the Coyote test tools that automatically find bugs in your code.  You can decide how much to invest in
your use of Coyote so you can find the sweet spot that maximizes your return on that investment.  Coyote provides
the following building blocks for more reliable software:

- [ControlledTask](/coyote/learn/programming-models/async/overview) - a wrapper on .NET Tasks that allows the Coyote
  test engine to take control of scheduling.
- [Actor](/coyote/learn/programming-models/actors/overview),
  [StateMachine](/coyote/learn/programming-models/actors/state-machines) and Event - base classes for the Coyote actor
  programming model
- [Specification](/coyote/learn/specifications/overview), [Monitor](/coyote/learn/specifications/liveness-checking) -
  ways to embed checks into your code that can be verified at test time.  This includes some pretty sophisticated ways
  of monitoring the "liveness" of your code (ensuring that it doesn't get stuck spinning it's wheels).
- [Timers](/coyote/learn/programming-models/actors/timers) - provide a way to model timing activities in your system
which is especially useful in the design of mocks that model external systems including their sources of non-determinism.
- [ControlledRandomValueGenerator](/coyote/learn/core/non-determinism) - allows you to tell Coyote where other sources
of non-determinism are in your code.  For example you can use this to model situations where a request on a backend
service might randomly fail (e.g. out of disk space, or host not found, etc).
- [Logging](/coyote/learn/advanced/logging) - allows you to see your debug messages in context with decisions being
made during a coyote test run, including nice ways to visualize what is happening as shown above.

Other than the above constructs, Coyote allows you to use the full power of the C# programming language. To get the best
test performance Coyote recommends that you mock all the systems outside of your control. This allows the Coyote test
tool to test your code locally on your laptop.  The following example shows a typical test setup, in this case a
shopping cart system with all external services written as Coyote mock actors:

![ecommerce](/coyote/assets/images/ecommerce.svg)

Larger teams can share their Coyote mocks for improved code reuse in testing.  In fact, you can publish your Coyote
mocks as a precise protocol definition of your public services. The coyote tester can then be used to fully certify
that new customer code is working properly with the mock model of your service before they even attempt to use your
production api's.

Coyote mock models are typically much more sophisticated than normal mocks.  They not only specify the asynchronous
api required to talk to a service, but also include sources of non-determinism, specifications that capture required
semantics, and can even include liveness monitoring and custom logging.  Most teams are already building mocks and
testing their code this way, so switching that over to work with Coyote usually requires minimal effort.

### Learn more and contribute

Coyote package is available on [NuGet](https://www.nuget.org/packages/Microsoft.Coyote/) so [getting
started](/coyote/learn/get-started/install) with Coyote is very simple.

Coyote is also available as [open source on github](http://github.com/microsoft/coyote) and is open to all who what to
provide feedback and suggestions.  We would love to see your pull requests if you have specific ideas on how to improve
Coyote.

We hope that you can also benefit from more confident coding of asynchronous systems using Coyote!
