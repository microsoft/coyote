param(
    [string]$store="",
    [string]$key="",
    [string]$local=""
)

Import-Module $PSScriptRoot\powershell\common.psm1

$history = Invoke-Expression "git log --pretty=oneline -n 1"
$words = $history.Split(' ')
$commit = $words[0]
if ($store -ne "") 
{
    $env:AZURE_COSMOSDB_ENDPOINT = $store
    $env:AZURE_STORAGE_PRIMARY_KEY = $key
    Write-Host "Results will be saved to $ENV:AZURE_COSMOSDB_ENDPOINT"
    $cosmos = "-cosmos"
} else {
    $cosmos = ""
}

$current_dir = (Get-Item -Path ".\").FullName
$benchmarks_dir = "$PSScriptRoot\..\Tools\BenchmarkRunner\bin\netcoreapp3.1"
$benchmark_runner = "BenchmarkRunner.exe"
$artifacts_dir = "$current_dir\benchmark_$commit"

if (-Not (Test-Path -Path "$benchmarks_dir"))
{
    throw "Please build coyote project first"
}

$custom = "D:\git\lovettchris\BenchmarkDotNet\src\BenchmarkDotNet\bin\Release\netstandard2.0"
if (Test-Path -Path $custom)
{
    Write-Host "==> Using a patched version of BenchmarkDotNet..."
    Copy-Item "$custom\BenchmarkDotNet.dll" "$benchmarks_dir\BenchmarkDotNet.dll" -Force
    Copy-Item "$custom\BenchmarkDotNet.Annotations.dll" "$benchmarks_dir\BenchmarkDotNet.Annotations.dll" -Force
}

if (Test-Path -Path $artifacts_dir -PathType Container)
{
    Remove-Item $artifacts_dir -Recurse
}

Write-Comment -prefix "." -text "Running the Coyote performance benchmarks, saving to $artifacts_dir" -color "yellow"

Invoke-Expression "$benchmarks_dir\$benchmark_runner -outdir $artifacts_dir -commit $commit $cosmos"

Write-Comment -prefix "." -text "Done" -color "green"

if ($local -ne "")
{
    # save the detailed perf results on the test machine with additional integer index to 
    # disambiguate duplicate runs for the same commit id.
    if (-not (Test-Path -Path $local))
    {
        New-Item -Path $local -ItemType Directory
    }
    $index = 1
    while (Test-Path -Path "$local\benchmark_$commit.$index")
    {
        $index = $index + 1
    }

    Move-Item -Path $artifacts_dir -Destination "$saved\benchmark_$commit.$index"
}
