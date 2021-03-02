## Upgrading from P# to Coyote
This document contains a list of changes from P# to Coyote. Please follow this guide in order to
upgrade your applications and services. Contact us if you have any questions or face any issues.

## General changes
- Consume the `Microsoft.Coyote` NuGet package, instead of the `Microsoft.PSharp` NuGet package.

## Namespace changes
- Rename each `using Microsoft.PSharp.*` to `Microsoft.Coyote.*`.
- State machines are now under the new `Microsoft.Coyote.Actors` namespace, to make it explicit that
  state machines are implementations of the actor programming model.
- The `Monitor` type is now under the `Microsoft.Coyote.Specifications` namespace.

## Type changes
- `Machine` was renamed to `StateMachine` to make it explicit that its a state machine.
- `MachineState` was renamed to `State`, as it is now a nested class inside `StateMachine`.
- `MonitorState` was renamed to `State`, as it is now a nested class inside `Monitor`.
- `MachineId` was renamed to `ActorId`, as state machines are implementations of the actor
  programming model.
- `Halt` was renamed to `HaltEvent` to make it explicit that its an event.
- `Default` was renamed to `DefaultEvent` to make it explicit that its an event.
- `IMachineRuntime` was renamed to `IActorRuntime`.
- `IMachineRuntimeLog` was renamed to `IActorRuntimeLog`.
- `RuntimeLogWriter` was renamed to `ActorRuntimeLogWriter`.

## Runtime API changes
- The static runtime factory was renamed from `PSharpRuntime` to `RuntimeFactory`, so you can
  now do `RuntimeFactory.Create()` to get an actor runtime instance (that can execute state
  machines).
- `IMachineRuntime.CreateMachine` was renamed to `IActorRuntime.CreateActor`.
- The previously deprecated method `IMachineRuntime.CreateMachineAndExecute` has been removed,
  please use `IActorRuntime.CreateActorAndExecuteAsync` instead (same semantics, just different
  method name).
- The previously deprecated method `IMachineRuntime.SendEventAndExecute` has been removed, please
  use `IActorRuntime.SendEventAndExecuteAsync` instead (same semantics, just different method name).

## Machine API changes
- `Machine.CreateMachine` was renamed `StateMachine.CreateActor`.
- `Machine.Raise` was renamed `StateMachine.RaiseEvent` to become more descriptive.
- `Machine.Goto` was renamed `StateMachine.RaiseGotoStateEvent` to become more descriptive.
- `Machine.Push` was renamed `StateMachine.RaisePushStateEvent` to become more descriptive.
- `Machine.Pop` was renamed `StateMachine.RaisePopStateEvent` to become more descriptive.
- `Machine.Receive` was renamed `StateMachine.ReceiveEventAsync` to become more descriptive.
- `Machine.Send` was renamed `StateMachine.SendEvent` to become more descriptive and match the
  runtime API.
- The `ReceivedEvent` property has been removed. You can now declare event handlers that take a
  single `Event` as an in-parameter, which is the `Event` that triggered the handler. A handler that
  does not require access to this `Event` can be still declared without an in-parameter. Some of the
  user callbacks (e.g. `OnException` and `OnHaltAsync`) now give access to the last dequeued event.
- Introduced new API `RaiseHaltEvent` for raising the HaltEvent on yourself (this is more efficient
  than halting via a `RaiseEvent`).

## Event API changes
- `Halt` (now `HaltEvent`) can no longer be constructed, use `HaltEvent.Instance` instead to get
  access to a singleton instance created once to reduce unnecessary allocations.

## Monitor API changes
- `Monitor.Raise` was renamed to `Monitor.RaiseEvent` to become more descriptive.
- Similar to the `Machine` type, `Goto` is `RaiseGotoStateEvent`.

## Test attribute changes
- `[Microsoft.PSharp.Test]` was renamed  to `[Microsoft.Coyote.SystematicTesting.Test]`

## Command line tool changes
- The `PSharpTester` and `PSharpReplayer` executables have now been merged into the `coyote` command
  line tool. To invoke the tester, you do `coyote test ...`. To invoke the replayer you do `coyote
  replay ...`. The command line options remain pretty much the same, but the way they are declared
  has changed from (for example) `-max-steps:100` to `--max-steps 100` (single character arguments
  are used with a single `-`, e.g. `-i 100`). Read more details in the documentation on using the
  `coyote` command line tool [here](../get-started/using-coyote.md).
