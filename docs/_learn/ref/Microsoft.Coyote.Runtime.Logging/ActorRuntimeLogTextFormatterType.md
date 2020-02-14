---
layout: reference
section: learn
title: ActorRuntimeLogTextFormatter
permalink: /learn/ref/Microsoft.Coyote.Runtime.Logging/ActorRuntimeLogTextFormatterType
---
# ActorRuntimeLogTextFormatter class

This class implements IActorRuntimeLog and generates output in a a human readable text format.

```csharp
public class ActorRuntimeLogTextFormatter : IActorRuntimeLog
```

## Public Members

| name | description |
| --- | --- |
| [ActorRuntimeLogTextFormatter](ActorRuntimeLogTextFormatter/ActorRuntimeLogTextFormatter)() | Initializes a new instance of the [`ActorRuntimeLogTextFormatter`](ActorRuntimeLogTextFormatterType) class. |
| [Logger](ActorRuntimeLogTextFormatter/Logger) { get; set; } | Get or set the TextWriter to write to. |
| virtual [OnAssertionFailure](ActorRuntimeLogTextFormatter/OnAssertionFailure)(…) | Invoked when the specified assertion failure has occurred. |
| virtual [OnCompleted](ActorRuntimeLogTextFormatter/OnCompleted)() | Invoked when a log is complete (and is about to be closed). |
| virtual [OnCreateActor](ActorRuntimeLogTextFormatter/OnCreateActor)(…) | Invoked when the specified actor has been created. |
| virtual [OnCreateMonitor](ActorRuntimeLogTextFormatter/OnCreateMonitor)(…) | Invoked when the specified monitor has been created. |
| virtual [OnCreateTimer](ActorRuntimeLogTextFormatter/OnCreateTimer)(…) | Invoked when the specified actor timer has been created. |
| virtual [OnDefaultEventHandler](ActorRuntimeLogTextFormatter/OnDefaultEventHandler)(…) | Invoked when the specified actor is idle (there is nothing to dequeue) and the default event handler is about to be executed. |
| virtual [OnDequeueEvent](ActorRuntimeLogTextFormatter/OnDequeueEvent)(…) | Invoked when the specified event is dequeued by an actor. |
| virtual [OnEnqueueEvent](ActorRuntimeLogTextFormatter/OnEnqueueEvent)(…) | Invoked when the specified event is about to be enqueued to an actor. |
| virtual [OnExceptionHandled](ActorRuntimeLogTextFormatter/OnExceptionHandled)(…) | Invoked when the specified OnException method is used to handle a thrown exception. |
| virtual [OnExceptionThrown](ActorRuntimeLogTextFormatter/OnExceptionThrown)(…) | Invoked when the specified actor throws an exception. |
| virtual [OnExecuteAction](ActorRuntimeLogTextFormatter/OnExecuteAction)(…) | Invoked when the specified actor executes an action. |
| virtual [OnGotoState](ActorRuntimeLogTextFormatter/OnGotoState)(…) | Invoked when the specified state machine performs a goto transition to the specified state. |
| virtual [OnHalt](ActorRuntimeLogTextFormatter/OnHalt)(…) | Invoked when the specified actor has been halted. |
| virtual [OnHandleRaisedEvent](ActorRuntimeLogTextFormatter/OnHandleRaisedEvent)(…) | Invoked when the specified actor handled a raised event. |
| virtual [OnMonitorExecuteAction](ActorRuntimeLogTextFormatter/OnMonitorExecuteAction)(…) | Invoked when the specified monitor executes an action. |
| virtual [OnMonitorProcessEvent](ActorRuntimeLogTextFormatter/OnMonitorProcessEvent)(…) | Invoked when the specified monitor is about to process an event. |
| virtual [OnMonitorRaiseEvent](ActorRuntimeLogTextFormatter/OnMonitorRaiseEvent)(…) | Invoked when the specified monitor raised an event. |
| virtual [OnMonitorStateTransition](ActorRuntimeLogTextFormatter/OnMonitorStateTransition)(…) | Invoked when the specified monitor enters or exits a state. |
| virtual [OnPopState](ActorRuntimeLogTextFormatter/OnPopState)(…) | Invoked when the specified state machine has popped its current state. |
| virtual [OnPopUnhandledEvent](ActorRuntimeLogTextFormatter/OnPopUnhandledEvent)(…) | Invoked when the specified event cannot be handled in the current state, its exit handler is executed and then the state is popped and any previous "current state" is reentered. This handler is called when that pop has been done. |
| virtual [OnPushState](ActorRuntimeLogTextFormatter/OnPushState)(…) | Invoked when the specified state machine is being pushed to a state. |
| virtual [OnRaiseEvent](ActorRuntimeLogTextFormatter/OnRaiseEvent)(…) | Invoked when the specified state machine raises an event. |
| virtual [OnRandom](ActorRuntimeLogTextFormatter/OnRandom)(…) | Invoked when the specified random result has been obtained. |
| virtual [OnReceiveEvent](ActorRuntimeLogTextFormatter/OnReceiveEvent)(…) | Invoked when the specified event is received by an actor. |
| virtual [OnSendEvent](ActorRuntimeLogTextFormatter/OnSendEvent)(…) | Invoked when the specified event is sent to a target actor. |
| virtual [OnStateTransition](ActorRuntimeLogTextFormatter/OnStateTransition)(…) | Invoked when the specified state machine enters or exits a state. |
| virtual [OnStopTimer](ActorRuntimeLogTextFormatter/OnStopTimer)(…) | Invoked when the specified actor timer has been stopped. |
| virtual [OnStrategyDescription](ActorRuntimeLogTextFormatter/OnStrategyDescription)(…) | Invoked to describe the specified scheduling strategy. |
| virtual [OnWaitEvent](ActorRuntimeLogTextFormatter/OnWaitEvent)(…) | Invoked when the specified actor waits to receive an event of a specified type. (2 methods) |

## Remarks

See [Logging](/coyote/learn/advanced/logging) for more information.

## See Also

* interface [IActorRuntimeLog](../Microsoft.Coyote.Runtime/IActorRuntimeLogType)
* namespace [Microsoft.Coyote.Runtime.Logging](../MicrosoftCoyoteRuntimeLoggingNamespace)
* assembly [Microsoft.Coyote](../MicrosoftCoyoteAssembly)

<!-- DO NOT EDIT: generated by xmldocmd for Microsoft.Coyote.dll -->
