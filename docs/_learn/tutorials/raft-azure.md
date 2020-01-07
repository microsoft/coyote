---
layout: reference
section: learn
title: Raft Example
permalink: /learn/tutorials/raft-azure
---

## Raft Azure Example

The [Raft Example ](http://github.com/microsoft/coyote-samples/) implements the [Raft Consensus Algorithm](https://raft.github.io/) as an Azure Service
built on the [Azure Service Bus](https://azure.microsoft.com/en-us/services/service-bus/).
See [animating state machine demo](/coyote/learn/core/demo) which shows the Coyote [systematic testing process](/learn/core/systematic-testing) in action on this application.

## What you will need

To run the Azure example, you will need an [Azure subscription](https://azure.microsoft.com/en-us/free/).
You will also need to install the [Azure Command-line tool](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest).  This tool is called the `Azure CLI`.

You will also need to:
- Install [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/).
- Build the [Coyote project](/coyote/learn/get-started/install).
- Clone the [Coyote Samples git repo](http://github.com/microsoft/coyote-samples).

## Setup Azure

- Open a Developer Command Prompt for Visual Studio 2019.
- Run `powershell -f setup.ps1` to create a new Azure Resource Group called `CoyoteRaftResourceGroup` and an Azure Service Bus namespace called `CoyoteRaftServiceBus`.

Then copy the connection string from your new service bus by going to Azure Portal, find the `CoyoteRaftServiceBus` resource, click on 'Shared access policies' and select the 'RootManageSharedAccessKey' and copy the contents of the field named 'Primary Connection String' then save this connection string in a new environment variable which we will use later to run the sample.

```
set CONNECTION_STRING=...
```

## Build the Sample

Build the `coyote-samples` repo by running the following command:

```
powershell -f build.ps1
```

## Run the Raft.Client application

Now you can run the Raft.Client application

```shell
dotnet .\bin\netcoreapp2.2\Raft.Client.dll --connection-string "%CONNECTION_STRING%" --topic-name rafttopic --num-requests 5 --local-cluster-size 5
```

Note we don't want to try and run Raft.Client using the `coyote` test tool until we complete the [mocking](raft-mocking) of the Azure Message Bus calls.

## Design

add info about the design of this app, some pretty pictures, how the Azure messaging plugs in and so on...
TBD...