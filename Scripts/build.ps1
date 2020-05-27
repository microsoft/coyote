param(
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release"
)

$ScriptDir = $PSScriptRoot

Import-Module $ScriptDir\powershell\common.psm1

Write-Comment -prefix "." -text "Building Coyote" -color "yellow"

function TraverLink($path)
{
    $item = Get-Item $path
    if ($item.LinkType -eq "SymbolicLink")
    {
        $target = $item.Target
        Write-Host "Traversing link $target"
        return TraverLink($target)
    }
    return $path
}

function GetMajorVersion($version)
{
    $parts = $version.Split('.')
    if ($parts.Length -gt 1) {
        return  $parts[0] + "." + $parts[1]
    }
    return $version
}

function GetMinorVersion($version)
{
    $parts = $version.Split('.')
    $len = $parts.Length
    if ($len -gt 2) {
        $number = $parts[2]
        return [int]::Parse($number)
    }
    return 0
}

# Check that the expected .NET SDK is installed.
$dotnet = "dotnet"
$dotnet_path = $ENV:PATH.split([System.IO.Path]::PathSeparator) | ForEach-Object {
    if (Test-Path -Path "$_\$dotnet.exe") {
        return $_
    }
    $candidate = [System.IO.Path]::Combine($_, "dotnet")
    if (Test-Path -Path $candidate)
    {
        Write-Host "Found path $candidate"
        $candidate = TraverLink($candidate)
        if (Test-Path -Path $candidate -PathType Leaf)
        {
            Write-Host "Found dotnet app $candidate"
            $candidate = [System.IO.Path]::GetDirectoryname($candidate)
            Write-Host "Returning path $candidate"
        }
        return $candidate
    }
}

if ($dotnet_path -is [array]){
    $dotnet_path = $dotnet_path[0]
}



$sdkpath = Join-Path -Path $dotnet_path -ChildPath "sdk"
$globalJson = "$ScriptDir\..\global.json"
$json = Get-Content $globalJson | Out-String | ConvertFrom-Json
$global_version = $json.sdk.version
Write-Host "Searching SDK path $sdkpath for version matching global.json: $global_version"
$prefix = GetMajorVersion($global_version)
$version = GetMinorVersion($global_version)
$matching_version = $null
if (-not ("" -eq $dotnet_path))
{
    foreach($item in Get-ChildItem "$sdkpath"  -directory)
    {
        $name = $item.Name
        if (-not $name.Contains("-preview"))
        {
            $found_prefix = GetMajorVersion($name)
            $found_version = GetMinorVersion($name)
            $vh = $version / 100
            $vh = [int]$vh
            $fvh = $found_version / 100
            $fvh = [int]$fvh
            if ($prefix -eq $found_prefix -and $vh -eq $fvh)
            {
                Write-Host "Found matching SDK version $name"
                $matching_version = $name
                if ($global_version -ne $name)
                {
                    Write-Host "updating global.json with version $name"
                    $json.sdk.version = $name
                    $new_content = $json | ConvertTo-Json
                    Set-Content $globalJson $new_content
                }
            }
        }
    }
}

if ($null -eq $matching_version)
{
    Write-Comment -text "The global.json file is pointing to version: $global_version but no matching version was found in $sdkpath." -color "yellow"
    Write-Comment -text "Please install .NET SDK version $global_version from https://dotnet.microsoft.com/download/dotnet-core." -color "yellow"
    exit 1
}

Write-Comment -text "Using .NET SDK version $versions at: $sdkpath" -color yellow

Write-Comment -prefix "..." -text "Configuration: $configuration" -color "white"
$solution = $ScriptDir + "\..\Coyote.sln"
$command = "build -c $configuration $solution"
$error_msg = "Failed to build Coyote"
Invoke-ToolCommand -tool $dotnet -command $command -error_msg $error_msg

Write-Comment -prefix "." -text "Successfully built Coyote" -color "green"
