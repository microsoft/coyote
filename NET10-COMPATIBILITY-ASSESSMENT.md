# Coyote compatibility assessment for .NET 10 assemblies

**Assessment date:** 2026-08-20
**Repository:** `microsoft/coyote`
**Branch / commit:** `main` / `f2c135d201341ee5eff3d82cac62bdb85b25139f`
**Coyote version:** 1.7.11 (`Common/version.props:5`)
**Probe SDK/runtime:** .NET SDK 10.0.303, .NET runtime 10.0.11, C# 14

**Implementation plan:** [NET10-UPGRADE-PLAN.md](NET10-UPGRADE-PLAN.md)

## Outcome

Current Coyote can **parse and rewrite many `net10.0` assemblies**, and its existing `net8.0` binaries can execute under the .NET 10 runtime. It cannot, however, be considered generally compatible with arbitrary .NET 10 / C# 14 concurrency code today.

The normal `coyote test` host is a `net8.0` process and fails to load a `net10.0` test assembly. A manual `dotnet exec` host override makes a baseline probe work. Once hosted on .NET 10, the rewriter still has concrete semantic coverage gaps for newer APIs and compiler lowering, including `Task.WhenAll(ReadOnlySpan<Task>)` and `System.Threading.Lock`. The latter is especially risky because it remains unreplaced without being reported as uncontrolled.

**Decision:** use against `net10.0` only as a constrained workaround, not as declared/sound .NET 10 support. Proper support requires a native .NET 10 host plus rewriter/API coverage work and regression tests.

## Direct answer

| Question | Answer |
|---|---|
| Can a `net10.0` project reference current Coyote assemblies? | **Yes.** The probe compiled against the existing `net8.0` Coyote binaries. `net10.0` is compatible with lower `netX.0` assets. |
| Can current Coyote rewrite a `net10.0` assembly? | **Yes for the tested assembly.** Mono.Cecil 0.11.4 parsed and rewrote C# 14 async state machines and emitted a valid modified assembly. This does not prove every new metadata shape is supported. |
| Does normal `coyote test MyNet10.dll` work? | **No.** The `net8.0` host cannot load `System.Runtime, Version=10.0.0.0`. |
| Is there a usable-today workaround? | **Yes, constrained.** Run the existing CLI under the installed .NET 10 runtime with `dotnet exec --fx-version 10.0.11 --roll-forward LatestMajor ...`. |
| Is arbitrary .NET 10/C# 14 concurrency code controlled correctly? | **No.** New overloads and lowering can bypass Coyote's exact-signature wrappers. Two concrete gaps were reproduced. |
| Is C# 14 itself broadly incompatible? | **No evidence of a general language-version incompatibility.** The risk is feature-specific emitted IL and calls to new runtime APIs. |

## Scope and method

The assessment used four evidence sources:

1. Static inspection of target frameworks, CLI runtime configuration, dependency conditions, rewriting type maps, method matching, wrappers, scripts, tests, and CI.
2. A baseline `net10.0` / C# 14 probe using `Task.Run`, `Task.Yield`, async state machines, `Monitor`-lowered `lock`, and an explicitly selected array overload of `Task.WhenAll`.
3. A new-API probe using `System.Threading.Lock` and the C# 14-selected `Task.WhenAll(ReadOnlySpan<Task>)` overload.
4. A forced `net10.0` build of the Coyote solution to identify native-retargeting failures.

This is a compatibility assessment, not an implementation. No Coyote source code was changed.

## Finding 1 — The published/test host is .NET 8, so normal execution fails

**Severity:** blocking for normal CLI use
**Confidence:** high; reproduced

The shared build targets declare `net8.0` as the primary target (`Common/build.props:47`). The tool project imports those targets (`Tools/Coyote/Coyote.csproj:18`). Its generated runtime configuration requests `Microsoft.NETCore.App` and `Microsoft.AspNetCore.App` 8.0 (`bin/net8.0/coyote.runtimeconfig.json:3-12`).

