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
- Exposed the `ReadableTrace` and `ReproducableTrace` members of
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
- Make ActorRuntimeLogGraphBuilder public.
- Added CreateStateMachine to IActorRuntimeLog.

## v1.0.1
- Fixed an issue in the runtime (there should always be a default task runtime instance).

## v1.0.0
- The initial release of the Coyote framework and test tools.