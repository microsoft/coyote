# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Run benchmarks on entire git history using the version of the benchmark test
# that is currently checked out, up to some given -max number of commits
param(    
    # The filter passed through to same arg on BenchmarkRunner.    
    [string]$filter = "",
    # The maximum number of commits.
    [int]$max = 0  
)

$ScriptDir = $PSScriptRoot
$RootDir = "$ScriptDir/.."
Import-Module $ScriptDir/common.psm1 -Force

# save the current benchmark code in a temp folder.
$source = "$ENV:TEMP/Benchmark"
if (Test-Path $source) {
    Remove-Item $source -Recurse -Force
}

if (Test-Path "$RootDir/tests/Performance.Tests/bin") {
    Remove-Item "$RootDir/tests/Performance.Tests/bin" -Recurse -Force
}

if (Test-Path "$RootDir/tests/Performance.Tests/obj") {
    Remove-Item "$RootDir/tests/Performance.Tests/obj" -Recurse -Force
}

Write-Host "Saving current benchmark source code"
Copy-Item "$RootDir/Tests/Performance.Tests" -Recurse "$source/Tests/Performance.Tests"
Copy-Item "$RootDir/Tools/BenchmarkRunner" -Recurse "$source/Tools/BenchmarkRunner"

function RestoreBenchmark() {
    Write-Host "Restoring latest benchmark source code"
    Remove-Item "$RootDir/Tests/Performance.Tests" -Recurse -Force
    Copy-Item "$source/Tests/Performance.Tests" -Recurse "$RootDir/Tests/Performance.Tests"
    Remove-Item "$RootDir/Tools/BenchmarkRunner" -Recurse -Force
    Copy-Item "$source/Tools/BenchmarkRunner" -Recurse "$RootDir/Tools/BenchmarkRunner"
    Invoke-Expression "sed -i 's/\\Performance.Tests.csproj/\\Microsoft.Coyote.Performance.Tests.csproj/' $RootDir\Coyote.sln"
}

$benchmarks_dir = "$RootDir/Tools/BenchmarkRunner/bin/net6.0"
$benchmark_runner = "BenchmarkRunner.exe"
$index = 0

function ProcessCommit($commit) {
    Write-Host "===> checking out $commit"
    Invoke-Expression  "git reset --hard"
    Invoke-Expression "git checkout $commit"
    RestoreBenchmark
    Invoke-ToolCommand -tool "dotnet" -cmd "build -c release"
    Sleep 5
    Invoke-ToolCommand -tool "dotnet" -cmd "build-server shutdown"
    Sleep 5
    $artifacts_dir = "$RootDir/benchmark_$commit"
    Invoke-Expression "$benchmarks_dir/$benchmark_runner -outdir $artifacts_dir -commit $commit -cosmos $filter"
    if (-not (Test-Path $artifacts_dir)) {
        Write-Error "Unable to find the benchmark results ($artifacts_dir)."
        Exit 1
    }
}

Set-Location -Path $RootDir
Invoke-Expression  "git reset --hard"
Invoke-Expression "git checkout main"
$history = Invoke-Expression "git log --pretty=oneline"

foreach ($line in $history) {
    $words = $line.Split(' ')
    $commit = $words[0]
    ProcessCommit $commit
    $index = $index + 1
    if (($max -ne 0) -And ($index -eq $max)) {
        Write-Host "Terminating after max tests: $max"
        break
    }
}
