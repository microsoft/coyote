# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Import-Module $PSScriptRoot/common.psm1 -Force

# Check that NuGet.exe is installed.
$nuget = "nuget"
if (-not (Get-Command $nuget -errorAction SilentlyContinue)) {
    Write-Comment -text "Please install the latest NuGet.exe from https://www.nuget.org/downloads and add it to the PATH environment variable." -color "yellow"
    exit 1
}

Write-Comment -prefix "." -text "Creating the Coyote NuGet packages" -color "yellow"

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

Write-Comment -prefix "..." -text "Creating the 'Microsoft.Coyote' package"

$command = "pack $PSScriptRoot/NuGet/Coyote.nuspec $cmd_options"
$error_msg = "Failed to create the Coyote NuGet package"
Invoke-ToolCommand -tool $nuget -cmd $command -error_msg $error_msg

Write-Comment -prefix "..." -text "Creating the 'Microsoft.Coyote.Test' package"

$command = "pack $PSScriptRoot/NuGet/Coyote.Test.nuspec $cmd_options"
$error_msg = "Failed to create the Coyote Test NuGet package"
Invoke-ToolCommand -tool $nuget -cmd $command -error_msg $error_msg

Write-Comment -prefix "..." -text "Creating the 'Microsoft.Coyote.CLI' package"

$command = "pack $PSScriptRoot/NuGet/Coyote.CLI.nuspec $cmd_options -Tool"
$error_msg = "Failed to create the Coyote CLI NuGet package"
Invoke-ToolCommand -tool $nuget -cmd $command -error_msg $error_msg

Write-Comment -prefix "." -text "Successfully created the Coyote NuGet packages" -color "green"
