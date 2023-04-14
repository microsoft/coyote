# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Import-Module $PSScriptRoot/../Scripts/common.psm1 -Force

$framework = "net7.0"
$targets = [ordered]@{
    "rewriting" = "Tests.Rewriting"
    "rewriting-helpers" = "Tests.Rewriting.Helpers"
    "testing" = "Tests.BugFinding"
    "actors" = "Tests.Actors"
    "actors-testing" = "Tests.Actors.BugFinding"
}

$expected_hashes = [ordered]@{
    "rewriting" = "1E8A0C0345C0179329AC170B0941E86A4DC9E6050F36DE2DDA1C84A9ED82D8C0"
    "rewriting-helpers" = "456BEB2AF21548054CA2E96F6C2EDDED1A0BBAF7D91AF06E87D40C90825BF27A"
    "testing" = "0F6D58F0B176611E475D70A9E6D00B2AA5BACA1ECEEB8432E3F61418E4A3FA86"
    "actors" = "D1549B451024EDF9E5D8F49CA755178E7793699D6C6F12A3ECD96F0AF2B5A108"
    "actors-testing" = "AD367B501456F79CB6E7B29B4BC48BD1A7B8982790888C0F2F75B3265434C8E3"
}

Write-Comment -prefix "." -text "Comparing the test rewriting diff logs" -color "yellow"

# Compare all IL diff logs.
$succeeded = $true
foreach ($kvp in $targets.GetEnumerator()) {
    $project = $($kvp.Value)
    if ($project -eq $targets["actors"]) {
        $project = $targets["actors-testing"]
    } elseif ($project -eq $targets["rewriting-helpers"]) {
        $project = $targets["rewriting"]
    }

    $new = "$PSScriptRoot/$project/bin/$framework/Microsoft.Coyote.$($kvp.Value).diff.json"
    $new_hash = $(Get-FileHash $new).Hash
    Write-Comment -prefix "..." -text "Computed IL diff hash '$new_hash' for '$($kvp.Value)' project"
    $expected_hash = $expected_hashes[$($kvp.Key)]
    if ($new_hash -ne $expected_hash) {
        Write-Error "The '$($kvp.Value)' project's IL diff hash '$new_hash' is not the expected '$expected_hash'."
        $succeeded = $false
    }
}

if (-not $succeeded) {
    exit 1
}

Write-Comment -prefix "." -text "Done" -color "green"
