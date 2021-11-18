# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

$PSScriptRoot = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition

$CoyoteRoot = Split-Path $PSScriptRoot

$gentoc = "$CoyoteRoot\bin\net6.0\GenToc.exe"
$ToolPath = "$CoyoteRoot\packages"

if (-Not (Test-Path -Path "$CoyoteRoot\bin")) {
    throw "please build coyote project first"
}

function InstallToolVersion {
    Param ([string] $name, [string] $version)

    $list = dotnet tool list --tool-path $ToolPath
    $line = $list | Where-Object { $_ -Match "$name[ ]*([0-9.\-a-z]+).*" }
    $install = $false
    if ($null -eq $line) {
        Write-Host "$name is not installed."
        $install = $true
    }
    elseif (-not ($Matches[1] -eq $version)) {
        $old = $Matches[1]
        Write-Host "upgrading $name from version $old"
        dotnet tool uninstall $name --tool-path $ToolPath
        $install = $true
    }
    if ($install) {
        Write-Host "installing $name version $version."
        dotnet tool install $name --version "$version" --tool-path $ToolPath
    }
    return $installed
}

$inheritdoc = "$ToolPath\InheritDoc.exe"
$xmldoc = "$ToolPath\xmldocmd.exe"
$target = "$CoyoteRoot\docs\ref"

# install InheritDocTool
$installed = InstallToolVersion -name "InheritDocTool" -version "2.5.1"

# install xmldocmd
$installed = InstallToolVersion -name "xmldocmd" -version "2.3.0"

$frameworks = Get-ChildItem -Path "$CoyoteRoot/bin" | Where-Object Name -ne "nuget" | Select-Object -expand Name
foreach ($name in $frameworks) {
    $framework_target = "$CoyoteRoot\bin\$name"
    Write-Host "processing inherit docs under $framework_target ..." -ForegroundColor Yellow
    & $inheritdoc --base "$framework_target" -o
}

# Completely clean the ref folder so we start fresh
if (Test-Path -Path $target) {
    Remove-Item -Recurse -Force $target
}

Write-Host "Generating new markdown under $target"

# --permalink pretty
& $xmldoc --namespace Microsoft.Coyote "$CoyoteRoot\bin\netcoreapp3.1\Microsoft.Coyote.dll" "$target" --visibility protected --toc --toc-prefix ref --skip-unbrowsable --namespace-pages
$coyotetoc = Get-Content -Path "$target\toc.yml"

& $xmldoc --namespace Microsoft.Coyote.Test "$CoyoteRoot\bin\netcoreapp3.1\Microsoft.Coyote.Test.dll" "$target" --visibility protected --toc --toc-prefix ref --skip-unbrowsable --namespace-pages
$newtoc = Get-Content -Path "$target\toc.yml"
$newtoc = [System.Collections.ArrayList]$newtoc
$newtoc.RemoveRange(0, 1); # remove -toc and assembly header
$newtoc.InsertRange(0, $coyotetoc)

# save the merged toc containing both the contents of Microsoft.Coyote.dll and Microsoft.Coyote.Test.dll
Set-Content -Path "$target\toc.yml" -Value $newtoc

Write-Host "Merging $toc..."
# Now merge the new toc

& $gentoc "$CoyoteRoot\mkdocs.yml" "$target\toc.yml"
