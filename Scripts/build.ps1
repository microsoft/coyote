param(
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release"
)

$ScriptDir = $PSScriptRoot

Import-Module $ScriptDir\powershell\common.psm1

Write-Comment -prefix "." -text "Building Coyote" -color "yellow"

# Check that the expected .NET SDK is installed.
$dotnet = "dotnet"
$dotnet_path = FindDotNet($dotnet)
$sdk_version = FindDotNetSdk($dotnet_path);

if ($null -eq $sdk_version)
{
    Write-Comment -text "The global.json file is pointing to version: $sdk_version but no matching version was found" -color "yellow"
    Write-Comment -text "Please install .NET SDK version $sdk_version from https://dotnet.microsoft.com/download/dotnet-core." -color "yellow"
    exit 1
}

Write-Comment -text "Using .NET SDK version $sdk_version" -color yellow

Write-Comment -prefix "..." -text "Configuration: $configuration" -color "white"
$solution = $ScriptDir + "\..\Coyote.sln"
$command = "build -c $configuration $solution"
$error_msg = "Failed to build Coyote"
Invoke-ToolCommand -tool $dotnet -command $command -error_msg $error_msg

Write-Comment -prefix "." -text "Successfully built Coyote" -color "green"
