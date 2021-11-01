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
    "rewriting" = "611BDAE4DE1DE025116805076A3BC33151BB1ABF1E369D6BEA819345C7BE8818"
    "testing" = "471916B83D10B7665D775817ECBCFCAE129B4859BE147DF414976420920C67C7"
    "actors" = "8A9EAED0963801134DC58273FAF30550482842860FBA52187942601F73FF4110"
    "actors-testing" = "88865B28250700738C4E9A0A7463E8447272C031A4D256C6026D26C92C98240A"
    "standalone" = "ABF26E1DC4CB7F3A65B4508229AF2DDDF896F3E351F3F4A928C86758F55742E0"
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
