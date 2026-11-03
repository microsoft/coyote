# PR #524 review remediation plan

**Reviewed:** 2026-10-09.
**Scope:** plan only; no review fixes or PR comments have been applied.
**PR head:** `b2439547df75d6c298dd2b01249b2f7c826ef78b`.
**Updated upstream and fork main:** `aabd3b477500977d43c28d89b993476438ec7ea9`.

## Review inventory and evidence

[PR #524](https://github.com/microsoft/coyote/pull/524) has two top-level
discussion comments from akashlal. At review time, GitHub returned no submitted
reviews, inline comments, or review threads. Both comments remain actionable;
there are no inline thread states to change.

| Source | Findings |
| --- | --- |
| [API coverage comment](https://github.com/microsoft/coyote/pull/524#issuecomment-6075141227) | Small-integer atomics, generic atomic contracts, memory barriers, named wait handles |
| [Correctness and gate comment](https://github.com/microsoft/coyote/pull/524#issuecomment-6075177742) | WaitAsync event ordering, cancellation during WaitAll, independent API discovery |

The reviewer reports native-versus-controlled reproductions for the two P1 wait
issues. Source inspection supports their explanations, but this planning task
did not independently run the reproducers. Treat the remaining findings as
coverage or inspection findings, not demonstrated runtime failures.

## Priorities and delivery order

The P1/P2 labels on the wait and gate issues come from the reviewer. The other
priorities below are proposed implementation ordering, not reviewer labels.

| Order | Priority | Work item | Completion criterion |
| --- | --- | --- | --- |
| 1 | P1 | Preserve the WaitAsync winner | Later cancellation cannot replace an already-observed source outcome |
| 2 | P1 | Cancel a blocked WaitAll | Cancellation wakes the controlled wait without completing pending tasks |
| 3 | High | Add small-integer atomics | All eight overloads rewrite and expose scheduling points |
| 4 | High | Match generic atomic contracts | Valid primitive, enum and reference calls rewrite to compatible methods |
| 5 | Boundary | Classify memory barriers | Explicit pass-through/model policy with an independently audited classification |
| 6 | Boundary | Reject named wait-handle synchronization | New overloads cannot silently introduce cross-process synchronization |
| 7 | P2 | Make API discovery independent | A real discovered, unclassified member fails the gate |

Land the two wait fixes as separate commits. The atomic changes can share a
commit if their tests and target-framework conditions remain easy to review.
The boundary policies and API gate should follow as focused commits. Draft the
gate's audited families early, then finalize classifications after items 3-6.

## 1. Preserve the first completion event in WaitAsync

**Location:** task wrapper, lines 251-276, 389-411 and 1006-1032; see the source
index below.

The shared helper currently chooses the cancellation task whenever it is
canceled at helper execution time. That samples the eventual state rather than
remembering whether source completion or cancellation won.

Replace this late selection with a shared, single-assignment winner state.
Observe source completion and cancellation at their event boundaries, before
the controlled helper consumes the winner. Use repository synchronization
patterns to arbitrate callbacks safely; do not move source propagation onto an
uncontrolled continuation or introduce blocking callback cleanup.

The exact observer mechanism needs a focused native-versus-controlled test
before implementation. In particular, asynchronous source continuations must
not make a later cancellation overwrite an outcome already delivered to the
wait. Reuse the same arbitration for generic and non-generic overloads.

Preserve completed-source and already-canceled-token fast paths, cancellation
token identity, source faults/cancellation, registration cleanup, and the
existing finite-timeout abstraction in systematic testing. Do not turn finite
timeouts into arbitrary scheduler races as part of this fix.

Extend the existing TaskWaitAsyncTests and rewriting tests with source result,
fault, and source cancellation followed immediately by wait-token cancellation,
without an intervening await. Cover both task forms and the reverse event order.
Use a default TaskCompletionSource for the reviewer's exact reproducer; also
retain coverage for RunContinuationsAsynchronously and fuzzing/native paths.

**Gate:** the default generic reproducer returns 42, the non-generic equivalent
succeeds, and cancellation-first cases preserve the wait token.

## 2. Observe cancellation while WaitAll is blocked

**Location:** task wrapper, lines 724-764; TaskServices, lines 44-57.

The enumerable overload delegates to the array path. That path checks the token
only before pausing; its wake predicate requires every task to complete.

Add a cancellation-aware controlled wait for this path. Wake when all tasks
complete **or** the token is canceled, then perform the appropriate cancellation
check before native blocking can occur. Preserve uncontrolled-task detection,
argument validation, empty-input behavior, task-fault propagation and the
existing timeout policy. Keep the helper's other callers unchanged unless they
explicitly supply a token.

In TaskWaitAllTests, test both array and enumerable calls with a permanently
pending task and a controlled canceler. Ensure the wait actually starts before
cancellation; merely testing a pre-canceled token misses the bug. Assert the
OperationCanceledException token and completion of the canceler, not a deadlock.
Add a focused completion-versus-cancellation case with native behavior as the
oracle rather than inventing precedence.

**Gate:** cancellation terminates the blocked wait without completing the
pending task; normal completion and genuine noncancelable deadlocks still work.

## 3. Add controlled small-integer atomic overloads

**Location:** Interlocked replacement; Exchange starts at line 151 and
CompareExchange at line 261.

Add Exchange and CompareExchange for byte, sbyte, short and ushort using the
existing ExploreInterleaving-then-native-operation pattern. Match the runtime's
ref parameters, return values and signedness. Compile these only for targets
whose reference assemblies expose the APIs; the PR currently ships .NET 10 and
older assets, so do not leak new references into the .NET 8 asset.

Extend InterlockedTests with a compact data-driven set covering all four types,
old-value returns and successful/unsuccessful comparisons. Add actual consumer
rewriting coverage for all eight signatures and a controlled contention case
that would lose an exploration point with native pass-through.

**Gate:** rewritten consumers execute without uncontrolled calls, and the API
gate independently discovers and classifies all eight overloads.

## 4. Match the expanded generic atomic contracts

**Location:** Interlocked replacement, lines 234-239 and 332-337; API gate
replacement matching, lines 230-313.

Conditionally remove the class constraint where the runtime supports primitive
and enum generic arguments. Keep the reference-only contract on older targets.
Delegate to the native generic implementation after the scheduling point so
the runtime retains its own accepted-type validation and exception behavior.
Update the replacement documentation to describe the actual contract.

Test explicit Exchange<T> and CompareExchange<T> calls for a reference type,
a primitive, and enums with different underlying widths. Explicit type
arguments matter: overload resolution must not accidentally test only the
nongeneric primitive overloads. Include an invalid value-type instantiation
where native .NET rejects it, comparing exception behavior.

Verify emitted IL, successful rewrite/load and controlled execution. Extend the
gate's method representation and compatibility checks to include generic arity,
parameter identity and constraints, rather than treating every generic
parameter as an indistinguishable wildcard.

**Gate:** a runtime-valid generic call cannot redirect to a replacement with
stricter constraints; older framework contracts continue to compile and run.

## 5. Explicitly classify Volatile memory barriers

**Policy proposal:** keep .NET 10 ReadBarrier and WriteBarrier as native
pass-through, provided the audit confirms this matches Coyote's existing
memory-model boundary. Do not claim Coyote explores weak-memory reorderings.

Audit the existing volatile and memory-barrier handling before finalizing this
decision. Explain whether these calls introduce scheduling points and why.
Document the supported boundary in binary-rewriting documentation and the
compatibility assessment, distinguishing unmodeled memory reordering from an
uncontrolled blocking synchronization operation.

Give both discovered signatures an explicit pass-through classification and a
nonempty rationale. Do not mark them as controlled replacements when none
exist. Add a rewriting/execution test that verifies the documented policy.

**Gate:** both members enter the independently discovered surface and their
classification is deliberate, testable and documented.

## 6. Reject unsupported named wait-handle synchronization

**Location:** EventWaitHandle creation wrappers, lines 21-49; uncontrolled
invocation pass, lines 147-178.

Audit every constructor and opening overload involving NamedWaitHandleOptions,
including EventWaitHandle and the already-rejected Mutex/Semaphore families.
Retain the current blanket rejection of opening methods and unsupported types.

Add creation wrappers or invocation diagnostics so named EventWaitHandle
construction fails explicitly during systematic testing, before acquiring an
OS resource. Apply the same boundary to old name-bearing constructors: the
existing Create path currently constructs a native event and registers a local
resource even when a name is supplied. Document null/empty-name behavior after
checking the native contract. Preserve supported unnamed-event behavior and
native behavior outside systematic testing.

Extend UnsupportedSynchronizationRewritingTests and relevant event tests.
Cover the new constructor forms, named creation, opening methods and unnamed
controls. Verify rejected calls cannot create or open the named OS event.
Use Windows for the NamedWaitHandleOptions runtime cases and explicit platform
conditions elsewhere; platform exceptions are not proof of Coyote rejection.

**Gate:** neither old nor new entry points silently model a cross-process
handle as a process-local resource. Cross-process model checking stays out of
scope.

## 7. Discover the audited API surface independently

**Location:** RuntimeApiDiffGateTests, lines 34-195 and 230-394.

Replace the hand-picked input with reflection over explicitly scoped runtime
types or method families. Keep discovery configuration independent of support
classifications: adding a runtime overload within an audited family must not
require changing discovery code.

Include the task wait/join families, Task<T> WaitAsync, Interlocked Exchange and
CompareExchange, Volatile barriers, Lock, and named wait-handle constructors and
opening families. Preserve the already-audited families, either expanding them
deliberately or documenting narrow family scope. Do not audit the entire BCL.

Represent constructors and relevant accessors as well as methods. Capture
generic arity/constraints, ref/out shape, static/instance form and return shape.
Keep explicit, framework-specific classifications for controlled replacement,
rejected/uncontrolled, and intentional native pass-through. Require reasons for
the latter two and fail on missing, overlapping or stale classifications.

Add a discovery-path regression using an audited fixture type with an extra
public overload absent from the classification manifest. Run that type through
the real reflection path and assert failure. Retain missing-replacement and
empty-reason tests; add an incompatible-generic-constraint test. Confirm WhenEach
and the span task overloads already supported by this PR appear in discovery.

**Gate:** runtime additions and incompatible replacement contracts cannot leave
the gate green merely because the hand-written list omitted them.

## Integration and validation

The requested fork-main update is complete: it advanced from d8f5c0fb to
aabd3b47 without rewriting any fork-only history. The two upstream commits add
SDK Dependabot configuration and reset the Monitor synchronized-block cache.
This task did **not** rebase or push feature/net10-support.

Before implementing the plan, integrate the updated main into the PR branch.
Inspect the upstream Monitor cache-reset change against the PR's Lock/Monitor
state separation, preserve both behaviors, and rerun synchronization tests.

Use the existing scripts on the updated PR branch, not the current main's older
test runner. Build first so tests use fresh rewritten assemblies. Run focused
WaitAsync/WaitAll/Interlocked tests and the affected rewriting/gate/boundary
tests under .NET 10 and .NET 8, then broaden to the repository CI matrix.

```powershell
.\Scripts\build.ps1 -ci -nuget
.\Scripts\run-tests.ps1 -framework net10.0 -test testing -filter "FullyQualifiedName~TaskWaitAsyncTests|FullyQualifiedName~TaskWaitAllTests|FullyQualifiedName~InterlockedTests"
.\Scripts\run-tests.ps1 -framework net10.0 -test rewriting
.\Scripts\run-tests.ps1 -framework net8.0 -test testing -filter "FullyQualifiedName~TaskWaitAsyncTests|FullyQualifiedName~TaskWaitAllTests|FullyQualifiedName~InterlockedTests"
.\Scripts\run-tests.ps1 -framework net8.0 -test rewriting
.\Tests\compare-rewriting-diff-logs.ps1 -framework net10.0
.\Tests\compare-rewriting-diff-logs.ps1 -framework net8.0
.\Tests\Compatibility\run-compatibility-matrix.ps1
.\Tests\Compatibility\run-package-smoke.ps1 -framework net10.0
.\Tests\Compatibility\run-package-smoke.ps1 -framework net8.0
.\Scripts\run-tests.ps1 -ci
```

Review intentional IL changes before refreshing snapshots. Use fresh probe
copies for compatibility cases, as the existing matrix requires. Keep package
smoke tests' isolated caches and local-package provenance checks.

For implementation delivery, require the existing Windows/Linux/macOS build,
compatibility, test and sample/package jobs to pass. Update the compatibility
assessment and binary-rewriting policy with the final boundaries. Attach test
evidence to replies addressing both discussion comments, using the required
"Frank the Clank says:" prefix. Do not describe any issue as resolved until its
acceptance criterion passes.

Async ZIP, JSON-over-pipelines and async LINQ remain under the existing external
library/uncontrolled-operation policy. Do not add TaskCompletionSource.SetFromTask
wrappers solely because they are absent; the reviewer explicitly excludes that
as evidence of a scheduling defect.

## Source index

Links pin the inspected PR revision; line numbers will move during implementation.

| Source | Pinned reference |
| --- | --- |
| Task wrapper | [Task.cs:251-411](https://github.com/frankshearar/coyote/blob/b2439547df75d6c298dd2b01249b2f7c826ef78b/Source/Test/Rewriting/Types/Threading/Tasks/Task.cs#L251-L411), [WaitAll:724-764](https://github.com/frankshearar/coyote/blob/b2439547df75d6c298dd2b01249b2f7c826ef78b/Source/Test/Rewriting/Types/Threading/Tasks/Task.cs#L724-L764), [generic WaitAsync:1006-1032](https://github.com/frankshearar/coyote/blob/b2439547df75d6c298dd2b01249b2f7c826ef78b/Source/Test/Rewriting/Types/Threading/Tasks/Task.cs#L1006-L1032) |
| Controlled wait predicate | [TaskServices.cs:44-57](https://github.com/frankshearar/coyote/blob/b2439547df75d6c298dd2b01249b2f7c826ef78b/Source/Core/Runtime/TaskServices.cs#L44-L57) |
| Atomic replacements | [Interlocked.cs:151-337](https://github.com/frankshearar/coyote/blob/b2439547df75d6c298dd2b01249b2f7c826ef78b/Source/Test/Rewriting/Types/Threading/Interlocked.cs#L151-L337) |
| Event construction | [EventWaitHandle.cs:21-49](https://github.com/frankshearar/coyote/blob/b2439547df75d6c298dd2b01249b2f7c826ef78b/Source/Test/Rewriting/Types/Threading/EventWaitHandle.cs#L21-L49) |
| Unsupported invocation policy | [UncontrolledInvocationRewritingPass.cs:147-178](https://github.com/frankshearar/coyote/blob/b2439547df75d6c298dd2b01249b2f7c826ef78b/Source/Test/Rewriting/Passes/Rewriting/UncontrolledInvocationRewritingPass.cs#L147-L178) |
| API discovery and classifier | [RuntimeApiDiffGateTests.cs:34-195](https://github.com/frankshearar/coyote/blob/b2439547df75d6c298dd2b01249b2f7c826ef78b/Tests/Tests.Rewriting/Types/RuntimeApiDiffGateTests.cs#L34-L195), [matching/classifier:230-394](https://github.com/frankshearar/coyote/blob/b2439547df75d6c298dd2b01249b2f7c826ef78b/Tests/Tests.Rewriting/Types/RuntimeApiDiffGateTests.cs#L230-L394) |
| Existing regression coverage | [TaskWaitAsyncTests.cs:286-347](https://github.com/frankshearar/coyote/blob/b2439547df75d6c298dd2b01249b2f7c826ef78b/Tests/Tests.BugFinding/Tasks/TaskWaitAsyncTests.cs#L286-L347) |
| Validation procedure | [Compatibility README](https://github.com/frankshearar/coyote/blob/b2439547df75d6c298dd2b01249b2f7c826ef78b/Tests/Compatibility/README.md), [test runner](https://github.com/frankshearar/coyote/blob/b2439547df75d6c298dd2b01249b2f7c826ef78b/Scripts/run-tests.ps1), [CI workflow](https://github.com/frankshearar/coyote/blob/b2439547df75d6c298dd2b01249b2f7c826ef78b/.github/workflows/test-coyote.yml) |
| Upstream integration | [Monitor cache reset #510](https://github.com/microsoft/coyote/pull/510), [SDK Dependabot #520](https://github.com/microsoft/coyote/pull/520) |
