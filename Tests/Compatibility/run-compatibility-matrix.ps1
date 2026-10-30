# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Runs the native .NET host compatibility matrix over the Coyote runtime compatibility probes.
#
# Every case copies a freshly built probe into an isolated workspace before it invokes a Coyote
# host, so that an assembly rewritten by an earlier case is never reused. Coyote skips an assembly
# that already carries a matching rewriting signature, which would otherwise hide the behavior that
# a case is meant to exercise.
#
# A supported pairing must rewrite the target and then execute it under Coyote control, while an
# unsupported pairing must fail with an actionable diagnostic and leave the workspace unchanged.

param(
    [ValidateSet("Release", "Debug")]
    [string]$config = "Release",
    [switch]$nobuild
)

Import-Module $PSScriptRoot/../../Scripts/common.psm1 -Force

CheckPSVersion

[System.Environment]::SetEnvironmentVariable('COYOTE_CLI_TELEMETRY_OPTOUT', '1')

$root_path = Join-Path -Path $PSScriptRoot -ChildPath ".." -AdditionalChildPath @("..")
$matrix_path = Join-Path -Path $PSScriptRoot -ChildPath "bin" -AdditionalChildPath @("matrix")
$iterations = 10

# The probes that the matrix rewrites and tests. The 'sdk' of a probe selects the SDK that builds
# it: 'probe' builds from the probe directory, which pins the SDK through the 'global.json' of the
# probe, and 'root' builds from the repository root, which pins the SDK of the root 'global.json'.
$probes = [ordered]@{
    "net8" = @{
        project = "Net8Probe"
        assembly = "Net8Probe.dll"
        output = "net8.0"
        sdk = "probe"
    }
    "net8-sdk10" = @{
        project = "Net8Probe"
        assembly = "Net8Probe.dll"
        output = "net8.0-sdk10"
        sdk = "root"
    }
    "net10" = @{
        project = "Net10Probe"
        assembly = "Net10Probe.dll"
        output = "net10.0"
        sdk = "probe"
    }
}

# The host and target pairings covered by the matrix. Each case starts from a fresh copy of the
# specified probe, which the optional 'setup_framework' host rewrites, before the
# 'host_framework' host runs the asserted 'command'.
$cases = @(
    @{
        name = "net8-host-rewrites-net8-probe"
        probe = "net8"
        host_framework = "net8.0"
        command = "rewrite"
        supported = $true
    },
    @{
        name = "net8-host-tests-net8-probe"
        probe = "net8"
        setup_framework = "net8.0"
        host_framework = "net8.0"
        command = "test"
        supported = $true
    },
    @{
        # The .NET runtime rolls forward, so the net10.0 host must still test a net8.0 assembly
        # that the net8.0 host rewrote.
        name = "net10-host-tests-net8-probe"
        probe = "net8"
        setup_framework = "net8.0"
        host_framework = "net10.0"
        command = "test"
        supported = $true
    },
    @{
        name = "net8-host-rewrites-net8-probe-built-with-sdk10"
        probe = "net8-sdk10"
        host_framework = "net8.0"
        command = "rewrite"
        supported = $true
    },
    @{
        name = "net8-host-tests-net8-probe-built-with-sdk10"
        probe = "net8-sdk10"
        setup_framework = "net8.0"
        host_framework = "net8.0"
        command = "test"
        supported = $true
    },
    @{
        name = "net10-host-rewrites-net10-probe"
        probe = "net10"
        host_framework = "net10.0"
        command = "rewrite"
        supported = $true
    },
    @{
        name = "net10-host-tests-net10-probe"
        probe = "net10"
        setup_framework = "net10.0"
        host_framework = "net10.0"
        command = "test"
        supported = $true
    },
    @{
        name = "net8-host-rewrites-net10-probe"
        probe = "net10"
        host_framework = "net8.0"
        command = "rewrite"
        supported = $false
        diagnostics = @(
            "The Coyote host is running on .NET 8.0"
            "targets .NETCoreApp,Version=v10.0"
            "which this host cannot load"
            "Run the net10.0 Coyote host to rewrite this assembly"
        )
    },
    @{
        name = "net10-host-rewrites-net8-probe"
        probe = "net8"
        host_framework = "net10.0"
        command = "rewrite"
        supported = $false
        diagnostics = @(
            "The Coyote host is running on .NET 10.0"
            "targets .NETCoreApp,Version=v8.0"
            "would inject .NET 10.0 runtime references"
            "Run the net8.0 Coyote host to rewrite this assembly"
        )
    },
    @{
        name = "net10-host-rewrites-net8-probe-built-with-sdk10"
        probe = "net8-sdk10"
        host_framework = "net10.0"
        command = "rewrite"
        supported = $false
        diagnostics = @(
            "The Coyote host is running on .NET 10.0"
            "targets .NETCoreApp,Version=v8.0"
            "would inject .NET 10.0 runtime references"
            "Run the net8.0 Coyote host to rewrite this assembly"
        )
    },
    @{
        name = "net8-host-tests-net10-probe"
        probe = "net10"
        setup_framework = "net10.0"
        host_framework = "net8.0"
        command = "test"
        supported = $false
        diagnostics = @(
            "The Coyote host is running on .NET 8.0"
            "requires .NETCoreApp,Version=v10.0"
            "Run the net10.0 Coyote host for this assembly"
        )
    }
)

