param(
    [string]$results = "",
    [string]$account_name = "",
    [string]$account_key = "",
    [string]$share_name = ""
)

Import-Module $PSScriptRoot\powershell\common.psm1

Write-Comment -prefix "." -text "Uploading the Coyote performance benchmark results to Azure Storage" -color "yellow"

if ($results -eq "") {
    $results = $($env:vso_benchmark_results)
    if ($results -eq "") {
        Write-Error "Unable to find the benchmark results ($results)."
        exit
    }
}

if (-not (Test-Path $results)) {
    Write-Error "Unable to find the benchmark results ($results)."
    exit
}

Write-Comment -prefix "." -text "Compressing the results" -color "yellow"
Compress-Archive -Path "$results" -DestinationPath "$results"

Write-Comment -prefix "." -text "Uploading the results" -color "yellow"
$result_file = Split-Path $results -leaf
$context = New-AzureStorageContext -StorageAccountName $account_name -StorageAccountKey $account_key
Set-AzureStorageFileContent -Context $context -ShareName $share_name -Source "$results.zip" -Path "benchmarks\$result_file.zip"

Write-Comment -prefix "." -text "Done" -color "green"
