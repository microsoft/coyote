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

Write-Comment -prefix "." -text "Comparing the Coyote rewriting diff logs" -color "yellow"

$log_dir = "$PSScriptRoot/Tests.Rewriting.Diff/Logs"
if (Test-Path -Path $log_dir) {
    Remove-Item -Path $log_dir -Recurse -Force
}

# Decompressing the IL diff logs.
Expand-Archive -LiteralPath "$log_dir.zip" -DestinationPath $log_dir -Force

# Compare all IL diff logs.
foreach ($kvp in $targets.GetEnumerator()) {
    $project = $($kvp.Value)
    if ($project -eq $targets["actors"]) {
        $project = $targets["actors-testing"]
    }

    $new = "$PSScriptRoot/$project/bin/$framework/Microsoft.Coyote.$($kvp.Value).diff.json"
    $original = "$log_dir/Microsoft.Coyote.$($kvp.Value).diff.json"

    if ($(Get-FileHash $new).Hash -ne $(Get-FileHash $original).Hash) {
        Write-Error "IL diff for Microsoft.Coyote.$($kvp.Value) is not matching."
        exit 1
    }
}

Write-Comment -prefix "." -text "Done" -color "green"
