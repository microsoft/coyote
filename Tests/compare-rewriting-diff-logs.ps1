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
    "rewriting" = "F2005E7C7C3B9A743193B14E612B3DBD3A73BF8FB4FDDF4755EAB3F0AFFEC873"
    "rewriting-helpers" = "DAA8156B3911954999DEB6FE30FFC9691F87FDDCEB8E14EC7B986D162F194BD5"
    "testing" = "B229C3D078A2D509C89C0CD4FDB8054023EF73626949090D354A1EA19EFEBBFD"
    "actors" = "A72210DC9BEF7A6961C5758E8E425388755C7490CEB51D55C2F3A8F73AA5548C"
    "actors-testing" = "AC7AB8C3FB4C6657C96A4FDCC581070A4483A6F6C8B865074A568389D59C144D"
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
