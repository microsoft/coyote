---
layout: reference
title: Azure Batch Service case study
section: case-studies
permalink: /case-studies/azure-batch-service
---

# Azure Batch Service

![icon](../assets/images/Azure-Batch.png)

## Background

[Azure Batch Service](https://azure.microsoft.com/en-us/services/batch/) is a cloud-scale
job-scheduling service. Users can submit a parallel job consisting of multiple tasks with a given set
of dependencies and Azure Batch Service will execute them on Azure, in dependency order, exploiting as much
parallelism as possible between independent tasks. Batch is a popular service, managing over hundreds
of thousands of VMs on Azure.

Integrating scheduling with virtual machine (VM) management, Azure Batch Service supports
auto-scaling the number of VMs created, spinning up or down according to the needs of the job.
This differs from many other schedulers—like Yarn or Mesos, for example—that must be installed
on a pre-created set of VMs.

## Challenge

The Batch team wanted to invest in a new microservices-based architecture that would reliably
scale to meet the demands of the service. The complex responsive design demanded that each
microservice be able to:

- Process requests asynchronously as they arrived.
- Support cancellation of an in-flight request, enabling quick turnaround for auto-scaling.
- Be resilient to failures of VMs hosting the service.

## Solution

Coding three of their core microservices with Coyote, the team used Coyote's state machines
programming model for fully asynchronous, non-blocking computation. The team also wrote
detailed functional specifications—as well as models of external services—to allow for
exhaustive testing of concurrent behaviors and failures. These services totalled to more than
100,000 lines of code.

## Coyote’s key advantages

The Batch team reported several key advantages of developing and testing their code using Coyote.

- Faster development time: adding a new feature for supporting low-priority VMs to the Coyote code took
just one developer month. The same feature took six developer months in the legacy code. Coyote
design and testing added agility and allowed progress at a much faster pace.
- Coyote removed reliance on overly complex, and often inadequate, unit tests for each small
component. The Batch team reported that Coyote's test coverage for end-to-end scenarios was _unheard
of_ previously.
- Features were developed in a test environment to first pass the Coyote tester. When dropped in
production, they simply worked from the start.
- Coyote gave developers a significant confidence boost by providing full failover and concurrency testing at each check-in, right on their desktops as the code was written.
