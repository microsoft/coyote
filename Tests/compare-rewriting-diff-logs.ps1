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
    "rewriting" = "2AD7B1BC91F600EA83A9BF4C4298E872538630A674E383CB5E2E85921CF48A9B"
    "rewriting-helpers" = "926BF5511B145986142667EB23C37C010891820F1087657B94DCA71F48EA70AD"
    "testing" = "456389C4EB33BC8D9731DCAC912B56AB0F1D48391CE46633B80344562B9723FB"
    "actors" = "DF1332130CED3477523AAC8D9A2D18F395DF6C9905DA54B243CE81CBDBEA6D07"
    "actors-testing" = "D2506412E9DB0B1E4FCEAB1F7A7D2FC58FAAA685A024535BF041BA7BFAD63607"
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
