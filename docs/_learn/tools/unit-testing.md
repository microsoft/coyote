---
layout: reference
title: Unit testing with the Coyote systematic tester
section: learn
permalink: /learn/tools/unit-testing
---

## Unit testing with the Coyote systematic tester

Common unit testing frameworks like
[MSTest](https://docs.microsoft.com/en-us/dotnet/core/testing/unit-testing-with-mstest),
[xUnit.net](https://xunit.net/) and [nunit](https://nunit.org/) cannot easily call the `coyote`
command line tool for testing. In this case you can use the Coyote `TestingEngine` directly.

The Coyote `TestingEngine` is included in the `Microsoft.Coyote.Test` package. The following shows
a complete example using xUnit. The project simply includes xUnit and the Coyote packages:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netcoreapp3.1</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Coyote" Version="1.0.9" />
    <PackageReference Include="Microsoft.Coyote.Test" Version="1.0.9" />
    <PackageReference Include="xunit" Version="2.4.1" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.4.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
  </ItemGroup>
</Project>
```

And then your `[Fact]` method which runs as an xUnit unit test can create a `TestingEngine` to run a
Coyote test method.  This test code can run in the Visual Studio Test Explorer or from a `dotnet
test` command line:

```c#
public class Test
{
    ITestOutputHelper Output;

    public Test(ITestOutputHelper output)
    {
        this.Output = output;
    }


    [Fact(Timeout = 5000)]
    public void RunCoyoteTest()
    {
        var config = Configuration.Create();
        TestingEngine engine = TestingEngine.Create(config, CoyoteTestMethod);
        engine.Run();
        var report = engine.TestReport;
        Output.WriteLine("Coyote found {0} bug.", report.NumOfFoundBugs);
    }

    private async Task CoyoteTestMethod()
    {
        // This is running as a Coyote test.
        await Task.Delay(10);
        Specification.Assert(false, "This test failed!");
    }
}
```

This will produce the following test output because the `Specification.Assert` is hard wired to
fail.

```
Coyote found 1 bug.
```

Most of the command line options you see on `coyote test` are available in the `Configuration`
class. Use the `With*` helper methods to set the various configurations, for example, to specify
`--sch-pct 10` use the following:

```c#
var config = Configuration.Create().WithPCTStrategy(false, 10);
```

For `--iterations` use `WithTestingIterations`. The `--graph` option maps to the `Configuration`
method `WithDgmlGraphEnabled`, while the `--coverage` option maps to `WithActivityCoverageEnabled`.
The `--xml-trace` option becomes `WithXmlLogEnabled` and so on.

If you want the rich Coyote log files, you can use the `TryEmitTraces` method on the `TestingEngine`
to produce those log files in the folder of your choice like this:

```c#
List<string> filenames = new List<string>(engine.TryEmitTraces("d:\\temp\\test", "mytest"));
foreach (var item in filenames)
{
    Output.WriteLine("See log file: {0}", item);
}
```

Note: `TryEmitTraces` is an iterator method, which means you must iterate the result in order to
produce the log files.  You will see the following output:
```
Coyote found 1 bugs
See log file: d:\temp\test\mytest_0.txt
See log file: d:\temp\test\mytest_0.schedule
```

And the log file contains the familiar output of `coyote test` as follows:

```xml
<TestLog> Running test.
<ErrorLog> This test failed!
<StackTrace>    at Microsoft.Coyote.SystematicTesting.OperationScheduler.NotifyAssertionFailure(String text,
Boolean killTasks, Boolean cancelExecution)
   at Microsoft.Coyote.SystematicTesting.ControlledRuntime.Assert(Boolean predicate, String s, Object[] args)
   at ConsoleApp14.Test.CoyoteTestMethod()
   at System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start[TStateMachine](TStateMachine stateMachine)
   at ConsoleApp14.Test.CoyoteTestMethod()
   at Microsoft.Coyote.SystematicTesting.ControlledRuntime.c__DisplayClass21_0.RunTestb__0d.MoveNext()
   at System.Runtime.CompilerServices.AsyncMethodBuilderCore.Start[TStateMachine](TStateMachine stateMachine)
   at Microsoft.Coyote.SystematicTesting.ControlledRuntime.c__DisplayClass21_0.RunTestb__0()
   at System.Threading.Tasks.Task.InnerInvoke()
   at System.Threading.Tasks.Task.c.cctorb__274_0(Object obj)
   at System.Threading.ExecutionContext.RunFromThreadPoolDispatchLoop(Thread threadPoolThread,
   ExecutionContext executionContext, ContextCallback callback, Object state)
   at System.Threading.Tasks.Task.ExecuteWithThreadLocal(Task currentTaskSlot, Thread threadPoolThread)
   at System.Threading.Tasks.Task.ExecuteEntryUnsafe(Thread threadPoolThread)
   at System.Threading.Tasks.Task.ExecuteFromThreadPool(Thread threadPoolThread)
   at System.Threading.ThreadPoolWorkQueue.Dispatch()
   at System.Threading._ThreadPoolWaitCallback.PerformWaitCallback()

<StrategyLog> Found bug using 'random' strategy.
<StrategyLog> Testing statistics:
<StrategyLog> Found 1 bug.
<StrategyLog> Scheduling statistics:
<StrategyLog> Explored 1 schedule: 1 fair and 0 unfair.
<StrategyLog> Found 100.00% buggy schedules.
<StrategyLog> Number of scheduling points in fair terminating schedules: 3 (min), 3 (avg), 3 (max).
```

The `TestEngine.Create` method has overloads for supporting Coyote test methods with
the following signatures:

```c#
Action
Action<ICoyoteRuntime>
Action<IActorRuntime>
Func<Task>
Func<ICoyoteRuntime, Task>
Func<IActorRuntime, Task>
```

Notice that you never create an `ICoyoteRuntime` or `IActorRuntime` yourself, the `TestingEngine`
will do that for you so it can provide the non-production systematic test version of those runtimes.

## Replaying a trace

You can also easily replay and debug a trace, similar to using `coyote replay` from the command line
tool. To do this you need to configure the `TestingEngine` to run in replay mode:
```c#
var trace = ...
var config = Configuration.Create().WithReplayStrategy(trace);
```
The input to the `WithReplayStrategy` method should either be the contents of a `.schedule` file or
the `string` value of `TestingEngine.ReproducableTrace` (from a previous run).

Then you add breakpoints to debug and replay as follows:

```c#
var trace = ...
var config = Configuration.Create().WithReplayStrategy(trace);
TestingEngine engine = TestingEngine.Create(config, CoyoteTestMethod);
engine.Run();
```

## Testing actors

Actors run asynchronously, so you will need to design your actors in a way such that you know when
they have finished doing what they are supposed to do. One way to do that is to use the Coyote
`TaskCompletionSource<bool>` as follows:

```c#
class TestConfigEvent : Event
{
    public TaskCompletionSource<bool> Completed = TaskCompletionSource.Create<bool>();
}

private async Task CoyoteTestActors(IActorRuntime runtime)
{
    // this method can be run by the Coyote TestingEngine.
    TestConfigEvent config = new TestConfigEvent();
    runtime.CreateActor(typeof(MyTestActor), config);
    await config.Completed.Task;
    Output.WriteLine("Coyote actor test passed");
}
```

Where `MyTestActor` sets the result on the `TaskCompletionSource` as follows:

```c#
[OnEventDoAction(typeof(MyEvent), nameof(HandleEvent))]
class MyTestActor : Actor
{
    TestConfigEvent config;

    protected override System.Threading.Tasks.Task OnInitializeAsync(Event initialEvent)
    {
        config = (TestConfigEvent)initialEvent;
        var actor = this.CreateActor(typeof(MyActor));
        this.SendEvent(actor, new MyEvent() { Caller = this.Id });

        return base.OnInitializeAsync(initialEvent);
    }

    private void HandleEvent(Event e)
    {
        config.Completed.SetResult(true);
    }
}
```

This test actor creates `MyActor`, sends an event to it, waits for a response, then sets
the `TaskCompletionSource` result.  `MyActor` is a simple ping-pong style actor:

```c#
class MyEvent : Event
{
    public ActorId Caller;
}

[OnEventDoAction(typeof(MyEvent), nameof(HandleEvent))]
class MyActor : Actor
{
    private void HandleEvent(Event e)
    {
        ActorId caller = ((MyEvent)e).Caller;
        this.SendEvent(caller, new MyEvent() { Caller = this.Id });
    }
}
```

If you run this test setting `WithXmlLogEnabled(true)` on the `Configuration` you will get the
following [DGML diagram](/coyote/learn/tools/dgml) showing you what happened during this test:

<svg stroke-linecap="round" font-size="12" font-family="Segoe UI" width="357.81000000000006" height="243.60750000000007" xmlns:xlink="http://www.w3.org/1999/xlink" xmlns="http://www.w3.org/2000/svg">
  <defs />
  <rect width="357.81000000000006" height="243.60750000000007" fill="#FFFFFF" />
  <g transform="translate(20,20)">
    <g id="ConsoleApp14.Test+MyActor-&gt;ConsoleApp14.Test+MyTestActor">
      <path d="M 133.619058382037,85.9599999999999 C 136.354194596442,93.61828969311 137.106233487294,101.27657938622 135.875175054592,108.93486907933" fill="none" stroke="#A0A0A0" stroke-width="1" />
      <path d="M 133.619058382037,117.6475 L 139.998135094063,108.969517498169 C 137.165935190988,109.269108586375 134.584414918197,108.600629572285 132.25357427569,106.964080455897 z" fill="#A0A0A0" stroke="#A0A0A0" stroke-width="1" stroke-linejoin="round" />
    </g>
    <g id="ConsoleApp14.Test+MyTestActor-&gt;ConsoleApp14.Test+MyActor">
      <path d="M 80.7776082846298,117.6475 C 78.042472070225,109.98921030689 77.2904331793732,102.33092061378 78.5214916120743,94.6726309206701" fill="none" stroke="#A0A0A0" stroke-width="1" />
      <path d="M 80.7776082846299,85.96 L 74.3985315726036,94.637982501831 C 77.2307314756787,94.3383914136249 79.8122517484699,95.0068704277154 82.1430923909771,96.6434195441026 z" fill="#A0A0A0" stroke="#A0A0A0" stroke-width="1" stroke-linejoin="round" />
    </g>
    <g id="ConsoleApp14.Test+MyActor">
      <rect x="10.276666666666635" rx="5" ry="5" width="193.84333333333333" height="85.960000000000036" fill="#57AC56" stroke="#A5A6A9" stroke-width="1" />
      <text x="30.276666666666635" y="17.950000000000003" fill="#FFFFFF">ConsoleApp14.Test+MyActor</text>
      <rect x="15.276666666666635" y="27.96" width="183.84333333333333" height="53.000000000000036" fill="#FFFFFF" stroke="#A5A6A9" stroke-width="1" />
      <g id="ConsoleApp14.Test+MyActor.MyActor">
        <rect x="74.629999999999967" y="40" rx="3" ry="3" width="65.136666666666684" height="25.960000000000008" fill="#FFFFFF" stroke="#A5A6A9" stroke-width="1" />
        <text x="84.629999999999967" y="57.95" fill="#3D3D3D">MyActor</text>
      </g>
    </g>
    <g id="ConsoleApp14.Test+MyTestActor">
      <rect y="117.64750000000004" rx="5" ry="5" width="214.3966666666667" height="85.960000000000036" fill="#57AC56" stroke="#A5A6A9" stroke-width="1" />
      <text x="20" y="135.59750000000003" fill="#FFFFFF">ConsoleApp14.Test+MyTestActor</text>
      <rect x="5" y="145.60750000000004" width="204.3966666666667" height="53.000000000000036" fill="#FFFFFF" stroke="#A5A6A9" stroke-width="1" />
      <g id="ConsoleApp14.Test+MyTestActor.MyTestActor">
        <rect x="64.353333333333339" y="157.64750000000004" rx="3" ry="3" width="85.690000000000012" height="25.960000000000008" fill="#FFFFFF" stroke="#A5A6A9" stroke-width="1" />
        <text x="74.353333333333339" y="175.59750000000003" fill="#3D3D3D">MyTestActor</text>
      </g>
    </g>
  </g>
</svg>

See the following API documentation for more information:
- [TestingEngine](/coyote/learn/ref/Microsoft.Coyote.SystematicTesting/TestingEngineType)
- [TestingReport](/coyote/learn/ref/Microsoft.Coyote.SystematicTesting/TestingReportType)
- [Configuration](/coyote/learn/ref/Microsoft.Coyote/ConfigurationType)
