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
$version_net48 = (Get-ItemProperty "HKLM:SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full").Release -ge 528040
$version_netcore31 = FindInstalledDotNetSdk -dotnet_path $dotnet_path -major "3.1" -minor 0
$sdk_version = FindDotNetSdk -dotnet_path $dotnet_path

if ($null -eq $sdk_version) {
    Write-Error "The global.json file is pointing to version '$sdk_version' but no matching version was found."
    Write-Error "Please install .NET SDK version '$sdk_version' from https://dotnet.microsoft.com/download/dotnet-core."
    exit 1
}

$PSVersionTable
Write-Output "$($IsMacOS)"
Write-Output "$($IsLinux)"
Write-Output "$($IsWindows)"

Write-Comment -prefix "..." -text "Using .NET SDK version $sdk_version" -color "white"

Write-Comment -prefix "..." -text "Configuration: $configuration" -color "white"
$solution = $ScriptDir + "\..\Coyote.sln"
$command = "build -c $configuration $solution"

if ($version_net48) {
    # build .NET Framework 4.8 as well as the new version.
    $command = $command + " /p:NET48_EXISTS=yes"
}

if ($null -ne $version_netcore31 -and $version_netcore31 -ne $sdk_version) {
    # build .NET Core 3.1 as well as the new version.
    $command = $command + " /p:NETCORE31_EXISTS=yes"
}

$error_msg = "Failed to build Coyote"
Invoke-ToolCommand -tool $dotnet -cmd $command -error_msg $error_msg

Write-Comment -prefix "." -text "Successfully built Coyote" -color "green"
