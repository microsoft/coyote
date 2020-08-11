$ErrorActionPreference = 'Stop'

Import-Module $PSScriptRoot\powershell\common.psm1 -Force

Write-Comment -prefix "." -text "Running the Coyote CLI package test" -color "yellow"

$root = $ENV:SYSTEMROOT
if ($null -ne $root -and $root.ToLower().Contains("windows")) {
    $coyote_path = $ENV:PATH.split([System.IO.Path]::PathSeparator) | Where-Object { Test-Path "$_/coyote.exe" }

    if (-not $coyote_path -eq "") {
        Write-Comment -prefix "..." -text "Uninstalling the Microsoft.Coyote.CLI package" -color "white"
        dotnet tool uninstall --global Microsoft.Coyote.CLI
    }

    Write-Comment -prefix "..." -text "Installing the Microsoft.Coyote.CLI package" -color "white"
    dotnet tool install --global --add-source $PSScriptRoot/../bin/nuget Microsoft.Coyote.CLI --no-cache
    if (!$?) {
        Exit 1
    }

    $profile = $Env:USERPROFILE
    $Env:Path += "$profile\.dotnet\tools"
}
else {
    $coyote_path = $ENV:PATH.split([System.IO.Path]::PathSeparator) | Where-Object { Test-Path "$_/coyote" }

    if (-not $coyote_path -eq "") {
        Write-Comment -prefix "..." -text "Uninstalling the coyote .NET tool" -color "white"
        dotnet tool uninstall --global coyote
    }

    $profile = $Env:HOME
    $Env:Path += "$profile\.dotnet\tools"

    Write-Comment -prefix "..." -text "Installing the coyote .NET tool" -color "white"
    dotnet tool install --global --add-source $PSScriptRoot/../bin coyote --no-cache
}

$help = (coyote -?) -join '\n'

if (!$help.Contains("usage: Coyote command path")) {
    Write-Error "### Unexpected output from coyote command"
    Write-Error $help
    Exit 1
}

Write-Comment -prefix "." -text "Test passed" -color "green"
Exit 0
