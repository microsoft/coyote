---
layout: reference
section: learn
title: ActorRuntimeLogGraphBuilder
permalink: /learn/ref/Microsoft.Coyote.Coverage/ActorRuntimeLogGraphBuilderType
---
# ActorRuntimeLogGraphBuilder class

Implements the [`IActorRuntimeLog`](../Microsoft.Coyote.Actors/IActorRuntimeLogType) and builds a directed graph from the recorded events and state transitions.

```csharp
public class ActorRuntimeLogGraphBuilder : IActorRuntimeLog
```

## Public Members

| name | description |
| --- | --- |
| [ActorRuntimeLogGraphBuilder](ActorRuntimeLogGraphBuilder/ActorRuntimeLogGraphBuilder)(…) | Initializes a new instance of the [`ActorRuntimeLogGraphBuilder`](ActorRuntimeLogGraphBuilderType) class. |
| [CollapseMachineInstances](ActorRuntimeLogGraphBuilder/CollapseMachineInstances) { get; set; } | Set this boolean to true to get a collapsed graph showing only machine types, states and events. This will not show machine "instances". |
| [Graph](ActorRuntimeLogGraphBuilder/Graph) { get; } | Get the Graph object built by this logger. |
| [Logger](ActorRuntimeLogGraphBuilder/Logger) { get; set; } | Get or set the underlying logging object. |
| [OnAssertionFailure](ActorRuntimeLogGraphBuilder/OnAssertionFailure)(…) | Invoked when the specified assertion failure has occurred. |
| [OnCompleted](ActorRuntimeLogGraphBuilder/OnCompleted)() | Invoked when a log is complete (and is about to be closed). |
| [OnCreateActor](ActorRuntimeLogGraphBuilder/OnCreateActor)(…) | Invoked when the specified actor has been created. |
| [OnCreateMonitor](ActorRuntimeLogGraphBuilder/OnCreateMonitor)(…) | Invoked when the specified monitor has been created. |
| [OnCreateStateMachine](ActorRuntimeLogGraphBuilder/OnCreateStateMachine)(…) | Invoked when the specified state machine has been created. |
| [OnCreateTimer](ActorRuntimeLogGraphBuilder/OnCreateTimer)(…) | Invoked when the specified actor timer has been created. |
| [OnDefaultEventHandler](ActorRuntimeLogGraphBuilder/OnDefaultEventHandler)(…) | Invoked when the specified actor is idle (there is nothing to dequeue) and the default event handler is about to be executed. |
| [OnDequeueEvent](ActorRuntimeLogGraphBuilder/OnDequeueEvent)(…) | Invoked when the specified event is dequeued by an actor. |
| [OnEnqueueEvent](ActorRuntimeLogGraphBuilder/OnEnqueueEvent)(…) | Invoked when the specified event is about to be enqueued to an actor. |
| [OnExceptionHandled](ActorRuntimeLogGraphBuilder/OnExceptionHandled)(…) | Invoked when the specified OnException method is used to handle a thrown exception. |
| [OnExceptionThrown](ActorRuntimeLogGraphBuilder/OnExceptionThrown)(…) | Invoked when the specified actor throws an exception. |
| [OnExecuteAction](ActorRuntimeLogGraphBuilder/OnExecuteAction)(…) | Invoked when the specified actor executes an action. |
| [OnGotoState](ActorRuntimeLogGraphBuilder/OnGotoState)(…) | Invoked when the specified state machine performs a goto transition to the specified state. |
| [OnHalt](ActorRuntimeLogGraphBuilder/OnHalt)(…) | Invoked when the specified actor has been halted. |
| [OnHandleRaisedEvent](ActorRuntimeLogGraphBuilder/OnHandleRaisedEvent)(…) | Invoked when the specified actor handled a raised event. |
| [OnMonitorError](ActorRuntimeLogGraphBuilder/OnMonitorError)(…) | Invoked when the specified monitor finds an error. |
| [OnMonitorExecuteAction](ActorRuntimeLogGraphBuilder/OnMonitorExecuteAction)(…) | Invoked when the specified monitor executes an action. |
| [OnMonitorProcessEvent](ActorRuntimeLogGraphBuilder/OnMonitorProcessEvent)(…) | Invoked when the specified monitor is about to process an event. |
| [OnMonitorRaiseEvent](ActorRuntimeLogGraphBuilder/OnMonitorRaiseEvent)(…) | Invoked when the specified monitor raised an event. |
| [OnMonitorStateTransition](ActorRuntimeLogGraphBuilder/OnMonitorStateTransition)(…) | Invoked when the specified monitor enters or exits a state. |
| [OnPopState](ActorRuntimeLogGraphBuilder/OnPopState)(…) | Invoked when the specified state machine has popped its current state. |
| [OnPopStateUnhandledEvent](ActorRuntimeLogGraphBuilder/OnPopStateUnhandledEvent)(…) | Invoked when the specified event cannot be handled in the current state, its exit handler is executed and then the state is popped and any previous "current state" is reentered. This handler is called when that pop has been done. |
| [OnPushState](ActorRuntimeLogGraphBuilder/OnPushState)(…) | Invoked when the specified state machine is being pushed to a state. |
| [OnRaiseEvent](ActorRuntimeLogGraphBuilder/OnRaiseEvent)(…) | Invoked when the specified state machine raises an event. |
| [OnRandom](ActorRuntimeLogGraphBuilder/OnRandom)(…) | Invoked when the specified controlled nondeterministic result has been obtained. |
| [OnReceiveEvent](ActorRuntimeLogGraphBuilder/OnReceiveEvent)(…) | Invoked when the specified event is received by an actor. |
| [OnSendEvent](ActorRuntimeLogGraphBuilder/OnSendEvent)(…) | Invoked when the specified event is sent to a target actor. |
| [OnStateTransition](ActorRuntimeLogGraphBuilder/OnStateTransition)(…) | Invoked when the specified state machine enters or exits a state. |
| [OnStopTimer](ActorRuntimeLogGraphBuilder/OnStopTimer)(…) | Invoked when the specified actor timer has been stopped. |
| [OnStrategyDescription](ActorRuntimeLogGraphBuilder/OnStrategyDescription)(…) | Invoked to describe the specified scheduling strategy. |
| [OnWaitEvent](ActorRuntimeLogGraphBuilder/OnWaitEvent)(…) | Invoked when the specified actor waits to receive an event of a specified type. (2 methods) |
| [SnapshotGraph](ActorRuntimeLogGraphBuilder/SnapshotGraph)(…) | Return current graph and reset for next iteration. |

## See Also

* interface [IActorRuntimeLog](../Microsoft.Coyote.Actors/IActorRuntimeLogType)
* namespace [Microsoft.Coyote.Coverage](../MicrosoftCoyoteCoverageNamespace)
* assembly [Microsoft.Coyote](../MicrosoftCoyoteAssembly)

<!-- DO NOT EDIT: generated by xmldocmd for Microsoft.Coyote.dll -->