Running the existing CLI normally against the rewritten `net10.0` probe failed during test discovery:

```text
Microsoft (R) Coyote version 1.7.11.0 for .NET 8.0.30
Unable to load one or more of the requested types.
Could not load file or assembly 'System.Runtime, Version=10.0.0.0 ...'
```

That is expected runtime-host behavior: an application hosted on .NET 8 cannot load assemblies compiled against .NET 10 reference assemblies.

A manual host override succeeded:

```powershell
dotnet exec --fx-version 10.0.11 --roll-forward LatestMajor `
  <coyote-repo>\bin\net8.0\coyote.dll `
  test .\bin\Debug\net10.0\Net10CoyoteProbe.dll -i 10
```

Observed result:

```text
Microsoft (R) Coyote version 1.7.11.0 for .NET 10.0.11
Found 0 bugs.
Explored 10 execution paths: 10 fair, 0 unfair, 10 unique.
Controlled 50 operations: 5 (min), 5 (avg), 5 (max).
```

This proves the existing assemblies can run under .NET 10. It is a workaround, not a normal supported tool invocation or package contract.

## Finding 2 — Basic C# 14 async IL is rewriteable

**Severity:** positive compatibility evidence
**Confidence:** high for the tested patterns

The baseline probe was built by SDK 10.0.303 with `TargetFramework=net10.0` and `LangVersion=14.0`. Coyote's .NET 8 rewriter successfully:

- read the assembly;
- rewrote `AsyncTaskMethodBuilder` to Coyote's builder;
- rewrote task awaiters and `Task.Run`;
- rewrote `Task.Yield`;
- rewrote ordinary object-based `lock` through the existing `Monitor` wrapper;
- wrote the modified `net10.0` assembly and an IL diff.

The compiler-generated async state-machine shape was therefore not intrinsically incompatible. Coyote's type map explicitly covers async builders and awaiters (`Source/Test/Rewriting/Passes/Rewriting/Types/TypeRewritingPass.cs:30-62`) and task/thread synchronization types (`TypeRewritingPass.cs:64-90`).

This finding should not be generalized to every C# 13/14 feature. Coyote rewrites exact emitted types and method signatures; language features that change lowering or overload selection can evade those mappings.

## Finding 3 — C# 14 selects a `Task.WhenAll` overload Coyote does not wrap

**Severity:** high; produces uncontrolled work and weakens systematic exploration
**Confidence:** high; reproduced

In the new-API probe, this source:

```csharp
await Task.WhenAll(first, second);
```

compiled to:

```text
Task.WhenAll(ReadOnlySpan<Task>)
```

The emitted IL used a compiler-generated inline array, converted it to `ReadOnlySpan<Task>`, and called the span overload. Microsoft documents that C# 14 makes span-based overloads applicable in more scenarios. The span overload itself is available in .NET 9 and later.

Coyote's wrapper contains only array and `IEnumerable` forms for `WhenAll` (`Source/Test/Rewriting/Types/Threading/Tasks/Task.cs:219-256`). There is no `ReadOnlySpan<Task>` or generic `ReadOnlySpan<Task<TResult>>` wrapper.

The rewritten IL retained the runtime call and injected `ThrowIfReturnedTaskNotControlled` rather than replacing it with a controlled Coyote call. Execution completed but reported:

```json
{"UncontrolledInvocations":["System.Threading.Tasks.Task.WhenAll"]}
```

Explicitly selecting the old overload avoids the gap:

```csharp
await Task.WhenAll(new Task[] { first, second });
```

That workaround ran with zero uncontrolled invocations.

Related static gaps in the .NET 10 `Task` surface include:

- `WhenAll(ReadOnlySpan<Task>)` and the generic equivalent;
- `WhenAny(ReadOnlySpan<Task>)` and the generic equivalent;
- `WaitAll(ReadOnlySpan<Task>)`;
- all `Task.WhenEach(...)` overloads introduced in .NET 9;
- `Task.Delay(..., TimeProvider, ...)`;
- instance `Task.WaitAsync(...)` overloads.

