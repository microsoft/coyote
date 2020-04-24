## v1.0.5
- Add a --version argument to the `coyote` command line tool.
- Add a dotnet tool package called `Microsoft.Coyote.CLI` to install the `coyote` command line tool
  and running it without an explicit path.
- Exposed the `ReadableTrace` and `ReproducableTrace` members of
  `Microsoft.Coyote.SystematicTesting.TestingEngine` as public.
- Fixed a bug in activity coverage reporting for `netcoreapp3.1`.
- Fixed some bugs in parallel testing.

## v1.0.4
- Add new `Microsoft.Coyote.Configuration.WithReplayStrategy` method for programmatically assigning
  a trace to replay.
- Add support for the `netstandard2.1`, `netcoreapp3.1` and `net48` targets.
- Removed support for the `netcoreapp2.2` target, which reached end of life.
- Various bug fixes in documentation.

## v1.0.3
- Fixed an issue when invoking `Microsoft.Coyote.Tasks.Task.ExploreContextSwitch` during a
  production run.

## v1.0.2
- Make ActorRuntimeLogGraphBuilder public.
- Add CreateStateMachine to IActorRuntimeLog.

## v1.0.1
- Fixes an issue in the runtime (there should always be a default task runtime instance).

## v1.0.0
- The initial release of the Coyote framework and test tools.