# Azure Blockchain Service

## Background

[Azure's Blockchain Service](https://azure.microsoft.com/en-in/services/blockchain-service/)
allows customers to provision blockchain nodes-as-a-service. It allows setting up consortiums
that include blockchain nodes from multiple organizations to govern shared resources.

## Challenge

The Blockchain Service has to deal with the complexity of three sources of concurrency
interacting with each other: (1) the incoming user requests, (2)
asynchronous processing of those requests within the service and (3)
reading and reacting to consortium governance data from the
blockchain and taking resulting actions within the service.
This interaction sometimes led to rare, but serious bugs, that had the
potential of stalling the entire blockchain network.

## Solution and Coyote's key advantages

The Blockchain Service code heavily utilized .NET tasks and the
corresponding async/await style of programming concurrent systems.
Integrating Coyote's Task-based programming model into the system was
easy and required minimal effort. The development team wrote mocks for their
external dependencies using Coyote. Writing of mocks is common for any
kind of unit testing. The Coyote mocks turned out to be concise and it
was easy to express a number of safety and failure scenarios in a localized
manner.

Coyote testing helped repro a couple of known safety and
liveness bugs in the system. It also revealed a series of bugs in recent changes
that had passed code review as well as manual testing. Coyote testing
provides confidence that the system will not regress: once a concurrency issue
is found and a corresponding Coyote test is put in place, similar issues get
caught out immediately in future code changes. This allowed the
team to make progress at a rapid pace.
