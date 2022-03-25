# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

$ErrorActionPreference = 'Stop'

Import-Module $PSScriptRoot\powershell\common.psm1 -Force

Write-Comment -prefix "." -text "Running the Coyote CLI package test" -color "yellow"

$root = $ENV:SYSTEMROOT
if ($null -ne $root -and $root.ToLower().Contains("windows")) {
    $coyote_path = $ENV:PATH.split([System.IO.Path]::PathSeparator) | Where-Object { Test-Path "$_/coyote.exe" }

    if (-not "$PSScriptRoot/../temp/coyote" -eq "") {
        Write-Comment -prefix "..." -text "Uninstalling the Microsoft.Coyote.CLI package"
        dotnet tool uninstall Microsoft.Coyote.CLI --tool-path temp
    }

    Write-Comment -prefix "..." -text "Installing the Microsoft.Coyote.CLI package"
    dotnet tool install --add-source $PSScriptRoot/../bin/nuget Microsoft.Coyote.CLI --no-cache --tool-path temp
}
else {
    $coyote_path = $ENV:PATH.split([System.IO.Path]::PathSeparator) | Where-Object { Test-Path "$_/coyote" }

    if (-not "$PSScriptRoot/../temp/coyote" -eq "") {
        Write-Comment -prefix "..." -text "Uninstalling the coyote .NET tool"
        dotnet tool uninstall coyote --tool-path temp
    }

    Write-Comment -prefix "..." -text "Installing the coyote .NET tool"
    dotnet tool install --add-source $PSScriptRoot/../bin coyote --no-cache --tool-path temp
}

$help = (& "$PSScriptRoot/../temp/coyote" -?) -join '\n'

if (!$help.Contains("coyote [command] [options]")) {
    Write-Error "### Unexpected output from coyote command"
    Write-Error $help
    Exit 1
}

Write-Comment -prefix "." -text "Test passed" -color "green"
Exit 0
