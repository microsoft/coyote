# ActorRuntimeLogTextFormatter class

This class implements IActorRuntimeLog and generates output in a a human readable text format.

```csharp
public class ActorRuntimeLogTextFormatter : IActorRuntimeLog
```

## Public Members

| name | description |
| --- | --- |
| [ActorRuntimeLogTextFormatter](ActorRuntimeLogTextFormatter/ActorRuntimeLogTextFormatter.md)() | Initializes a new instance of the [`ActorRuntimeLogTextFormatter`](ActorRuntimeLogTextFormatter.md) class. |
| [Logger](ActorRuntimeLogTextFormatter/Logger.md) { get; set; } | Get or set the [`ILogger`](../Microsoft.Coyote.IO/ILogger.md) interface to the logger. |
| virtual [OnAssertionFailure](ActorRuntimeLogTextFormatter/OnAssertionFailure.md)(…) | Invoked when the specified assertion failure has occurred. |
| virtual [OnCompleted](ActorRuntimeLogTextFormatter/OnCompleted.md)() | Invoked when a log is complete (and is about to be closed). |
| virtual [OnCreateActor](ActorRuntimeLogTextFormatter/OnCreateActor.md)(…) | Invoked when the specified actor has been created. |
| virtual [OnCreateMonitor](ActorRuntimeLogTextFormatter/OnCreateMonitor.md)(…) | Invoked when the specified monitor has been created. |
| [OnCreateStateMachine](ActorRuntimeLogTextFormatter/OnCreateStateMachine.md)(…) | Invoked when the specified state machine has been created. |
| virtual [OnCreateTimer](ActorRuntimeLogTextFormatter/OnCreateTimer.md)(…) | Invoked when the specified actor timer has been created. |
| virtual [OnDefaultEventHandler](ActorRuntimeLogTextFormatter/OnDefaultEventHandler.md)(…) | Invoked when the specified actor is idle (there is nothing to dequeue) and the default event handler is about to be executed. |
| virtual [OnDequeueEvent](ActorRuntimeLogTextFormatter/OnDequeueEvent.md)(…) | Invoked when the specified event is dequeued by an actor. |
| virtual [OnEnqueueEvent](ActorRuntimeLogTextFormatter/OnEnqueueEvent.md)(…) | Invoked when the specified event is about to be enqueued to an actor. |
| virtual [OnExceptionHandled](ActorRuntimeLogTextFormatter/OnExceptionHandled.md)(…) | Invoked when the specified OnException method is used to handle a thrown exception. |
| virtual [OnExceptionThrown](ActorRuntimeLogTextFormatter/OnExceptionThrown.md)(…) | Invoked when the specified actor throws an exception. |
| virtual [OnExecuteAction](ActorRuntimeLogTextFormatter/OnExecuteAction.md)(…) | Invoked when the specified actor executes an action. |
| virtual [OnGotoState](ActorRuntimeLogTextFormatter/OnGotoState.md)(…) | Invoked when the specified state machine performs a goto transition to the specified state. |
| virtual [OnHalt](ActorRuntimeLogTextFormatter/OnHalt.md)(…) | Invoked when the specified actor has been halted. |
| virtual [OnHandleRaisedEvent](ActorRuntimeLogTextFormatter/OnHandleRaisedEvent.md)(…) | Invoked when the specified actor handled a raised event. |
| virtual [OnMonitorError](ActorRuntimeLogTextFormatter/OnMonitorError.md)(…) | Invoked when the specified monitor finds an error. |
| virtual [OnMonitorExecuteAction](ActorRuntimeLogTextFormatter/OnMonitorExecuteAction.md)(…) | Invoked when the specified monitor executes an action. |
| virtual [OnMonitorProcessEvent](ActorRuntimeLogTextFormatter/OnMonitorProcessEvent.md)(…) | Invoked when the specified monitor is about to process an event. |
| virtual [OnMonitorRaiseEvent](ActorRuntimeLogTextFormatter/OnMonitorRaiseEvent.md)(…) | Invoked when the specified monitor raised an event. |
| virtual [OnMonitorStateTransition](ActorRuntimeLogTextFormatter/OnMonitorStateTransition.md)(…) | Invoked when the specified monitor enters or exits a state. |
| virtual [OnPopState](ActorRuntimeLogTextFormatter/OnPopState.md)(…) | Invoked when the specified state machine has popped its current state. |
| virtual [OnPopStateUnhandledEvent](ActorRuntimeLogTextFormatter/OnPopStateUnhandledEvent.md)(…) | Invoked when the specified event cannot be handled in the current state, its exit handler is executed and then the state is popped and any previous "current state" is reentered. This handler is called when that pop has been done. |
| virtual [OnPushState](ActorRuntimeLogTextFormatter/OnPushState.md)(…) | Invoked when the specified state machine is being pushed to a state. |
| virtual [OnRaiseEvent](ActorRuntimeLogTextFormatter/OnRaiseEvent.md)(…) | Invoked when the specified state machine raises an event. |
| virtual [OnRandom](ActorRuntimeLogTextFormatter/OnRandom.md)(…) | Invoked when the specified controlled nondeterministic result has been obtained. |
| virtual [OnReceiveEvent](ActorRuntimeLogTextFormatter/OnReceiveEvent.md)(…) | Invoked when the specified event is received by an actor. |
| virtual [OnSendEvent](ActorRuntimeLogTextFormatter/OnSendEvent.md)(…) | Invoked when the specified event is sent to a target actor. |
| virtual [OnStateTransition](ActorRuntimeLogTextFormatter/OnStateTransition.md)(…) | Invoked when the specified state machine enters or exits a state. |
| virtual [OnStopTimer](ActorRuntimeLogTextFormatter/OnStopTimer.md)(…) | Invoked when the specified actor timer has been stopped. |
| virtual [OnStrategyDescription](ActorRuntimeLogTextFormatter/OnStrategyDescription.md)(…) | Invoked to describe the specified scheduling strategy. |
| virtual [OnWaitEvent](ActorRuntimeLogTextFormatter/OnWaitEvent.md)(…) | Invoked when the specified actor waits to receive an event of a specified type. (2 methods) |

## Remarks

See [Logging](/coyote/learn/core/logging) for more information.

## See Also

* interface [IActorRuntimeLog](IActorRuntimeLog.md)
* namespace [Microsoft.Coyote.Actors](../Microsoft.Coyote.ActorsNamespace.md)
* assembly [Microsoft.Coyote](../Microsoft.Coyote.md)

<!-- DO NOT EDIT: generated by xmldocmd for Microsoft.Coyote.dll -->
