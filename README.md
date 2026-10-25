# `Coyote`

[![NuGet](https://img.shields.io/nuget/v/Microsoft.Coyote.svg)](https://www.nuget.org/packages/Microsoft.Coyote/)
[![Nuget](https://img.shields.io/nuget/dt/Microsoft.Coyote?color=informational)](https://www.nuget.org/packages/Microsoft.Coyote/)
![Build and Test CI](https://github.com/microsoft/coyote/actions/workflows/test-coyote.yml/badge.svg?branch=main)
![CodeQL](https://github.com/microsoft/coyote/actions/workflows/codeql-analysis.yml/badge.svg?branch=main)

Coyote is a cross-platform library and tool for testing concurrent C# code and deterministically reproducing bugs.

Using Coyote, you can easily test the *concurrency* and other *nondeterminism* in your C# code, by
writing what we call a *concurrency unit test*. These look like your regular unit tests, but can
reliably test concurrent workloads (such as actors, tasks, or concurrent requests to ASP.NET
controllers). In regular unit tests, you would typically avoid concurrency due to flakiness, but
with Coyote you are encouraged to embrace concurrency in your tests to find bugs.

Coyote is used by many teams in [Azure](https://azure.microsoft.com/) to test their distributed
systems and services, and has found hundreds of concurrency-related bugs before deploying code
in production and affecting users. In the words of an Azure service architect:
> Coyote found several issues early in the dev process, this sort of issues that would usually bleed
> through into production and become very expensive to fix later.

Coyote is made with :heart: by Microsoft Research.

## How it works

Consider the following simple test:
```csharp
[Fact]
public async Task TestTask()
{
  int value = 0;
  Task task = Task.Run(() =>
  {
    value = 1;
  });

  Assert.Equal(0, value);
  await task;
}
```

This test will pass most of the time because the assertion will typically execute before the task
starts, but there is one schedule where the task starts fast enough to set `value` to `1` causing
the assertion to fail. Of course, this is a very naive example and the bug is obvious, but you could
imagine much more complicated race conditions that are hidden in complex execution paths.

The way Coyote works, is that you first convert the above test to a concurrency unit test using the
Coyote `TestingEngine` API:
```csharp
using Microsoft.Coyote;
using Microsoft.Coyote.SystematicTesting;

[Fact]
public async Task CoyoteTestTask()
{
  var configuration = Configuration.Create().WithTestingIterations(10);
  using var engine = TestingEngine.Create(configuration, TestTask);
  engine.Run();
  engine.ThrowIfBugFound();
  Assert.Equal(ExplorationStatus.Complete, engine.TestReport.GetVerdict().Status);
}
```

Next, you run the `coyote rewrite` command from the CLI (typically as a post-build task) to
automatically rewrite the IL of your test and production binaries. This allows Coyote to inject
hooks that take control of the concurrent execution during testing.

You can then run the concurrent unit test from your favorite unit testing framework (such as
[xUnit](https://xunit.net/)). Coyote will take over and repeatedly execute the test from
beginning to the end for N iterations (in the above example N was configured to `10`). Under the
hood, Coyote uses intelligent search strategies to explore all kinds of execution paths that might
hide a bug in each iteration.

The awesome thing is that once a bug is found, Coyote gives you a trace through the `engine.TestReport`
API that you can use to reliably *reproduce* the bug as many times as you want, making debugging and
fixing the issue significantly easier.

## Exploration verdicts and CI

This branch adds exploration verdicts, strict CLI options and machine-readable JSON reports. These
additions are not included in the published `1.7.11` packages. Use the CLI and libraries built from
the same revision; see [building from source](docs/get-started/build-source.md).

Finding zero bugs does not necessarily mean that a test explored useful interleavings. For example,
execution paths can reach a scheduling-step limit, or the test can execute without making any
scheduling decisions. Inspect the exploration verdict as well as the bug count:

| Verdict | Meaning |
| --- | --- |
| `Complete` | No bug or reported exploration limitation was detected under the configured strategy and bounds. |
| `Incomplete` | No bug was found, but exploration had limitations listed in `reasons`. |
| `BugFound` | At least one bug was found; `bugs.reports` contains the diagnostic messages. |
| `InternalError` | The test report contains an internal testing error. |

`Complete` does not prove that every possible execution was explored. A `BugFound` verdict can also
include incomplete reasons that affect confidence in reproducing the bug. Internal errors and bugs
take priority over an incomplete verdict.

For CI, rewrite the test and production binaries as described above, then enable strict exploration
and JSON output:

```plain
coyote rewrite path/to/Tests.dll
coyote test path/to/Tests.dll --iterations 1000 --strict --json-report
```

The CLI normally returns `0` when no bugs were found, `2` when a bug was found, and `3` for an
internal testing error. With `--strict`, an `Incomplete` verdict returns `4` instead of `0`.
Command or setup errors return `1`. Treat nonzero exit codes as unsuccessful test runs.

The following rewritten test cases illustrate the difference from the published `1.7.11` CLI:

| Test case | Published `1.7.11` | This branch with `--strict` |
| --- | --- | --- |
| Normal scheduling, no bugs, no limitations | Exit `0` | `Complete`, exit `0` |
| Paths truncated at the scheduling-step limit, no bugs | Exit `0`; text reports the limit being reached | `Incomplete`, reason `TruncatedExecutionPaths`, exit `4` |
| Test executes but makes no scheduling decisions | Exit `0`; text reports zero scheduling steps | `Incomplete`, reason `NoSchedulingDecisions`, exit `4` |

Other incomplete reasons include `UncontrolledInvocations`, `NoExecutionPaths`,
`InsufficientExecutionPaths`, `TestingCanceled`, `ExecutionInterrupted` and `TestAssemblyNotRewritten`. A strategy that reports
exhaustion can finish before the requested iteration count without adding `InsufficientExecutionPaths`;
other limitations still apply. `InsufficientExecutionPaths` is only reported when no timeout is configured.

`reasons` identify exploration limitations, while `warnings` do not change the verdict by themselves.
For example, fair paths that reach the unfair step bound normally produce an
`ExceededUnfairStepsBound` warning; `--strict-bounds` enables strict exploration and treats this as an
incomplete reason instead. In-process callers can use `Configuration.WithStrictBoundCheckingEnabled()`
to apply the same warning-to-reason rule. A timeout before any paths were explored adds `NoExecutionPaths`
and `TestingTimedOut` reasons. Reaching the time budget after exploring paths produces a `TestingTimedOut`
warning; other incomplete reasons can still prevent a `Complete` verdict.
Uncontrolled concurrency also adds `UncontrolledInvocations` when the runtime cannot identify a source
method, so the list of named uncontrolled calls can be empty. A step bound of zero is disabled.
Paths that finish normally exactly at their step limit are not counted as truncated.
An ignored potential deadlock or an unexpected `TaskCanceledException` that ends exploration while
the engine has not been canceled records `ExecutionInterrupted`.
A replay scheduling mismatch records an internal error instead of a successful exploration.
With `--json-report`, an internal setup failure also attempts to emit its diagnostic report before
returning the original command error (`1`); report emission does not hide the original exception.

In-process callers can inspect `engine.TestReport.GetVerdict()` and obtain JSON with
`engine.GetJsonReport()`. As shown in the unit-test example above, assert the required verdict explicitly;
`ThrowIfBugFound()` preserves the bug diagnostic, while the verdict assertion also rejects incomplete
exploration. The strict CLI exit-code policy does not automatically fail an in-process test wrapper.
To emit report files in-process, enable `Configuration.WithJsonReportEnabled()` and call
`engine.TryEmitReports(...)` with an existing output directory. `GetJsonReport()` returns the JSON string
without the `artifacts` field and does not emit files.

## JSON reports

`--json-report` emits a `.report.json` file alongside the other reports. Its main fields are:

| Field | Contents |
| --- | --- |
| `coyoteVersion`, `testName` | Coyote assembly version and test method name, when available. |
| `assembly` | Test-entry assembly name, path, SHA-256 and `isRewritten` flag. |
| `settings` | Strategy, seed, requested iterations, time and step bounds, and strict-mode settings. |
| `verdict` | `status`, incomplete `reasons`, and non-fatal `warnings`. |
| `bugs`, `internalErrors`, `uncontrolledInvocations` | Bug count and messages, internal errors, and detected uncontrolled calls. |
| `uncontrolledConcurrencyDetected` | Whether the runtime detected uncontrolled concurrency, including when its source method is unknown. |
| `exploration` | Fair, unfair and unique path counts, truncation counts, the number of visited state hashes, scheduling steps, controlled operations and concurrency statistics. |
| `artifacts` | Paths to emitted traces and other diagnostic reports, when present. |
| `elapsedSeconds` | Execution time measured by the testing engine. |

Statistics such as scheduling steps use `min`, `avg` and `max`; averages are integer values. Null
fields are omitted. `coyoteVersion` is an assembly version, not a source revision identifier.
The assembly hash identifies the test entry, not the Coyote engine or every dependency, and
`isRewritten` does not prove that all concurrent APIs were controlled. State counts depend on the
configured state hashing and are not a business-state coverage percentage.
JSON verdicts and statistics use a snapshot of the accumulated results. The CLI uses the same snapshot
for these JSON fields, the console summary and exit code, including when a background iteration finishes
after a timeout. Trace and coverage artifacts are emitted separately from this statistics snapshot.

The path and concurrency statistics were already available in text reports; JSON makes them directly
usable by automation. The new verdict and strict exit code distinguish "no bugs found" from incomplete
exploration. For more details, see [machine-readable results](docs/get-started/using-coyote.md#machine-readable-results).

## Get started

Getting started with Coyote is easy! First, follow this
[guide](https://microsoft.github.io/coyote/#get-started/install/) to install the `coyote`
command-line tool from [NuGet](https://www.nuget.org/packages/Microsoft.Coyote/). You are now ready
to check out the Coyote [website](https://microsoft.github.io/coyote/) for tutorials, documentation,
how-tos, samples and more information about the project. Enjoy!

Upgrading your `coyote` dependencies? Check the changelog [here](History.md).

## Support

Note that Coyote is an open-source project that is provided "as-is". We are not able to provide
any formal support. For Microsoft employees we have the [Friends of Coyote Teams
channel](https://teams.microsoft.com/l/channel/19%3a1fe966b4fdc544bca648d89bf25c3c56%40thread.tacv2/General?groupId=7a6d8afc-c23d-4e5d-b9cb-9124118c0220&tenantId=72f988bf-86f1-41af-91ab-2d7cd011db47), which is an internal community that can help answer questions and
learn from each other.

## Contributing

This project welcomes contributions and suggestions. Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a
CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repositories using our CLA.

## Code of Conduct

This project has adopted the [Microsoft Open Source Code of
Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of
Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact
[opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