# Builds the specified probe from scratch, so that the matrix never consumes a stale assembly.
# NOTE: a probe that pins its own SDK clears the whole build directory of its project, so it must
# be built before any probe of the same project that is built with the SDK of the repository root.
function Build-Probe($probe) {
    $project_path = Join-Path -Path $PSScriptRoot -ChildPath $probe.project
    $output_path = Join-Path -Path $project_path -ChildPath "bin" -AdditionalChildPath @($config, $probe.output)
    Write-Comment -prefix "..." -text "Building the '$($probe.project)' probe into '$($probe.output)'"
    if ($probe.sdk -eq "probe") {
        Remove-Item -Path (Join-Path -Path $project_path -ChildPath "bin") -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item -Path (Join-Path -Path $project_path -ChildPath "obj") -Recurse -Force -ErrorAction SilentlyContinue

        # Build from the probe directory so that the SDK pinned by its 'global.json' is used.
        Push-Location $project_path
        Invoke-ToolCommand -tool "dotnet" -cmd "build -c $config" `
            -error_msg "Failed to build the '$($probe.project)' probe"
        Pop-Location
    } else {
        Remove-Item -Path $output_path -Recurse -Force -ErrorAction SilentlyContinue

        # Build from the repository root so that the SDK pinned by the root 'global.json' is used.
        # The build is not incremental, else it can reuse the assembly compiled by the other SDK.
        $project_file = Join-Path -Path $project_path -ChildPath "$($probe.project).csproj"
        Push-Location $root_path
        Invoke-ToolCommand -tool "dotnet" -cmd "build $project_file -c $config -o $output_path --no-incremental" `
            -error_msg "Failed to build the '$($probe.project)' probe with the SDK of the repository root"
        Pop-Location
    }
}

