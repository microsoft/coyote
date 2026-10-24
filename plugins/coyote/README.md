# Coyote plugin for GitHub Copilot

An installable skill for using Microsoft.Coyote in a consumer's .NET codebase:
project setup, binary rewriting, concurrent scenarios, specifications, scheduling,
deadlock diagnosis, and deterministic replay. This is not a set of instructions
for contributing to Coyote itself.

## Install and use

With a version of GitHub Copilot CLI that supports Agent Plugins 1.0:

```text
copilot plugin install microsoft/coyote:plugins/coyote
```

Start a new Copilot session in your application repository and ask it to use the
`coyote` skill, for example: "Use Coyote to test this queue for lost updates."
The plugin supplies instructions and optional probe source; it does not install
the .NET SDK, Coyote packages, or a global Coyote tool.

To install from a local checkout instead, run from the Coyote repository root:

```powershell
copilot plugin install .\plugins\coyote
```

Plugin installs are cached. Reinstall the local path after editing it. For a
remote installation, use `copilot plugin update coyote` to fetch updates.

## Versions and compatibility

The plugin's version is **0.1.0**, declared in [plugin.json](plugin.json).
It is independent of the Coyote library and CLI package versions.

| Plugin version | Supported Coyote versions | Executable regression environment |
| --- | --- | --- |
| 0.1.0 | **1.7.11 only** | .NET 8, Windows, Linux, macOS |

No other Coyote version is currently claimed as supported, including other
1.7.x releases or unreleased source builds. The regression environment is not
a claim that the probe covers every Coyote API or target framework.
See [compatibility.md](skills/coyote/references/compatibility.md) for version
detection and .NET Framework considerations.

## Development and release policy

The skill originated in the SpecDrivenHarness Coyote skill. This directory is
the canonical Coyote-owned copy; consumers should install it rather than fork
the instructions.

Run the executable gate from the repository root with PowerShell 7:

```powershell
.\plugins\coyote\skills\coyote\scripts\probe\run.ps1 -SelfTest
```

The gate restores a repo-local CLI and packages pinned to 1.7.11, builds, rewrites
a separate copy of the output, checks controlled APIs, detects an intentional
lost update, replays its recorded schedule, and checks rejection of uncontrolled
concurrency and data nondeterminism. It requires network access to NuGet on the
first restore. It neither installs nor updates a global tool.

For each plugin release:

1. Update the skill and API reference alongside relevant API, CLI, or rewriting
   changes. Default guidance targets the latest released version actually
   validated by this plugin, not automatically the current `main` branch.
2. Before expanding support, run the gate against each newly claimed Coyote
   version and review version-specific API and framework differences. Update the
   compatibility table, probe package/tool pins, and CI coverage together.
   Do not infer a supported range from a single passing version.
3. Update `plugin.json` and [CHANGELOG.md](CHANGELOG.md). Use independent semantic
   versioning: patches for corrections, minors for compatible capabilities or
   additional supported Coyote versions, and majors for breaking plugin usage
   changes or removal of supported Coyote versions. During 0.x development,
   document breaking changes in a minor release.
4. Keep prior guidance in Git history and identify releases with plugin-specific
   tags such as `coyote-plugin-v0.1.0`; do not reuse Coyote package version tags.

Only add historical compatibility notes when they affect a supported workflow.
Keep the main skill focused and put version-specific details in references.
