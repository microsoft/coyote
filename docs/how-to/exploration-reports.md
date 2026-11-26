## Check exploration verdicts and reports

Finding zero bugs does not necessarily mean that a test explored useful interleavings. The test
might have invoked APIs that Coyote does not control, some iterations might have been truncated,
or the test might not make any scheduling decisions. Coyote summarizes these conditions as an
_exploration verdict_, and can emit it with the exploration statistics as a machine-readable JSON
report.

The exploration verdict API, strict CLI options, and JSON reports described here are available
in the current source, but not in the published `1.7.11` packages. Use the CLI and libraries
[built from the same source revision](../get-started/build-source.md).

### Produce an exploration report

Run the `coyote` tester on a [rewritten](../concepts/binary-rewriting.md) assembly that has a
[test entry point](../get-started/using-coyote.md#test-entry-point):

```plain
coyote test path/to/Tests.dll --iterations 1000 --json-report --strict
```

The tester prints the verdict at the end of the test report. `--json-report` also writes a
`.report.json` file next to the other reports, and `--strict` fails the run when exploration is
incomplete. To check the verdict from a unit test, see [in-process reporting](#in-process-reporting).

### Verdicts

| Verdict | Meaning |
| --- | --- |
| `Complete` | No bugs, internal errors, or incomplete reasons were reported. Warnings can still be present. |
| `Incomplete` | No bug was found, but exploration had limitations listed in `reasons`. |
| `BugFound` | At least one bug was found; `bugs.reports` contains the diagnostic messages. |
| `InternalError` | The test report contains an internal testing error. |

A `Complete` verdict does not prove that every possible program execution was explored.
A `BugFound` verdict can also include incomplete reasons that affect confidence in reproducing
the bug. Internal errors take priority over bugs, and bugs take priority over incomplete exploration.
`reasons` identify exploration limitations; `warnings` do not change the verdict by themselves.

### Exit codes and strict mode

By default, the `coyote` tester exits with code `0` when no bugs were found, `2` when a bug was
found, and `3` on an internal testing error. Command or setup errors return `1`.
Treat all nonzero exit codes as unsuccessful runs. Exit code `0` does not by itself mean that
the exploration was exhaustive. The `--strict` flag makes the tester exit with code `4` when the
verdict is `Incomplete`. A detected bug or an internal error retains its verdict and exit-code
priority.

The `--strict-bounds` flag enables strict exploration and additionally treats fair execution paths
that exceeded the unfair `--max-steps` bound as incomplete exploration (`ExceededUnfairStepsBound`),
which is otherwise only reported as a warning.

### Incomplete reasons and warnings

Fair paths that finish normally exactly at the unfair bound do not produce the
`ExceededUnfairStepsBound` warning
or incomplete reason; only paths with more decisions than that positive bound are counted.
A step bound of zero is disabled. Paths that finish normally exactly at their step limit are
not counted as truncated (`TruncatedExecutionPaths`).

Tests that execute without scheduling decisions add `NoSchedulingDecisions`, including tests
that only make random choices. Uncontrolled concurrency adds `UncontrolledInvocations` even
when the runtime cannot identify a source method, so the list of named uncontrolled calls can
be empty.

An exploration that executes no paths is `Incomplete` with reason `NoExecutionPaths`, including
when `--iterations 0` is used or test initialization exceeds the timeout. If the timeout expires
before any paths were explored, `TestingTimedOut` is also an incomplete reason. After paths were
explored, reaching the testing time budget is instead a `TestingTimedOut` warning. Such a run
can be `Complete` if no other incomplete reasons were detected; uncontrolled invocations,
truncated paths and other limitations still make it `Incomplete`. A call to `TestingEngine.Stop`
is reported as reason `TestingCanceled` even if some paths were explored.

An ignored potential deadlock or an unexpected `TaskCanceledException` that ends exploration while
the engine has not been canceled adds `ExecutionInterrupted`, even when some paths were explored.
A replay scheduling mismatch or normal completion with unconsumed trace steps records an internal
error and its diagnostic. When an internal setup exception aborts the command, `--json-report`
also attempts to emit the failure report; the command
keeps its original error exit code (`1`), and a secondary report-writing failure does not replace
the original exception.

If the test entry assembly has no rewriting signature matching the current Coyote version,
the engine records `TestAssemblyNotRewritten`. In the absence of a bug or internal error,
the verdict is `Incomplete`, even when an explicit scheduling point produces nonzero step counts.
This also applies to actor-only tests and tests run through an in-process delegate: the delegate's
actual assembly is checked.
Testing continues so bugs and reports are still available. A run with no bug or internal error
exits with code `4` with `--strict`, or `0` without it. Automation should accept the new reason
in the JSON report. A matching signature only identifies the test entry assembly; it does not
prove that every dependency or native concurrent API was rewritten and controlled.

The internal DFS strategy can report that no execution paths remain before the requested iteration
budget is reached. The engine then omits `InsufficientExecutionPaths`; other incomplete reasons
and warnings still apply. `InsufficientExecutionPaths` is only reported when no timeout is configured.
When reports are merged, exhaustion is retained only if every report with explored paths recorded
it. Reports with no explored paths are neutral for exhaustion,
but their recorded limitations still apply. Merged verdicts use the destination configuration
and aggregate path counts, so they do not establish that each contributing run met its own budget.

### JSON reports

`--json-report` emits a `.report.json` file next to the other reports for use by automation.
The path and concurrency statistics are also available in text reports. The main JSON fields are:

| Field | Contents |
| --- | --- |
| `coyoteVersion`, `testName` | Coyote assembly version and test method name, when available. |
| `assembly` | Test-entry assembly name, path, SHA-256 and `isRewritten` flag. |
| `settings` | Strategy, initial seed (including one generated by Coyote), requested iterations, time and step bounds, and strict-mode settings. |
| `verdict` | `status`, incomplete `reasons`, and non-fatal `warnings`. |
| `bugs`, `internalErrors`, `uncontrolledInvocations` | Bug count and messages, internal errors, and detected uncontrolled calls. |
| `uncontrolledConcurrencyDetected` | Whether the runtime detected uncontrolled concurrency, including when its source method is unknown. |
| `exploration` | Fair, unfair and unique path counts, truncation counts, visited state hashes, scheduling steps, controlled operations and concurrency statistics. |
| `artifacts` | Paths to emitted traces and other diagnostic reports, when present. |
| `elapsedSeconds` | Execution time measured by the testing engine. |

Statistics such as scheduling steps use `min`, `avg` and `max`; averages are integer values.
Null fields are omitted. `coyoteVersion` is an assembly version, not a source revision identifier.
The assembly hash identifies the test entry, not the Coyote engine or every dependency, and
`isRewritten` does not prove that all concurrent APIs were controlled. State counts depend on
the configured state hashing and are not a business-state coverage percentage.

When a run starts with a portfolio, `settings.strategy` is `"portfolio"` and
`settings.portfolioStrategies` lists its initial members in scheduling order. Each member has a
`strategy` name and, if applicable, a `strategyBound`. Single-strategy reports keep
`settings.strategyBound`; portfolio reports omit it because each member has its own settings.

The timeout ends the wait for testing; it does not synchronously terminate arbitrary user
initialization or an iteration still running in the background. JSON verdicts and statistics use
a snapshot of the accumulated results. The CLI uses the same snapshot for these JSON fields,
the console summary, and the exit code, even if a background iteration finishes after a timeout.
Trace and coverage artifacts are emitted separately from this statistics snapshot. Separate
in-process report calls can read different snapshots if background iterations update the results.

### In-process reporting

Inspect `engine.TestReport.GetVerdict()` and obtain JSON with `engine.GetJsonReport()`.
Assert the required verdict explicitly in a unit-test wrapper:
`engine.ThrowIfBugFound()` preserves the bug diagnostic, but does not reject incomplete exploration.
The strict CLI exit-code policy does not automatically fail an in-process test wrapper.
Use `Configuration.WithStrictBoundCheckingEnabled()` to apply the same warning-to-reason rule
as `--strict-bounds`.

To emit JSON report files in-process, enable `Configuration.WithJsonReportEnabled()` and call
`engine.TryEmitReports(...)` with an existing output directory. `GetJsonReport()` returns a JSON
string without the `artifacts` field and does not emit files.

### Example: continuous integration

To gate a CI pipeline on the exploration verdict, rewrite the test and production binaries, then run
the tester with `--strict` and treat every nonzero exit code as a failed run. When the test and the
code under test are in the same assembly:

```plain
coyote rewrite path/to/Tests.dll
coyote test path/to/Tests.dll --iterations 1000 --strict --json-report
```

To rewrite multiple assemblies, list them in a [rewriting
configuration](../concepts/binary-rewriting.md#configuration) and [test the rewritten
assembly](../get-started/using-coyote.md#rewrite-your-binaries-for-testing). Publish the
`.report.json` file as a build artifact to inspect the verdict, reasons and statistics of each run.
