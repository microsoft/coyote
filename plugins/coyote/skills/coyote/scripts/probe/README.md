# Coyote 1.7.11 API probe and regression gate

Use this focused harness for additional evidence when the
[API reference](../../references/supported-apis.md) and the version-matched source
do not settle whether a call is controlled. Both the package and repo-local CLI
are pinned to **1.7.11**. There is no global-tool dependency or version override.

## Prerequisites and use

Install the .NET 8 SDK and PowerShell 7. For an application-specific investigation,
copy this entire directory, including `.config` and `NuGet.Config`, to a scratch
directory in your application repository before editing it; do not edit an
installed plugin's cache. Run the following in the copied directory:

```powershell
.\run.ps1
.\run.ps1 -Iterations 200
```

`Scenario.cs` starts two tasks over shared state. Its default work uses controlled
`Task.Delay`, `Thread.Sleep`, `Interlocked.Increment`, and `Task.Yield`. Replace
the marked block with a focused API use and adjust its assertion to your property.
Ensure the operation is actually executed; unused imports or skipped branches
give no evidence.

The script restores the pinned local CLI and public NuGet packages, builds,
copies the complete output into a fresh `bin/rewritten` directory, rewrites that
copy, and runs it in strict concurrency and data-nondeterminism mode. The original
build output remains untouched. A local `NuGet.Config` keeps this portable and
avoids inheriting Coyote's source-build-only package feed.
The explicit `System.Text.Json` 8.0.6 reference replaces the vulnerable 8.0.0
transitive dependency without changing the Coyote 1.7.11 package pins.

The executable exits 0 only when all requested iterations complete without
reported bugs, internal errors, or uncontrolled calls. Bugs exit 1; invalid usage
or unrewritten output exits 3. The script turns a nonzero result into an error.
It intentionally says **"No control failure observed"**, not "API supported":
undetected or unexercised third-party concurrency can still exist.

Diagnose any assertion, deadlock, liveness, or uncontrolled-call report on its
merits. An invariant failure does not establish that the API is controllable,
and a deadlock is not automatically a harness artifact.

## Maintainer regression gate

Before editing the default scenario, or after restoring it, run:

```powershell
.\run.ps1 -SelfTest
```

The gate requires:

- Unrewritten output is rejected, rather than accepted as a passing test.
- All 100 default-scenario iterations reach their final assertion with no bugs.
- An intentional lost update fails with its specific invariant message.
- The recorded schedule reproduces that same failure on the same rewritten build.
- A known uncontrolled timer and `Guid.NewGuid()` each fail in strict mode.

Unexpected failures cannot satisfy the expected-race check. If the assertion or
race is removed, or rewriting fails to intercept the known unsupported calls, the
gate fails. A fixed random seed makes these small regression scenarios repeatable;
it is not a recommendation to rely on only one seed for application testing.
