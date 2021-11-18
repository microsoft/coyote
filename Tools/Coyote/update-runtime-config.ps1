param(
    [string]$outputPath = ""
)

$file = "$outputPath\coyote.runtimeconfig.json"
$json = Get-Content $file | ConvertFrom-Json

if (-not ($json.runtimeOptions | Get-Member -MemberType NoteProperty -Name "framework"))
{
    return
}

Write-Host "Framework: $json.runtimeOptions.framework"

$tfm = $json.runtimeOptions.tfm
$originalFrameworkName = $json.runtimeOptions.framework.name
$originalFrameworkVersion = $json.runtimeOptions.framework.version

# Construct the updated JSON object that includes the aspnet framework.
# This is required so that the tool can resolve aspnet related assemblies
# during IL rewriting.
$newJson = @"
{
    "runtimeOptions": {
        "tfm": "$tfm",
        "frameworks": [{
            "name": "$originalFrameworkName",
            "version": "$originalFrameworkVersion"
        },
        {
            "name": "Microsoft.AspNetCore.App",
            "version": "$originalFrameworkVersion"
        }]
    }
}
"@

$newJson | Out-File $file -Force
