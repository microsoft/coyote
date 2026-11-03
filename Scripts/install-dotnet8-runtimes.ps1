# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

$ErrorActionPreference = "Stop"

# Install beside the active SDK so that its host can find both runtime versions.
$command = Get-Command dotnet -CommandType Application | Select-Object -First 1
$dotnet = Get-Item $command.Source
if ($dotnet.LinkType) {
    $dotnet = $dotnet.ResolveLinkTarget($true)
}
$installDir = $dotnet.DirectoryName
$extension = if ($IsWindows) { "ps1" } else { "sh" }
$installer = Join-Path ([IO.Path]::GetTempPath()) "$([Guid]::NewGuid()).$extension"

try {
    Invoke-WebRequest "https://dot.net/v1/dotnet-install.$extension" -OutFile $installer
    foreach ($runtime in @("dotnet", "aspnetcore")) {
        if ($IsWindows) {
            & pwsh -NoProfile -File $installer -Channel 8.0 -Runtime $runtime `
                -InstallDir $installDir -NoPath -SkipNonVersionedFiles
        } else {
            & bash $installer --channel 8.0 --runtime $runtime `
                --install-dir $installDir --no-path --skip-non-versioned-files
        }
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to install the .NET 8 '$runtime' runtime (exit code $LASTEXITCODE)."
        }
    }

    $runtimes = & $dotnet.FullName --list-runtimes
    if ($LASTEXITCODE -ne 0) {
        throw "Failed to list the installed .NET runtimes."
    }
    foreach ($runtime in @("Microsoft.NETCore.App", "Microsoft.AspNetCore.App")) {
        if (-not ($runtimes -match "^$([regex]::Escape($runtime)) 8\.0\.")) {
            throw "The active .NET host cannot find the .NET 8 '$runtime' runtime."
        }
    }
    $runtimes
} finally {
    if (Test-Path $installer) {
        Remove-Item $installer -Force
    }
}
