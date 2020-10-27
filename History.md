## v1.2.0
- Added support for systematically testing actors and tasks together using rewriting.
- Hardened the systematic testing runtime.

## v1.1.5
- Improved detection of uncontrolled tasks during systematic testing.
- Added detection of invoking unsupported APIs during systematic testing.

## v1.1.4
- Added missing `coyote rewrite` dependencies in the `Microsoft.Coyote.Test` package.

## v1.1.3
- Optimizations and fixes in binary rewriting.

## v1.1.2
- Added basic support for the `System.Threading.Tasks.Parallel` type during rewriting.
- Fixed a bug in `coyote rewrite` that was incorrectly copying dependencies after rewriting.

## v1.1.1
- Renamed `TestingEngine.ReproducibleTrace` to fix typo in the API name.
- Fixed some bugs in `coyote rewrite`.

## v1.1.0
- Added experimental support for testing unmodified task-based programs using binary rewriting.
- Added support for log severity in the logger and converted to an `ILogger` interface.
- Optimized various internals of the task testing runtime.

## v1.0.17
- Fixed a bug in the `Actor` logic related to event handlers.
- Fixed a bug in `Microsoft.Coyote.Task.WhenAny`.

## v1.0.16
- Added support for cancellations in `Task.Run` APIs.
- Optimized various internals of the task testing runtime.

## v1.0.15
- Fixed the `Task.WhenAny` and `Task.WhenAll` APIs so that they execute asynchronously during
  systematic testing.
- Fixed the `Task.WhenAny` and `Task.WhenAll` APIs so that they throw the proper argument exceptions
  during systematic testing.

## v1.0.14
- Added missing `Task<TResult>.UncontrolledTask` API.
- Fixed a bug in the testing runtime for controlled tasks.

## v1.0.13
- Fixed a bug in the testing runtime for controlled tasks that could lead to a stack overflow.
- Optimized various internals of the testing runtime.

## v1.0.12
- Introduced a new `EventGroup` API for actors, which replaces operation groups, that allows
  improved tracing and awaiting of long running actor operations.
- The `Task.Yield` API can now be used to de-prioritize the executing operation during testing.
- Added missing APIs in the `Microsoft.Coyote.Tasks.Semaphore` type.
- Fixed two bugs in the systematic testing scheduler.

## v1.0.11
- Fixed an issue that did not allow systematic and non-systematic unit tests to run on the same
  process.
- Fixed a bug in the `TestingEngine` logger.

## v1.0.10
- Fixed the NuGet symbol packages.

## v1.0.9
- Introduced a new `Microsoft.Coyote.Test` package that contains the `Test` attribute and the
  `TestingEngine` type for writing unit tests.
- The core `Microsoft.Coyote` does not contain anymore `Test` and `TestingEngine`, which were moved
  to the `Microsoft.Coyote.Test` package.
- Added support for optional anonymized telemetry in the `TestingEngine`.
- Optimized various internals of the systematic testing scheduler.
- Fixed some issues in the scripts.

## v1.0.8
- The core `Microsoft.Coyote` project is now targeting only .NET Standard, allowing it to be
  consumed by any project that supports `netstandard2.0` and above.
- Removed the `net46` target.
- Fixed bug in using the global dotnet tool.

## v1.0.7
- Added support for building Coyote on Linux and macOS.
- Building Coyote locally now ignores .NET targets that are not installed.
- Added optional anonymized telemetry in the `coyote` tool.
- Fixed a bug in the `SynchronizedBlock` type.

## v1.0.6
- Added a `SynchronizedBlock` type to model the semantics of the C# `lock` statement.
- Simplified the `Configuration` APIs for setting max-steps and liveness related heuristics.
- Fixed code coverage and added support for code coverage on `netcoreapp3.1`.

## v1.0.5
- Added a --version argument to the `coyote` command line tool.
- Added a dotnet tool package called `Microsoft.Coyote.CLI` to install the `coyote` command line
  tool and running it without an explicit path.
- Exposed the `ReadableTrace` and `ReproducibleTrace` members of
  `Microsoft.Coyote.SystematicTesting.TestingEngine` as public.
- Fixed a bug in activity coverage reporting for `netcoreapp3.1`.
- Fixed some bugs in parallel testing.

## v1.0.4
- Added new `Microsoft.Coyote.Configuration.WithReplayStrategy` method for programmatically
  assigning a trace to replay.
- Added support for the `netstandard2.1`, `netcoreapp3.1` and `net48` targets.
- Removed support for the `netcoreapp2.2` target, which reached end of life.
- Fixed various bugs in the documentation.

## v1.0.3
- Fixed an issue when invoking `Microsoft.Coyote.Tasks.Task.ExploreContextSwitch` during a
  production run.

## v1.0.2
- Made ActorRuntimeLogGraphBuilder public.
- Added CreateStateMachine to IActorRuntimeLog.

## v1.0.1
- Fixed an issue in the runtime (there should always be a default task runtime instance).

## v1.0.0
- The initial release of the Coyote set of libraries and test tools.