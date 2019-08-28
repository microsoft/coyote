param(
    [Parameter(Mandatory=$true)]
    [string]$account_name,
    [Parameter(Mandatory=$true)]
    [string]$account_key,
    [Parameter(Mandatory=$true)]
    [string]$share_name
)

Import-Module $PSScriptRoot\powershell\common.psm1

$package_dir = "$PSScriptRoot\..\bin\nuget"

Write-Comment -prefix "." -text "Uploading the Coyote NuGet packages to Azure Storage" -color "yellow"

$context = New-AzureStorageContext -StorageAccountName $account_name -StorageAccountKey $account_key

$packages = Get-ChildItem "$package_dir"
foreach ($p in $packages) {
    Set-AzureStorageFileContent -Context $context -ShareName $share_name -Source "$package_dir\$p" -Path "nuget\$p"
}

Write-Comment -prefix "." -text "Successfully uploaded the Coyote NuGet packages to Azure Storage" -color "green"
