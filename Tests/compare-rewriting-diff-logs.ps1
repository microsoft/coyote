# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

param(
    [ValidateSet("net11.0", "net10.0", "net8.0")]
    [string]$framework = "net11.0"
)

Import-Module $PSScriptRoot/../Scripts/common.psm1 -Force

$targets = [ordered]@{
    "rewriting" = "Tests.Rewriting"
    "rewriting-helpers" = "Tests.Rewriting.Helpers"
    "testing" = "Tests.BugFinding"
    "actors" = "Tests.Actors"
    "actors-testing" = "Tests.Actors.BugFinding"
}

$expected_hashes = [ordered]@{
    "net11.0|rewriting" = "360BB3881F0EDB1B6FB4AF01285E4EB9A97D6C6826D7E0DEEB6D70FF3B9F4967"
    "net11.0|rewriting-helpers" = "6C25B8F64593309BD37E258A2C59683FB56B63B281AA20A9B41015D2BFDD2D85"
    "net11.0|testing" = "912C47625E1E4D0528F63C5BD65640EF0B4A4F92D913D67D0329F84B1C9A5455"
    "net11.0|actors" = "DB9D2B2F42B3882267ABE983DD088CB36CE806BC9853B28B43691335BC1254BC"
    "net11.0|actors-testing" = "2DCA68B878FE20E9EA59203DAEA20C331875155BF4A915D593C89FF729D3C54B"
    "net10.0|rewriting" = "360BB3881F0EDB1B6FB4AF01285E4EB9A97D6C6826D7E0DEEB6D70FF3B9F4967"
    "net10.0|rewriting-helpers" = "6C25B8F64593309BD37E258A2C59683FB56B63B281AA20A9B41015D2BFDD2D85"
    "net10.0|testing" = "912C47625E1E4D0528F63C5BD65640EF0B4A4F92D913D67D0329F84B1C9A5455"
    "net10.0|actors" = "DB9D2B2F42B3882267ABE983DD088CB36CE806BC9853B28B43691335BC1254BC"
    "net10.0|actors-testing" = "2DCA68B878FE20E9EA59203DAEA20C331875155BF4A915D593C89FF729D3C54B"
    "net8.0|rewriting" = "DFE1EAAC34880A98321121D81A7DA4E8EEF3CAB35BB1A56159CE9F4A14846971"
    "net8.0|rewriting-helpers" = "6C25B8F64593309BD37E258A2C59683FB56B63B281AA20A9B41015D2BFDD2D85"
    "net8.0|testing" = "101B7AFE57751B98ACC5F1F4225ADB86C7160B7BD4AFAA270D90AE2ACAD82152"
    "net8.0|actors" = "0BF6ADDEB428B9DE17BCF2A6B4EFC97A680674F6ADB85641526CD7AAAF6102AC"
    "net8.0|actors-testing" = "8A3F2C008BB108C5C587DECF3205B10817E316D489CB670332119C460460AC4D"
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
    $expected_hash = $expected_hashes["$framework|$($kvp.Key)"]
    if ($new_hash -ne $expected_hash) {
        Write-Error "The '$($kvp.Value)' project's IL diff hash '$new_hash' is not the expected '$expected_hash'."
        $succeeded = $false
    }
}

if (-not $succeeded) {
    exit 1
}

Write-Comment -prefix "." -text "Done" -color "green"
