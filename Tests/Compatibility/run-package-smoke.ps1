# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

param(
    [ValidateSet("net10.0", "net8.0")]
    [string]$framework = "net10.0",
    [string]$dependencySource = "https://api.nuget.org/v3/index.json"
)

$ErrorActionPreference = "Stop"
Import-Module $PSScriptRoot/../../Scripts/common.psm1 -Force
CheckPSVersion

function Invoke-CheckedCommand([string]$tool, [string[]]$arguments) {
    Write-Host "Invoking $tool $($arguments -join ' ')"
    $output = (& $tool @arguments 2>&1 | Out-String)
    $exitCode = $LASTEXITCODE
    Write-Host $output
    if ($exitCode -ne 0) {
        throw "'$tool' failed with exit code $exitCode."
    }

    return $output
}

$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot "../.."))
$packageSource = Join-Path $root "bin/nuget"
[xml]$versionProps = Get-Content (Join-Path $root "Common/version.props")
$version = $versionProps.Project.PropertyGroup.VersionPrefix
$suffix = $versionProps.Project.PropertyGroup.VersionSuffix
if ($suffix) {
    $version = "$version-$suffix"
}

foreach ($package in @("Microsoft.Coyote", "Microsoft.Coyote.Core", "Microsoft.Coyote.Actors",
    "Microsoft.Coyote.Test", "Microsoft.Coyote.CLI")) {
    if (-not (Test-Path (Join-Path $packageSource "$package.$version.nupkg"))) {
        throw "Missing locally built package '$package.$version.nupkg'. Run Scripts/build.ps1 -ci -nuget first."
    }
}

# A new workspace and package cache prevent a published package of the same version from
# satisfying the test. Only the local source can supply Microsoft.Coyote packages.
$workspace = Join-Path $PSScriptRoot "bin/package-smoke/$framework/$([Guid]::NewGuid())"
$consumer = Join-Path $workspace "consumer"
$toolPath = Join-Path $workspace "tool"
$cache = Join-Path $workspace "packages"
New-Item -ItemType Directory -Path $consumer -Force | Out-Null
$probeName = if ($framework -eq "net10.0") { "Net10Probe" } else { "Net8Probe" }
foreach ($file in @("$probeName.csproj", "CompatibilityProbe.cs", "global.json")) {
    Copy-Item (Join-Path $PSScriptRoot "$probeName/$file") $consumer
}

$config = Join-Path $workspace "NuGet.config"
$escapedSource = [System.Security.SecurityElement]::Escape($packageSource)
$escapedCache = [System.Security.SecurityElement]::Escape($cache)
$escapedDependencySource = [System.Security.SecurityElement]::Escape($dependencySource)
@"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <config>
    <add key="globalPackagesFolder" value="$escapedCache" />
  </config>
  <packageSources>
    <clear />
    <add key="local" value="$escapedSource" />
    <add key="dependencies" value="$escapedDependencySource" />
  </packageSources>
  <disabledPackageSources>
    <clear />
  </disabledPackageSources>
  <packageSourceMapping>
    <clear />
    <packageSource key="local">
      <package pattern="Microsoft.Coyote*" />
    </packageSource>
    <packageSource key="dependencies">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
"@ | Set-Content $config

$previousPackages = $env:NUGET_PACKAGES
$previousTelemetry = $env:COYOTE_CLI_TELEMETRY_OPTOUT
try {
    $env:NUGET_PACKAGES = $cache
    $env:COYOTE_CLI_TELEMETRY_OPTOUT = "1"
    Push-Location $consumer
    try {
        $null = Invoke-CheckedCommand "dotnet" @("tool", "install", "Microsoft.Coyote.CLI",
            "--version", $version, "--framework", $framework, "--tool-path", $toolPath,
            "--configfile", $config, "--no-cache")
        $null = Invoke-CheckedCommand "dotnet" @("restore", "$probeName.csproj",
            "--configfile", $config, "-p:CoyotePackageVersion=$version", "--no-cache")
        $null = Invoke-CheckedCommand "dotnet" @("build", "$probeName.csproj",
            "-c", "Release", "--no-restore", "-p:CoyotePackageVersion=$version")

        $assets = Get-Content "obj/project.assets.json" -Raw | ConvertFrom-Json
        $target = $assets.targets.PSObject.Properties[$framework].Value
        foreach ($package in @("Microsoft.Coyote.Core", "Microsoft.Coyote.Actors", "Microsoft.Coyote.Test")) {
            $library = $target.PSObject.Properties["$package/$version"].Value
            $compileAssets = @($library.compile.PSObject.Properties.Name)
            if (-not ($compileAssets | Where-Object { $_ -like "lib/$framework/*.dll" })) {
                throw "'$package' did not select its $framework compile assets: $($compileAssets -join ', ')."
            }
        }

        $tool = Join-Path $toolPath $(if ($IsWindows) { "coyote.exe" } else { "coyote" })
        $assembly = Join-Path $consumer "bin/Release/$framework/$probeName.dll"
        $before = (Get-FileHash $assembly).Hash
        $rewrite = Invoke-CheckedCommand $tool @("rewrite", $assembly)
        $major = $framework.Substring(3).Split('.')[0]
        if (-not $rewrite.Contains("for .NET $major.") -or
            -not $rewrite.Contains("Writing the modified") -or
            (Get-FileHash $assembly).Hash -eq $before) {
            throw "The installed $framework CLI did not rewrite the package consumer using the expected host."
        }

        $test = Invoke-CheckedCommand $tool @("test", $assembly, "-i", "10")
        $controlled = [regex]::Match($test, "Controlled (\d+) operations")
        if (-not $test.Contains("for .NET $major.") -or
            -not $test.Contains("Found 0 bugs.") -or
            -not $test.Contains("Explored 10 execution paths") -or
            -not $controlled.Success -or [int]$controlled.Groups[1].Value -eq 0) {
            throw "The installed $framework CLI did not successfully control the package consumer."
        }
    } finally {
        Pop-Location
    }
} finally {
    $env:NUGET_PACKAGES = $previousPackages
    $env:COYOTE_CLI_TELEMETRY_OPTOUT = $previousTelemetry
}

Write-Host "The $framework package consumer and installed CLI smoke test passed."
