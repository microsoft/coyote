# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

$root_dir = "$PSScriptRoot\.."
$packages_path = "$root_dir\packages"
$framework = "net6.0"

# Build the GenDoc tool.
$path = Join-Path -Path $PSScriptRoot -ChildPath ".." -AdditionalChildPath "Tools", "GenDoc"
$project = Join-Path -Path $path -ChildPath "GenDoc.csproj"
$command = "build -c Release $project /p:Platform=""Any CPU"""
Invoke-ToolCommand -tool "dotnet" -cmd $command -error_msg "Failed to build the GenDoc tool."
$gendoc = Join-Path -Path $path -ChildPath "bin" -AdditionalChildPath $framework, "GenDoc.exe"

function InstallToolVersion {
    Param ([string] $name, [string] $version)

    $list = dotnet tool list --tool-path $packages_path
    $line = $list | Where-Object { $_ -Match "$name[ ]*([0-9.\-a-z]+).*" }
    $install = $false
    if ($null -eq $line) {
        Write-Host "The tool '$name' is not installed."
        $install = $true
    } elseif (-not ($Matches[1] -eq $version)) {
        $old = $Matches[1]
        Write-Host "Upgrading '$name' from version '$old'."
        dotnet tool uninstall $name --tool-path $packages_path
        $install = $true
    }

    if ($install) {
        Write-Host "Installing '$name' with version '$version'."
        dotnet tool install $name --version "$version" --tool-path $packages_path
    }
}

# Install InheritDocTool.
InstallToolVersion -name "InheritDocTool" -version "2.5.2"

$framework_target = "$root_dir\bin\$framework"
Write-Host "Processing inherit docs under $framework_target ..." -ForegroundColor Yellow
& "$packages_path\InheritDoc.exe" --base "$framework_target" -o

# Completely clean the ref folder so we start fresh
$target = "$root_dir\docs\ref"
if (Test-Path -Path $target) {
    Remove-Item -Recurse -Force $target
}

Write-Host "Generating new markdown under $target"
& $gendoc gen "$root_dir\bin\$framework\Microsoft.Coyote.dll" -o $target --namespace Microsoft.Coyote
$coyotetoc = Get-Content -Path "$target\toc.yml"

& $gendoc gen "$root_dir\bin\$framework\Microsoft.Coyote.Test.dll" -o $target --namespace Microsoft.Coyote.Test
$newtoc = Get-Content -Path "$target\toc.yml"
$newtoc = [System.Collections.ArrayList]$newtoc
$newtoc.RemoveRange(0, 1); # remove -toc and assembly header
$newtoc.InsertRange(0, $coyotetoc)

# Save the merged toc containing both the contents of Microsoft.Coyote.dll and Microsoft.Coyote.Test.dll.
Set-Content -Path "$target\toc.yml" -Value $newtoc

Write-Host "Merging $toc..."
# Now merge the new toc.

& $gendoc merge "$root_dir\mkdocs.yml" "$target\toc.yml"
