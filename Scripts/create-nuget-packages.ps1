param(
    [string]$nuget="$PSScriptRoot\NuGet\nuget.exe"
)

Import-Module $PSScriptRoot\powershell\common.psm1

$nuget_exe_dir = "$PSScriptRoot\NuGet"
$nuget_exe_url = "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe"

Write-Comment -prefix "." -text "Creating the Coyote NuGet packages" -color "yellow"
if (-not (Test-Path $nuget)) {
    Write-Comment -prefix "..." -text "Downloading latest 'nuget.exe'" -color "white"
    Invoke-WebRequest "$nuget_exe_url" -OutFile "$nuget_exe_dir\nuget.exe"
    if (-not (Test-Path $nuget)) {
        Write-Error "Unable to download 'nuget.exe'. Please download '$nuget_exe_url' and place in '$nuget_exe_dir\' directory."
        exit 1
    }
    Write-Comment -prefix "..." -text "Installed 'nuget.exe' in '$nuget_exe_dir'" -color "white"
}

if (Test-Path $PSScriptRoot\..\bin\nuget) {
    Remove-Item $PSScriptRoot\..\bin\nuget\*
}

$command = "pack $nuget_exe_dir\Coyote.nuspec -OutputDirectory $PSScriptRoot\..\bin\nuget"
$error_msg = "Failed to create the Coyote NuGet packages"
Invoke-ToolCommand -tool $nuget -command $command -error_msg $error_msg

Write-Comment -prefix "." -text "Successfully created the Coyote NuGet packages" -color "green"
