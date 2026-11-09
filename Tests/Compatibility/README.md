# Runtime compatibility probes

`Net8Probe` and `Net10Probe` exercise the native .NET 8 and .NET 10 Coyote
hosts. Both probes use the .NET 10 SDK pinned by the repository's `global.json`.
SDK 8 is not required. Running the probes requires the .NET and ASP.NET Core
runtimes for both .NET 8 and .NET 10. After installing SDK 10, run
`./Scripts/install-dotnet8-runtimes.ps1` to add the .NET 8 runtimes.

## Async builder lifetime

The Task and ValueTask builders retain the runtime, awaiter, and native builder
needed for task registration when a continuation can complete before await
registration returns. The first await captures the native builder after .NET
initializes its task; later awaits capture it before publishing the continuation.
Task registration and the control-status check still occur after native await
registration. This does not add scheduling points or delay continuations.

`AsyncMethodBuilderLifetimeTests` checks the exact registered task identity,
control-status timing, and completion, fault, and cancellation outcomes without
sleeps or timing-based coordination. Run these regressions on .NET 8 and .NET 10
in both Debug and Release builds.

## Testing package delivery

After building the local NuGet packages on Windows with `.\Scripts\build.ps1 -ci -nuget`,
run the package-consumer smoke tests:

```powershell
.\Tests\Compatibility\run-package-smoke.ps1 -framework net10.0
.\Tests\Compatibility\run-package-smoke.ps1 -framework net8.0
```

These tests also run on Windows, Linux, and macOS in the sample CI jobs, using the
Windows-built packages. Each test installs the locally built `Microsoft.Coyote.CLI`
package for the requested framework and builds a fresh probe referencing the locally
built `Microsoft.Coyote` meta-package rather than repository binaries. It checks that
the library packages select the matching framework assets, then uses the installed
CLI to rewrite the probe and execute ten controlled test iterations.

Each invocation uses a new workspace and package cache under `bin/package-smoke`.
Package-source mapping requires all `Microsoft.Coyote*` packages to come from
`bin/nuget`, preventing published packages with the same version from satisfying the
test. Other dependencies are restored from NuGet.org; `-dependencySource` can select
an alternative feed for those dependencies without changing the Coyote package source.
Both runtimes can be installed; the test selects the CLI framework explicitly, so
this is not a runtime-isolation test.

The samples target .NET 10. Their `-local -nuget` build scripts install the locally
built CLI with `--framework net10.0` into `Samples/bin/coyote/net10.0`. Both sample
rewrite targets invoke that tool directly, independently of local-tool manifest
resolution under the .NET 10 SDK. The library still supports .NET 8, which is
covered separately by the .NET 8 compatibility probes and package smoke tests.

## Lock delegates

On .NET 10, saved `System.Threading.Lock` methods use the same controlled lock
ownership as direct calls. For example, `Action release = gate.Exit` can release
a lock acquired by `gate.Enter()` during systematic testing. Rewriting binds
the original lock to the corresponding controlled forwarding method.

When unrewritten code returns a native `Lock.Scope`, the caller preserves its
native disposal through an adapter. Before invoking that code, systematic testing
reports unsupported synchronization, even when partial control is allowed.
With `WithSystematicFuzzingFallbackEnabled()`, the controlled iteration stops before
the call and subsequent iterations use fuzzing, where both sides use native locks.
Without fallback, the executed path reports an error; unused paths do not fail.
To keep systematic control, include the library in rewriting.
This also applies to constrained calls through external interfaces on generic structs;
rewriting preserves the call prefixes so the output remains valid IL.

Rewriting rejects incompatible `Lock.Scope` signatures across unrewritten boundaries:

- Implementing an external interface or overriding an external base method with a
  scope return or parameter, including `ref` and `out`.
- Passing a scope to an external method or constructor, or receiving a scope by reference.
- Binding a delegate to an external method with a scope return or parameter.
- Reading, writing, or taking the address of an external scope field.

The rewrite-time diagnostic names the affected type or method and the external
assembly. Include that assembly in rewriting with its callers and implementations,
and remove `SkipRewriting` from the contract type if present. This applies to
implicit and explicit implementations, inherited contracts, and generic base types.
Rewriting these types together remains supported. The direct by-value native return
adapter and the built-in `System.Threading.Lock` delegate support described above
are unchanged.

## Running the matrix

Build Coyote and then run the matrix from the repository root:

```powershell
.\Scripts\build.ps1
.\Tests\Compatibility\run-compatibility-matrix.ps1
```

`run-compatibility-matrix.ps1` is the same command that the `Coyote CI` workflow
runs. It rebuilds each probe from scratch, copies that build into a workspace of
its own before every case, and fails if a host succeeds where it must fail, if
an expected diagnostic is missing, if a rejected command mutates the workspace,
or if a supported case does not execute the probe under Coyote control. Pass
`-nobuild` to reuse the existing probe builds.

## The matrix

| Host | Target | Expected |
| ---- | ------ | -------- |
| net8.0 | net8.0 probe | `rewrite` and `test` succeed |
| net10.0 | net10.0 probe | `rewrite` and `test` succeed |
| net10.0 | net8.0 probe rewritten by the net8.0 host | `test` succeeds, because the runtime rolls forward |
| net8.0 | net10.0 probe | `rewrite` and `test` are rejected |
| net10.0 | net8.0 probe | `rewrite` is rejected |

Each probe is built once with SDK 10. The matrix tests runtime compatibility,
not compatibility with the older SDK 8 compiler.

