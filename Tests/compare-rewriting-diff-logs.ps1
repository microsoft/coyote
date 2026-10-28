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
    "net10.0|rewriting" = "DA9F6666584D95DD2C771DDA59D986EA3B8F590C0B40EB418B4F03BDCDAF53AA"
    "net10.0|rewriting-helpers" = "DF8CF299C162ECA5392793BF5E3E6D7C8B61A75E029501ED6F41C1DD1AD3183B"
    "net10.0|testing" = "D1CDF37F45A3DA828FC2F59DE059DC42AFEADB346157F4C2B0E55F67EC6C2AF3"
    "net10.0|actors" = "4532F902A1C0A9D8F499D5D7512CEBF2E0D1AE83502B3BD2180E4897DC663963"
    "net10.0|actors-testing" = "D11AFDFE4EA1D604423B07E650F5BC4495B04D70E48EA644D38387D3B2775716"
    "net8.0|rewriting" = "3723AE933C0EBE5ACD6B0B3F230D4A0EAFFC18900F55B856A57B89CD799AF09D"
    "net8.0|rewriting-helpers" = "DF8CF299C162ECA5392793BF5E3E6D7C8B61A75E029501ED6F41C1DD1AD3183B"
    "net8.0|testing" = "744150251F7BB2293EAC43522E91869B021E390B8CA6BFE82F85AC63DBF5F72B"
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
