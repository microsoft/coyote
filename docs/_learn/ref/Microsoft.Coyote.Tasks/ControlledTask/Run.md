---
layout: reference
section: learn
title: Run
permalink: /learn/ref/Microsoft.Coyote.Tasks/ControlledTask/Run
---
# ControlledTask.Run method (1 of 8)

Queues the specified work to run on the thread pool and returns a [`ControlledTask`](../ControlledTaskType) object that represents that work. A cancellation token allows the work to be cancelled.

```csharp
public static ControlledTask Run(Action action)
```

| parameter | description |
| --- | --- |
| action | The work to execute asynchronously. |

## Return Value

Task that represents the work to run asynchronously.

## See Also

* class [ControlledTask](../ControlledTaskType)
* namespace [Microsoft.Coyote.Tasks](../ControlledTaskType)
* assembly [Microsoft.Coyote](../../MicrosoftCoyoteAssembly.md)

---

# ControlledTask.Run method (2 of 8)

Queues the specified work to run on the thread pool and returns a proxy for the [`ControlledTask`](../ControlledTaskType) returned by the function.

```csharp
public static ControlledTask Run(Func<ControlledTask> function)
```

| parameter | description |
| --- | --- |
| function | The work to execute asynchronously. |

## Return Value

Task that represents the work to run asynchronously.

## See Also

* class [ControlledTask](../ControlledTaskType)
* namespace [Microsoft.Coyote.Tasks](../ControlledTaskType)
* assembly [Microsoft.Coyote](../../MicrosoftCoyoteAssembly.md)

---

# ControlledTask.Run method (3 of 8)

Queues the specified work to run on the thread pool and returns a [`ControlledTask`](../ControlledTaskType) object that represents that work.

```csharp
public static ControlledTask Run(Action action, CancellationToken cancellationToken)
```

| parameter | description |
| --- | --- |
| action | The work to execute asynchronously. |
| cancellationToken | Cancellation token that can be used to cancel the work. |

## Return Value

Task that represents the work to run asynchronously.

## See Also

* class [ControlledTask](../ControlledTaskType)
* namespace [Microsoft.Coyote.Tasks](../ControlledTaskType)
* assembly [Microsoft.Coyote](../../MicrosoftCoyoteAssembly.md)

---

# ControlledTask.Run method (4 of 8)

Queues the specified work to run on the thread pool and returns a proxy for the [`ControlledTask`](../ControlledTaskType) returned by the function. A cancellation token allows the work to be cancelled.

```csharp
public static ControlledTask Run(Func<ControlledTask> function, CancellationToken cancellationToken)
```

| parameter | description |
| --- | --- |
| function | The work to execute asynchronously. |
| cancellationToken | Cancellation token that can be used to cancel the work. |

## Return Value

Task that represents the work to run asynchronously.

## See Also

* class [ControlledTask](../ControlledTaskType)
* namespace [Microsoft.Coyote.Tasks](../ControlledTaskType)
* assembly [Microsoft.Coyote](../../MicrosoftCoyoteAssembly.md)

---

# ControlledTask.Run&lt;TResult&gt; method (5 of 8)

Queues the specified work to run on the thread pool and returns a proxy for the [`ControlledTask`](../ControlledTask-1Type) returned by the function.

```csharp
public static ControlledTask<TResult> Run<TResult>(Func<ControlledTask<TResult>> function)
```

| parameter | description |
| --- | --- |
| TResult | The result type of the task. |
| function | The work to execute asynchronously. |

## Return Value

Task that represents the work to run asynchronously.

## See Also

* class [ControlledTask&lt;TResult&gt;](../ControlledTask-1Type)
* class [ControlledTask](../ControlledTaskType)
* namespace [Microsoft.Coyote.Tasks](../ControlledTaskType)
* assembly [Microsoft.Coyote](../../MicrosoftCoyoteAssembly.md)

---

# ControlledTask.Run&lt;TResult&gt; method (6 of 8)

Queues the specified work to run on the thread pool and returns a [`ControlledTask`](../ControlledTaskType) object that represents that work.

```csharp
public static ControlledTask<TResult> Run<TResult>(Func<TResult> function)
```

| parameter | description |
| --- | --- |
| TResult | The result type of the task. |
| function | The work to execute asynchronously. |

## Return Value

Task that represents the work to run asynchronously.

## See Also

* class [ControlledTask&lt;TResult&gt;](../ControlledTask-1Type)
* class [ControlledTask](../ControlledTaskType)
* namespace [Microsoft.Coyote.Tasks](../ControlledTaskType)
* assembly [Microsoft.Coyote](../../MicrosoftCoyoteAssembly.md)

---

# ControlledTask.Run&lt;TResult&gt; method (7 of 8)

Queues the specified work to run on the thread pool and returns a proxy for the [`ControlledTask`](../ControlledTask-1Type) returned by the function. A cancellation token allows the work to be cancelled.

```csharp
public static ControlledTask<TResult> Run<TResult>(Func<ControlledTask<TResult>> function, 
    CancellationToken cancellationToken)
```

| parameter | description |
| --- | --- |
| TResult | The result type of the task. |
| function | The work to execute asynchronously. |
| cancellationToken | Cancellation token that can be used to cancel the work. |

## Return Value

Task that represents the work to run asynchronously.

## See Also

* class [ControlledTask&lt;TResult&gt;](../ControlledTask-1Type)
* class [ControlledTask](../ControlledTaskType)
* namespace [Microsoft.Coyote.Tasks](../ControlledTaskType)
* assembly [Microsoft.Coyote](../../MicrosoftCoyoteAssembly.md)

---

# ControlledTask.Run&lt;TResult&gt; method (8 of 8)

Queues the specified work to run on the thread pool and returns a [`ControlledTask`](../ControlledTaskType) object that represents that work. A cancellation token allows the work to be cancelled.

```csharp
public static ControlledTask<TResult> Run<TResult>(Func<TResult> function, 
    CancellationToken cancellationToken)
```

| parameter | description |
| --- | --- |
| TResult | The result type of the task. |
| function | The work to execute asynchronously. |
| cancellationToken | Cancellation token that can be used to cancel the work. |

## Return Value

Task that represents the work to run asynchronously.

## See Also

* class [ControlledTask&lt;TResult&gt;](../ControlledTask-1Type)
* class [ControlledTask](../ControlledTaskType)
* namespace [Microsoft.Coyote.Tasks](../ControlledTaskType)
* assembly [Microsoft.Coyote](../../MicrosoftCoyoteAssembly.md)

<!-- DO NOT EDIT: generated by xmldocmd for Microsoft.Coyote.dll -->
