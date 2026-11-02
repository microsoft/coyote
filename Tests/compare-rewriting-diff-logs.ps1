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
    "net10.0|rewriting" = "1650285D538EC13DB1ECDDF193C60AC74A8EE8F5AE30F2988DCDC0DB433FE46F"
    "net10.0|rewriting-helpers" = "DF8CF299C162ECA5392793BF5E3E6D7C8B61A75E029501ED6F41C1DD1AD3183B"
    "net10.0|testing" = "6B07752FB928FE84A1AD49A9155BC9B1EC81A86E654CFACE2986C8657A7C1575"
    "net10.0|actors" = "4532F902A1C0A9D8F499D5D7512CEBF2E0D1AE83502B3BD2180E4897DC663963"
    "net10.0|actors-testing" = "D11AFDFE4EA1D604423B07E650F5BC4495B04D70E48EA644D38387D3B2775716"
    "net8.0|rewriting" = "1DCA2063224F764DDEF498B696050248A60056924E33824551F698CA422C19B9"
    "net8.0|rewriting-helpers" = "DF8CF299C162ECA5392793BF5E3E6D7C8B61A75E029501ED6F41C1DD1AD3183B"
    "net8.0|testing" = "E7B9BF6E072958891BB746AB2AB5616C75721EFF66C02A559873545BC34B41A2"
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
