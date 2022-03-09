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
    "rewriting" = "09A2A9A00C6D125EC128FEBF5D2EFE69D1B02651E73D02FBFF8D6C4D4971F7EF"
    "rewriting-helpers" = "BB40BC833E9417CDAC5BCDAA2E7B72F6BA43E3EE1E40D51DF60DBEECBA43F28B"
    "testing" = "8B45ADC814D89BABD08A519829114947D8DC231E9BF5EDA64D1FEF9DF682A425"
    "actors" = "4DCD75CF2E1AF45A74DCD7A1D2F2BC4669D2EDDC26C8C15EB4C40821CD76C89A"
    "actors-testing" = "D4D2EE760DABF90D02E2E85068AAC4B714F3DD25BE61652B973A86132E8638F6"
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
