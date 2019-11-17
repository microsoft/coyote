---
layout: reference
title: Tracking operation groups
section: learn
permalink: /learn/programming-models/state-machines/tracking-operation-groups
---

## Tracking operation groups

For some applications, it is useful to know which machine is processing an event derived from some
user request. Coyote offers the notion of an _operations group_ that can be tracked automatically. The
following `IActorRuntime` APIs take an optional `Guid` parameter.

```c#
ActorId CreateStateMachine(Type type, Event e = null, Guid operationGroupId = default);
void SendEvent(ActorId target, Event e, Guid operationGroupId = default);
```

When you pass a non-default operation group (default is `Guid.Empty`), the runtime takes care of
propagating it. Each machine has a field that tracks its current operations-group:

```c#
Guid OperationGroupId;
```

Additionally you may use the following `IActorRuntime` API to get the operations group of a machine.
However, you must ensure that `currentMachine` is the currently executing machine (otherwise the
`coyote` tester will report an assertion failure).

```c#
Guid GetCurrentOperationGroupId(ActorId currentMachine);
```

The semantics of group tracking are as follows. The default value of `OperationsGroupId` is
`Guid.Empty`. When a machine `M1` is created with a non-default operations group `G`, then
`M1.OperationGroupId` is assigned `G`. If this machine then calls `SendEvent(M2,e,G2)`, then the event
`e` is tagged with `G2`, if `G2` is not `Guid.Empty`, else it is tagged with `G1`. When such a tagged
event `e` is dequeued by `M2`, then it acquires the operations group tag of `e`.

Note that the `StateMachine` class has APIs for creating a machine and sending an event. These internally
call the runtime APIs with the current machine's operation group.

So how do you use these operation groups? You should call the runtime APIs with a fresh guid when the
processing of a newly-arrived user request is about to start. Afterwards, you just call the `StateMachine`
APIs to create and send events, which will in turn automatically propagate the operations group. Note
that this functionality is offered as a convenience only. You can easily encode your own tracking logic
in Coyote code.
