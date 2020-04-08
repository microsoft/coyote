---
layout: reference
title: Why Actor
section: learn
permalink: /learn/programming-models/actors/why-actors
---

#  How are Coyote Actors different from existing Microsoft Actor frameworks?

As you build highly concurrent systems, designed to defending against race conditions, arbitrary
faults, etc, how do you test such systems? How do you know that you got the code right? These
questions were the motivation behind creating Coyote.

Coyote focuses on writing [specifications](/coyote/learn/core/specifications) and providing
high-coverage testing via [systematic testing](/coyote/learn/core/systematic-testing). The
capability of writing better tests, getting (much) better coverage, accelerates the development
process.

## How is it different from other actor implementations?

Coyote does not have a distributed runtime, so it cannot be a replacement for systems like
[Orleans](https://dotnet.github.io/orleans/), [dapr](https://dapr.io/), [Azure Service
Fabric](https://azure.microsoft.com/en-us/services/service-fabric/), or [Reliable
Actors](https://docs.microsoft.com/en-us/azure/service-fabric/service-fabric-reliable-actors-introduction),
each of which provide distributed hosting goodies such as state persistence, networking,
load-balancing, etc.

Coyote embodies its ideas in programming models such as
[actors](/coyote/learn/programming-models/actors/overview) and
[tasks](/coyote/learn/programming-models/async/overview). Each of these only have in-memory
representations. Think of them in the same way as C# Tasks: they're just programming constructs.

Coyote is complimentary to these other frameworks. Some Azure teams, for instance, use Service
Fabric for hosting their application and Coyote for expressing the logic. There have also been
instances where Reliable Actors were used, and each Reliable actor hosted a Coyote
[StateMachine](/coyote/learn/programming-models/actors/state-machines) inside. (The ReliableActor
would receive messages and use them to drive the state machine.) Coyote testing vets correctness,
and Service Fabric provides all the hosting capabilities.

Coyote has minimal dependencies, so it should be easy to integrate.  There is also a tutorial on
how to use Coyote Actors to implement a [Raft server
cluster](https://microsoft.github.io/coyote/learn/tutorials/raft-azure) on top of Azure Service Bus.

## What scenarios to use Coyote as opposed to other systems?

As mentioned above, using Coyote does not rule out the other systems. Use Coyote when there is
complexity in your design and you're interested in high-coverage testing of your logic (against
concurrency, failures, timers, race conditions, etc.)

Coyote is not just for distributed systems. You can use it for single-box scenarios as well, e.g.,
asynchronous code using Tasks (running on a multi-core machine).
