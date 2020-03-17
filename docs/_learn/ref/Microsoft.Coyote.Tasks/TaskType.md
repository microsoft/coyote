---
layout: reference
section: learn
title: Task
permalink: /learn/ref/Microsoft.Coyote.Tasks/TaskType
---
# Task class

Represents an asynchronous operation. Each [`Task`](TaskType) is a thin wrapper over Task and each call simply invokes the wrapped task. During testing, a [`Task`](TaskType) is controlled by the runtime and systematically interleaved with other asynchronous operations to find bugs.

```csharp
public class Task : IDisposable
```

## Public Members

| name | description |
| --- | --- |
| static [CompletedTask](Task/CompletedTask) { get; } | A [`Task`](TaskType) that has completed successfully. |
| static [Delay](Task/Delay)(…) | Creates a [`Task`](TaskType) that completes after a time delay. (4 methods) |
| static [FromCanceled](Task/FromCanceled)(…) | Creates a [`Task`](TaskType) that is completed due to cancellation with a specified cancellation token. |
| static [FromException](Task/FromException)(…) | Creates a [`Task`](TaskType) that is completed with a specified exception. |
| static [Run](Task/Run)(…) | Queues the specified work to run on the thread pool and returns a [`Task`](TaskType) object that represents that work. A cancellation token allows the work to be cancelled. (4 methods) |
| static [WhenAll](Task/WhenAll)(…) | Creates a [`Task`](TaskType) that will complete when all tasks in the specified array have completed. (2 methods) |
| [Exception](Task/Exception) { get; } | Gets the AggregateException that caused the task to end prematurely. If the task completed successfully or has not yet thrown any exceptions, this will return null. |
| [Id](Task/Id) { get; } | The id of this task. |
| [IsCanceled](Task/IsCanceled) { get; } | Value that indicates whether the task completed execution due to being canceled. |
| [IsCompleted](Task/IsCompleted) { get; } | Value that indicates whether the task has completed. |
| [IsFaulted](Task/IsFaulted) { get; } | Value that indicates whether the task completed due to an unhandled exception. |
| [Status](Task/Status) { get; } | The status of this task. |
| [UncontrolledTask](Task/UncontrolledTask) { get; } | The uncontrolled Task that is wrapped inside this controlled [`Task`](TaskType). |
| [ConfigureAwait](Task/ConfigureAwait)(…) | Configures an awaiter used to await this task. |
| [Dispose](Task/Dispose)() | Disposes the [`Task`](TaskType), releasing all of its unmanaged resources. |
| [GetAwaiter](Task/GetAwaiter)() | Gets an awaiter for this awaitable. |
| [Wait](Task/Wait)() | Waits for the task to complete execution. |
| [Wait](Task/Wait)(…) | Waits for the task to complete execution within a specified time interval. (4 methods) |
| static [CurrentId](Task/CurrentId) { get; } | Returns the id of the currently executing [`Task`](TaskType). |
| static [ExploreContextSwitch](Task/ExploreContextSwitch)() | Injects a context switch point that can be systematically explored during testing. |
| static [FromCanceled&lt;TResult&gt;](Task/FromCanceled)(…) | Creates a [`Task`](Task-1Type) that is completed due to cancellation with a specified cancellation token. |
| static [FromException&lt;TResult&gt;](Task/FromException)(…) | Creates a [`Task`](Task-1Type) that is completed with a specified exception. |
| static [FromResult&lt;TResult&gt;](Task/FromResult)(…) | Creates a [`Task`](Task-1Type) that is completed successfully with the specified result. |
| static [Run&lt;TResult&gt;](Task/Run)(…) | Queues the specified work to run on the thread pool and returns a proxy for the [`Task`](Task-1Type) returned by the function. (4 methods) |
| static [WaitAll](Task/WaitAll)(…) | Waits for all of the provided [`Task`](TaskType) objects to complete execution. (5 methods) |
| static [WaitAny](Task/WaitAny)(…) | Waits for any of the provided [`Task`](TaskType) objects to complete execution. (5 methods) |
| static [WhenAll&lt;TResult&gt;](Task/WhenAll)(…) | Creates a [`Task`](TaskType) that will complete when all tasks in the specified array have completed. (2 methods) |
| static [WhenAny](Task/WhenAny)(…) | Creates a [`Task`](TaskType) that will complete when any task in the specified array have completed. (2 methods) |
| static [WhenAny&lt;TResult&gt;](Task/WhenAny)(…) | Creates a [`Task`](TaskType) that will complete when any task in the specified array have completed. (2 methods) |
| static [WhenAnyTaskCompletesAsync&lt;TResult&gt;](Task/WhenAnyTaskCompletesAsync)(…) | Creates a [`Task`](TaskType) that will complete when any task in the specified enumerable collection have completed. |
| static [WhenAnyTaskCompletesInProductionAsync&lt;TResult&gt;](Task/WhenAnyTaskCompletesInProductionAsync)(…) | Creates a [`Task`](TaskType) that will complete when any task in the specified enumerable collection have completed. |
| static [Yield](Task/Yield)() | Creates an awaitable that asynchronously yields back to the current context when awaited. |

## Protected Members

| name | description |
| --- | --- |
| virtual [Dispose](Task/Dispose)(…) | Disposes the [`Task`](TaskType), releasing all of its unmanaged resources. |

## Remarks

See [Programming model: asynchronous tasks](/coyote/learn/programming-models/async/overview) for more information.

## See Also

* namespace [Microsoft.Coyote.Tasks](../MicrosoftCoyoteTasksNamespace)
* assembly [Microsoft.Coyote](../MicrosoftCoyoteAssembly)

<!-- DO NOT EDIT: generated by xmldocmd for Microsoft.Coyote.dll -->
