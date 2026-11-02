# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

param(
    [string]$dotnet = "dotnet"
)

Import-Module $PSScriptRoot/../../Scripts/common.psm1 -Force
CheckPSVersion

$manifest = Get-Content "$PSScriptRoot/../.config/dotnet-tools.json" -Raw | ConvertFrom-Json
$version = $manifest.tools."microsoft.coyote.cli".version
$toolPath = [IO.Path]::GetFullPath("$PSScriptRoot/../bin/coyote/net10.0")
$config = [IO.Path]::GetFullPath("$PSScriptRoot/../../NuGet.config")
$command = "tool update Microsoft.Coyote.CLI --version $version --framework net10.0 " +
    "--tool-path `"$toolPath`" --configfile `"$config`" --no-cache"
Invoke-ToolCommand -tool $dotnet -cmd $command -error_msg "Failed to install the local .NET 10 Coyote sample tool"
