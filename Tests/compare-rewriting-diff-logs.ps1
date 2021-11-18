# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Import-Module $PSScriptRoot/../Scripts/powershell/common.psm1 -Force

$framework = "net6.0"
$targets = [ordered]@{
    "rewriting" = "Tests.Rewriting"
    "rewriting-helpers" = "Tests.Rewriting.Helpers"
    "testing" = "Tests.BugFinding"
    "actors" = "Tests.Actors"
    "actors-testing" = "Tests.Actors.BugFinding"
}

$expected_hashes = [ordered]@{
    "rewriting" = "C174400B3E1C10D9AF4986E597C5E595BB46E578AA3785B39B5C3018519C0EAD"
    "rewriting-helpers" = "1867813B408FC0B20FD9DB972952968EAD40E54068D0BFB1BE269FFDBB32FB5F"
    "testing" = "954FF5FC53F7E0A16D39FF1A987C5074744F1D3A2C652630610770E265EF03FB"
    "actors" = "E8146A76F0A6C16F2468A80B9D48AF2AD3E96D9E1FC42624F82765DC02D81D3B"
    "actors-testing" = "400EC56DF424EF4556F7D631744613A60CDDE0D014C69B4768996DB4D30867B4"
}

Write-Comment -prefix "." -text "Comparing the test rewriting diff logs" -color "yellow"

# Compare all IL diff logs.
foreach ($kvp in $targets.GetEnumerator()) {
    $project = $($kvp.Value)
    if ($project -eq $targets["actors"]) {
        $project = $targets["actors-testing"]
    } elseif ($project -eq $targets["rewriting-helpers"]) {
        $project = $targets["rewriting"]
    }

    $new = "$PSScriptRoot/$project/bin/$framework/Microsoft.Coyote.$($kvp.Value).diff.json"
    $new_hash = $(Get-FileHash $new).Hash
    Write-Comment -prefix "..." -text "Computed IL diff hash '$new_hash' for '$($kvp.Value)' project" -color "white"
    $expected_hash = $expected_hashes[$($kvp.Key)]
    if ($new_hash -ne $expected_hash) {
        Write-Error "The '$($kvp.Value)' project's IL diff hash '$new_hash' is not the expected '$expected_hash'."
        exit 1
    }
}

Write-Comment -prefix "." -text "Done" -color "green"
