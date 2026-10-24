# Coyote 1.7.11 API reference

**Scope: Coyote 1.7.11 only.** This is a source-derived guide, not a guarantee that
every member or overload of a listed type is rewritten. Check the exact wrapper
and target framework when a member is not listed. Read
[compatibility.md](compatibility.md) before applying it to a consumer.

The 1.7.11 NuGet packages identify source commit
[`4d2f9dd4e0fdeb528557e7058f3e4379c184c6da`](https://github.com/microsoft/coyote/tree/4d2f9dd4e0fdeb528557e7058f3e4379c184c6da).
Source locations in the Coyote repository:

- `Source/Test/Rewriting/Types/NameCache.cs`: known type names.
- `Source/Test/Rewriting/Passes/Rewriting/Types/TypeRewritingPass.cs`: active type
  mappings and option/framework gates.
- `Source/Test/Rewriting/Passes/Rewriting/UncontrolledInvocationRewritingPass.cs`:
  known uncontrolled and data-nondeterministic calls.
- `Source/Test/Rewriting/Types/`: exact implemented wrapper members.
- `Source/Test/Rewriting/RewritingOptions.cs`, `Source/Core/Configuration.cs`,
  and `Source/Core/Runtime/CoyoteRuntime.cs`: defaults and runtime behavior.

Consult source matching the installed release, not automatically the latest `main`.

## Strict mode and its limits

By default 1.7.11 permits partially controlled concurrency and data nondeterminism.
Detected uncontrolled work can emit warnings and continue with reduced exploration
and replay guarantees. For a strict test, use both:

```csharp
Configuration.Create()
    .WithPartiallyControlledConcurrencyAllowed(false)
    .WithPartiallyControlledDataNondeterminismAllowed(false);
```

A clean strict-mode run only means no violation was detected on explored paths.
Unknown third-party code is not automatically instrumented or certified.

## Rewritten types and commonly controlled operations

| Area | Types / operations | Qualification |
| --- | --- | --- |
| Tasks | `Task`, `Task<T>`, `Run`, `Delay`, `WhenAll`, `WhenAny`, `Wait`, `WaitAll`, `WaitAny`, `Yield` | Check the exact overload; unsupported members can remain uncontrolled. |
| Async plumbing | Task/ValueTask awaiters, configured awaitables, async method builders | Supports rewritten `async`/`await`; does not make arbitrary awaited I/O controlled. |
| Task helpers | `ValueTask`, `ValueTask<T>`, `TaskCompletionSource<T>`, task factories, `TaskExtensions`, `Parallel` | Type mappings are not blanket member coverage. Non-generic `TaskCompletionSource` is modern .NET only. |
| Threads | `Thread` creation/start/join and `Sleep` wrappers | Certain lifecycle methods remain uncontrolled; see below. |
| Synchronization | `Monitor` (`lock`), `SemaphoreSlim`, `Interlocked`, `Volatile`, `SpinWait` | Check member support. |
| Wait handles | `AutoResetEvent`, `ManualResetEvent`, `EventWaitHandle`, `WaitHandle` | `OpenExisting`, `TryOpenExisting`, and `SignalAndWait` have exceptions below. |
| HTTP | `HttpClient`, `HttpRequestMessage` | Modern .NET mappings only; does not make a real remote service deterministic. |

Rewritten `Task.Delay` and `Thread.Sleep` use scheduler-controlled delay behavior.
They are not inherently uncontrolled, but do not provide a precise virtual
wall-clock model for application deadlines or timer callbacks.

## Conditional collection instrumentation

| Types | Rewriting option | Default |
| --- | --- | --- |
| `ConcurrentBag<T>`, `ConcurrentDictionary<K,V>`, `ConcurrentQueue<T>`, `ConcurrentStack<T>` | `IsRewritingConcurrentCollections` | Enabled |
| `List<T>`, `Dictionary<K,V>`, `HashSet<T>` | `IsDataRaceCheckingEnabled` | Disabled |

Collection wrappers can introduce scheduling points (for example,
`ConcurrentDictionary` operations call `ExploreInterleaving`). Do not assume that
collection instrumentation only detects races without affecting scheduling.
Rewriting options and runtime race-checking options are distinct.

## Scheduling granularity

Coyote's default scheduling points include supported task and synchronization
operations. They are designed to provide useful concurrency exploration without
the cost of scheduling at individual memory accesses. A synchronous sequence
with no active scheduling point can execute without an interleaving inside it.
For example, a read/modify/write of an ordinary field need not expose its
lost-update window under the default configuration.

In 1.7.11, `WithMemoryAccessRaceCheckingEnabled` is **off by default**, as is
`WithControlFlowRaceCheckingEnabled`. The rewriter's
`IsRewritingMemoryLocations` option is enabled by default, but emitting hooks
does not activate these runtime exploration options.

**Preserve these defaults.** Enabling memory-access interleaving can substantially
increase execution overhead and the number of schedules, significantly degrading
the default experience under a fixed testing budget. This reference explains
the option so the agent can accurately describe coverage; it is not a
recommendation to enable it for every test, every shared field, or every clean run.
Any deliberately requested finer-grained experiment should remain separate from
the default setup, with its additional cost and coverage limits stated.

Even with the option enabled, 1.7.11 does not instrument every possible memory
access: the memory-access pass targets instance field load/store instructions
and skips constructors, property accessors, and async state-machine types.
Consult `MemoryAccessRewritingPass.cs` and the exact code being tested rather
than promising exhaustive memory-race detection. Increasing the iteration count
does not create scheduling points that are absent or disabled.

## Known uncontrolled concurrency

| API | Flagged operations | Preferred approach |
| --- | --- | --- |
| `Task` | `ContinueWith`, `RunSynchronously`; `Run` calls left unredirected by type rewriting are also flagged | Use supported `Task.Run` overloads and `await` composition. |
| `Thread` | `Interrupt`, `Suspend`, `Resume`, `Abort`, `ResetAbort`, `BeginCriticalRegion`, `EndCriticalRegion` | Cooperative cancellation and supported synchronization. |
| `ThreadPool` | `QueueUserWorkItem`, unsafe queue variants, wait registration variants | Supported `Task.Run` overloads. |
| `EventWaitHandle` | `OpenExisting`, `TryOpenExisting` | Supported in-process event primitives. |
| `WaitHandle` | `SignalAndWait` | Model with supported synchronization. |
| `Process` | Process start, wait, lifecycle and output-control calls flagged by the rewriter | Fake process boundaries; do not run external processes in a scenario. |

The rewriter also flags calls to these whole types: `Mutex`, `Semaphore`,
`ManualResetEventSlim`, `ReaderWriterLock`, `ReaderWriterLockSlim`, `SpinLock`,
`System.Threading.Timer`, `System.Timers.Timer`, `ExecutionContext`,
`SynchronizationContext`, and `RegisteredWaitHandle`.

Use supported synchronization where its semantics fit. Do not replace a
reader-writer lock with a mutex or a semaphore merely to make a test pass if
that changes the property; model the relevant semantics explicitly.

## Data nondeterminism and external state

`DateTime.Now`, `DateTime.Today`, `DateTime.UtcNow`, and `Guid.NewGuid()` are
explicitly flagged as data-nondeterministic. Inject a virtual clock or ID source,
or use Coyote-controlled choices. `Stopwatch`, arbitrary random generators,
filesystem state, and third-party I/O are not made deterministic merely because
they are absent from the flagged list.

## Answering "can Coyote control this API?"

1. Match the exact type, member, overload, framework, and rewriting options.
2. Inspect the version-matched wrapper and known uncontrolled-call checks.
3. For third-party code, inspect its implementation and whether its dependencies
   are rewritten. Pure deterministic computation needs no scheduling wrapper.
4. Use the [probe](../scripts/probe/README.md) for additional evidence. A reported
   uncontrolled call is useful negative evidence; zero bugs is not proof that
   arbitrary hidden concurrency is controlled. Deadlocks and invariant failures
   must be diagnosed, not reclassified as "supported".
