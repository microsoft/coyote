# Compatibility policy

| Plugin version | Supported Coyote library and CLI versions |
| --- | --- |
| 0.1.0 | **1.7.11 only** |

The plugin version comes from its `plugin.json`, not the consumer's NuGet version.
The API reference and executable probe target Coyote 1.7.11. No support is claimed
for earlier releases, later releases, or unreleased builds with the same version
prefix. Do not generalize this to "all 1.7.x".

## Detect the consumer's environment

Before generating commands or changing dependencies:

1. Inspect project files, `Directory.Packages.props`, and resolved dependencies
   (`dotnet list <project> package --include-transitive`). A declared range is
   not necessarily the resolved version. Note the target framework and SDK.
2. Inspect `.config/dotnet-tools.json` and existing rewrite/build scripts.
   Use `dotnet tool list --local` or, if a global tool is actually used,
   `dotnet tool list --global`. Do not assume a `coyote` on PATH is the local tool.
3. Require matching 1.7.11 versions of the runtime/testing packages and rewriting
   CLI for the documented workflow. Preserve the consumer's package management
   and tool conventions; do not silently replace manifests or install global tools.
4. For any other version, state that it is outside this plugin's supported set.
   Check that version's source and CLI help before suggesting version-specific
   commands. Ask before an upgrade or migration; never silently apply the 1.7.11
   API table.

## Framework differences

The bundled probe and CI gate use .NET 8 on Windows, Linux, and macOS. The Coyote
1.7.11 release also has other framework targets; their API availability is not
identical. Non-generic `TaskCompletionSource` and the HTTP rewriting mappings in
the API reference are modern-.NET-specific.

For .NET Framework, inspect the consumer's existing setup and use the
framework-targeted `coyote.exe` from `Microsoft.Coyote.Tool` 1.7.11, rather than
assuming the modern .NET global/local tool can resolve the framework assembly
closure. The bundled .NET 8 probe does not validate .NET Framework rewriting.

## Maintaining compatibility

The main skill describes supported workflows, not the history of every Coyote
release. Add version-specific notes only for material differences. Broader
support requires executable coverage and source review for every claimed version;
update the plugin README, this table, the probe/tool pins, and CI together.
Old instructions remain available through Git history and plugin release tags.
