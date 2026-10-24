# Consumer walkthrough: Coyote 1.7.11

Read this when setting up a consumer project or implementing completion checks,
trace persistence, replay, or a monitor. The main skill covers the general
workflow; this reference supplies the wiring. These examples keep the default
fair portfolio and leave memory-access interleaving **off**.

## A small application and harness

For an existing application, preserve its project structure, package management,
and test framework. The following standalone console harness avoids introducing
a test-runner dependency and demonstrates two assemblies that must be rewritten.
Place the four files below under a scratch example directory.

`Counter/Counter.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
</Project>
```

`Counter/Counter.cs`:

```csharp
using System.Threading.Tasks;

namespace Example
{
    public sealed class Counter
    {
        private readonly object Sync = new object();
        public int Value { get; private set; }

        public async Task IncrementAsync()
        {
            await Task.Yield();
            lock (this.Sync)
            {
                this.Value++;
            }
        }
    }
}
```

This is an illustrative asynchronous component, not advice to insert yields
into production code. The scenario below awaits both operations before reading
the result. There is no real I/O or clock dependency.

`Harness/Harness.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../Counter/Counter.csproj" />
    <PackageReference Include="Microsoft.Coyote" Version="1.7.11" />
    <PackageReference Include="System.Text.Json" Version="8.0.6" />
  </ItemGroup>
</Project>
```

The JSON package reference replaces the vulnerable 8.0.0 transitive dependency
while leaving Coyote at 1.7.11. In a centrally managed consumer, express these
versions in its existing package-management files instead.

`Harness/Program.cs`:

```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using Example;
using Microsoft.Coyote;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.SystematicTesting;

internal static class Program
{
    private static int Completed;

    private static async Task Scenario()
    {
        var counter = new Counter();
        await Task.WhenAll(
            Task.Run(counter.IncrementAsync),
            Task.Run(counter.IncrementAsync));
        Specification.Assert(counter.Value == 2, "Both increments must be preserved.");
        Completed++;
    }

    public static int Main(string[] args)
    {
        if (args.Length != 1 && !(args.Length == 2 && args[0] == "replay"))
        {
            Console.Error.WriteLine("Usage: Harness <trace-output> | Harness replay <trace-input>");
            return 2;
        }

        bool replay = args.Length == 2;
        string tracePath = Path.GetFullPath(args[replay ? 1 : 0]);
        uint iterations = replay ? 1u : 100u;
        var configuration = Configuration.Create()
            .WithTestingIterations(iterations)
            .WithPartiallyControlledConcurrencyAllowed(false)
            .WithPartiallyControlledDataNondeterminismAllowed(false)
            .WithFailureOnMaxStepsBoundEnabled();
        if (replay)
        {
            configuration.WithReproducibleTrace(File.ReadAllText(tracePath));
        }

        Completed = 0;
        using var engine = TestingEngine.Create(configuration, Scenario);
        if (!engine.IsTestRewritten())
        {
            throw new InvalidOperationException("Run the rewritten harness.");
        }

        engine.Run();
        var report = engine.TestReport;
        Console.WriteLine(report.GetText(configuration, "[harness]"));
        if (!replay && !string.IsNullOrEmpty(engine.ReproducibleTrace))
        {
            File.WriteAllText(tracePath, engine.ReproducibleTrace);
            Console.WriteLine($"Saved failing schedule: {tracePath}");
        }

        if (report.InternalErrors.Count != 0)
        {
            throw new InvalidOperationException(string.Join(Environment.NewLine, report.InternalErrors));
        }

        if (report.NumOfFoundBugs != 0)
        {
            return 1;
        }

        if (replay)
        {
            throw new InvalidOperationException("The recorded failure was not reproduced.");
        }

        int explored = report.NumOfExploredFairPaths + report.NumOfExploredUnfairPaths;
        if (explored != iterations || Completed != iterations)
        {
            throw new InvalidOperationException("Not all requested scenarios completed.");
        }

        return 0;
    }
}
```

The completion count is outside per-iteration state and incremented only after
the final assertion. Zero bugs alone is insufficient if the engine truncated
the execution before that assertion. The example elects to treat step-bound
exhaustion as a failure because it is a terminating regression scenario; this
does not change Coyote's defaults or require that policy for intentionally
bounded, nonterminating models.

The rewriting check confirms the entry assembly only. The explicit assembly list
below is still necessary for the component being tested. When integrating with
xUnit, NUnit, or MSTest, replace the result/exception handling with the framework's
assertions and run Coyote fixtures serially.

## Build, rewrite, and run

