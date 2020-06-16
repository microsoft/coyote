---
layout: reference
section: learn
title: Actor
permalink: /learn/ref/Microsoft.Coyote.Actors/ActorType
---
# Actor class

Type that implements an actor. Inherit from this class to declare a custom actor.

```csharp
public abstract class Actor
```

## Public Members

| name | description |
| --- | --- |
| [CurrentEventGroup](Actor/CurrentEventGroup) { get; set; } | An optional operation associated with the current event being handled. An actor that handles an event can choose to complete the operation with a result object. Typically the operation will be an [`AwaitableEventGroup`](AwaitableEventGroup-1Type) and the target actor will know what type the result is. |
| override [Equals](Actor/Equals)(…) | Determines whether the specified object is equal to the current object. |
| override [GetHashCode](Actor/GetHashCode)() | Returns the hash code for this instance. |
| override [ToString](Actor/ToString)() | Returns a string that represents the current actor. |

## Protected Members

| name | description |
| --- | --- |
| [Actor](Actor/Actor)() | Initializes a new instance of the [`Actor`](ActorType) class. |
| virtual [HashedState](Actor/HashedState) { get; } | User-defined hashed state of the actor. Override to improve the accuracy of stateful techniques during testing. |
| [Id](Actor/Id) { get; } | Unique id that identifies this actor. |
| [Logger](Actor/Logger) { get; } | The installed runtime logger. |
| [Assert](Actor/Assert)(…) | Checks if the assertion holds, and if not, throws an AssertionFailureException exception. (5 methods) |
| [CreateActor](Actor/CreateActor)(…) | Creates a new actor of the specified type and with the specified optional [`Event`](../Microsoft.Coyote/EventType). This [`Event`](../Microsoft.Coyote/EventType) can only be used to access its payload, and cannot be handled. (3 methods) |
| [Monitor](Actor/Monitor)(…) | Invokes the specified monitor with the specified event. |
| [Monitor&lt;T&gt;](Actor/Monitor)(…) | Invokes the specified monitor with the specified [`Event`](../Microsoft.Coyote/EventType). |
| virtual [OnEventDequeuedAsync](Actor/OnEventDequeuedAsync)(…) | Asynchronous callback that is invoked when the actor successfully dequeues an event from its inbox. This method is not called when the dequeue happens via a receive statement. |
| virtual [OnEventHandledAsync](Actor/OnEventHandledAsync)(…) | Asynchronous callback that is invoked when the actor finishes handling a dequeued event, unless the handler of the dequeued event caused the actor to halt (either normally or due to an exception). The actor will either become idle or dequeue the next event from its inbox. |
| virtual [OnEventUnhandledAsync](Actor/OnEventUnhandledAsync)(…) | Asynchronous callback that is invoked when the actor receives an event that it is not prepared to handle. The callback is invoked first, after which the actor will necessarily throw an [`UnhandledEventException`](UnhandledEventExceptionType). |
| virtual [OnException](Actor/OnException)(…) | User callback when the actor throws an exception. By default, the actor throws the exception causing the runtime to fail. |
| virtual [OnExceptionHandledAsync](Actor/OnExceptionHandledAsync)(…) | Asynchronous callback that is invoked when the actor handles an exception. |
| virtual [OnHaltAsync](Actor/OnHaltAsync)(…) | Asynchronous callback that is invoked when the actor halts. |
| virtual [OnInitializeAsync](Actor/OnInitializeAsync)(…) | Asynchronous callback that is invoked when the actor is initialized with an optional event. |
| virtual [RaiseHaltEvent](Actor/RaiseHaltEvent)() | Raises a [`HaltEvent`](HaltEventType) to halt the actor at the end of the current action. |
| [RandomBoolean](Actor/RandomBoolean)() | Returns a nondeterministic boolean choice, that can be controlled during analysis or testing. |
| [RandomBoolean](Actor/RandomBoolean)(…) | Returns a nondeterministic boolean choice, that can be controlled during analysis or testing. The value is used to generate a number in the range [0..maxValue), where 0 triggers true. |
| [RandomInteger](Actor/RandomInteger)(…) | Returns a nondeterministic integer, that can be controlled during analysis or testing. The value is used to generate an integer in the range [0..maxValue). |
| [ReceiveEventAsync](Actor/ReceiveEventAsync)(…) | Waits to receive an [`Event`](../Microsoft.Coyote/EventType) of the specified type that satisfies an optional predicate. (3 methods) |
| [SendEvent](Actor/SendEvent)(…) | Sends an asynchronous [`Event`](../Microsoft.Coyote/EventType) to a target. |
| [StartPeriodicTimer](Actor/StartPeriodicTimer)(…) | Starts a periodic timer that sends a [`TimerElapsedEvent`](../Microsoft.Coyote.Actors.Timers/TimerElapsedEventType) to this actor after the specified due time, and then repeats after each specified period. The timer accepts an optional payload to be used during timeout. The timer can be stopped by invoking the [`StopTimer`](Actor/StopTimer) method. |
| [StartTimer](Actor/StartTimer)(…) | Starts a timer that sends a [`TimerElapsedEvent`](../Microsoft.Coyote.Actors.Timers/TimerElapsedEventType) to this actor after the specified due time. The timer accepts an optional payload to be used during timeout. The timer is automatically disposed after it timeouts. To manually stop and dispose the timer, invoke the [`StopTimer`](Actor/StopTimer) method. |
| [StopTimer](Actor/StopTimer)(…) | Stops and disposes the specified timer. |
| class [OnEventDoActionAttribute](ActorOnEventDoActionAttributeType) | Attribute for declaring which action should be invoked to handle a dequeued event of the specified type. |

## Remarks

See [Programming model: asynchronous actors](/coyote/learn/programming-models/actors/overview) for more information.

## See Also

* namespace [Microsoft.Coyote.Actors](../MicrosoftCoyoteActorsNamespace)
* assembly [Microsoft.Coyote](../MicrosoftCoyoteAssembly)

<!-- DO NOT EDIT: generated by xmldocmd for Microsoft.Coyote.dll -->
