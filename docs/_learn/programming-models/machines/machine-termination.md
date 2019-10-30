---
layout: reference
title: State Machine termination
section: learn
permalink: /learn/programming-models/machines/machine-termination
---

## Explicit termination of a machine

Coyote machines continue running unless they are explicitly terminated. The runtime will mark a machine
as idle if it has no work to do, but it will not reclaim any resources held by the machine unless it is
terminated. A machine is terminated when it dequeues a special built-in event called `Halt`.

A `Halt` event can be raised (for terminating self) and/or sent to another machine to terminate that
machine. Termination of a machine due to an unhandled `Halt` event is valid behavior (the Coyote
runtime does not report an error). An event sent to a halted machine is simply dropped. A halted
machine cannot be restarted; it remains halted forever.

From the point of view of formal operational semantics, a halted machine is fully receptive and
consumes any event that is sent to it. The Coyote runtime implements this semantics efficiently by
cleaning up resources allocated to a halted machine and recording that the machine has halted.
