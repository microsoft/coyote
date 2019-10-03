---
layout: reference
title: Machine termination
section: learn
permalink: /learn/programming-models/machines/machine-termination
---

## Explicit termination of a machine

In order to terminate a Coyote machine explicitly, it must dequeue a special event named `Halt`, which is built-in Coyote.

A `Halt` event can be raised and/or sent to another machine. Termination of a machine due to an unhandled `halt` event is valid behavior (the Coyote runtime does not report an error). From the point of view of formal operational semantics, a halted machine is fully receptive and consumes any event that is sent to it. The Coyote runtime implements this semantics efficiently by cleaning up resources allocated to a halted machine and recording that the machine has halted.

An event sent to a halted machine is simply dropped.

A halted machine cannot be restarted; it remains halted forever.
