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
    "rewriting" = "B92E9E4014815733CC634B9F4F86359C2112B8293629FA67321059AB8D3CBE92"
    "rewriting-helpers" = "F355C7286FC8F9AD4E4FC030C90B4E2E5AE605E471994A60DAC07227AD8B0191"
    "testing" = "BD3105726FAE08511856B6CCD1A3C20101116F5443686687FD50007A3D9CB7D5"
    "actors" = "0E61202FE4FBAB38ABC0092590E89B83F0746DF8E54AE397C933021E5CB30DB0"
    "actors-testing" = "EAAE0631B9A0781240325AD5DDAF367134FAC90032AEE490E8C656E13560844D"
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
