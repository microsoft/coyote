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
    "rewriting" = "8C2120F12EB0D078C8DAAE3EC2DF8A726B2ACE8EEB95D72627827CE2D3D16DE9"
    "testing" = "11674E359626C7D2DC694F67CCA4A65D131BB7F423BD1ED7D9A4CC53A0C6B799"
    "actors" = "9A863B22CD587208BEAC37AD07A8672D94CD32D035D750A2D8D72AAF363FB9C7"
    "actors-testing" = "4AB2024003281520B29BE30D70560F49CE5CD4C6BDBDD6BDFCF044133896F2D5"
    "standalone" = "C25C82E533BABC560742359491DBB58F262AE739284B16CA395E2534F7CF295C"
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
    Write-Comment -prefix "..." -text "Microsoft.Coyote.$($kvp.Value) has IL diff hash '$new_hash'" -color "white"
    $expected_hash = $expected_hashes[$($kvp.Key)]
    if ($new_hash -ne $expected_hash) {
        Write-Error "Microsoft.Coyote.$($kvp.Value) IL diff hash '$new_hash' is not the expected '$expected_hash'."
        exit 1
    }
}

Write-Comment -prefix "." -text "Done" -color "green"
