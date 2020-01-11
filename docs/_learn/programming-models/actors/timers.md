---
layout: reference
title: Using timers in actors
section: learn
permalink: /learn/programming-models/actors/timers
---

## Using timers in actors

The Coyote actor model has built-in support for timers.  Timers are themselves a type of `Actor` that
send a `TimerElapsedEvent` to the actor that created them upon timeout.  Timers are handy for modeling
a common type of asynchronous behavior in distributed systems, so you can find more bugs in your code
relating to how it deals with the uncertainty of various kinds of timeouts.

Timers support a once only timeout event and periodic timeouts, which can continually send such events
on a user-defined interval until they are stopped.

To make use of timers, you must include the `Microsoft.Coyote.Actors.Timers` namespace.
You can start a non-periodic timer using the function `StartTimer`.

```c#
TimerInfo StartTimer(TimeSpan dueTime, object payload = null)
```

`StartTimer` takes as argument:
1. `TimeSpan dueTime`, which is the amount of time to wait before sending the timeout event.
2. `object payload`, which is an optional payload of the timeout event that will be passed through in the `TimerElapsedEvent` event.

`StartTimer` returns `TimerInfo`, which contains information about the created non-periodic timer. The
non-periodic timer is automatically disposed after it timeouts. You can also pass the `TimerInfo` to
the `StopTimer` method to manually stop and dispose the timer. The timer sends events of type
`TimerElapsedEvent` back to the inbox of the actor which created it. The `TimerElapsedEvent` contains
as payload, the same `TimerInfo` returned during the timer creation (and the `Payload` is a public
field within the `TimerInfo`). If you create multiple timers, then the `TimerInfo` object can be used
to distinguish the sources of the different `TimerElapsedEvent` events.

You can start a periodic timer using the function `StartPeriodicTimer`.

```c#
TimerInfo StartPeriodicTimer(TimeSpan dueTime, TimeSpan period, object payload = null)
```

`StartPeriodicTimer` takes as argument:
1. `TimeSpan dueTime`, which is the amount of time to wait before sending the first timeout event.
2. `TimeSpan period`, which is the time interval between timeout events.
3. `object payload`, which is an optional payload of the timeout event.

This API and timer work similarly to `StartTimer`, however you need to manually stop a periodic timer
using the `StopTimer` method if you want it to stop. Note that when an actor [halts](termination), it automatically stops and disposes all
its periodic and non-periodic timers.

A sample which demonstrates the use of such timers is provided in `https://github.com/microsoft/coyote-samples/tree/master/Timers`
which is exampled in detail below.

First you need to declare on your Actor that it is expecting to receive the `TimerElapsedEvent` so the class is defined like this:

```c#
   [OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
   internal class Client : Actor
   {
```

To kick things off the initialization method starts a non-periodic timer:

```c#
   protected override Task OnInitializeAsync(Event initialEvent)
   {
      this.WriteMessage("<Client> Starting a non-periodic timer named 'Foo'");
      this.StartTimer(TimeSpan.FromSeconds(1), "Foo");
      return base.OnInitializeAsync(initialEvent);
   }
```

Notice here we pass a simple string label as the `payload` for the timer.  The `HandleTimeout` method then receives this initial
timeout and starts and stops a periodic timer as follows:

```c#
   private void HandleTimeout(Event e)
   {
      TimerElapsedEvent te = (TimerElapsedEvent)e;
      string label = te.Info.Payload.ToString();
      this.WriteMessage("<Client> Handling timeout from timer '{0}'", label);

      if (this.Count == 0)
      {
            this.WriteMessage("<Client> Starting a period timer named 'Bar'");
            this.PeriodicTimer = this.StartPeriodicTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), "Bar");
      }

      this.Count++;
      if (this.Count == 3)
      {
            this.WriteMessage("<Client> Stopping the periodic timer");
            this.StopTimer(this.PeriodicTimer);
      }
   }
```
Notice how you can extract the payload from the `Event` by casting to `TimerElapsedEvent`.

The sample code turns on verbose logging so we can also see what is going on inside the Coyote runtime regarding timers,
so the verbose output of this program is as follows:

```
<CreateLog> 'Coyote.Examples.Timers.Client(0)' was created by the runtime.
Press Enter to terminate...
<Client> Starting a non-periodic timer named 'Foo'
<TimerLog> Timer 'a5f60f9f-ebf2-436b-a7fe-71ba0e62e989' (due-time:1000ms) was created by 'Coyote.Examples.Timers.Client(0)'.
<SendLog> The runtime sent event 'Microsoft.Coyote.Actors.Timers.TimerElapsedEvent' to 'Coyote.Examples.Timers.Client(0)'.
<EnqueueLog> 'Coyote.Examples.Timers.Client(0)' enqueued event 'Microsoft.Coyote.Actors.Timers.TimerElapsedEvent'.
<DequeueLog> 'Coyote.Examples.Timers.Client(0)' dequeued event 'Microsoft.Coyote.Actors.Timers.TimerElapsedEvent' in state ''.
<TimerLog> Timer 'a5f60f9f-ebf2-436b-a7fe-71ba0e62e989' was stopped and disposed by 'Coyote.Examples.Timers.Client(0)'.
<ActionLog> 'Coyote.Examples.Timers.Client(0)' invoked action 'HandleTimeout' in state ''.
<Client> Handling timeout from timer 'Foo'
<Client> Starting a period timer named 'Bar'
<TimerLog> Timer '8a3b78f3-b376-409f-9815-6e71f5ae365e' (due-time:1000ms; period :1000ms) was created by 'Coyote.Examples.Timers.Client(0)'.
<SendLog> The runtime sent event 'Microsoft.Coyote.Actors.Timers.TimerElapsedEvent' to 'Coyote.Examples.Timers.Client(0)'.
<EnqueueLog> 'Coyote.Examples.Timers.Client(0)' enqueued event 'Microsoft.Coyote.Actors.Timers.TimerElapsedEvent'.
<DequeueLog> 'Coyote.Examples.Timers.Client(0)' dequeued event 'Microsoft.Coyote.Actors.Timers.TimerElapsedEvent' in state ''.
<ActionLog> 'Coyote.Examples.Timers.Client(0)' invoked action 'HandleTimeout' in state ''.
<Client> Handling timeout from timer 'Bar'
<SendLog> The runtime sent event 'Microsoft.Coyote.Actors.Timers.TimerElapsedEvent' to 'Coyote.Examples.Timers.Client(0)'.
<EnqueueLog> 'Coyote.Examples.Timers.Client(0)' enqueued event 'Microsoft.Coyote.Actors.Timers.TimerElapsedEvent'.
<DequeueLog> 'Coyote.Examples.Timers.Client(0)' dequeued event 'Microsoft.Coyote.Actors.Timers.TimerElapsedEvent' in state ''.
<ActionLog> 'Coyote.Examples.Timers.Client(0)' invoked action 'HandleTimeout' in state ''.
<Client> Handling timeout from timer 'Bar'
<Client> Stopping the periodic timer
<TimerLog> Timer '8a3b78f3-b376-409f-9815-6e71f5ae365e' was stopped and disposed by 'Coyote.Examples.Timers.Client(0)'.
```

Notice all our `<Client>` messages show the `Foo` event and the three `bar` timer events were handled as expected.
