# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Import-Module $PSScriptRoot/../Scripts/powershell/common.psm1 -Force

$framework = "net5.0"
$targets = [ordered]@{
    "rewriting" = "Tests.Rewriting"
    "testing" = "Tests.BugFinding"
    "actors" = "Tests.Actors"
    "actors-testing" = "Tests.Actors.BugFinding"
    "standalone" = "Tests.Standalone"
}

$expected_hashes = [ordered]@{
    "rewriting" = "3AE655C9586D10AC0D599226CEBC7314E8F743B4E6BF921DF22B98181F87648D"
    "testing" = "FF77454582428126712A51C8EAF910444505DA38BC0BE56659CF3BDD9E8845F5"
    "actors" = "38744E42DDD648ED448ABDF083A24875F2812C0A6FB350C121B916665BC1E9E9"
    "actors-testing" = "B6898714431A66E4C1C4D6A3F978BD25906374F8E54D27D7924D4BDF2424F8D9"
    "standalone" = "FB0286E1172EFCD94E51E392829BCAADD7E32F7834D71F109FACC155A48BB030"
}

Write-Comment -prefix "." -text "Comparing the test rewriting diff logs" -color "yellow"

# Compare all IL diff logs.
foreach ($kvp in $targets.GetEnumerator()) {
    $project = $($kvp.Value)
    if ($project -eq $targets["actors"]) {
        $project = $targets["actors-testing"]
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
