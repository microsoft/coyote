# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

# Invokes the specified coyote tool command on the specified target.
function Invoke-CoyoteTool([String]$cmd, [String]$dotnet, [String]$framework, [String]$target, [String]$key) {
    Write-Comment -prefix "..." -text "Rewriting '$target' ($framework)"

    $tool = Join-Path -Path "." -ChildPath "bin" -AdditionalChildPath @($framework, "coyote.exe")
    $command = "$cmd $target"

    if (-not (Test-Path $tool)) {
        $tool = $dotnet
        $coyote = Join-Path -Path "." -ChildPath "bin" -AdditionalChildPath @($framework, "coyote.dll")
        $command = "$coyote $cmd $target"
    }

    if ($command -eq "rewrite" -and $framework -ne "netcoreapp3.1" -and $framework -ne "net5.0" -and $framework -ne "net6.0" -and $IsWindows) {
        # NOTE: Mono.Cecil cannot sign assemblies on unix platforms.
        $command = "$command -snk $key"
    }

    Write-Comment -prefix "..." -text "$tool"
    $error_msg = "Failed to $cmd '$target'"
    Invoke-ToolCommand -tool $tool -cmd $command -error_msg $error_msg
}

# Builds the specified .NET project
function Invoke-DotnetBuild([String]$dotnet, [String]$solution, [String]$config, [bool]$local) {
    Write-Comment -prefix "." -text "Building $solution" -color "yellow"
    Write-Comment -prefix "..." -text "Configuration: $config" -color "white"

    $command = "build -c $config $solution"
    if ($local) {
        $command = "$command /p:UseLocalCoyote=true"
    }

    $error_msg = "Failed to build $solution"
    Invoke-ToolCommand -tool $dotnet -command $command -error_msg $error_msg
}

# Runs the specified .NET test using the specified framework.
function Invoke-DotnetTest([String]$dotnet, [String]$project, [String]$target, [string]$filter, [string]$framework, [string]$logger, [string]$verbosity) {
    Write-Comment -prefix "..." -text "Testing '$project' ($framework)"
    if (-not (Test-Path $target)) {
        Write-Error "tests for '$project' ($framework) not found."
        exit
    }

    # TODO: workaround until .NET fixes normal logging.
    # See https://github.com/dotnet/sdk/issues/16122
    # $command = "test $target -f $framework --no-build -v $verbosity --blame"
    $command = "test $target -f $framework --no-build -v $verbosity --logger 'console;verbosity=normal' --blame"
    if (!($filter -eq "")) {
        $command = "$command --filter $filter"
    }

    if (!($logger -eq "")) {
        $command = "$command --logger $logger"
    }

    $error_msg = "Failed to test '$project'"
    Invoke-ToolCommand -tool $dotnet -cmd $command -error_msg $error_msg
}

# Runs the specified tool command.
function Invoke-ToolCommand([String]$tool, [String]$cmd, [String]$error_msg) {
    Write-Host "Invoking $tool $cmd"
    Invoke-Expression "$tool $cmd"
    if (-not ($LASTEXITCODE -eq 0)) {
        Write-Error $error_msg
        exit $LASTEXITCODE
    }
}

function TraverseLink($path) {
    $item = Get-Item $path
    if ($item.LinkType -eq "SymbolicLink") {
        $target = $item.Target
        Write-Host "Traversing link $target"
        return TraverseLink($target)
    }
    return $path
}

function FindProgram([String]$name) {
    $result = $null
    $path = $ENV:PATH.split([System.IO.Path]::PathSeparator) | ForEach-Object {
        $test = Join-Path -Path $_ -ChildPath $name
        if ($IsWindows) {
            $test = $test + ".exe"
        }
        if (Test-Path -Path $test) {
            $result = $test
        }
    }
    return $result
}

function GetAssemblyName([String]$path){
    $AssemblyName = $null;
    $doc = [System.Xml.Linq.XDocument]::Load($path);
    $name = [System.Xml.Linq.XName]::Get("AssemblyName", $r.Name.Namespace);
    $doc.Root.Descendants($name) | ForEach-Object { $AssemblyName = $_.Value };
    return $AssemblyName
}

