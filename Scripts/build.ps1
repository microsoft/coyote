param(
    [string]$dotnet="dotnet",
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release"
)

Import-Module $PSScriptRoot\powershell\common.psm1

# check that dotnet sdk is installed...
Function FindInPath() {
    param ([string]$name)
    $ENV:PATH.split(';') | ForEach-Object {
        If (Test-Path -Path $_\$name) {
            return $_
        }
    }
    return $null
}

$json = Get-Content '$PSScriptRoot\..\global.json' | Out-String | ConvertFrom-Json
$pattern = $json.sdk.version.Trim("0") + "*"

$dotnet=$dotnet.Replace(".exe","")
$versions = $null
$dotnetpath=FindInPath "$dotnet.exe"
if ($dotnetpath -is [array]){
    $dotnetpath = $dotnetpath[0]
}
$sdkpath = Join-Path -Path $dotnetpath -ChildPath "sdk"
if (-not ("" -eq $dotnetpath))
{
    $versions = Get-ChildItem "$sdkpath"  -directory | Where-Object {$_ -like $pattern}
}

if ($null -eq $versions)
{
    Write-Comment -text "The global.json file is pointing to version: $pattern but no matching version was found in $sdkpath." -color "yellow"
    Write-Comment -text "Please install dotnet sdk version $pattern from https://dotnet.microsoft.com/download/dotnet-core." -color "yellow"
    exit 1
}
else
{
    if ($versions -is [array]){
        $versions = $versions[0]
    }
    Write-Comment -text "Using dotnet sdk version $versions at: $sdkpath" -color yellow
}


Write-Comment -prefix "." -text "Building Coyote" -color "yellow"
Write-Comment -prefix "..." -text "Configuration: $configuration" -color "white"
$solution = $PSScriptRoot + "\..\Coyote.sln"
$command = "build -c $configuration $solution"
$error_msg = "Failed to build Coyote"
Invoke-ToolCommand -tool $dotnet -command $command -error_msg $error_msg

Write-Comment -prefix "." -text "Successfully built Coyote" -color "green"