From the example directory, use the .NET 8 SDK and PowerShell. If a tool manifest
does not already exist, create it with `dotnet new tool-manifest`. Install the
local CLI with `dotnet tool install --local Microsoft.Coyote.CLI --version 1.7.11`.
For an existing manifest with that pin, use `dotnet tool restore` instead.

Build, then copy the complete output into a dedicated directory. Do not use an
ordinary `dotnet run` or a rebuilding test command against the original output.

```powershell
$ErrorActionPreference = "Stop"
dotnet build .\Harness\Harness.csproj -c Release
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

if (Test-Path -LiteralPath .\rewritten) {
    throw "Use a fresh example output directory or archive the previous rewritten build first."
}
New-Item -ItemType Directory -Path .\rewritten | Out-Null
Get-ChildItem -LiteralPath .\Harness\bin\Release\net8.0 |
    Copy-Item -Destination .\rewritten -Recurse
```

Create `rewrite.json` in the example directory:

```json
{
  "AssembliesPath": "./rewritten",
  "Assemblies": ["Harness.dll", "Counter.dll"]
}
```

Then run:

```powershell
dotnet tool run coyote -- rewrite .\rewrite.json
if ($LASTEXITCODE -ne 0) { throw "Rewriting failed." }
dotnet .\rewritten\Harness.dll .\failure.trace.json
if ($LASTEXITCODE -ne 0) { throw "Harness failed; inspect the report and any saved trace." }
```

The normal example should complete cleanly. Keep ordinary test runs on the
original, uninstrumented output. Do not rewrite unrelated framework dependencies
indiscriminately; establish the boundary of the scenario and its controlled code.

## Saving and replaying failures

The harness writes a trace only when Coyote supplies one; a clean run does not
produce a new failure artifact. An old trace file is not evidence about a later
run. Use a distinct output path for each run and retain the failing rewritten
build and dependency closure with the trace.

After a reported bug, replay without rebuilding or rewriting:

```powershell
dotnet .\rewritten\Harness.dll replay .\failure.trace.json
```

Exit 1 is expected when this harness reproduces a reported bug. Compare the bug
kind and invariant message with the original failure; a different bug or replay
error is not a successful reproduction. The probe's `-SelfTest` demonstrates an
automated exact-message comparison.

To exercise this manually, in a separate example copy replace the counter's
method body with a read of `Value`, then `await Task.Yield()`, then an assignment
of the saved value plus one. This deliberately makes the update non-atomic across
an existing asynchronous scheduling point; memory-access interleaving is not
needed. Rebuild and rewrite fresh output, require the lost-update assertion to
fail, and replay that failure. Restore the locked implementation and build fresh
output again before expecting a clean result.

The bundled API probe currently prints failures but does not save traces in
normal mode. Use the trace-persistence pattern above for a consumer harness;
do not assume a normal probe invocation created a replay artifact.

## Monitor example

For an event-driven obligation, add the following as `Harness/WorkMonitor.cs`.
It represents one outstanding batch, not arbitrary concurrent request IDs:

```csharp
using Microsoft.Coyote.Specifications;

internal sealed class WorkMonitor : Monitor
{
    internal sealed class Started : Event { }
    internal sealed class Finished : Event { }

    [Start]
    [Cold]
    [OnEventGotoState(typeof(Started), typeof(Busy))]
    [OnEventDoAction(typeof(Finished), nameof(UnexpectedEvent))]
    private sealed class Idle : State { }

    [Hot]
    [OnEventGotoState(typeof(Finished), typeof(Idle))]
    [OnEventDoAction(typeof(Started), nameof(UnexpectedEvent))]
    private sealed class Busy : State { }

    private void UnexpectedEvent()
    {
        this.Assert(false, "Batch lifecycle events are out of order.");
    }
}
```

Inside `Scenario`, register the monitor and emit `Started` before starting the
two tasks:

```csharp
Specification.RegisterMonitor<WorkMonitor>();
Specification.Monitor<WorkMonitor>(new WorkMonitor.Started());
```

After awaiting both tasks and checking the counter, emit `Finished` before
incrementing `Completed`:

```csharp
Specification.Monitor<WorkMonitor>(new WorkMonitor.Finished());
```

Registration occurs inside each scenario iteration. The monitor expresses
**safety** by rejecting a finish without a start or overlapping starts, and
**liveness** through the outstanding hot state. Omitting `Finished` leaves the
monitor hot when the scenario ends; a long-running hot execution can also exceed
the temperature threshold under fair scheduling. Keep the default fair portfolio;
this example does not need memory-access interleaving.

Verify the monitor observes meaningful events: in a scratch variant omit
`Finished` and require the hot-state error; in another send `Finished` before
`Started` and require the ordering assertion. For multiple outstanding requests,
model identities and outstanding obligations rather than reusing this single-batch
state machine unchanged.
