param(
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release"
)

Import-Module $PSScriptRoot\powershell\common.psm1

Write-Comment -prefix "." -text "Building Coyote" -color "yellow"

# Check that the expected .NET SDK is installed.
$dotnet = "dotnet"
$dotnet_path = $ENV:PATH.split(';') | ForEach-Object {
    if (Test-Path -Path "$_\$dotnet.exe") {
        return $_
    }
}

if ($dotnet_path -is [array]){
    $dotnet_path = $dotnet_path[0]
}

$sdkpath = Join-Path -Path $dotnet_path -ChildPath "sdk"
$json = Get-Content '$PSScriptRoot\..\global.json' | Out-String | ConvertFrom-Json
$pattern = $json.sdk.version.Trim("0") + "*"
$versions = $null
if (-not ("" -eq $dotnet_path))
{
    $versions = Get-ChildItem "$sdkpath"  -directory | Where-Object {$_ -like $pattern}
}

if ($null -eq $versions)
{
    Write-Comment -text "The global.json file is pointing to version: $pattern but no matching version was found in $sdkpath." -color "yellow"
    Write-Comment -text "Please install .NET SDK version $pattern from https://dotnet.microsoft.com/download/dotnet-core." -color "yellow"
    exit 1
}
else
{
    if ($versions -is [array]){
        $versions = $versions[0]
    }

    Write-Comment -text "Using .NET SDK version $versions at: $sdkpath" -color yellow
}

Write-Comment -prefix "..." -text "Configuration: $configuration" -color "white"
$solution = $PSScriptRoot + "\..\Coyote.sln"
$command = "build -c $configuration $solution"
$error_msg = "Failed to build Coyote"
Invoke-ToolCommand -tool $dotnet -command $command -error_msg $error_msg

Write-Comment -prefix "." -text "Successfully built Coyote" -color "green"
