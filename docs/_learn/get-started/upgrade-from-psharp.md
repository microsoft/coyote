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
- Rename each `using Microsoft.PSharp.*` to `Microsoft.Coyote.*`.
- State machines are now under a dedicated namespace `Microsoft.Coyote.Machines`, since we have added support for a new asynchronous tasks programming model (which is available under the `Microsoft.Coyote.Threading.Tasks` namespace, mirroring `System.Threading.Tasks`).
- Monitors are now under a dedicated namespace `Microsoft.Coyote.Specifications`.
- The `SchedulingStrategy` enum moved from `Microsoft.Coyote.Utilities` to `Microsoft.Coyote.Runtime.Exploration`.

## Runtime interface changes
- The static runtime factory is renamed from `PSharpRuntime` to `MachineRuntime`, so you can now do `MachineRuntime.Create()` to get a machine runtime instance.
- The previously deprecated method `IMachineRuntime.CreateMachineAndExecute` has been removed, please use `IMachineRuntime.CreateMachineAndExecuteAsync` instead (same semantics, just different method name).
- The previously deprecated method `IMachineRuntime.SendEventAndExecute` has been removed, please use `IMachineRuntime.SendEventAndExecuteAsync` instead (same semantics, just different method name).

## Command line tool changes
- The `PSharpTester` and `PSharpReplayer` executables have now been merged into the `coyote` command line tool. To invoke the tester, you do `coyote.exe test ...`. To invoke the replayer you do `coyote.exe replay ...`. The command line options remain pretty much the same, but the way they are declared has changed from (for example) `-max-steps:100` to `--max-steps 100` (single character arguments are used with a single `-`, e.g. `-i 100`). Read more details in the documentation on using the `coyote` command line tool [here](using-coyote.md).