# Finds the path of the .NET SDK.
function FindDotNetSdkPath([String]$dotnet) {
    $dotnet_sdks = Invoke-Expression "$dotnet --list-sdks"
    $dotnet_sdk_path = $dotnet_sdks | ForEach-Object {
        $sdk_path = ($_ -split {$_ -eq '[' -or $_ -eq ']'})[1]
        return $sdk_path
    }

    if ($dotnet_sdk_path -is [array]) {
        $dotnet_sdk_path = $dotnet_sdk_path[0]
    }

    return $dotnet_sdk_path
}

# Finds the closest match for installed dotnet SDK build.
function FindInstalledDotNetSdk([String]$dotnet_sdk_path, [version]$version) {
    $matching_version = $null
    $best_match = $null
    $exact_match = $false
    if ("" -ne $dotnet_sdk_path) {
        foreach ($item in Get-ChildItem "$dotnet_sdk_path"  -directory) {
            $name = $item.Name
            $global_version = $name
            if ($name.Contains("-preview")) {
                # For the string to be legal in global.json it must
                # be major.minor.patch or major.minor.patch-preview.
                # So we have to remove any preview version like you see
                # in "5.0.100-preview.7.20366.6"
                $name = $name.Split("-preview")[0]
                $global_version = "$name-preview"
            }

            try {
              $v = [version] $name
              if ($v.Major -eq $version.Major -and $v.Minor -eq $version.Minor ) {
                if ($null -eq $best_match) {
                    $best_match = $v
                    $matching_version = $global_version
                }
                elseif ($v.Build -eq $version.Build) {
                    $exact_match = $true
                    $best_match = $v
                    $matching_version = $global_version
                }
                elseif ($v -gt $best_match -and $exact_match -eq $false) {
                    # Use the newest version then.
                    $best_match = $v
                    $matching_version = $global_version
                }
              }
            } catch {
               # Ignore 'NuGetFallbackFolder' and other none version numbered folders.
            }
        }

        return [string] $matching_version
    }
}

# Finds the dotnet SDK version.
function FindDotNetSdkVersion([String]$dotnet_sdk_path) {
    $globalJson = Join-Path -Path $PSScriptRoot -ChildPath ".." -AdditionalChildPath @("..", "global.json")
    $json = Get-Content $globalJson | Out-String | ConvertFrom-Json
    $global_version = $json.sdk.version
    Write-Comment -prefix "..." -text "Searching .NET SDK version '$global_version' in '$dotnet_sdk_path'"
    $matching_version = FindInstalledDotNetSdk -dotnet_sdk_path $dotnet_sdk_path -version $global_version
    if ($null -ne $matching_version) {
        if ($global_version -eq $matching_version) {
            Write-Comment -prefix "....." -text "Found .NET SDK version '$matching_version'" -color "green"
        }
        else {
            Write-Comment -prefix "....." -text "Updating global.json to .NET SDK version '$matching_version'" -color "yellow"
            $json.sdk.version = $matching_version
            $new_content = $json | ConvertTo-Json
            Set-Content $globalJson $new_content
        }
    }

    return $matching_version
}

function FindNetCoreApp([String]$dotnet_sdk_path, $version) {
    # Find the matching version in 'C:\Program Files\dotnet\shared\Microsoft.NETCore.App\*'.
    $result = $null
    $latest = $null
    $path = $dotnet_sdk_path.TrimEnd('sdk')
    $path = Join-Path -Path $path -ChildPath "shared" -AdditionalChildPath "Microsoft.NETCore.App"
    foreach ($item in Get-ChildItem -Path $path -Directory) {
        if ($item.Name.StartsWith($version)) {
            $v = [version] $item.Name
            if ($null -eq $latest -or $v -gt $latest) {
                $latest = $v
                $result = $item.FullName
            }
        }
    }

    if ($null -eq $result) {
        Write-Error("Microsoft.NETCore.App folder matching $version not found.")
        exit 1
    }

    return $result
}

function Write-Comment([String]$prefix, [String]$text, [String]$color = "white") {
    Write-Host "$prefix " -b "black" -nonewline; Write-Host $text -b "black" -f $color
}

function Write-Error([String]$text) {
    Write-Host "Error: $text" -b "black" -f "red"
}
