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
$version_net5 = FindMatchingVersion -path $dotnet_sdk_path -version "5.0.0"
$sdk_version = FindDotNetSdkVersion -dotnet_sdk_path $dotnet_sdk_path

if ($null -eq $sdk_version) {
    Write-Error "The global.json file is pointing to version '$sdk_version' but no matching version was found."
    Write-Error "Please install .NET SDK version '$sdk_version' from https://dotnet.microsoft.com/download/dotnet."
    exit 1
}

Write-Comment -text "Using configuration '$configuration'." -color "magenta"
$solution = Join-Path -Path $ScriptDir -ChildPath ".." -AdditionalChildPath "Coyote.sln"
$command = "build -c $configuration $solution /p:Platform=""Any CPU"""

if ($ci.IsPresent) {
    # Build any supported .NET versions that are installed on this machine.
    if ($version_net4) {
        # Build .NET Framework 4.x as well as the latest version.
        $command = $command + " /p:BUILD_NET462=yes"
    }

    if ($null -ne $version_netcore31 -and $version_netcore31 -ne $sdk_version) {
        # Build .NET Core 3.1 as well as the latest version.
        $command = $command + " /p:BUILD_NETCORE31=yes"
    }

    if ($null -ne $version_net5 -and $version_net5 -ne $sdk_version) {
        # Build .NET 5.0 as well as the latest version.
        $command = $command + " /p:BUILD_NET5=yes"
    }
}

$error_msg = "Failed to build Coyote"
Invoke-ToolCommand -tool $dotnet -cmd $command -error_msg $error_msg

if ($nuget.IsPresent) {
    if ($IsWindows) {
        # Check that NuGet.exe is installed.
        $nuget_cli = "nuget"
        if (-not (Get-Command $nuget_cli -errorAction SilentlyContinue)) {
            Write-Comment -text "Please install the latest NuGet.exe from https://www.nuget.org/downloads and add it to the PATH environment variable." -color "yellow"
            exit 1
        }

        Write-Comment -text "Building the Coyote NuGet packages." -color "blue"

        # Extract the package version.
        $version_file = "$PSScriptRoot/../Common/version.props"
        $version_node = Select-Xml -Path $version_file -XPath "/" | Select-Object -ExpandProperty Node
        $version = $version_node.Project.PropertyGroup.VersionPrefix
        $version_suffix = $version_node.Project.PropertyGroup.VersionSuffix

        # Setup the command line options for nuget pack.
        $cmd_options = "-OutputDirectory $PSScriptRoot/../bin/nuget -Version $version"
        $cmd_options = "$cmd_options -Symbols -SymbolPackageFormat snupkg"
        if ($version_suffix) {
            $cmd_options = "$cmd_options -Suffix $version_suffix"
        }

        Write-Comment -text "Creating the 'Microsoft.Coyote' package." -color "magenta"
        $command = "pack $PSScriptRoot/NuGet/Coyote.nuspec $cmd_options"
        $error_msg = "Failed to build the Coyote NuGet package"
        Invoke-ToolCommand -tool $nuget_cli -cmd $command -error_msg $error_msg

        Write-Comment -text "Creating the 'Microsoft.Coyote.Test' package." -color "magenta"
        $command = "pack $PSScriptRoot/NuGet/Coyote.Test.nuspec $cmd_options"
        $error_msg = "Failed to build the Coyote Test NuGet package"
        Invoke-ToolCommand -tool $nuget_cli -cmd $command -error_msg $error_msg

        Write-Comment -text "Creating the 'Microsoft.Coyote.CLI' package." -color "magenta"
        $command = "pack $PSScriptRoot/NuGet/Coyote.CLI.nuspec $cmd_options -Tool"
        $error_msg = "Failed to build the Coyote CLI NuGet package"
        Invoke-ToolCommand -tool $nuget_cli -cmd $command -error_msg $error_msg
    } else {
        Write-Comment -text "Building the Coyote NuGet packages supports only Windows." -color "yellow"
    }
} elseif ($IsWindows) {
    Write-Comment -text "Skipped building the Coyote NuGet packages (enable with -nuget)." -color "yellow"
}

Write-Comment -text "Done." -color "green"
