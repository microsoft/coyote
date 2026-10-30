# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

param(
    [ValidateSet("net10.0", "net8.0")]
    [string]$framework = "net10.0"
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
    "net10.0|rewriting" = "A94C7239176A7E14BFE47080D3044088E6E96F7DAF540773E7EC31BC55F614F9"
    "net10.0|rewriting-helpers" = "DF8CF299C162ECA5392793BF5E3E6D7C8B61A75E029501ED6F41C1DD1AD3183B"
    "net10.0|testing" = "770F326FE6797BFA5DE7FB4A4172EEBA0EDD68EA24B5F1CA2767C2F5DCC4A62E"
    "net10.0|actors" = "4532F902A1C0A9D8F499D5D7512CEBF2E0D1AE83502B3BD2180E4897DC663963"
    "net10.0|actors-testing" = "D11AFDFE4EA1D604423B07E650F5BC4495B04D70E48EA644D38387D3B2775716"
    "net8.0|rewriting" = "3723AE933C0EBE5ACD6B0B3F230D4A0EAFFC18900F55B856A57B89CD799AF09D"
    "net8.0|rewriting-helpers" = "DF8CF299C162ECA5392793BF5E3E6D7C8B61A75E029501ED6F41C1DD1AD3183B"
    "net8.0|testing" = "39679B639903B72EB6A0B4A93B3F5389E6E548B0DFFA28C55C1AB9D5926421A0"
    "net8.0|actors" = "7E219081D30C11F60AC0689EA86E7F809795FFB07C07CA86B378DB6FBAF54349"
    "net8.0|actors-testing" = "29D71EE8298B402FF3477D5EE89639C22B5295027B3C09EE366373DAE37A5D59"
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
