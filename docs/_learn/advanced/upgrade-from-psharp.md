---
layout: reference
section: learn
title: Upgrading from P# to Coyote
permalink: /learn/advanced/upgrade
---

# Upgrading from P# to Coyote
This document contains a list of changes from P# to Coyote. Please follow this guide in order to upgrade your applications and services.

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
