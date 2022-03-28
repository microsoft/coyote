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
    [switch]$ci
)

Import-Module $PSScriptRoot/common.psm1 -Force

$all_frameworks = (Get-Variable "framework").Attributes.ValidValues
$targets = [ordered]@{
    "rewriting" = "Tests.Rewriting"
    "testing" = "Tests.BugFinding"
    "actors" = "Tests.Actors"
    "actors-testing" = "Tests.Actors.BugFinding"
}

# Find that paths to the installed .NET runtime.
$dotnet = "dotnet"
$dotnet_runtime_path = FindDotNetRuntimePath -dotnet $dotnet -runtime "NETCore"
$aspnet_runtime_path = FindDotNetRuntimePath -dotnet $dotnet -runtime "AspNetCore"
$runtime_version = FindDotNetRuntimeVersion -dotnet_runtime_path $dotnet_runtime_path

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
        if ((-not $ci.IsPresent) -and ($f -ne $framework)) {
            continue
        }

        $target = "$PSScriptRoot/../Tests/$($kvp.Value)/$($kvp.Value).csproj"
        if ($f -eq "net6.0") {
            $AssemblyName = GetAssemblyName($target)
            $command = [IO.Path]::Combine($PSScriptRoot, "..", "Tests", $($kvp.Value), "bin", "net6.0", "$AssemblyName.dll")
            $command = $command + ' -r "' + [IO.Path]::Combine( `
                $PSScriptRoot, "..", "Tests", $($kvp.Value), "bin", "net6.0", "*.dll") + '"'
            $command = $command + ' -r "' + [IO.Path]::Combine($PSScriptRoot, "..", "bin", "net6.0", "*.dll") + '"'
            $command = $command + ' -r "' + [IO.Path]::Combine($dotnet_runtime_path, $runtime_version, "*.dll") + '"'
            $command = $command + ' -r "' + [IO.Path]::Combine($aspnet_runtime_path, $runtime_version, "*.dll") + '"'
            Invoke-ToolCommand -tool $ilverify -cmd $command -error_msg "found corrupted assembly rewriting"
        }

        Invoke-DotnetTest -dotnet $dotnet -project $($kvp.Name) -target $target `
            -filter $filter -logger $logger -framework $f -verbosity $v
    }
}

Write-Comment -prefix "." -text "Done" -color "green"