Never rewrite a probe inside its build directory. Coyote skips an assembly that
is already rewritten with a matching signature, so a second host invoked on that
same file reports success without doing anything, which is what previously made
this matrix look green. Copy the build of the probe into an empty directory
before each host invocation, exactly like the script does.

## Reproducing a case by hand

Every case below starts from a fresh copy of a probe build. On Linux and macOS
there is no `coyote.exe`, so invoke each host as `dotnet ./bin/net8.0/coyote.dll`
and `dotnet ./bin/net10.0/coyote.dll`, which is what the script does there too.
Build both probes from the repository root with its pinned SDK:

```powershell
dotnet build .\Tests\Compatibility\Net8Probe -c Release --no-incremental
dotnet build .\Tests\Compatibility\Net10Probe -c Release --no-incremental
```

The .NET 8 host rewrites and tests the .NET 8 probe:

```powershell
New-Item -Path .\Tests\Compatibility\bin\manual\net8-host -ItemType Directory -Force
Copy-Item -Path .\Tests\Compatibility\Net8Probe\bin\Release\net8.0\* -Destination .\Tests\Compatibility\bin\manual\net8-host -Recurse -Force
.\bin\net8.0\coyote.exe rewrite .\Tests\Compatibility\bin\manual\net8-host\Net8Probe.dll
.\bin\net8.0\coyote.exe test .\Tests\Compatibility\bin\manual\net8-host\Net8Probe.dll -i 10
```

The .NET 10 host tests the .NET 8 probe that the .NET 8 host rewrote, because
the .NET runtime rolls forward:

```powershell
New-Item -Path .\Tests\Compatibility\bin\manual\net10-host-net8-probe -ItemType Directory -Force
Copy-Item -Path .\Tests\Compatibility\Net8Probe\bin\Release\net8.0\* -Destination .\Tests\Compatibility\bin\manual\net10-host-net8-probe -Recurse -Force
.\bin\net8.0\coyote.exe rewrite .\Tests\Compatibility\bin\manual\net10-host-net8-probe\Net8Probe.dll
.\bin\net10.0\coyote.exe test .\Tests\Compatibility\bin\manual\net10-host-net8-probe\Net8Probe.dll -i 10
```

The .NET 10 host rewrites and tests the .NET 10 probe:

```powershell
New-Item -Path .\Tests\Compatibility\bin\manual\net10-host -ItemType Directory -Force
Copy-Item -Path .\Tests\Compatibility\Net10Probe\bin\Release\net10.0\* -Destination .\Tests\Compatibility\bin\manual\net10-host -Recurse -Force
.\bin\net10.0\coyote.exe rewrite .\Tests\Compatibility\bin\manual\net10-host\Net10Probe.dll
.\bin\net10.0\coyote.exe test .\Tests\Compatibility\bin\manual\net10-host\Net10Probe.dll -i 10
```

The .NET 10 host must reject the fresh .NET 8 probe, because rewriting it would
inject .NET 10 runtime references that the target cannot load, and it must leave
the assembly unchanged:

```powershell
New-Item -Path .\Tests\Compatibility\bin\manual\net10-host-rewrites-net8 -ItemType Directory -Force
Copy-Item -Path .\Tests\Compatibility\Net8Probe\bin\Release\net8.0\* -Destination .\Tests\Compatibility\bin\manual\net10-host-rewrites-net8 -Recurse -Force
.\bin\net10.0\coyote.exe rewrite .\Tests\Compatibility\bin\manual\net10-host-rewrites-net8\Net8Probe.dll
```

```text
The Coyote host is running on .NET 10.0, but assembly '...\Net8Probe.dll'
targets .NETCoreApp,Version=v8.0. Rewriting it with this host would inject
.NET 10.0 runtime references and the rewritten assembly would fail to load on
.NET 8.0. Run the net8.0 Coyote host to rewrite this assembly.
```

The .NET 8 host must reject the fresh .NET 10 probe when rewriting it:

```powershell
New-Item -Path .\Tests\Compatibility\bin\manual\net8-host-rewrites-net10 -ItemType Directory -Force
Copy-Item -Path .\Tests\Compatibility\Net10Probe\bin\Release\net10.0\* -Destination .\Tests\Compatibility\bin\manual\net8-host-rewrites-net10 -Recurse -Force
.\bin\net8.0\coyote.exe rewrite .\Tests\Compatibility\bin\manual\net8-host-rewrites-net10\Net10Probe.dll
```

```text
The Coyote host is running on .NET 8.0, but assembly '...\Net10Probe.dll'
targets .NETCoreApp,Version=v10.0, which this host cannot load. Run the net10.0
Coyote host to rewrite this assembly.
```

The .NET 8 host must also reject the .NET 10 probe before reflection based test
discovery, and report both the host runtime and the required target runtime:

```powershell
New-Item -Path .\Tests\Compatibility\bin\manual\net8-host-tests-net10 -ItemType Directory -Force
Copy-Item -Path .\Tests\Compatibility\Net10Probe\bin\Release\net10.0\* -Destination .\Tests\Compatibility\bin\manual\net8-host-tests-net10 -Recurse -Force
.\bin\net10.0\coyote.exe rewrite .\Tests\Compatibility\bin\manual\net8-host-tests-net10\Net10Probe.dll
.\bin\net8.0\coyote.exe test .\Tests\Compatibility\bin\manual\net8-host-tests-net10\Net10Probe.dll -i 10
```

```text
The Coyote host is running on .NET 8.0, but test assembly '...\Net10Probe.dll'
requires .NETCoreApp,Version=v10.0. Run the net10.0 Coyote host for this
assembly.
```

Each rejected command exits with a non-zero exit code and leaves the target
assembly byte for byte unchanged.
