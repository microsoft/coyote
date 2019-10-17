---
layout: reference
title: How does Coyote work?
section: learn
permalink: /learn/overview/how
---

## How does it work?

The `Coyote` test tool takes control of all asynchronous branching and sources of non-determinism, and systematically explores every possible asynchronous interleaving in your code until it finds a bug. A developer can fix each bug as they go, or keep running Coyote to find other bugs.

If a bug is found, Coyote reports a reproducible bug trace that provides a global order of all concurrent events and nondeterministic choices in the system. This makes a Coyote bug significantly easier to debug than regular unit-/integration-tests and logs from production or stress tests, which are typically nondeterministic.

The exact mechanism `Coyote` uses to do this depends on the programming model you have chosen. `Coyote` also provides several advanced scheduling strategies that have been found to be very useful in uncovering different types of bugs. New scheduling strategies are being developed in Microsoft Research based on a wealth of experience gathered from the Microsoft product groups that are using `Coyote` today.

[todo: announce the newest cool thing...]