Not every missing wrapper is new to .NET 10, but these APIs are part of the current `net10.0` surface and should be audited for intended Coyote semantics.

## Finding 4 — `System.Threading.Lock` is silently outside Coyote control

**Severity:** critical soundness risk for code using the recommended modern lock type
**Confidence:** high that it is not rewritten; runtime consequences require dedicated stress tests

Starting with .NET 9 and C# 13, Microsoft recommends `System.Threading.Lock`. When the compiler knows the operand has that type, a `lock` statement lowers to approximately:

```csharp
using (syncObject.EnterScope())
{
    // critical section
}
```

It does **not** lower to `Monitor.Enter` / `Monitor.Exit`.

Coyote's rewrite map covers `Monitor`, `SemaphoreSlim`, `Interlocked`, wait handles, and related older primitives, but has no `System.Threading.Lock` entry (`Source/Test/Rewriting/Passes/Rewriting/Types/TypeRewritingPass.cs:78-90`). A repository search found no `System.Threading.Lock` or `EnterScope` implementation.

The rewritten probe IL still contained:

```text
System.Threading.Lock::EnterScope()
System.Threading.Lock/Scope::Dispose()
```

Coyote's uncontrolled-invocation pass does not classify `System.Threading.Lock` as uncontrolled. Its threading checks cover selected `Thread`, `ThreadPool`, and event-handle methods (`Source/Test/Rewriting/Passes/Rewriting/UncontrolledInvocationRewritingPass.cs:115-149`), but not `Lock`.

Therefore, code can appear to run under Coyote without a warning while lock acquisition and release remain outside the scheduler's model. Coyote may still inject scheduling points around memory accesses, but that is not equivalent to controlling the synchronization primitive and can permit real blocking or miss relevant schedules.

Using a dedicated `object` lock forces the older `Monitor` lowering and stays on the currently modeled path.

## Finding 5 — Rewriter coverage is exact-signature driven

**Severity:** architectural source of future compatibility gaps
**Confidence:** high; code inspection

The type rewriter maps known BCL type names to Coyote replacement types (`TypeRewritingPass.cs:18-20`, `30-111`). Method replacement then requires a matching method name, static/instance form, parameter count, and parameter full names (`MethodBodyTypeRewritingPass.cs:168-223`, `380-426`).

That architecture works well for known signatures but does not automatically inherit support when .NET adds overloads or the compiler starts choosing a different overload. The `ReadOnlySpan<Task>` result is a direct example.

Compatibility testing should therefore be source-pattern based as well as API-list based: compile representative concurrency syntax with each supported SDK/language version, inspect the emitted calls, rewrite it, and assert there are no uncontrolled invocations.

## Finding 6 — Native `net10.0` retargeting is incomplete

**Severity:** blocking for shipping a native .NET 10 tool asset
**Confidence:** high; reproduced and statically confirmed

A forced solution build with SDK 10.0.303 and `TargetFrameworks=net10.0` restored successfully after switching to the requested package source. Results:

- `Source/Core` compiled to `bin/net10.0/Microsoft.Coyote.dll`;
- `Source/Actors` compiled to `bin/net10.0/Microsoft.Coyote.Actors.dll`;
- `Source/Test` failed because `Microsoft.Extensions.DependencyModel` was absent.

`Source/Test/Test.csproj` only adds that dependency for `net8.0` and `net6.0` (`Test.csproj:22-27`). The CLI/tool projects likewise only add framework references for those exact TFMs (`Tools/Coyote/Coyote.csproj:25-38`, `Tools/CLI/Coyote.CLI.csproj:23-30`).

Additional retargeting work identified by static inspection:

