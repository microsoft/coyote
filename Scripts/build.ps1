# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

param(
    [ValidateSet("Debug", "Release")]
    [string]$configuration = "Release",
    [switch]$nuget,
    [switch]$ci
)

$ScriptDir = $PSScriptRoot

Import-Module $ScriptDir/common.psm1 -Force

CheckPSVersion

Write-Comment -text "Building Coyote." -color "blue"

if ($host.Version.Major -lt 7)
{
    Write-Error "Please use PowerShell v7.x or later (see https://docs.microsoft.com/en-us/powershell/scripting/install/installing-powershell-core-on-windows?view=powershell-7)."
    exit 1
}

# Check that the expected .NET SDK is installed.
$dotnet = "dotnet"
$dotnet_sdk_path = FindDotNetSdkPath -dotnet $dotnet
$version_net4 = $IsWindows -and (Get-ItemProperty "HKLM:SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full").Release -ge 528040
$version_netcore31 = FindMatchingVersion -path $dotnet_sdk_path -version "3.1.0"
$version_net6 = FindMatchingVersion -path $dotnet_sdk_path -version "6.0.0"
$version_net7 = FindMatchingVersion -path $dotnet_sdk_path -version "7.0.0"
$sdk_version = FindDotNetSdkVersion -dotnet_sdk_path $dotnet_sdk_path

if ($null -eq $sdk_version) {
    Write-Error "The global.json file is pointing to version '$sdk_version' but no matching version was found."
    Write-Error "Please install .NET SDK version '$sdk_version' from https://dotnet.microsoft.com/download/dotnet."
    exit 1
}

$extra_frameworks = ""
if ($ci.IsPresent) {
    # Build any supported .NET versions that are installed on this machine.
    if ($version_net4) {
        # Build .NET Framework 4.x as well as the latest version.
        $extra_frameworks = $extra_frameworks + " /p:BUILD_NET462=yes"
    }

    if ($null -ne $version_netcore31 -and $version_netcore31 -ne $sdk_version) {
        # Build .NET Core 3.1 as well as the latest version.
        $extra_frameworks = $extra_frameworks + " /p:BUILD_NETCORE31=yes"
    }

    if ($null -ne $version_net6 -and $version_net6 -ne $sdk_version) {
        # Build .NET 6.0 as well as the latest version.
        $extra_frameworks = $extra_frameworks + " /p:BUILD_NET6=yes"
    }

    if ($null -ne $version_net7 -and $version_net7 -ne $sdk_version) {
        # Build .NET 6.0 as well as the latest version.
        $extra_frameworks = $extra_frameworks + " /p:BUILD_NET7=yes"
    }
}

Write-Comment -text "Using configuration '$configuration'." -color "magenta"
$solution = Join-Path -Path $ScriptDir -ChildPath ".." -AdditionalChildPath "Coyote.sln"
$command = "build -c $configuration /p:Platform=""Any CPU"" $extra_frameworks $solution"

$error_msg = "Failed to build Coyote"
Invoke-ToolCommand -tool $dotnet -cmd $command -error_msg $error_msg

if ($nuget.IsPresent -and $ci.IsPresent) {
    if ($IsWindows) {
        Write-Comment -text "Building the Coyote NuGet packages." -color "blue"
        $cmd = "pack -c $configuration $extra_frameworks"

        Write-Comment -text "Building the 'Microsoft.Coyote.Core' package." -color "magenta"
        $command = "$cmd --no-build $PSScriptRoot/../Source/Core/Core.csproj"
        $error_msg = "Failed to build the 'Microsoft.Coyote.Core' package"
        Invoke-ToolCommand -tool $dotnet -cmd $command -error_msg $error_msg

        Write-Comment -text "Building the 'Microsoft.Coyote.Actors' package." -color "magenta"
        $command = "$cmd --no-build $PSScriptRoot/../Source/Actors/Actors.csproj"
        $error_msg = "Failed to build the 'Microsoft.Coyote.Actors' package"
        Invoke-ToolCommand -tool $dotnet -cmd $command -error_msg $error_msg

        Write-Comment -text "Building the 'Microsoft.Coyote.Test' package." -color "magenta"
        $command = "$cmd --no-build $PSScriptRoot/../Source/Test/Test.csproj"
        $error_msg = "Failed to build the 'Microsoft.Coyote.Test' package"
        Invoke-ToolCommand -tool $dotnet -cmd $command -error_msg $error_msg

        Write-Comment -text "Building the 'Microsoft.Coyote.Tool' package." -color "magenta"
        $command = "$cmd --no-build $PSScriptRoot/../Tools/Coyote/Coyote.csproj"
        $error_msg = "Failed to build the 'Microsoft.Coyote.Tool' package"
        Invoke-ToolCommand -tool $dotnet -cmd $command -error_msg $error_msg

        Write-Comment -text "Building the 'Microsoft.Coyote.CLI' package." -color "magenta"
        $command = "$cmd $PSScriptRoot/../Tools/CLI/Coyote.CLI.csproj"
        $error_msg = "Failed to build the 'Microsoft.Coyote.CLI' package"
        Invoke-ToolCommand -tool $dotnet -cmd $command -error_msg $error_msg

        Write-Comment -text "Building the 'Microsoft.Coyote' meta-package." -color "magenta"
        $command = "$cmd $PSScriptRoot/NuGet/Coyote.Meta.csproj"
        $error_msg = "Failed to build the 'Microsoft.Coyote' meta-package"
        Invoke-ToolCommand -tool $dotnet -cmd $command -error_msg $error_msg
    } else {
        Write-Comment -text "Building the Coyote NuGet packages supports only Windows." -color "yellow"
    }
} elseif ($IsWindows) {
    Write-Comment -text "Skipped building the Coyote NuGet packages (enable with -nuget -ci)." -color "yellow"
}

Write-Comment -text "Done." -color "green"
