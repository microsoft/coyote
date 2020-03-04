---
layout: reference
section: learn
title: Raft Example
permalink: /learn/tutorials/raft-azure
---

## Raft consensus protocol on Azure

The [CloudMessaging](https://github.com/microsoft/coyote-samples/tree/master/CloudMessaging) sample
implements the [Raft consensus algorithm](https://raft.github.io/) as an Azure Service built on the
[Azure Service Bus](https://azure.microsoft.com/en-us/services/service-bus/). See [animating state
machine demo](/coyote/learn/programming-models/actors/state-machine-demo) which shows the Coyote
[systematic testing process](/coyote/learn/core/systematic-testing) in action on this application.

This example is organized into the following projects:
- **Raft** - a .NET core C# class library that implements the [Raft Consensus
  Algorithm](https://raft.github.io/) using the Coyote [Actor Programming
  Model](../programming-models/actors/overview).
- **Raft.Azure** - a C# executable that shows how to run Coyote messages through an [Azure Service
  Bus](https://azure.microsoft.com/en-us/services/service-bus/).
- **Raft.Mocking** - demonstrates how to use mocks to systematically test the CloudMessaging sample
  application, in-memory on your local machine without using any Azure messaging, discussed in more
  detail in [Mocking Example](raft-mocking).
- **Raft.Nondeterminism** - demonstrates how to introduce controlled nondeterminism in your Coyote
  tests to systematically exercise corner-cases.

## What you will need

To run the Azure example, you will need an [Azure
subscription](https://azure.microsoft.com/en-us/free/). You will also need to install the [Azure
Command-line
tool](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest). This tool
is called the `Azure CLI`.

You will also need to:
- Install [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/).
- Build the [Coyote project](/coyote/learn/get-started/install).
- Clone the [Coyote Samples git repo](http://github.com/microsoft/coyote-samples).

## Setup Azure

- Open a Developer Command Prompt for Visual Studio 2019.
- Run `powershell -f setup.ps1` to create a new Azure Resource Group called `CoyoteSamplesRG` and an
  Azure Service Bus namespace called `CoyoteSampleMessageBus`.

This script will provide the connection string you need so the sample can connect to the Azure
Service Bus. Copy the command line from the output of `setup.ps1` and paste it into your console
window:

```
set CONNECTION_STRING=...
```

If you need to find this connection string again later you can get it from your [Azure
Portal](http://portal.azure.com), find the message bus resource you created above, click on 'Shared
access policies' and select the 'RootManageSharedAccessKey' and wait for the keys to load, then copy
the contents of the field named 'Primary Connection String'.

## Build the sample

Build the `coyote-samples` repo by running the following command:

```
powershell -f build.ps1
```

## Run the Raft.Azure application

Now you can run the Raft.Azure application

```shell
dotnet ./bin/netcoreapp2.2/Raft.Azure.dll --connection-string "%CONNECTION_STRING%" --topic-name rafttopic --num-requests 5 --local-cluster-size 5
```

Note: you don't want to try and run Raft.Azure client using the `coyote test` tool until you
complete the [mocking](raft-mocking) of the Azure Message Bus calls.

## Design

The `Raft.dll` library contains a `Server` [state
machine](../programming-models/actors/state-machines), a `Client` actor, and a `ClusterManager`
state machine. It also contains an interface named `IServerManager` and some Coyote `Event`
declarations which describe the message types that are sent between the Server and Client.

![image](../../assets/images/cloudmessaging.svg)

The `ClusterManager` is an abstract state machine that models the concept of being able to broadcast
messages to all `Servers` registered in a cluster. Sending an event to this cluster will result in
all `Servers` getting that same event. So, as the raft protocol requires, broadcasting
`VoteRequestEvents` can be done by a `Server` using this `SendEvent` instruction:

```c#
    this.SendEvent(this.ClusterManager, new VoteRequestEvent(this.CurrentTerm, this.Manager.ServerId, lastLogIndex, lastLogTerm));
```

The second project builds `Raft.Azure.dll` and contains all the `Azure` specific code that hooks all
this up to an `Azure Service Bus`. Notice it implements the `IServerManager` interface in a class
named `AzureServer` and it subclasses the `ClusterManager` in a state machine named
`AzureClusterManager`. This subclass forwards all events received to an Azure Service Bus topic. It
also has an `AzureMessageReceiver` class which subscribes to the Azure service bus topic, and
forwards those events back into the local Coyote state machine using `SendEvent`.

Multiple processes are spawned using `Raft.Azure.dll`. The first one is the `Client` process, then
one for each `Server` instance as requested by the command line argument `--local-cluster-size`.
The following diagram shows what this looks like when we have 2 servers in the cluster:

![servers](../../assets/images/RaftServers.svg)

The `Server` state machine is where the interesting code lives, it is a complete implementation of
the `Raft` protocol. All the `Server` instances then form a fault tolerant server cluster, that can
handle `ClientRequestEvents` in a reliable way. `Server` instances can come and go, and the
`cluster` protocol is able to figure out which server should handle which client request, and how to
replicate the logs across all servers for safe keeping and reliability. The whole idea is that with
this cluster there is no single point of failure. Clearly for this to be reliable, it must also be
bug free, and therefore is an excellent candidate for thorough testing by the `coyote test` tool.

The `RunClient` method shows how you would use the `cluster` by sending a `ClientRequestEvent` and
waiting for the async response that comes back from the service bus in the `ClientResponseEvent`.
The client example code can also use the `ClusterManager` to kick things off with this:

```c#
runtime.SendEvent(clusterManager, new ClientRequestEvent(command));
```

This goes to all servers in the cluster via the Azure Service Bus. The servers then implement their
Raft voting protocol to figure out which server will handle the request. Eventually a response
comes back over the Azure Service Bus which is received by a C# event call back `ResponseReceived`
on the `AzureMessageReceiver`. For this example, we setup a `CancellationTokenSource` which the
sample code waits on before kicking off the next `ClientRequestEvent`.

The `Server` setup is performed by `RunServer` which delegates to the `AzureServer` class. This
class has to be careful to ensure the `Server` state machine is up and running before it starts
trying to process any messages from the Azure Service Bus. For this reason we are using the runtime
method `CreateActorIdFromName`.

```c#
this.HostedServer = this.Runtime.CreateActorIdFromName(typeof(Server), this.ServerId);
```

This creates only the `ActorId` object and does not actually create the actor. This is handy when
you need to give that `ActorId` to another `Actor` as part of an initialization process. Then at a
later time when everything is ready we can create the actual actor using this predetermined id as
follows:

```c#
this.Runtime.CreateActor(this.HostedServer, typeof(Server), new Server.SetupServerEvent(this, this.ClusterManager));
```

This example also tells the `Server` actors to start processing events by sending the following
event:

```
this.Runtime.SendEvent(this.HostedServer, new NotifyJoinedServiceEvent());
```

Notice the `Server` when it is in the start `Init` state, is deferring all events using a special
wild card:

```c#
[DeferEvents(typeof(WildCardEvent))]
```

Then only when `NotifyJoinedServiceEvent` arrives does it go to the `Follower` state where the raft
protocol begins.

A real application of this protocol would simply add whatever new `[DataMember]` fields that you
need on `ClientRequestEvent`. These serializable members will be automatically replicated across the
Servers.