- `global.json` pins SDK 8.0.404 (`global.json:3`);
- `Common/build.props` gives `net10.0` the fallback C# 8 language version because only `net8.0` and `net6.0` receive C# 10 (`Common/build.props:19-23`);
- `Scripts/run-tests.ps1` rejects `net10.0` and hard-codes `net8.0` ILVerify paths (`Scripts/run-tests.ps1:5-6`, `62-69`);
- CI installs/tests .NET 8 and legacy .NET 6, not .NET 10 (`.github/workflows/test-coyote.yml:33-40`, `80-83`; `.github/workflows/codeql-analysis.yml:29-32`; `.github/workflows/test-performance.yml:26-29`);
- the .NET 10 restore reports `NU1510` for the unconditional `System.Threading.Tasks.Extensions` reference in `Source/Core/Core.csproj:17`.

## Finding 7 — Existing tests cannot detect the demonstrated C# 14 gap

**Severity:** high regression risk
**Confidence:** high; code inspection

`Tests/Tests.Rewriting/Types/TaskRewritingTests.cs:17-27` calls `Task.WhenAll(Task.CompletedTask)` and the generic equivalent. Under the repository's `net8.0` / C# 10 build, these compile to the existing array overload and pass through the current wrapper.

There is no test compiled under C# 14 that verifies the overload actually selected by the newer compiler. There is also no test for `System.Threading.Lock` lowering. The current tests validate known wrapper behavior, not cross-SDK source compatibility.

## Compatibility classification

### Memory barriers and the memory-model boundary

Systematic testing executes one controlled operation at a time and does not explore weak-memory reorderings. Volatile reads and writes and `Interlocked` operations are rewritten to add a scheduling point before the memory access. Barriers that do not access memory are left as native calls with no scheduling point: `Thread.MemoryBarrier`, `Interlocked.MemoryBarrier`/`MemoryBarrierProcessWide`, and the .NET 10 `Volatile.ReadBarrier`/`WriteBarrier`. This was audited for PR #524. The policy is deliberate pass-through, not controlled support: a barrier never blocks, so it is not an uncontrolled synchronization operation, and it only constrains reorderings that Coyote does not model. The runtime API gate classifies both .NET 10 barriers as pass-through with a stated reason, and rewriting tests check that they stay native calls and add no scheduling point. See "How memory ordering is modeled" in `docs/concepts/binary-rewriting.md`.

### Named wait handles

Named wait handles are system objects that other processes can share, and Coyote does not model synchronization across processes. Before PR #524 remediation, the name-bearing `EventWaitHandle` constructors created the native named event and registered it as a process-local controlled resource, and the .NET 10 `NamedWaitHandleOptions` constructors were not intercepted at all. During systematic testing, any `EventWaitHandle` constructor with a non-empty name is now reported as a bug and throws `NotSupportedException` before the system event is created or opened. A `null` or empty name natively creates an unnamed, unshared event, so it remains controlled. `EventWaitHandle.OpenExisting`/`TryOpenExisting` (including the `NamedWaitHandleOptions` overloads, which the runtime API gate classifies with a reason) and all `Mutex` and `Semaphore` members stay reported as uncontrolled invocations. See "Named wait handles" in `docs/concepts/binary-rewriting.md`.

### Runtime API gate

`Tests/Tests.Rewriting/Types/RuntimeApiDiffGateTests.cs` discovers, through reflection, every public member of a fixed list of audited families, independently of how those members are classified. The families are: the `Task` wait, join, delay and `RunSynchronously` methods (including `WhenEach` and the span overloads); `Task<T>.WaitAsync`; the `Monitor` and `SemaphoreSlim` locking and waiting methods; `Interlocked.Increment`, `Exchange` and `CompareExchange`; the `Volatile` barriers; `WaitHandle.WaitOne`; `Thread.Sleep`; all of `System.Threading.Lock`; and the constructors and opening methods of `EventWaitHandle`, `Mutex` and `Semaphore`. Signatures capture constructor, static or instance form, generic arity and constraints, ref/out/in parameters and the return type. Each discovered member must have exactly one framework-specific classification: controlled (a compatible replacement must exist, with no stricter generic constraints), rejected (the uncontrolled-invocation rewriting pass must report it, with a reason), or pass-through (no replacement and not rejected, with a reason). The gate fails on unclassified, missing-replacement, overlapping, stale and empty-reason classifications, on .NET 10, 8, 6 and .NET Framework 4.6.2. It deliberately does not audit the whole BCL; new overloads in an audited family fail the gate automatically, while new families must be added to the list.

