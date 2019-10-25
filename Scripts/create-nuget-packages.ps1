Import-Module $PSScriptRoot\powershell\common.psm1

Write-Comment -prefix "." -text "Creating the Coyote NuGet package" -color "yellow"

# Check that NuGet.exe is installed.
$nuget = "nuget"
if (-not (Get-Command $nuget -errorAction SilentlyContinue)) {
    Write-Comment -text "Please install the latest NuGet.exe from https://www.nuget.org/downloads and add it to the PATH environment variable." -color "yellow"
    exit 1
}

# Extract the package version.
$version_file = "$PSScriptRoot\..\Common\version.props"
$version_node = Select-Xml -Path $version_file -XPath "/" | Select-Object -ExpandProperty Node
$version = $version_node.Project.PropertyGroup.VersionPrefix
$version_suffix = $version_node.Project.PropertyGroup.VersionSuffix

# Setup the command line options for nuget pack.
$command_options = "-OutputDirectory $PSScriptRoot\..\bin\nuget -Version $version"
if ($version_suffix) {
    $command_options = "$command_options -Suffix $version_suffix"
}

$command = "pack $PSScriptRoot\NuGet\Coyote.nuspec $command_options"
$error_msg = "Failed to create the Coyote NuGet package"
Invoke-ToolCommand -tool $nuget -command $command -error_msg $error_msg

Write-Comment -prefix "." -text "Successfully created the Coyote NuGet package" -color "green"
