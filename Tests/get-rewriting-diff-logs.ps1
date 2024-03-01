# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

Import-Module $PSScriptRoot/../Scripts/common.psm1 -Force

$framework = "net8.0"
$targets = [ordered]@{
    "rewriting" = "Tests.Rewriting"
    "rewriting-helpers" = "Tests.Rewriting.Helpers"
    "testing" = "Tests.BugFinding"
    "actors" = "Tests.Actors"
    "actors-testing" = "Tests.Actors.BugFinding"
}

Write-Comment -prefix "." -text "Gathering the test rewriting diff logs" -color "yellow"

# Get all IL diff logs.
foreach ($kvp in $targets.GetEnumerator()) {
    $project = $($kvp.Value)
    if ($project -eq $targets["actors"]) {
        $project = $targets["actors-testing"]
    } elseif ($project -eq $targets["rewriting-helpers"]) {
        $project = $targets["rewriting"]
    }

    $suffix = "diff.json"
    $fileName = "Microsoft.Coyote.$($kvp.Value)"
    $path = "$PSScriptRoot/$project/bin/$framework/$fileName.$suffix"
    $destination = "$PSScriptRoot/$fileName.$suffix"
    if (Test-Path -path $destination) {
        $destination = "$PSScriptRoot/$fileName.new.$suffix"
    }

    Copy-Item -Path $path -Destination $destination -Force
}

Write-Comment -prefix "." -text "Done" -color "green"