Running the gate on the supported frameworks found three gaps that have since been fixed: `Monitor.TryEnter(object, int)` and the .NET 7+ `Task.Wait(TimeSpan, CancellationToken)` had no controlled replacement and blocked natively, and the .NET Framework `EventWaitHandle` constructor that takes `EventWaitHandleSecurity` created a named event without being reported.

### Works today with the forced .NET 10 host

- Loading current Coyote `net8.0` libraries from a `net10.0` process.
- Reading and writing the tested .NET 10 assembly with Mono.Cecil 0.11.4.
- Standard async `Task` state machines.
- `Task.Run`, `Task.Yield`, awaiters, and existing wrapped signatures.
- Object-based `lock` lowered through `Monitor`.

### Workaround-only

- Running `coyote test` against a `net10.0` assembly by forcing the existing CLI onto .NET 10.
- Avoiding span overloads by explicitly constructing arrays or casting to an older signature.
- Avoiding `System.Threading.Lock` and using an object-backed `Monitor` lock.

### Not controlled or not supported

- Normal .NET 8-hosted CLI loading a .NET 10 assembly.
- `Task.WhenAll(ReadOnlySpan<Task>)` in the reproduced C# 14 case.
- `System.Threading.Lock` acquisition/release.
- Other unwrapped .NET 9/10 Task overloads until individually implemented and tested.

## Required upgrades

### P0 — minimum credible .NET 10 support

1. **Ship a native `net10.0` CLI/tool asset.** Update the SDK pin, shared target matrix, framework references, dependency conditions, scripts, and package layout. Do not rely on users knowing the `dotnet exec` host override.
2. **Add wrappers for span-based Task combinators.** At minimum cover generic/non-generic `WhenAll` and `WhenAny`, plus span-based `WaitAll` where Coyote intends to control blocking waits.
3. **Model `System.Threading.Lock`.** Add rewrite mapping/interception for `EnterScope`/scope disposal, or explicitly reject it as uncontrolled until sound modeling exists. Silent pass-through is the worst current behavior.
4. **Add .NET 10/C# 14 compiled probes to CI.** Assert successful discovery/execution and zero uncontrolled invocations for supported source patterns.

### P1 — complete current Task surface audit

1. Decide and test semantics for `Task.WhenEach`.
2. Decide and test `Task.WaitAsync` and `TimeProvider`-based delay APIs.
3. Generate an API-diff checklist between supported runtime versions and Coyote wrapper types so newly added overloads cannot silently escape coverage.
4. Test both direct API calls and compiler-selected overloads from ordinary source syntax.

### P2 — hardening

1. Add a startup diagnostic when the CLI host runtime is older than the target assembly runtime, replacing the current reflection loader failure with an actionable message.
2. Consider treating unknown synchronization types/methods as explicit uncontrolled invocations rather than silently allowing them.
3. Validate Mono.Cecil against a wider corpus of .NET 10 assemblies and new metadata shapes before deciding whether its version must be upgraded. The basic probe does not itself require an upgrade.
4. Remove or condition packages that trigger .NET 10 package-pruning warnings.

## Recommended policy for users today

If immediate use is unavoidable:

1. Install the matching .NET 10 runtime.
2. Run Coyote via the explicit .NET 10 host override.
3. Avoid `System.Threading.Lock`.
4. Avoid implicit binding to span-based Task overloads; select array or enumerable overloads explicitly.
5. Treat any uncontrolled invocation report as a failed compatibility check, not a warning to ignore.
6. Run a representative smoke suite before trusting exploration results.