# Creates an isolated workspace that contains a fresh copy of the specified probe build.
function New-Workspace([String]$case_name, $probe) {
    $source_path = Join-Path -Path $PSScriptRoot -ChildPath $probe.project `
        -AdditionalChildPath @("bin", $config, $probe.output)
    if (-not (Test-Path $source_path)) {
        Write-Error "Unable to find the '$($probe.project)' probe build in '$source_path'."
        exit 1
    }

    $workspace_path = Join-Path -Path $matrix_path -ChildPath $case_name
    Remove-Item -Path $workspace_path -Recurse -Force -ErrorAction SilentlyContinue
    New-Item -Path $workspace_path -ItemType Directory -Force | Out-Null
    Copy-Item -Path (Join-Path -Path $source_path -ChildPath "*") -Destination $workspace_path -Recurse -Force
    return $workspace_path
}

# Invokes the Coyote host of the specified framework and returns its exit code and output.
function Invoke-CoyoteHost([String]$framework, [String]$command, [String]$target) {
    $tool = Join-Path -Path $root_path -ChildPath "bin" -AdditionalChildPath @($framework, "coyote.exe")
    $arguments = "$command $target"
    if (-not (Test-Path $tool)) {
        # NOTE: only Windows builds an executable host, so use the dotnet driver elsewhere.
        $assembly = Join-Path -Path $root_path -ChildPath "bin" -AdditionalChildPath @($framework, "coyote.dll")
        if (-not (Test-Path $assembly)) {
            Write-Error "Unable to find the $framework Coyote host, build Coyote before running the matrix."
            exit 1
        }

        $arguments = "$assembly $arguments"
        $tool = "dotnet"
    }

    if ($command -eq "test") {
        $arguments = "$arguments -i $iterations"
    }

    Write-Comment -prefix "....." -text "Invoking $tool $arguments"
    $output = Invoke-Expression "$tool $arguments 2>&1 | Out-String"
    return @{ exit_code = $LASTEXITCODE; output = [String]$output }
}

# Returns true if the specified assembly carries a Coyote rewriting signature. The signature is an
# assembly level attribute, so the name of its type is in the metadata of a rewritten assembly.
function Test-RewritingSignature([String]$assembly_path) {
    $bytes = [System.IO.File]::ReadAllBytes($assembly_path)
    return [System.Text.Encoding]::Latin1.GetString($bytes).Contains("RewritingSignatureAttribute")
}

# Returns the hash of every file in the specified directory, keyed by its relative path.
function Get-Snapshot([String]$directory) {
    $snapshot = @{}
    foreach ($file in Get-ChildItem -Path $directory -Recurse -File) {
        $snapshot[$file.FullName.Substring($directory.Length)] = $(Get-FileHash $file.FullName).Hash
    }

    return $snapshot
}

# Returns how the specified directory snapshots differ.
function Compare-Snapshot($before, $after) {
    $differences = @()
    foreach ($path in $before.Keys) {
        if (-not $after.ContainsKey($path)) {
            $differences += "deleted '$path'"
        } elseif ($after[$path] -ne $before[$path]) {
            $differences += "modified '$path'"
        }
    }

    foreach ($path in $after.Keys) {
        if (-not $before.ContainsKey($path)) {
            $differences += "created '$path'"
        }
    }

    return , $differences
}

# Records a failed expectation of the specified case.
function Assert-Expectation([String]$case_name, [bool]$condition, [String]$message) {
    if (-not $condition) {
        Write-Error "[$case_name] $message"
        $script:failures += "[$case_name] $message"
    }
}

# Asserts that the specified host reported that it runs on the expected .NET version.
function Assert-Host([String]$case_name, [String]$framework, $result) {
    $version = $framework.Substring(3).Split('.')[0]
    Assert-Expectation $case_name $result.output.Contains("for .NET $version.") `
        "The $framework host did not report that it runs on .NET $version."
}

$failures = @()

Write-Comment -prefix "." -text "Running the native host compatibility matrix" -color "yellow"

if ($nobuild.IsPresent) {
    Write-Comment -prefix "..." -text "Reusing the existing probe builds"
} else {
    foreach ($kvp in $probes.GetEnumerator()) {
        Build-Probe -probe $($kvp.Value)
    }
}

