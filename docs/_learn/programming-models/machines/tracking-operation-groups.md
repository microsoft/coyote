---
layout: reference
title: Tracking operation groups
section: learn
permalink: /learn/programming-models/machines/tracking-operation-groups
---

## Tracking operation groups

For some applications, it is useful to know which machine is processing an event derived from some user request. Coyote offers the notion of an _operations group_ that can be tracked automatically. The following `IMachineRuntime` APIs take an optional `Guid` parameter.

```C#
public MachineId CreateMachine(Type type, Event e = null, Guid? operationGroupId = null);
public void SendEvent(MachineId target, Event e, Guid? operationGroupId = null);
```

When you pass a non-null operation group, the runtime takes care of propagating it. Each machine has a field that tracks its current operations-group:

```C#
Guid OperationGroupId;
```

Additionally you may use the following `IMachineRuntime` API to get the operations group of a machine. However, you must ensure that `currentMachine` is the currently executing machine (otherwise the `Coyote` tester will report an assertion failure).

```C#
public Guid GetCurrentOperationGroupId(MachineId currentMachine);
```

The semantics of group tracking are as follows. The default value of `OperationsGroupId` is `Guid.Empty`. When a machine `M` is created with a non-null operations group `G`, then `M.OperationGroupId` is assigned `G`. When `SendEvent(M,e,G)` is called, then `e` is tagged with `G`. When `e` is dequeued by `M`, it acquires the operations group of `e`, i.e., `M.OperationsGroupId` is assigned `G`.

Note that the `Machine` class also has APIs of creating a machine and sending an event. These internally call the runtime APIs with the current machine's operation group.

You should call the runtime APIs when a new operations group starts (say, when a new request arrives). You can then use the `Machine` APIs to create and send events, which will in turn propagate the operations group automatically. Note that this functionality is offered as a convenience only. You can easily encode your own tracking logic in Coyote code.