This policy is operationally possible but fragile. It should not be advertised as full .NET 10 support.

## Plan

1. Establish the current supported host/TFM/package contract from project and CI files.
2. Audit rewriter coverage against .NET 9/10 concurrency and C# 13/14 lowering changes.
3. Reproduce current behavior with a pinned `net10.0`/C# 14 probe.
4. Classify findings as supported, workaround-only, uncontrolled, or blocked.
5. Write and verify a persistent markdown assessment report; no product implementation changes.

## Execution log

- **Step 1 — host/TFM contract:** inspected shared targets, tool projects, runtime config, scripts, and CI; verified the shipped host is `net8.0` and no native .NET 10 path exists.
- **Step 2 — rewriter audit:** inspected the known type map, exact method matching, task wrappers, uncontrolled-invocation pass, and rewriting tests; identified concrete missing signatures and silent `Lock` handling.
- **Step 3 — probes:** built two assemblies with SDK 10.0.303/C# 14, rewrote both, tested default and forced hosts, and inspected original/rewritten IL.
- **Step 4 — classification:** separated verified baseline support, host workaround, reproduced semantic gaps, and static-audit candidates.
- **Step 5 — report:** recorded evidence and prioritized required upgrades here.

## Files / ids touched

- `NuGet.config` — previously replaced at the user's request; unrelated to Coyote compatibility logic.
- `coyote-net10-compatibility-assessment-20260820` — this report artifact.
- No Coyote product source files were modified by the assessment.

## Verification

- Repository's standard .NET 8 build succeeded after removing one SDK-10-generated stale `project.assets.json` file.
- Baseline .NET 10 probe build: succeeded under SDK 10.0.303.
- Baseline rewrite: succeeded and emitted modified assembly/IL diff.
- Default-host test: failed loading `System.Runtime, Version=10.0.0.0`.
- Forced-.NET-10-host baseline test: succeeded, 10 paths, 50 controlled operations, zero uncontrolled invocations.
- New-API probe build/rewrite: succeeded.
- Rewritten IL retained `System.Threading.Lock::EnterScope()` and `Task.WhenAll(ReadOnlySpan<Task>)`.
- New-API test: completed but reported one uncontrolled invocation, `System.Threading.Tasks.Task.WhenAll`.
- Forced native `net10.0` Coyote build: Core and Actors succeeded; Test failed on missing `Microsoft.Extensions.DependencyModel` references.

## Sources

- Microsoft, [Select which .NET version to use](https://learn.microsoft.com/dotnet/core/versions/selection)
- Microsoft, [Target frameworks in SDK-style projects](https://learn.microsoft.com/dotnet/standard/frameworks)
- Microsoft, [C# 14 overload resolution with span parameters](https://learn.microsoft.com/dotnet/core/compatibility/core-libraries/10.0/csharp-overload-resolution)
- Microsoft, [The `lock` statement](https://learn.microsoft.com/dotnet/csharp/language-reference/statements/lock)
- Microsoft, [`Task.WhenAll` overloads for .NET 10](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task.whenall?view=net-10.0)
- Microsoft, [`Task.WhenEach` for .NET 10](https://learn.microsoft.com/dotnet/api/system.threading.tasks.task.wheneach?view=net-10.0)

## Deferred / open

- Full implementation is intentionally out of scope.
- This assessment did not exhaustively execute every Coyote wrapper against every .NET 10 overload.
- `System.Threading.Lock` needs a focused design because its `ref struct` scope and blocking semantics differ from a simple static method wrapper.
- Broader Mono.Cecil validation should include assemblies using newer metadata features beyond the tested async/inline-array patterns.

## Suggested next

Turn the P0 section into an implementation plan with four independently verifiable workstreams: native host/packaging, Task span overloads, `System.Threading.Lock`, and cross-SDK CI probes.
