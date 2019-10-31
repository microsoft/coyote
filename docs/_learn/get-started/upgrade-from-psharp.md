---
layout: reference
section: learn
title: Upgrading from P# to Coyote
permalink: /learn/get-started/upgrade
---

## Upgrading from P# to Coyote
This document contains a list of changes from P# to Coyote. Please follow this guide in order to upgrade your applications and services. Contact us if you have any questions or face any issues.

## General changes
- Consume the `Microsoft.Coyote` NuGet package, instead of the `Microsoft.PSharp` NuGet package.

## Namespace changes
- Rename each `using Microsoft.PSharp.*` to `Microsoft.Coyote.*`.
- State machines are now under the new `Microsoft.Coyote.Actors` namespace, to make it explicit
that state machines are implementations of the actor programming model.
- The `Monitor` type is now under the `Microsoft.Coyote.Specifications` namespace.

## Type changes
- `Machine` changed to `StateMachine` to make it explicit that its a state machine.
- `MachineState` changed to `State`, as it is now a nested class inside `StateMachine`.
- `MonitorState` changed to `State`, as it is now a nested class inside `Monitor`.
- `MachineId` changed to `ActorId`, as state machines are implementations of the
actor programming model.
- `IMachineRuntime` changed to `IActorRuntime`.
- `IMachineRuntimeLog` changed to `IActorRuntimeLog`.
- `RuntimeLogWriter` changed to `ActorRuntimeLogWriter`.

## Runtime API changes
- The static runtime factory is renamed from `PSharpRuntime` to `ActorRuntimeFactory`, so you can now do `ActorRuntimeFactory.Create()` to get an actor runtime instance (that can execute state machines).
- `IActorRuntime.CreateMachine` is renamed to `IActorRuntime.CreateStateMachine`.
- The previously deprecated method `IMachineRuntime.CreateMachineAndExecute` has been removed, please use `IActorRuntime.CreateStateMachineAndExecuteAsync` instead (same semantics, just different method name).
- The previously deprecated method `IMachineRuntime.SendEventAndExecute` has been removed, please use `IActorRuntime.SendEventAndExecuteAsync` instead (same semantics, just different method name).

## Machine API changes
- `Machine.CreateMachine` becomes `StateMachine.CreateStateMachine`.
- `Machine.Raise` becomes `StateMachine.RaiseEvent` to become more descriptive.
- `Machine.Receive` becomes `StateMachine.ReceiveEventAsync` to become more descriptive.
- `Machine.Send` becomes `StateMachine.SendEvent` to become more descriptive and match the runtime API.

## Monitor API changes
- `Monitor.Raise` is renamed to `Monitor.RaiseEvent` to become more descriptive.

## Command line tool changes
- The `PSharpTester` and `PSharpReplayer` executables have now been merged into the `coyote` command line tool. To invoke the tester, you do `coyote test ...`. To invoke the replayer you do `coyote replay ...`. The command line options remain pretty much the same, but the way they are declared has changed from (for example) `-max-steps:100` to `--max-steps 100` (single character arguments are used with a single `-`, e.g. `-i 100`). Read more details in the documentation on using the `coyote` command line tool [here](using-coyote.md).
