---
layout: reference
title: Tracking operation groups
section: learn
permalink: /learn/programming-models/actors/tracking-operation-groups
---

## Tracking operation groups

For some applications, it is useful to know which actor is processing an event derived from some
user request. Coyote offers the notion of an _operations group_ that can be tracked automatically. The
following `IActorRuntime` APIs take an optional `Guid` parameter.

```c#
ActorId CreateActor(Type type, Event e = null, Guid operationGroupId = default);
void SendEvent(ActorId target, Event e, Guid operationGroupId = default);
```

When you pass a non-default operation group (default is `Guid.Empty`), the runtime takes care of
propagating it. Each actor has a field that tracks its current operations-group:

```c#
Guid OperationGroupId;
```

Additionally you may use the following `IActorRuntime` API to get the operations group of a actor.
However, you must ensure that the specified actor is the currently executing actor (otherwise the
`coyote` tester will report an assertion failure).

```c#
Guid GetCurrentOperationGroupId(ActorId currentActorId);
```

The semantics of group tracking are as follows. The default value of `OperationsGroupId` is
`Guid.Empty`. When actor `M1` is created with a non-default operations group `G`, then
`M1.OperationGroupId` is assigned `G`. If this actor then calls `SendEvent(M2,e,G2)`, then the event
`e` is tagged with `G2`, if `G2` is not `Guid.Empty`, else it is tagged with `G1`. When such a tagged
event `e` is dequeued by `M2`, then it acquires the operations group tag of `e`.

Note that the `Actor` class has APIs for creating an actor and sending an event. These internally
call the runtime APIs with the current actor's operation group.

So how do you use these operation groups? You should call the runtime APIs with a fresh guid when the
processing of a newly-arrived user request is about to start. Afterwards, you just call the `Actor`
APIs to create and send events, which will in turn automatically propagate the operations group. Note
that this functionality is offered as a convenience only. You can also encode your own tracking logic
in custom code in the case that this semantic doesn't match your requirements exactly.
