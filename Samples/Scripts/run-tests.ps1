param(
    [string]$dotnet="dotnet"
)

Import-Module $PSScriptRoot/../../Scripts/common.psm1 -Force
CheckPSVersion

Write-Comment -prefix "." -text "Testing the Coyote samples" -color "yellow"

$framework = "net7.0"
$tests = "$PSScriptRoot/../Common/bin/$framework/TestDriver.dll"
if (-not (Test-Path $tests)) {
    Write-Error "tests for the Coyote samples not found."
    exit
}

$error_msg = "Failed to test the Coyote samples"
Invoke-ToolCommand -tool $dotnet -cmd $tests -error_msg $error_msg

Write-Comment -prefix "." -text "Successfully tested the Coyote samples" -color "green"
