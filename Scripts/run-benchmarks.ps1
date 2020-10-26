Import-Module $PSScriptRoot\powershell\common.psm1

$current_dir = (Get-Item -Path ".\").FullName
$benchmarks_dir = "$PSScriptRoot\..\Tools\bin\netcoreapp3.1"
$benchmark_runner = "BenchmarkRunner.exe"
$artifacts_dir = "$current_dir\BenchmarkDotNet.Artifacts"
$timestamp = (Get-Date).ToString('yyyy_MM_dd_hh_mm_ss')
$results = "benchmark_results_$timestamp"

Write-Comment -prefix "." -text "Running the Coyote performance benchmarks" -color "yellow"

Invoke-Expression "$benchmarks_dir\$benchmark_runner"

if (-not (Test-Path $artifacts_dir)) {
    Write-Error "Unable to find the benchmark results ($artifacts_dir)."
    exit
}

Rename-Item -path $artifacts_dir -newName "$results"

Write-Comment -prefix "." -text "Done" -color "green"
