# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.
#requires -Version 7.0

[CmdletBinding()]
param(
    [ValidateRange(1, 10000)]
    [int]$Iterations = 100,
    [switch]$SelfTest
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $false
$project = Join-Path $PSScriptRoot "Probe.csproj"
$nugetConfig = Join-Path $PSScriptRoot "NuGet.Config"
$toolManifest = Join-Path (Join-Path $PSScriptRoot ".config") "dotnet-tools.json"
$output = Join-Path (Join-Path (Join-Path (Join-Path $PSScriptRoot "bin") "Release") "net8.0") "Probe.dll"
$rewriteDirectory = Join-Path (Join-Path $PSScriptRoot "bin") "rewritten"

[xml]$projectXml = Get-Content -LiteralPath $project -Raw
$packageVersion = $projectXml.Project.PropertyGroup.CoyoteVersion
$tools = Get-Content -LiteralPath $toolManifest -Raw | ConvertFrom-Json
if ($packageVersion -ne "1.7.11" -or $tools.tools.'microsoft.coyote.cli'.version -ne $packageVersion) {
    throw "This probe supports Coyote 1.7.11 only; the package and CLI pins must match."
}

Push-Location $PSScriptRoot
try {
    dotnet tool restore --tool-manifest $toolManifest --configfile $nugetConfig
    if ($LASTEXITCODE -ne 0) { throw "Coyote CLI restore failed." }
    dotnet restore $project --configfile $nugetConfig --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw "Probe package restore failed." }
    dotnet build $project --configuration Release --no-restore --verbosity quiet
    if ($LASTEXITCODE -ne 0) { throw "Probe build failed." }

    if ($SelfTest) {
        $unrewrittenOutput = & dotnet $output --self-test 2>&1
        if ($LASTEXITCODE -ne 3 -or ($unrewrittenOutput -join "`n") -notmatch "probe assembly is not rewritten") {
            throw "The probe did not reject unrewritten output as expected: $unrewrittenOutput"
        }
        Write-Host "PASS: unrewritten output rejected."
    }

    if (Test-Path -LiteralPath $rewriteDirectory) {
        Remove-Item -LiteralPath $rewriteDirectory -Recurse -Force
    }
    New-Item -ItemType Directory -Path $rewriteDirectory | Out-Null
    Get-ChildItem -LiteralPath (Split-Path -Parent $output) |
        Copy-Item -Destination $rewriteDirectory -Recurse
    $rewriteConfig = Join-Path $rewriteDirectory "rewrite.json"
    $json = @{ AssembliesPath = $rewriteDirectory; Assemblies = @("Probe.dll") } | ConvertTo-Json
    [System.IO.File]::WriteAllText($rewriteConfig, $json, [System.Text.UTF8Encoding]::new($false))

    dotnet tool run coyote -- rewrite $rewriteConfig
    if ($LASTEXITCODE -ne 0) { throw "Probe rewriting failed." }
    [string[]]$probeArguments = if ($SelfTest) { @("--self-test") } else { @("$Iterations") }
    dotnet (Join-Path $rewriteDirectory "Probe.dll") @probeArguments
    if ($LASTEXITCODE -ne 0) { throw "Probe failed with exit code $LASTEXITCODE." }
}
finally {
    Pop-Location
}
