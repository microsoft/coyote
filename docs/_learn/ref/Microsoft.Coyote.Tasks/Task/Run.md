---
layout: reference
section: learn
title: Run
permalink: /learn/ref/Microsoft.Coyote.Tasks/Task/Run
---
# Task.Run method (1 of 8)

Queues the specified work to run on the thread pool and returns a [`Task`](../TaskType) object that represents that work. A cancellation token allows the work to be cancelled.

```csharp
public static Task Run(Action action)
```

| parameter | description |
| --- | --- |
| action | The work to execute asynchronously. |

## Return Value

Task that represents the work to run asynchronously.

## See Also

* class [Task](../TaskType)
* namespace [Microsoft.Coyote.Tasks](../TaskType)
* assembly [Microsoft.Coyote](../../MicrosoftCoyoteAssembly)

---

# Task.Run method (2 of 8)

Queues the specified work to run on the thread pool and returns a proxy for the [`Task`](../TaskType) returned by the function.

```csharp
public static Task Run(Func<Task> function)
```

| parameter | description |
| --- | --- |
| function | The work to execute asynchronously. |

## Return Value

Task that represents the work to run asynchronously.

## See Also

* class [Task](../TaskType)
* namespace [Microsoft.Coyote.Tasks](../TaskType)
* assembly [Microsoft.Coyote](../../MicrosoftCoyoteAssembly)

---

# Task.Run method (3 of 8)

Queues the specified work to run on the thread pool and returns a [`Task`](../TaskType) object that represents that work.

```csharp
public static Task Run(Action action, CancellationToken cancellationToken)
```

| parameter | description |
| --- | --- |
| action | The work to execute asynchronously. |
| cancellationToken | Cancellation token that can be used to cancel the work. |

## Return Value

Task that represents the work to run asynchronously.

## See Also

* class [Task](../TaskType)
* namespace [Microsoft.Coyote.Tasks](../TaskType)
* assembly [Microsoft.Coyote](../../MicrosoftCoyoteAssembly)

---

# Task.Run method (4 of 8)

Queues the specified work to run on the thread pool and returns a proxy for the [`Task`](../TaskType) returned by the function. A cancellation token allows the work to be cancelled.

```csharp
public static Task Run(Func<Task> function, CancellationToken cancellationToken)
```

| parameter | description |
| --- | --- |
| function | The work to execute asynchronously. |
| cancellationToken | Cancellation token that can be used to cancel the work. |

## Return Value

Task that represents the work to run asynchronously.

## See Also

* class [Task](../TaskType)
* namespace [Microsoft.Coyote.Tasks](../TaskType)
* assembly [Microsoft.Coyote](../../MicrosoftCoyoteAssembly)

---

# Task.Run&lt;TResult&gt; method (5 of 8)

Queues the specified work to run on the thread pool and returns a proxy for the [`Task`](../Task-1Type) returned by the function.

```csharp
public static Task<TResult> Run<TResult>(Func<Task<TResult>> function)
```

| parameter | description |
| --- | --- |
| TResult | The result type of the task. |
| function | The work to execute asynchronously. |

## Return Value

Task that represents the work to run asynchronously.

## See Also

* class [Task&lt;TResult&gt;](../Task-1Type)
* class [Task](../TaskType)
* namespace [Microsoft.Coyote.Tasks](../TaskType)
* assembly [Microsoft.Coyote](../../MicrosoftCoyoteAssembly)

---

# Task.Run&lt;TResult&gt; method (6 of 8)

Queues the specified work to run on the thread pool and returns a [`Task`](../TaskType) object that represents that work.

```csharp
public static Task<TResult> Run<TResult>(Func<TResult> function)
```

| parameter | description |
| --- | --- |
| TResult | The result type of the task. |
| function | The work to execute asynchronously. |

## Return Value

Task that represents the work to run asynchronously.

## See Also

* class [Task&lt;TResult&gt;](../Task-1Type)
* class [Task](../TaskType)
* namespace [Microsoft.Coyote.Tasks](../TaskType)
* assembly [Microsoft.Coyote](../../MicrosoftCoyoteAssembly)

---

# Task.Run&lt;TResult&gt; method (7 of 8)

Queues the specified work to run on the thread pool and returns a proxy for the [`Task`](../Task-1Type) returned by the function. A cancellation token allows the work to be cancelled.

```csharp
public static Task<TResult> Run<TResult>(Func<Task<TResult>> function, 
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

* class [Task&lt;TResult&gt;](../Task-1Type)
* class [Task](../TaskType)
* namespace [Microsoft.Coyote.Tasks](../TaskType)
* assembly [Microsoft.Coyote](../../MicrosoftCoyoteAssembly)

---

# Task.Run&lt;TResult&gt; method (8 of 8)

Queues the specified work to run on the thread pool and returns a [`Task`](../TaskType) object that represents that work. A cancellation token allows the work to be cancelled.

```csharp
public static Task<TResult> Run<TResult>(Func<TResult> function, 
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

* class [Task&lt;TResult&gt;](../Task-1Type)
* class [Task](../TaskType)
* namespace [Microsoft.Coyote.Tasks](../TaskType)
* assembly [Microsoft.Coyote](../../MicrosoftCoyoteAssembly)

<!-- DO NOT EDIT: generated by xmldocmd for Microsoft.Coyote.dll -->
