---
layout: reference
title: What is Coyote?
section: learn
permalink: /learn/overview/what-is-coyote
---

# What is Coyote?
Coyote is a framework for rapid development of reliable asynchronous software. A growing project that welcomes new contribution, Coyote is currently used by several teams in Azure to design, implement, and automatically test production distributed systems and services.

## Features
The Coyote framework provides:
- An actor-based programming model for building event-driven asynchronous applications. The unit of concurrency in Coyote is an asynchronous communicating state-machine, which is basically an actor that can create new machines, send, and receive events, and transition to different states. Using Coyote machines, you can express your design and code at a higher level that is a natural fit for many cloud services.
- An efficient, lightweight runtime that is built on top of the Task Parallel Library (TPL). This runtime can be used to deploy a Coyote program in production. The Coyote runtime is very flexible and can work with any communication and storage layer.
- The capability to easily write safety and liveness specifications (similar to TLA+) programmatically in C#.
- A systematic testing engine that can control the Coyote program schedule, as well as all declared sources of nondeterminism (e.g. failures and timeouts), and systematically explore the actual executable code to discover bugs (e.g. crashes or specification violations). If a bug is found, the Coyote testing engine will report a deterministic reproducible trace that can be replayed using the Visual Studio Debugger.
