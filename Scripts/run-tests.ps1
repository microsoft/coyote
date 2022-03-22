# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

param(
    [ValidateSet("net6.0", "net5.0", "netcoreapp3.1", "net462")]
    [string]$framework = "net6.0",
    [ValidateSet("all", "rewriting", "testing", "actors", "actors-testing", "standalone")]
    [string]$test = "all",
    [string]$filter = "",
    [string]$logger = "",
    [ValidateSet("quiet", "minimal", "normal", "detailed", "diagnostic")]
    [string]$v = "normal",
    [bool]$ci = $false
)

Import-Module $PSScriptRoot/powershell/common.psm1 -Force

$all_frameworks = (Get-Variable "framework").Attributes.ValidValues
$targets = [ordered]@{
    "rewriting" = "Tests.Rewriting"
    "testing" = "Tests.BugFinding"
    "actors" = "Tests.Actors"
    "actors-testing" = "Tests.Actors.BugFinding"
}

# Check that the expected .NET SDK is installed.
$dotnet = "dotnet"
$dotnet_sdk_path = FindDotNetSdkPath($dotnet)
$dotnet_path = $dotnet_sdk_path.TrimEnd('sdk')
$sdk_version = FindDotNetSdkVersion -dotnet_sdk_path $dotnet_sdk_path
if ($null -eq $sdk_version) {
    Write-Error "The global.json file is pointing to version '$sdk_version' but no matching version was found."
    Write-Error "Please install .NET SDK version '$sdk_version' from https://dotnet.microsoft.com/download/dotnet-core."
    exit 1
}

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

    $frameworks = Get-ChildItem -Path "$PSScriptRoot/../Tests/$($kvp.Value)/bin" | `
        Where-Object Name -CIn $all_frameworks | Select-Object -expand Name
    foreach ($f in $frameworks) {
        if ((-not $ci) -and ($f -ne $framework)) {
            continue
        }

        $target = "$PSScriptRoot/../Tests/$($kvp.Value)/$($kvp.Value).csproj"
        if ($f -eq "net6.0") {
            $AssemblyName = GetAssemblyName($target)
            $NetCoreApp = FindNetCoreApp -dotnet_sdk_path $dotnet_sdk_path -version "6.0"
            $command = "$PSScriptRoot/../Tests/$($kvp.Value)/bin/net6.0/$AssemblyName.dll"
            $command = $command + ' -r "' + "$PSScriptRoot/../Tests/$($kvp.Value)/bin/net6.0/" + '"'
            $command = $command + ' -r "' + "$dotnet_path/packs/Microsoft.NETCore.App.Ref/$sdk_version/ref/net6.0/" + '"'
            $command = $command + ' -r "' + "$PSScriptRoot/../bin/net6.0/" + '"'
            $command = $command + ' -r "' + $NetCoreApp + '/"'
            Invoke-ToolCommand -tool $ilverify -cmd $command -error_msg "found corrupted assembly rewriting"
        }

        Invoke-DotnetTest -dotnet $dotnet -project $($kvp.Name) -target $target `
            -filter $filter -logger $logger -framework $f -verbosity $v
    }
}

Write-Comment -prefix "." -text "Done" -color "green"
