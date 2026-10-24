---
name: coyote
description: >
  Write, run, and troubleshoot Microsoft.Coyote systematic concurrency tests in
  .NET applications. Covers version detection, binary rewriting, controlled APIs,
  deterministic scenarios, safety and liveness monitors, scheduling strategies,
  deadlock diagnosis, CI, and bug replay. Use when adding a Coyote test, investigating
  a concurrency failure, or checking whether an API can be controlled. Supports
  Coyote 1.7.11 only; do not assume compatibility with other versions.
---

# Microsoft.Coyote systematic testing

Coyote explores scheduling and controlled nondeterministic choices in concurrent
.NET code, checks specifications, and records failing schedules for replay.
It performs bounded exploration: a clean run is not a proof of correctness.

## 1. Establish the version before changing a project

This plugin supports **Coyote 1.7.11 only**. Read
[references/compatibility.md](references/compatibility.md) first. Inspect the
consumer's resolved Coyote package versions, tool manifest, target framework, and
rewriting/test commands. Keep library and CLI versions aligned. Do not silently
upgrade dependencies or apply these API tables to an unsupported version.

## 2. Setup and rewriting

- `Microsoft.Coyote` is the NuGet meta-package; consumers can alternatively
  reference `Microsoft.Coyote.Core` and `Microsoft.Coyote.Test` explicitly.
  `TestingEngine` is in `Microsoft.Coyote.SystematicTesting`.
- Prefer a version-pinned repo-local CLI. If the consumer has no tool manifest,
  create one with `dotnet new tool-manifest`, then install:

  ```text
  dotnet tool install --local Microsoft.Coyote.CLI --version 1.7.11
  ```

  Existing manifests should be inspected, not replaced. Collaborators restore
  tools with `dotnet tool restore` and invoke `dotnet tool run coyote -- ...`.
- Rewrite both the scenario and relevant production assemblies before testing.
  Coyote redirects supported framework calls to its scheduler. An unrewritten
  dependency can introduce uncontrolled work even if the entry assembly is rewritten.
- Keep original build output separate from rewritten output. Copy a fresh build
  closure into a dedicated directory and rewrite the copy; incremental builds do
  not reliably replace externally modified assemblies. Never run ordinary tests
  against instrumented binaries.
- Drive tests in-process with `TestingEngine` from the existing test framework,
  or use a Coyote test entry point with `coyote test`. For standalone CLI tests,
  a public static method with `[Microsoft.Coyote.SystematicTesting.Test]` is a
  straightforward entry point. Check `test --help` for invocation options.

For first-time setup or an executable example, use the
[consumer walkthrough](references/consumer-walkthrough.md). It covers package
references, rewriting test and production assemblies, completion-aware gates,
and saving and replaying a failing schedule.

Run in-process Coyote fixtures serially; do not run multiple testing engines
concurrently. Normal, uninstrumented tests can retain their usual parallelism.

## 3. Control nondeterminism without changing the property

Read [references/supported-apis.md](references/supported-apis.md) for the
version-specific API map. A listed type does not imply every overload is controlled.

- **Delays:** rewritten `Task.Delay`, `Task.Yield`, and `Thread.Sleep` have
  controlled implementations in 1.7.11. Do not label them inherently uncontrolled.
  Controlled delays model scheduling, not precise wall-clock passage.
- **Time and timers:** `DateTime.Now`/`UtcNow`, `Stopwatch`, and real timers do not
  become a domain clock. Inject a virtual clock when testing deadlines, expiry,
  or backoff; explicitly model advancing time and triggering callbacks.
- **Randomness and IDs:** use `Microsoft.Coyote.Random.Generator`, controlled
  runtime choices, or deterministic injected sources. Real `Guid.NewGuid()` is
  data-nondeterministic; arbitrary random libraries are not automatically controlled.
- **External I/O:** fake network, database, filesystem, and process boundaries
  in-process. Preserve the semantics relevant to the property, such as atomicity,
  ETags, transactions, errors, and cancellation.
- **Scheduling inside fakes:** `await Task.Yield()` in rewritten code can expose
  interleavings within a fake operation. It does not inherently deadlock ordinary
  unrewritten async code. Use deliberate gates for fault injection when needed,
  but do not accidentally script away the race being explored.
- **Strictness:** partially controlled concurrency and data nondeterminism are
  allowed by default in 1.7.11. Disable both allowances for a strict gate, as
  above. Strict mode catches detected violations; it cannot certify arbitrary
  third-party code as fully controlled.

## 4. Effective scenarios and specifications

