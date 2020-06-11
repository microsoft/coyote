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
    Invoke-ToolCommand -tool $dotnet -command $command -error_msg $error_msg
}

# Runs the specified tool command.
function Invoke-ToolCommand([String]$tool, [String]$command, [String]$error_msg) {
    Invoke-Expression "$tool $command"
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

function TraverseLink($path)
{
    $item = Get-Item $path
    if ($item.LinkType -eq "SymbolicLink")
    {
        $target = $item.Target
        Write-Host "Traversing link $target"
        return TraverseLink($target)
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

function FindDotNet($dotnet)
{
    $dotnet_path = $ENV:PATH.split([System.IO.Path]::PathSeparator) | ForEach-Object {
        if (Test-Path -Path "$_\$dotnet.exe") {
            return $_
        }
        $candidate = [System.IO.Path]::Combine($_, "dotnet")
        if (Test-Path -Path $candidate)
        {
            Write-Host "Found path $candidate"
            $candidate = TraverseLink($candidate)
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
    return $dotnet_path
}

function FindDotNetSdk($dotnet_path)
{
    $sdkpath = Join-Path -Path $dotnet_path -ChildPath "sdk"
    $globalJson = "$PSScriptRoot/../../global.json"
    $json = Get-Content $globalJson | Out-String | ConvertFrom-Json
    $global_version = $json.sdk.version
    Write-Host "Searching SDK path $sdkpath for version matching global.json: $global_version"
    $prefix = GetMajorVersion($global_version)
    $version = GetMinorVersion($global_version)
    $matching_version = $null
    $best_match = $null
    $exact_match = $false
    if ("" -ne $dotnet_path)
    {
        foreach($item in Get-ChildItem "$sdkpath"  -directory)
        {
            $name = $item.Name
            if (-not $name.Contains("-preview"))
            {
                $found_prefix = GetMajorVersion($name)
                $found_version = GetMinorVersion($name)
                if ($prefix -eq $found_prefix)
                {
                    if ($null -eq $best_match)
                    {
                        $best_match = $found_version
                        $matching_version = $name
                    }
                    elseif ($found_version -eq $version)
                    {
                        $exact_match = $true
                        $best_match = $found_version
                        $matching_version = $name
                    }
                    elseif ($found_version -gt $best_match -and $exact_match -eq $false)
                    {
                        # use the newest version then.
                        $best_match = $found_version
                        $matching_version = $name
                    }
                }
            }
        }

        if ($null -ne $best_match)
        {
            if ($global_version -eq $matching_version)
            {
                Write-Host "Found the correct SDK version $matching_version"
            }
            else
            {
                Write-Comment -text "Updating global.json to select version $matching_version" -color "yellow"
                $json.sdk.version = $matching_version
                $new_content = $json | ConvertTo-Json
                Set-Content $globalJson $new_content
            }
        }

        return $matching_version
    }
}
