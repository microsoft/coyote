# This script uses the "az" tool to setup a new resource group and Azure Service Bus used by this sample.
# It starts by running "az login" and "az account set --subscription ..." to select your desired
# subscription for running this sample and" az configure --defaults location=<location>" to configure your
# desired data center location then it creates a new Azure Resource Group named 'CoyoteSamplesRG' and
# and Azure Service Bus namespace 'CoyoteSampleMessageBus'

$saved = $HOST.UI.RawUI.ForegroundColor
$account = az account show
$HOST.UI.RawUI.ForegroundColor = $saved

$account

if ($null -eq $account) {
    $accounts = az login  | ConvertFrom-Json;
    foreach ($a in $accounts) {
      $name = $a.name
      $id = $a.id
      Write-Host "$name            $id"
    }
    $HOST.UI.RawUI.ForegroundColor = $saved
    $subscription = Read-Host -Prompt 'Enter the Azure subscription id to use'
    az account set --subscription $subscription
}

$location = Read-Host -Prompt 'Enter the Azure location to use (default "westus", see az account list-locations for full list)'
if ($location -eq "" ) {
    $location = "westus"
}

az configure --defaults location=$location

$resourceGroup = Read-Host -Prompt "Enter the name of the resource group (default 'CoyoteSamplesRG')"

if ($resourceGroup -eq "" ) {
    $resourceGroup = "CoyoteSamplesRG"
}

Write-Host "Creating resource group $resourceGroup..."
az group create --name $resourceGroup

$namespace = Read-Host -Prompt "Enter the name of your new service bus namespace (default 'CoyoteSampleMessageBus')"

if ($namespace -eq "" ) {
    $namespace = "CoyoteSampleMessageBus"
}

# We need standard SKU so we can programatically create Topics.
Write-Host "Creating service bus..."
az servicebus namespace create --name $namespace --resource-group $resourceGroup --sku Standard

Write-Host "Fetching connection string..."
$json = az servicebus namespace authorization-rule keys list  --resource-group $resourceGroup --namespace-name $namespace --name RootManageSharedAccessKey | ConvertFrom-Json;

$connection_string=$json.primaryConnectionString
Write-Host "Please set the value of the primaryConnectionString to a new environment variable named CONNECTION_STRING, like this:"
Write-Host "set CONNECTION_STRING=$connection_string"

