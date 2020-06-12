---
layout: reference
title: Tracking operations
section: learn
permalink: /learn/programming-models/actors/tracking-operations
---

## Tracking operations

For some applications, it is useful to know which actor is processing an event derived from some
user request. Coyote offers the notion of an _Operation_ that can be tracked automatically. The
following `IActorRuntime` APIs take an optional `Operation` parameter.

```c#
ActorId CreateActor(Type type, Event e = null, Operation op = null);
void SendEvent(ActorId target, Event e, Operation op = null);
```

When you pass a non-null Operation the runtime takes care of propagating it to any subsequent
actors that might be created as a result and across any `SendEvent` calls to any other actors. So all
the work performed as a result of these actors and events can be grouped into a logical operation.

The `Actor` class has a field that returns the current operation in progress:

```c#
Operation CurrentOperation { get; set; }
```

Additionally you may use the following `IActorRuntime` API to get the current Operations of any
actor.

```c#
Operation GetCurrentOperation(ActorId actorId);
```

The `CurrentOperation` is automatically passed along whenever you use `CreateActor` or `SendEvent`
as shown below:

<div class="embed-responsive embed-responsive-16by9">
{% include OperationGroups.svg %}
</div>

### Operation

The base `Operation` class contains the following:

```c#
public Guid Id { get; internal set; }
public string Name { get; internal set; }
public bool IsCompleted { get; set; }
```

The `Guid` is automatically assigned to `Guid.NewGuid`.  The `Name` defaults to null but you
can provide any friendly name you want there.  `IsCompleted` can be set by your target actors.

### Operation<<T>>

As a convenience the following typed operation is also provided:

```c#
public class Operation<T> : Operation
{
    public TaskCompletionSource<T> Completion { get; }
    public virtual void SetResult(T result);
}
```

You can pass this to your actors so that one of those actors can decide at some point to call
`SetResult`. Then you can "wait" on the `TaskCompletionSource`. In this way you can asynchronously
return a result from your system of actors.  `SetResult` also sets `IsCompleted` to true.

### QuiescentOperation

A special operation called `QuiescentOperation` is completed by the `ActorRuntime` when the actor
reaches a quiescent state, meaning the inbox cannot process any events, either it is empty or
everything in it is deferred.

```c#
public class QuiescentOperation : Operation<bool>
{
}
```

Notice that this is a subclass of `Operation<bool>` which means it has a
`TaskCompletionSource<bool>` that you can wait on. The wait will complete when the first actor you
sent this to reaches a quiescent state.

**Note :** Quiescence is never reached on a `StateMachine` that has a `DefaultEvent` handler. In
that case the default event handler can be considered a type of quiescence notification.

### Custom Operations

The Operation class is unsealed so you can create any custom class that you need. The following is
an example that counts a certain number of steps before completing the boolean operation:

```c#
public class OperationCounter : Operation<bool>
{
    public int ExpectedCount;

    public OperationCounter(int expected)
    {
        this.ExpectedCount = expected;
    }

    public override void SetResult(bool result)
    {
        var count = Interlocked.Decrement(ref this.ExpectedCount);
        if (count == 0)
        {
            base.SetResult(result);
        }
    }
}
```

This way you can have multiple actors calling `SetResult` and the outer code that is waiting is not
released until the expected count is reached.

Similarly you can create an `Operation` that gathers multiple results from various actors like this:

```c#
public class OperationList : Operation<bool>
{
    public List<string> Items = new List<string>();

    public void AddItem(string msg)
    {
        this.Items.Add(msg);
    }

    public override string ToString()
    {
        return string.Join(", ", this.Items);
    }
}
```

When the final actor calls `SetResult(true)` the list of gathered items are then available to the
caller.

### Clearing the CurrentOperation

You might need to clear the current operation at some point in your `Actor`. To do this you can
simply set the property to null:

```c#
this.CurrentOperation = null;
```

However, this could be overridden by any subsequent event that is dequeued from the event queue
because the `Operation` is stored with the event and `CurrentOperation` is set when the event is
dequeued.

Alternatively, since the Operation argument on `SendEvent` is optional, the value `null` means pick
up the `CurrentOperation` and pass it along to the target actor. If you do not want the
`CurrentOperation` to be passed along to the target actor you can pass a special `NullOperation` like
this:

```c#
this.SendEvent(target, e, Operation.NullOperation);
```

This will put a `null` in the event queue of the target actor so that when this event is dequeued
the `CurrentOperation` of the target actor will be set to `null`.