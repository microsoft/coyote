param(
    [string]$dotnet="dotnet",
    [ValidateSet("Debug","Release")]
    [string]$configuration="Release",
    [bool]$local = $false
)

Import-Module $PSScriptRoot\..\..\Scripts\powershell\common.psm1 -Force

Write-Comment -prefix "." -text "Building the Coyote samples" -color "yellow"

# Check that the expected .NET SDK is installed.
$dotnet = "dotnet"
$dotnet_sdk_path = FindDotNetSdkPath -dotnet $dotnet
$sdk_version = FindDotNetSdkVersion -dotnet_sdk_path $dotnet_sdk_path

if ($null -eq $sdk_version) {
    Write-Error "The global.json file is pointing to version '$sdk_version' but no matching version was found."
    Write-Error "Please install .NET SDK version '$sdk_version' from https://dotnet.microsoft.com/download/dotnet."
    exit 1
}

# Build the task-based samples.
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\..\AccountManager\AccountManager.sln" `
    -config $configuration -local $local
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\..\BoundedBuffer\BoundedBuffer.sln" `
    -config $configuration -local $local
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\..\CoffeeMachineTasks\CoffeeMachineTasks.sln" `
    -config $configuration -local $local

# Build the actor samples.
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\..\HelloWorldActors\HelloWorldActors.sln" `
    -config $configuration -local $local
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\..\CoffeeMachineActors\CoffeeMachineActors.sln" `
    -config $configuration -local $local
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\..\DrinksServingRobotActors\DrinksServingRobotActors.sln" `
    -config $configuration -local $local
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\..\CloudMessaging\CloudMessaging.sln" `
    -config $configuration -local $local
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\..\Timers\Timers.sln" `
    -config $configuration -local $local
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\..\Monitors\Monitors.sln" `
    -config $configuration -local $local

# Build the web app samples.
# Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\..\WebApps\ImageGalleryAspNet\ImageGallery.sln" `
    # -config $configuration -local $local
Invoke-DotnetBuild -dotnet $dotnet -solution "$PSScriptRoot\..\WebApps\PetImagesAspNet\PetImages.sln" `
    -config $configuration -local $local

Write-Comment -prefix "." -text "Successfully built the Coyote samples" -color "green"
