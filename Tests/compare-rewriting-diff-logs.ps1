# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Import-Module $PSScriptRoot/../Scripts/common.psm1 -Force

$framework = "net6.0"
$targets = [ordered]@{
    "rewriting" = "Tests.Rewriting"
    "rewriting-helpers" = "Tests.Rewriting.Helpers"
    "testing" = "Tests.BugFinding"
    "actors" = "Tests.Actors"
    "actors-testing" = "Tests.Actors.BugFinding"
}

$expected_hashes = [ordered]@{
    "rewriting" = "D2B4D0086F5C1ABA4EF816E29AEA252B010388F720DD949E96BB4B8BD1928683"
    "rewriting-helpers" = "723ADDB6816252461FD49E1673619EC62734FF27BA54A6850512F9048387D521"
    "testing" = "FC5C1D203559E87B009C5C721BE5A8E6E9D99990CCD5F68CCB228E21357530F4"
    "actors" = "0ED909199FD1D55F08806784A956AE1C3FE82D1E74C90B4C444BEE8001E51CE0"
    "actors-testing" = "CF0BD83290E41792AD86D90C926A7B9FA55B0A9842EDC18DE336527865F59D6A"
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
