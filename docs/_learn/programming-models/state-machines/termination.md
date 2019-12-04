---
layout: reference
title: State machine termination
section: learn
permalink: /learn/programming-models/state-machines/termination
---

## Explicit termination of a state machine

Coyote state machines continue running unless they are explicitly terminated.
The runtime will mark a state machine as idle if it has no work to do, but it will not
reclaim any resources held by the machine unless it is terminated. A state machine is
terminated when it performs the `Halt` transition, as seen in the following example:

```c#
private class ExampleStateMachine : StateMachine
{
  [Start]
  [OnEventDoAction(typeof(SomeEvent), nameof(HandleSomeEvent))]
  private class Init : State
  {
  }

  private Transition HandleSomeEvent() => this.Halt();

  protected override Task OnHaltAsync(Event e)
  {
    // Do some cleanup on halt.
    return Task.CompletedTask;
  }
}
```

Alternatively, a state machine can halt when it dequeues a special built-in event
called `HaltEvent`. This event can be raised (for terminating itself) or sent to
another state machine to terminate that state machine.
Termination of a state machine due to an unhandled `HaltEvent` event is valid behavior
(the Coyote runtime does not report an error).
An event sent to a halted state machine is simply dropped.
A halted state machine cannot be restarted; it remains halted forever.

From the point of view of formal operational semantics, a halted state machine is fully
receptive and consumes any event that is sent to it. The Coyote runtime implements
this semantics efficiently by cleaning up resources allocated to a halted state machine
and recording that the state machine has halted.
