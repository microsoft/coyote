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

Write-Comment -prefix "." -text "Copying the Coyote rewriting diff logs" -color "yellow"

$log_dir = "$PSScriptRoot/Tests.Rewriting.Diff/Logs"
if (-not (Test-Path -Path $log_dir)) {
    New-Item -Path $log_dir -ItemType Directory
}

# Copy all diff logs.
foreach ($kvp in $targets.GetEnumerator()) {
    $project = $($kvp.Value)
    if ($project -eq $targets["actors"]) {
        $project = $targets["actors-testing"]
    }

    Copy-Item "$PSScriptRoot/$project/bin/$framework/Microsoft.Coyote.$($kvp.Value).diff.json" $log_dir
}

# Compressing the logs.
Compress-Archive -LiteralPath $log_dir -DestinationPath "$log_dir.zip" -Force

Write-Comment -prefix "." -text "Done" -color "green"
