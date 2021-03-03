
## Using timers in actors

The Coyote actor programming model has built-in support for timers. Timers are themselves a type of `Actor` that
send a `TimerElapsedEvent` upon timeout to the actor that created the timer. Timers are handy for
modeling a common type of asynchronous behavior in distributed systems, so you can find more bugs in
your code relating to how it deals with the uncertainty of various kinds of timeouts.

Timers provide a once only timeout event or a periodic timeout, which can continually send such
events on a user-defined interval until they are stopped.

To make use of timers, you must include the `Microsoft.Coyote.Actors.Timers` namespace. You can
start a non-periodic timer using the function `StartTimer`.

```csharp
TimerInfo StartTimer(TimeSpan startDelay, TimerElapsedEvent customEvent)
```

`StartTimer` takes as argument:
1. `TimeSpan startDelay`, which is the amount of time to wait before sending the timeout event.
2. `TimerElapsedEvent customEvent`, an optional custom event (of a user-specified subclass of
   `TimerElapsedEvent`) to raise instead of the default `TimerElapsedEvent`.

`StartTimer` returns `TimerInfo`, which contains information about the created non-periodic timer.
The non-periodic timer is automatically disposed after it timeouts. You can also pass the
`TimerInfo` to the `StopTimer` method to manually stop and dispose the timer. The timer enqueues
events of type `TimerElapsedEvent` (or of a user-specified subclass of `TimerElapsedEvent`) in the
inbox of the actor that created it. The `TimerElapsedEvent` contains as payload, the same
`TimerInfo` returned during the timer creation. If you create multiple timers, then the `TimerInfo`
object can be used to distinguish the sources of the different `TimerElapsedEvent` events.

You can start a periodic timer using the function `StartPeriodicTimer`.

```csharp
TimerInfo StartPeriodicTimer(TimeSpan startDelay, TimeSpan period, TimerElapsedEvent customEvent)
```

`StartPeriodicTimer` takes as argument:
1. `TimeSpan startDelay`, which is the amount of time to wait before sending the first timeout
   event.
2. `TimeSpan period`, which is the time interval between timeout events.
3. `TimerElapsedEvent customEvent`, an optional custom event (of a user-specified subclass of
   `TimerElapsedEvent`) to raise instead of the default `TimerElapsedEvent`.

Periodic timers work similarly to normal timers, however you need to manually stop a periodic timer
using the `StopTimer` method if you want it to stop sending `TimerElapsedEvent` events. Note that
when an actor [halts](termination.md), it automatically stops and disposes all its periodic and
non-periodic timers.

A sample which demonstrates the use of such timers is provided in the [Timers
Sample](https://github.com/microsoft/coyote-samples/tree/main/Timers) on github, which is
explained in detail below.

First you need to declare on your Actor that it is expecting to receive the `TimerElapsedEvent` so
the class is defined like this:

```csharp
[OnEventDoAction(typeof(TimerElapsedEvent), nameof(HandleTimeout))]
internal class Client : Actor
{
    ...
}
```

To kick things off the initialization method starts a non-periodic timer:

```csharp
protected override Task OnInitializeAsync(Event initialEvent)
{
    Console.WriteLine("<Client> Starting a non-periodic timer");
    this.StartTimer(TimeSpan.FromSeconds(1));
    return base.OnInitializeAsync(initialEvent);
}
```

The `HandleTimeout` method then receives this timeout and starts a periodic timer as follows:

```csharp
private void HandleTimeout(Event e)
{
    TimerElapsedEvent te = (TimerElapsedEvent)e;

    this.WriteMessage("<Client> Handling timeout from timer");

    this.WriteMessage("<Client> Starting a period timer");
    this.PeriodicTimer = this.StartPeriodicTimer(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), new CustomTimerEvent());
}
```

In this case we use a `CustomTimerEvent` instead of the default `TimerElapsedEvent`, this custom event is defined as follows:

```csharp
internal class CustomTimerEvent : TimerElapsedEvent
{
    /// <summary>
    /// Count of timeout events processed.
    /// </summary>
    internal int Count;
}
```

This custom event makes it possible to route the period timeouts to a different handler using this on the actor class:

```csharp
[OnEventDoAction(typeof(CustomTimerEvent), nameof(HandlePeriodicTimeout))]
```

In the HandlePeriodicTimeout method we count the number of timeouts and stop when we reach 3:

```csharp
private void HandlePeriodicTimeout(Event e)
{
    this.WriteMessage("<Client> Handling timeout from periodic timer");
    if (e is CustomTimerEvent ce)
    {
        ce.Count++;
        if (ce.Count == 3)
        {
            this.WriteMessage("<Client> Stopping the periodic timer");
            this.StopTimer(this.PeriodicTimer);
        }
    }
}
```

Notice how we can cast the `Event` into our custom `CustomTimerEvent`, and we get the same instance
of `CustomTimerEvent` on each period call, so this way the `CustomTimerEvent` can contain useful
state, in this case the `Count`.

The output of this program is as follows:

```xml
<Client> Starting a non-periodic timer
<Client> Handling timeout from timer
<Client> Starting a period timer
<Client> Handling timeout from periodic timer
<Client> Handling timeout from periodic timer
<Client> Handling timeout from periodic timer
<Client> Stopping the periodic timer
```

## Implementation notes

Note that timers are implemented differently depending on what mode your program is running in.
Normal "production" mode execution uses an optimized timer built on `System.Threading.Timer`.

Timers that run during systematic testing, however, are quite different and are implemented in a
`MockTimerActor` class.  This mock actually removes all concept of real time intervals, and replaces
that with a random decision to timeout or not to timeout.  This makes your test run much faster. But
it can also make your test see a lot more timeout events that you may be expecting.  This is good
for finding bugs, but can overwhelm the test with timeout events.  To reduce the number of timeouts
there is a `--timeout-delay` command line option on the `coyote test` tool.  This timeout delay is
given to the mock timer and it will only fire a timeout when `RandomInteger(delay) == 0`.  The
default value of `--timeout-delay` is 10, which means timeouts only fire once every 10 times the
timer gets scheduled by the systematic testing runtime.  This turns out to be a pretty good default
that stops your test from getting too overwhelmed by timeout events, but if you still see too many
timeout events in your test logs, then you can increase this number.