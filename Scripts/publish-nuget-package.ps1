param(
    [string]$api_key="",
    [string]$source="https://www.nuget.org/api/v2/"
)

if ($api_key -eq ""){
    Write-Error "Please provide api-key for the nuget push command"
    exit 1
}

Import-Module $PSScriptRoot\powershell\common.psm1

Write-Comment -prefix "." -text "Uploading the Coyote Nuget package to http://www.nuget.org" -color "yellow"

$package_dir = "$PSScriptRoot\..\bin\nuget"

$package = (Get-ChildItem -Path $package_dir\*.* -Filter *.nupkg)

if ($null -eq $package) {
    Write-Error "Found no nuget packages in $package_dir"
    exit 1
}

if ($package -is [array]) {
    Write-Error "Too many nuget packages in $package_dir"
    exit 1
}

Write-Host "Uploading package: $package"

$nuget_exe = "$PSScriptRoot\NuGet\NuGet.exe"

if (-not (Test-Path $nuget_exe)) {
    Write-Error "Unable to find the nuget.exe in ($nuget_exe), please run create-nuget-package.ps1 first."
    exit 1
}

$command = "push $package $api_key -Source $source"
Invoke-ToolCommand -tool $nuget_exe -command $command -error_msg $error_msgpackage
