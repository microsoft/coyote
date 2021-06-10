# Invokes the specified coyote tool command on the specified target.
function Invoke-CoyoteTool([String]$cmd, [String]$dotnet, [String]$framework, [String]$target, [String]$key) {
    Write-Comment -prefix "..." -text "Rewriting '$target' ($framework)" -color "white"

    $tool = "./bin/$framework/coyote.exe"
    $command = "$cmd $target"

    if (-not (Test-Path $tool)) {
        $tool = $dotnet
        $command = "./bin/$framework/coyote.dll $cmd $target"
    }

    if ($command -eq "rewrite" -and $framework -ne "netcoreapp3.1" -and $framework -ne "net5.0" -and [System.Environment]::OSVersion.Platform -eq "Win32NT") {
        # note: Mono.Cecil cannot sign assemblies on unix platforms.
        $command = "$command -snk $key"
    }

    Write-Comment -prefix "..." -text "$tool" -color "white"
    $error_msg = "Failed to $cmd '$target'"
    Invoke-ToolCommand -tool $tool -cmd $command -error_msg $error_msg
}

# Runs the specified .NET test using the specified framework.
function Invoke-DotnetTest([String]$dotnet, [String]$project, [String]$target, [string]$filter, [string]$framework, [string]$logger, [string]$verbosity) {
    Write-Comment -prefix "..." -text "Testing '$project' ($framework)" -color "white"
    if (-not (Test-Path $target)) {
        Write-Error "tests for '$project' ($framework) not found."
        exit
    }

    $command = "test $target -f $framework --no-build -v $verbosity"
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

function Write-Comment([String]$prefix, [String]$text, [String]$color) {
    Write-Host "$prefix " -b "black" -nonewline; Write-Host $text -b "black" -f $color
}

function Write-Error([String]$text) {
    Write-Host "Error: $text" -b "black" -f "red"
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

function GetMajorVersion($version) {
    $parts = $version.Split('.')
    if ($parts.Length -gt 1) {
        return  $parts[0] + "." + $parts[1]
    }
    return $version
}

function GetMinorVersion($version) {
    $parts = $version.Split('.')
    $len = $parts.Length
    if ($len -gt 2) {
        $number = $parts[2].Split('-')[0]
        return [int]::Parse($number)
    }
    return 0
}

function FindProgram($name) {
    $result = $null
    $path = $ENV:PATH.split([System.IO.Path]::PathSeparator) | ForEach-Object {
        if (Test-Path -Path "$_\$name") {
            $result = "$_\$name"
        }
    }
    return $result
}

function GetAssemblyName($path){
    $AssemblyName = $null;
    $doc = [System.Xml.Linq.XDocument]::Load($path);
    $name = [System.Xml.Linq.XName]::Get("AssemblyName", $r.Name.Namespace);
    $doc.Root.Descendants($name) | ForEach-Object { $AssemblyName = $_.Value };
    return $AssemblyName
}

function FindDotNet($dotnet) {
    $dotnet_path = $ENV:PATH.split([System.IO.Path]::PathSeparator) | ForEach-Object {
        if (Test-Path -Path "$_\$dotnet.exe") {
            return $_
        }
        $candidate = [System.IO.Path]::Combine($_, "dotnet")
        if (Test-Path -Path $candidate) {
            Write-Host "Found path $candidate"
            $candidate = TraverseLink($candidate)
            if (Test-Path -Path $candidate -PathType Leaf) {
                Write-Host "Found dotnet app $candidate"
                $candidate = [System.IO.Path]::GetDirectoryname($candidate)
                Write-Host "Returning path $candidate"
            }
            return $candidate
        }
    }

    if ($dotnet_path -is [array]) {
        $dotnet_path = $dotnet_path[0]
    }
    return $dotnet_path
}
# find the closest match for installed dotnet SDK, for example:
# FindInstalledDotNetSdk -dotnet_path "c:\program files\dotnet" -major "3.1" -minor 0
# returns "3.1.401" assuming that is the newest 3.1 version installed.
function FindInstalledDotNetSdk($dotnet_path, $major, $minor) {
    $sdkpath = Join-Path -Path $dotnet_path -ChildPath "sdk"
    $matching_version = $null
    $best_match = $null
    $exact_match = $false
    if ("" -ne $dotnet_path) {
        foreach ($item in Get-ChildItem "$sdkpath"  -directory) {
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
            $found_major = GetMajorVersion($name)
            $found_version = GetMinorVersion($name)
            if ($major -eq $found_major) {
                if ($null -eq $best_match) {
                    $best_match = $found_version
                    $matching_version = $global_version
                }
                elseif ($found_version -eq $minor) {
                    $exact_match = $true
                    $best_match = $found_version
                    $matching_version = $global_version
                }
                elseif ($found_version -gt $best_match -and $exact_match -eq $false) {
                    # use the newest version then.
                    $best_match = $found_version
                    $matching_version = $global_version
                }
            }
        }

        return $matching_version
    }
}

function FindDotNetSdk($dotnet_path) {
    $sdkpath = Join-Path -Path $dotnet_path -ChildPath "sdk"
    $globalJson = "$PSScriptRoot/../../global.json"
    $json = Get-Content $globalJson | Out-String | ConvertFrom-Json
    $global_version = $json.sdk.version
    Write-Host "Searching SDK path $sdkpath for version matching global.json: $global_version"
    $major = GetMajorVersion($global_version)
    $minor = GetMinorVersion($global_version)

    $matching_version = FindInstalledDotNetSdk -dotnet_path $dotnet_path -major $major -minor $minor

    if ($null -ne $matching_version) {
        if ($global_version -eq $matching_version) {
            Write-Host "Found the correct SDK version $matching_version"
        }
        else {
            Write-Comment -text "Updating global.json to select version $matching_version" -color "yellow"
            $json.sdk.version = $matching_version
            $new_content = $json | ConvertTo-Json
            Set-Content $globalJson $new_content
        }
    }
    return $matching_version
}
