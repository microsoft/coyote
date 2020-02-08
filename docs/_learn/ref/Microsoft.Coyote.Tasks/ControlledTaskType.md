---
layout: reference
section: learn
title: ControlledTask
permalink: /learn/ref/Microsoft.Coyote.Tasks/ControlledTaskType
---
# ControlledTask class

Represents an asynchronous operation. Each [`ControlledTask`](ControlledTaskType) is a thin wrapper over Task and each call simply invokes the wrapped task. During testing, a [`ControlledTask`](ControlledTaskType) is controlled by the runtime and systematically interleaved with other asynchronous operations to find bugs.

```csharp
public class ControlledTask : IDisposable
```

## Public Members

| name | description |
| --- | --- |
| static [CompletedTask](ControlledTask/CompletedTask) { get; } | A [`ControlledTask`](ControlledTaskType) that has completed successfully. |
| static [Delay](ControlledTask/Delay)(…) | Creates a [`ControlledTask`](ControlledTaskType) that completes after a time delay. (4 methods) |
| static [FromCanceled](ControlledTask/FromCanceled)(…) | Creates a [`ControlledTask`](ControlledTaskType) that is completed due to cancellation with a specified cancellation token. |
| static [FromException](ControlledTask/FromException)(…) | Creates a [`ControlledTask`](ControlledTaskType) that is completed with a specified exception. |
| static [Run](ControlledTask/Run)(…) | Queues the specified work to run on the thread pool and returns a [`ControlledTask`](ControlledTaskType) object that represents that work. A cancellation token allows the work to be cancelled. (4 methods) |
| static [WhenAll](ControlledTask/WhenAll)(…) | Creates a [`ControlledTask`](ControlledTaskType) that will complete when all tasks in the specified array have completed. (2 methods) |
| [Exception](ControlledTask/Exception) { get; } | Gets the AggregateException that caused the task to end prematurely. If the task completed successfully or has not yet thrown any exceptions, this will return null. |
| [Id](ControlledTask/Id) { get; } | The id of this task. |
| [IsCanceled](ControlledTask/IsCanceled) { get; } | Value that indicates whether the task completed execution due to being canceled. |
| [IsCompleted](ControlledTask/IsCompleted) { get; } | Value that indicates whether the task has completed. |
| [IsFaulted](ControlledTask/IsFaulted) { get; } | Value that indicates whether the task completed due to an unhandled exception. |
| [Status](ControlledTask/Status) { get; } | The status of this task. |
| [ConfigureAwait](ControlledTask/ConfigureAwait)(…) | Configures an awaiter used to await this task. |
| [Dispose](ControlledTask/Dispose)() | Disposes the [`ControlledTask`](ControlledTaskType), releasing all of its unmanaged resources. |
| [GetAwaiter](ControlledTask/GetAwaiter)() | Gets an awaiter for this awaitable. |
| [ToTask](ControlledTask/ToTask)() | Converts the specified [`ControlledTask`](ControlledTaskType) into a Task. |
| [Wait](ControlledTask/Wait)() | Waits for the task to complete execution. |
| [Wait](ControlledTask/Wait)(…) | Waits for the task to complete execution within a specified time interval. (4 methods) |
| static [CurrentId](ControlledTask/CurrentId) { get; } | Returns the id of the currently executing [`ControlledTask`](ControlledTaskType). |
| static [ExploreContextSwitch](ControlledTask/ExploreContextSwitch)() | Injects a context switch point that can be systematically explored during testing. |
| static [FromCanceled&lt;TResult&gt;](ControlledTask/FromCanceled)(…) | Creates a [`ControlledTask`](ControlledTask-1Type) that is completed due to cancellation with a specified cancellation token. |
| static [FromException&lt;TResult&gt;](ControlledTask/FromException)(…) | Creates a [`ControlledTask`](ControlledTask-1Type) that is completed with a specified exception. |
| static [FromResult&lt;TResult&gt;](ControlledTask/FromResult)(…) | Creates a [`ControlledTask`](ControlledTask-1Type) that is completed successfully with the specified result. |
| static [Run&lt;TResult&gt;](ControlledTask/Run)(…) | Queues the specified work to run on the thread pool and returns a proxy for the [`ControlledTask`](ControlledTask-1Type) returned by the function. (4 methods) |
| static [WaitAll](ControlledTask/WaitAll)(…) | Waits for all of the provided [`ControlledTask`](ControlledTaskType) objects to complete execution. (5 methods) |
| static [WaitAny](ControlledTask/WaitAny)(…) | Waits for any of the provided [`ControlledTask`](ControlledTaskType) objects to complete execution. (5 methods) |
| static [WhenAll&lt;TResult&gt;](ControlledTask/WhenAll)(…) | Creates a [`ControlledTask`](ControlledTaskType) that will complete when all tasks in the specified array have completed. (2 methods) |
| static [WhenAny](ControlledTask/WhenAny)(…) | Creates a [`ControlledTask`](ControlledTaskType) that will complete when any task in the specified array have completed. (2 methods) |
| static [WhenAny&lt;TResult&gt;](ControlledTask/WhenAny)(…) | Creates a [`ControlledTask`](ControlledTaskType) that will complete when any task in the specified array have completed. (2 methods) |
| static [WhenAnyTaskCompletesAsync&lt;TResult&gt;](ControlledTask/WhenAnyTaskCompletesAsync)(…) | Creates a [`ControlledTask`](ControlledTaskType) that will complete when any task in the specified enumerable collection have completed. |
| static [WhenAnyTaskCompletesInProductionAsync&lt;TResult&gt;](ControlledTask/WhenAnyTaskCompletesInProductionAsync)(…) | Creates a [`ControlledTask`](ControlledTaskType) that will complete when any task in the specified enumerable collection have completed. |
| static [Yield](ControlledTask/Yield)() | Creates an awaitable that asynchronously yields back to the current context when awaited. |

## Protected Members

| name | description |
| --- | --- |
| virtual [Dispose](ControlledTask/Dispose)(…) | Disposes the [`ControlledTask`](ControlledTaskType), releasing all of its unmanaged resources. |

## See Also

* namespace [Microsoft.Coyote.Tasks](../MicrosoftCoyoteTasksNamespace)
* assembly [Microsoft.Coyote](../MicrosoftCoyoteAssembly.md)

<!-- DO NOT EDIT: generated by xmldocmd for Microsoft.Coyote.dll -->