Keep each scenario small and focused on one race hypothesis. Start competing
operations and let Coyote explore their ordering rather than using sleeps to
force a particular schedule. Await all relevant work and reset mutable test state
at the start of each iteration.

**Keep memory-access interleaving off by default.** Coyote's default scheduling
points are useful without exploring every field access. Finer-grained exploration
can substantially increase overhead and the schedule space; do not enable it as
routine setup or infer that a clean default run covers every memory race. See
[scheduling granularity](references/supported-apis.md#scheduling-granularity).

Assert the actual property, such as "at most one writer commits" or "a reader
never observes a partial write", not incidental output.

Use `Specification.Assert` for local invariants. For shared event-driven
invariants, derive from `Microsoft.Coyote.Specifications.Monitor`, register with
`Specification.RegisterMonitor<TMonitor>()` inside the scenario, and emit events
with `Specification.Monitor<TMonitor>(event)`. The monitor checks only events you
actually report; it does not automatically observe every action or establish
linearizability without an appropriate specification.

Monitors can express safety with assertions and liveness with hot/cold states.
Hot means an obligation remains outstanding; cold means it has been satisfied.
See the [monitor example](references/consumer-walkthrough.md#monitor-example)
when implementing event-driven safety or liveness checks.

## 5. Exploration budgets, strategies, and liveness

- **Iterations:** `WithTestingIterations(n)` controls schedules attempted.
  Hundreds of iterations of a tiny focused scenario can be more useful than a
  large, poorly specified scenario. Record the seed and bounds.
- **Length:** `WithMaxSchedulingSteps(n)` sets the unfair step bound and a fair
  bound of ten times that value. Defaults are 10,000 unfair / 100,000 fair steps.
  By default a bound can truncate an iteration without a bug. Consider
  `WithFailureOnMaxStepsBoundEnabled()` / `--fail-on-max-steps` when completion is
  required, and check that the scenario reached its final assertion.
- **Strategies:** 1.7.11 defaults to a fair portfolio. Explicit strategy selection
  disables the portfolio. `WithRandomStrategy()` is a useful baseline;
  `WithPrioritizationStrategy(isFair: true, priorityChangeBound: 10)` adds fair
  probabilistic-concurrency-testing exploration. The CLI also exposes these via
  `--strategy` and portfolio selection via `--portfolio-mode`.
- **Liveness:** temperature checking during execution uses fair scheduling.
  The threshold defaults to half the fair step bound unless explicitly set with
  `WithLivenessTemperatureThreshold(t)`. Ending an execution with a monitor still
  hot also reports a liveness bug. Ensure bounds accommodate legitimate progress;
  threshold failures alone do not prove a system can never make progress.

## 6. Diagnose deadlocks without dismissing real bugs

"No controlled operations are enabled" can mean a real synchronization deadlock,
an awaited dependency nobody completes, or uncontrolled work. A potential-hang
timeout can also indicate slow execution. The message alone does not decide which.

1. Verify that the test is using freshly rewritten output and the intended package
   and CLI versions. Inspect the full report, internal errors, and uncontrolled
   invocation warnings.
2. Check the awaited tasks, locks, gates, and their producers. Inspect unsupported
   APIs and dependencies not covered by rewriting; use strict mode.
3. Replay the failing schedule. If necessary, reduce the scenario while preserving
   the dependency cycle or missing completion.
4. A direct run against uninstrumented code can help inspect behavior, but passing
   once does not disprove a race or deadlock. Avoid sync-over-async as a workaround.
5. Do not suppress deadlock reporting merely to obtain zero bugs. If a scenario is
   deliberately abandoned or truncated, disclose that its final assertions were
   not checked. Raise hang timeouts only when justified by observed slow work.

## 7. Regression gates and replay

A reliable gate builds, rewrites, runs, and fails on reported bugs or internal
errors. Preserve the seed, configuration, rewritten build identity, and
`engine.ReproducibleTrace`. Replay with
`Configuration.Create().WithReproducibleTrace(trace)` and the same scenario and
binaries; do not treat a different build or changed scenario as a valid replay.

Before crediting a regression test, temporarily enable the bug it guards, rebuild
and rewrite, and require the expected invariant to fail. Restore the fix, rebuild
and rewrite again, and require the scenario to complete cleanly.

The optional [API probe](scripts/probe/README.md) checks a focused snippet in
strict mode. Its `-SelfTest` gate checks controlled delays and atomic updates,
intentional race detection and replay, and rejection of known uncontrolled calls.
A clean probe is evidence about exercised paths, not a universal API-support verdict.
