param(
    [string]$dotnet="dotnet",
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release",
    [bool]$local = $false
)

Import-Module $PSScriptRoot\..\Common\helpers.psm1 -Force
Write-Comment -prefix "." -text "Building the Coyote samples" -color "yellow"
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\..\Common\TestDriver\TestDriver.csproj" `
    -config $configuration -local $local
Write-Comment -prefix "." -text "Successfully built the Coyote samples" -color "green"
