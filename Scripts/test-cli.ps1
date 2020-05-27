$ErrorActionPreference = 'Stop'

$root = $ENV:SYSTEMROOT
if ($null -ne $root -and $root.ToLower().Contains("windows")) {

    $coyote_path = $ENV:PATH.split([System.IO.Path]::PathSeparator) | Where-Object { Test-Path "$_/coyote.exe" }

    if (-not $coyote_path -eq "")
    {
        Write-Host "Uninstalling Microsoft.Coyote.CLI..."
        dotnet tool uninstall --global Microsoft.Coyote.CLI
    }

    Write-Host "Installing Microsoft.Coyote.CLI..."
    dotnet tool install --global --add-source $PSScriptRoot/../bin/nuget Microsoft.Coyote.CLI --no-cache
    if (!$?)
    {
        Exit 1
    }

    $profile = $Env:USERPROFILE
    $Env:Path += "$profile\.dotnet\tools"
}
else
{
    $coyote_path = $ENV:PATH.split([System.IO.Path]::PathSeparator) | Where-Object { Test-Path "$_/coyote" }

    if (-not $coyote_path -eq "")
    {
        Write-Host "Uninstalling coyote..."
        dotnet tool uninstall --global coyote
    }

    $profile = $Env:HOME
    $Env:Path += "$profile\.dotnet\tools"

    Write-Host "Installing coyote..."
    dotnet tool install --global --add-source $PSScriptRoot/../bin coyote --no-cache
}

$help = (coyote -?) -join '\n'

if (!$help.Contains("Microsoft (R) Coyote version"))
{
    Write-Host "### Unexpected output from coyote command"
    Write-Host $help
    Exit 1
}

Write-Host "Test passed"
Exit 0