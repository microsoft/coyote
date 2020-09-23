param(
    [ValidateSet("Debug", "Release")]
    [string]$configuration = "Release"
)

$ScriptDir = $PSScriptRoot

Import-Module $ScriptDir\powershell\common.psm1 -Force

Write-Comment -prefix "." -text "Building Coyote" -color "yellow"

# Check that the expected .NET SDK is installed.
$dotnet = "dotnet"
$dotnet_path = FindDotNet($dotnet)
$version31 = FindInstalledDotNetSdk -dotnet_path $dotnet_path -major "3.1" -minor 0
$sdk_version = FindDotNetSdk -dotnet_path $dotnet_path

if ($null -eq $sdk_version) {
    Write-Error "The global.json file is pointing to version '$sdk_version' but no matching version was found."
    Write-Error "Please install .NET SDK version '$sdk_version' from https://dotnet.microsoft.com/download/dotnet-core."
    exit 1
}

Write-Comment -prefix "..." -text "Using .NET SDK version $sdk_version" -color "white"

Write-Comment -prefix "..." -text "Configuration: $configuration" -color "white"
$solution = $ScriptDir + "\..\Coyote.sln"
$command = "build -c $configuration $solution"
if ($null -ne $version31 -and $version31 -ne $sdk_version) {
    # build dotnet 3.1 as well as the new version.
    $command = $command + " /p:DOTNET31=yes"
}
$error_msg = "Failed to build Coyote"
Invoke-ToolCommand -tool $dotnet -cmd $command -error_msg $error_msg

Write-Comment -prefix "." -text "Successfully built Coyote" -color "green"
