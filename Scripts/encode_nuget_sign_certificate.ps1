# Copyright (c) Microsoft Corporation.
# Licensed under the MIT License.

param(
    [Parameter(Mandatory=$true)]
    [string]$url = ""
)

$ScriptDir = $PSScriptRoot

Import-Module $ScriptDir/common.psm1 -Force

Write-Comment -prefix "." -text "Encoding NuGet signing certificate" -color "yellow"
Write-Comment -prefix "..." -text "You might need to run 'az login' first" -color "blue"

$guid = [GUID]::NewGuid().ToString()
$result_file = "$ScriptDir/$guid"
Write-Host $result_file

# # The "Secret Identifier" url from the certificate details.
az keyvault secret download --file $result_file --encoding base64 --id $url

$file_content_bytes = Get-Content $result_file -AsByteStream
$base64_value = [System.Convert]::ToBase64String($file_content_bytes)
Set-Clipboard $base64_value

Remove-Item $result_file

Write-Comment -prefix "." -text "Done" -color "green"
