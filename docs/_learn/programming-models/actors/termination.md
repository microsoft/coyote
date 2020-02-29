---
layout: reference
title: Actor termination
section: learn
permalink: /learn/programming-models/actors/termination
---

## Explicit termination of an actor

Coyote actors and state machines continue running unless they are explicitly terminated.
The runtime will mark a actor as idle if it has no work to do, but it will not
reclaim any resources held by the actor unless it is terminated. An actor is
terminated when it performs the `Halt` operation, as seen in the following example:

```c#
private class Example : Actor
{
    private void SomeAction()
    {
        if (this.timeToStop)
        {
            this.Halt();
        }
    }

    protected override Task OnHaltAsync(Event e)
    {
        // Do some cleanup on halt.
        return Task.CompletedTask;
    }
}
```

Additionally, an actor can be halted by another actor by sending a special built-in event
called `HaltEvent`. On state machines this event can also be used for self termination using `RaiseEvent`.
Termination of an actor due to an unhandled `HaltEvent` event is valid behavior
(the Coyote runtime does not report an error).
An event sent to a halted actor is simply dropped.
A halted actor cannot be restarted; it remains halted forever.

From the point of view of formal operational semantics, a halted actor is fully
receptive and consumes any event that is sent to it. The Coyote runtime implements
this semantics efficiently by cleaning up resources allocated to a halted actor
and recording that the actor has halted.
