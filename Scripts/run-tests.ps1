# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

param(
    [string]$dotnet = "dotnet",
    [ValidateSet("all", "net6.0", "net5.0", "netcoreapp3.1", "net462")]
    [string]$framework = "all",
    [ValidateSet("all", "rewriting", "testing", "actors", "actors-testing", "standalone")]
    [string]$test = "all",
    [string]$filter = "",
    [string]$logger = "",
    [ValidateSet("quiet", "minimal", "normal", "detailed", "diagnostic")]
    [string]$v = "normal"
)

Import-Module $PSScriptRoot/powershell/common.psm1 -Force

$targets = [ordered]@{
    "rewriting" = "Tests.Rewriting"
    "testing" = "Tests.BugFinding"
    "actors" = "Tests.Actors"
    "actors-testing" = "Tests.Actors.BugFinding"
}

$dotnet_path = FindDotNet("dotnet");

# Restore the local ilverify tool.
&dotnet tool restore
$ilverify = "dotnet ilverify"

[System.Environment]::SetEnvironmentVariable('COYOTE_CLI_TELEMETRY_OPTOUT', '1')

Write-Comment -prefix "." -text "Running the Coyote tests" -color "yellow"

# Run all enabled tests.
foreach ($kvp in $targets.GetEnumerator()) {
    if (($test -ne "all") -and ($test -ne $($kvp.Name))) {
        continue
    }

    foreach ($f in $frameworks) {
        if (($framework -ne "all") -and ($f -ne $framework)) {
            continue
        }

        $target = "$PSScriptRoot/../Tests/$($kvp.Value)/$($kvp.Value).csproj"

        if ($f -eq "net6.0") {
            $AssemblyName = GetAssemblyName($target)
            $NetCoreApp = FindNetCoreApp -dotnet_path $dotnet_path -version "6.0"
            $command = "$PSScriptRoot/../Tests/$($kvp.Value)/bin/net6.0/$AssemblyName.dll"
            $command = $command + ' -r "' + "$PSScriptRoot/../Tests/$($kvp.Value)/bin/net6.0/*.dll" + '"'
            $command = $command + ' -r "' + "$dotnet_path/packs/Microsoft.NETCore.App.Ref/6.0.0/ref/net6.0/*.dll" + '"'
            $command = $command + ' -r "' + "$PSScriptRoot/../bin/net6.0/*.dll" + '"'
            $command = $command + ' -r "' + $NetCoreApp + '/*.dll"'
            Invoke-ToolCommand -tool $ilverify -cmd $command -error_msg "found corrupted assembly rewriting"
        }

        Invoke-DotnetTest -dotnet $dotnet -project $($kvp.Name) -target $target `
            -filter $filter -logger $logger -framework $f -verbosity $v
    }
}

Write-Comment -prefix "." -text "Done" -color "green"