foreach ($case in $cases) {
    $expectation = if ($case.supported) { "supported" } else { "unsupported" }
    Write-Comment -prefix ".." -text "Running the $expectation '$($case.name)' case" -color "yellow"

    $probe = $probes[$case.probe]
    $workspace_path = New-Workspace -case_name $case.name -probe $probe
    $target = Join-Path -Path $workspace_path -ChildPath $probe.assembly

    # A case that consumes an already rewritten assembly is not exercising its host, because
    # Coyote skips any assembly that carries a matching rewriting signature.
    if (Test-RewritingSignature $target) {
        Assert-Expectation $case.name $false `
            "The '$($probe.project)' probe build is already rewritten, so the case is not exercised."
        continue
    }

    if ($case.setup_framework) {
        $setup = Invoke-CoyoteHost -framework $case.setup_framework -command "rewrite" -target $target
        if ($setup.exit_code -ne 0 -or -not (Test-RewritingSignature $target)) {
            Write-Host $setup.output
            Assert-Expectation $case.name $false `
                "The $($case.setup_framework) host failed to rewrite the '$($probe.project)' probe."
            continue
        }
    }

    $before = Get-Snapshot $workspace_path
    $result = Invoke-CoyoteHost -framework $case.host_framework -command $case.command -target $target
    Write-Host $result.output
    $after = Get-Snapshot $workspace_path
    $differences = Compare-Snapshot -before $before -after $after
    Assert-Host -case_name $case.name -framework $case.host_framework -result $result

    if (-not $case.supported) {
        Assert-Expectation $case.name ($result.exit_code -ne 0) `
            "The $($case.host_framework) host unexpectedly succeeded to $($case.command) the target."
        foreach ($diagnostic in $case.diagnostics) {
            Assert-Expectation $case.name $result.output.Contains($diagnostic) `
                "The diagnostic of the $($case.host_framework) host is missing '$diagnostic'."
        }

        # A rejected command must leave the target assembly, and any other file, untouched.
        Assert-Expectation $case.name ($differences.Count -eq 0) `
            "The rejected command mutated the workspace: $($differences -join ', ')."
        if ($case.command -eq "test") {
            Assert-Expectation $case.name (-not $result.output.Contains("Iteration #1")) `
                "The $($case.host_framework) host ran the test instead of rejecting the target."
        }

        continue
    }

    Assert-Expectation $case.name ($result.exit_code -eq 0) `
        "The $($case.host_framework) host failed to $($case.command) the target with exit code $($result.exit_code)."
    if ($case.command -eq "rewrite") {
        Assert-Expectation $case.name `
            (-not $result.output.Contains("Skipping as assembly is already rewritten")) `
            "The $($case.host_framework) host skipped the target instead of rewriting it."
        Assert-Expectation $case.name $result.output.Contains("Writing the modified") `
            "The $($case.host_framework) host did not write the rewritten target."
        Assert-Expectation $case.name (Test-RewritingSignature $target) `
            "The rewritten target does not carry a rewriting signature."
        Assert-Expectation $case.name ($differences -contains "modified '$([IO.Path]::DirectorySeparatorChar)$($probe.assembly)'") `
            "The rewritten target was not modified: $($differences -join ', ')."
    } else {
        Assert-Expectation $case.name (-not $result.output.Contains("Assembly is not rewritten for testing")) `
            "The $($case.host_framework) host tested an assembly that is not rewritten."
        Assert-Expectation $case.name $result.output.Contains("Found 0 bugs.") `
            "The $($case.host_framework) host did not report a passing test."
        Assert-Expectation $case.name $result.output.Contains("Explored $iterations execution paths") `
            "The $($case.host_framework) host did not explore $iterations execution paths."

        $controlled = [regex]::Match($result.output, "Controlled (\d+) operations")
        Assert-Expectation $case.name ($controlled.Success -and [int]$controlled.Groups[1].Value -gt 0) `
            "The $($case.host_framework) host did not control any operation of the target."
        Assert-Expectation $case.name (-not ($differences -contains "modified '$([IO.Path]::DirectorySeparatorChar)$($probe.assembly)'")) `
            "Testing the target modified it: $($differences -join ', ')."
    }
}

if ($failures.Count -gt 0) {
    Write-Comment -prefix "." -text "The native host compatibility matrix found $($failures.Count) failures:" -color "red"
    foreach ($failure in $failures) {
        Write-Error $failure
    }

    exit 1
}

Write-Comment -prefix "." -text "Done" -color "green"

# NOTE: a rejected host command leaves a non-zero '$LASTEXITCODE' behind, which some CI shells
# report as a failure of the whole script.
exit 0
