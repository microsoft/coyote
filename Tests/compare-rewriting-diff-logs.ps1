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
    "rewriting" = "EB18121F7B1F87FC759D861A366E61E51AFCA22B9223C28649F9C996E2C3EE98"
    "rewriting-helpers" = "456BEB2AF21548054CA2E96F6C2EDDED1A0BBAF7D91AF06E87D40C90825BF27A"
    "testing" = "3A87A143E5E32600BFAB67CB6F25A98A18BFFC9F8A3F97855322B0A3563726C1"
    "actors" = "0FB1A527FD024C7663755DB5609BCF172F799E07992913F4D48822198387E040"
    "actors-testing" = "BBA7582B5212A52F2E227A766C66AD5D70F9A908FD0C419F66C9A6226F2A67BB"
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
