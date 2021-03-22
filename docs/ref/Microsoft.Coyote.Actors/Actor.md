# Actor class

Type that implements an actor. Inherit from this class to declare a custom actor.

```csharp
public abstract class Actor
```

## Public Members

| name | description |
| --- | --- |
| [CurrentEventGroup](Actor/CurrentEventGroup.md) { get; set; } | An optional EventGroup associated with the current event being handled. |
| override [Equals](Actor/Equals.md)(…) | Determines whether the specified object is equal to the current object. |
| override [GetHashCode](Actor/GetHashCode.md)() | Returns the hash code for this instance. |
| override [ToString](Actor/ToString.md)() | Returns a string that represents the current actor. |

## Protected Members

| name | description |
| --- | --- |
| [Actor](Actor/Actor.md)() | Initializes a new instance of the [`Actor`](Actor.md) class. |
| virtual [HashedState](Actor/HashedState.md) { get; } | User-defined hashed state of the actor. Override to improve the accuracy of stateful techniques during testing. |
| [Id](Actor/Id.md) { get; } | Unique id that identifies this actor. |
| [Logger](Actor/Logger.md) { get; } | The logger installed to the runtime. |
| [Assert](Actor/Assert.md)(…) | Checks if the assertion holds, and if not, throws an [`AssertionFailureException`](../Microsoft.Coyote.Runtime/AssertionFailureException.md) exception. (5 methods) |
| [CreateActor](Actor/CreateActor.md)(…) | Creates a new actor of the specified type and with the specified optional [`Event`](../Microsoft.Coyote/Event.md). This [`Event`](../Microsoft.Coyote/Event.md) can only be used to access its payload, and cannot be handled. (3 methods) |
| [Monitor](Actor/Monitor.md)(…) | Invokes the specified monitor with the specified event. |
| [Monitor&lt;T&gt;](Actor/Monitor.md)(…) | Invokes the specified monitor with the specified [`Event`](../Microsoft.Coyote/Event.md). |
| virtual [OnEventDeferred](Actor/OnEventDeferred.md)(…) | Callback that is invoked when the actor defers dequeing an event from its inbox. |
| virtual [OnEventDequeuedAsync](Actor/OnEventDequeuedAsync.md)(…) | Asynchronous callback that is invoked when the actor successfully dequeues an event from its inbox. This method is not called when the dequeue happens via a receive statement. |
| virtual [OnEventHandledAsync](Actor/OnEventHandledAsync.md)(…) | Asynchronous callback that is invoked when the actor finishes handling a dequeued event, unless the handler of the dequeued event caused the actor to halt (either normally or due to an exception). The actor will either become idle or dequeue the next event from its inbox. |
| virtual [OnEventIgnored](Actor/OnEventIgnored.md)(…) | Callback that is invoked when the actor ignores an event and removes it from its inbox. |
| virtual [OnEventUnhandledAsync](Actor/OnEventUnhandledAsync.md)(…) | Asynchronous callback that is invoked when the actor receives an event that it is not prepared to handle. The callback is invoked first, after which the actor will necessarily throw an [`UnhandledEventException`](UnhandledEventException.md). |
| virtual [OnException](Actor/OnException.md)(…) | Callback that is invoked when the actor throws an exception. By default, the actor throws the exception causing the runtime to fail. |
| virtual [OnExceptionHandledAsync](Actor/OnExceptionHandledAsync.md)(…) | Asynchronous callback that is invoked when the actor handles an exception. |
| virtual [OnHaltAsync](Actor/OnHaltAsync.md)(…) | Asynchronous callback that is invoked when the actor halts. |
| virtual [OnInitializeAsync](Actor/OnInitializeAsync.md)(…) | Asynchronous callback that is invoked when the actor is initialized with an optional event. |
| [RaiseHaltEvent](Actor/RaiseHaltEvent.md)() | Raises a [`HaltEvent`](HaltEvent.md) to halt the actor at the end of the current action. |
| [RandomBoolean](Actor/RandomBoolean.md)() | Returns a nondeterministic boolean choice, that can be controlled during analysis or testing. |
| [RandomBoolean](Actor/RandomBoolean.md)(…) | Returns a nondeterministic boolean choice, that can be controlled during analysis or testing. The value is used to generate a number in the range [0..maxValue), where 0 triggers true. |
| [RandomInteger](Actor/RandomInteger.md)(…) | Returns a nondeterministic integer, that can be controlled during analysis or testing. The value is used to generate an integer in the range [0..maxValue). |
| [ReceiveEventAsync](Actor/ReceiveEventAsync.md)(…) | Waits to receive an [`Event`](../Microsoft.Coyote/Event.md) of the specified type that satisfies an optional predicate. (3 methods) |
| [SendEvent](Actor/SendEvent.md)(…) | Sends an asynchronous [`Event`](../Microsoft.Coyote/Event.md) to a target. |
| [StartPeriodicTimer](Actor/StartPeriodicTimer.md)(…) | Starts a periodic timer that sends a [`TimerElapsedEvent`](../Microsoft.Coyote.Actors.Timers/TimerElapsedEvent.md) to this actor after the specified due time, and then repeats after each specified period. The timer accepts an optional payload to be used during timeout. The timer can be stopped by invoking the [`StopTimer`](Actor/StopTimer.md) method. |
| [StartTimer](Actor/StartTimer.md)(…) | Starts a timer that sends a [`TimerElapsedEvent`](../Microsoft.Coyote.Actors.Timers/TimerElapsedEvent.md) to this actor after the specified due time. The timer accepts an optional payload to be used during timeout. The timer is automatically disposed after it timeouts. To manually stop and dispose the timer, invoke the [`StopTimer`](Actor/StopTimer.md) method. |
| [StopTimer](Actor/StopTimer.md)(…) | Stops and disposes the specified timer. |
| class [OnEventDoActionAttribute](Actor.OnEventDoActionAttribute.md) | Attribute for declaring which action should be invoked to handle a dequeued event of the specified type. |

## Remarks

See [Programming model: asynchronous actors](/coyote/concepts/actors/overview) for more information.

## See Also

* namespace [Microsoft.Coyote.Actors](../Microsoft.Coyote.ActorsNamespace.md)
* assembly [Microsoft.Coyote](../Microsoft.Coyote.md)

<!-- DO NOT EDIT: generated by xmldocmd for Microsoft.Coyote.dll -->
