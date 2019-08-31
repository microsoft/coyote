param(
    [string]$dotnet="dotnet",
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release"
)

Import-Module $PSScriptRoot\..\Scripts\powershell\common.psm1

Write-Comment -prefix "." -text "Building the Coyote framework samples" -color "yellow"
Write-Comment -prefix "..." -text "Configuration: $configuration" -color "white"

$solution = $PSScriptRoot + "\Samples.sln"
$command = "build -c $configuration $solution"
$error_msg = "Failed to build the Coyote framework samples"

Invoke-ToolCommand -tool $dotnet -command $command -error_msg $error_msg

Write-Comment -prefix "." -text "Successfully built the Coyote framework samples" -color "green"
