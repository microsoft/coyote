---
layout: reference
title: Azure Batch Service case study
section: case-studies
permalink: /case-studies/azure-batch-service
---

## Azure Batch Service case study

### Background

Azure Batch Service is a popular cloud-scale job-scheduling service. Users can execute a parallel job consisting of multiple tasks with a given set of dependencies and Azure Batch Service will perform these in dependency order, exploiting as much parallelism as possible between independent tasks. 

Integrating scheduling with virtual machine (VM) management, Azure Batch Service supports a massive pool of VMs as it auto-scales the number created, spinning up or down according to the needs of the job. This differs from many other schedulers—like Yarn or Mesos, for example—that must be installed on a pre-created set of VMs.

### Challenge

The team wanted to invest in a new architecture of multiple microservices that would reliably scale to meet the demands of the service. The complex responsive design demanded that each microservice be able to:


- Process requests asynchronously as they arrived. 
- Support cancellation of an in-flight request, enabling quick turnaround for auto-scaling. 
- Be resilient to failures of VMs hosting the service. 


### Solution

Coding three of their core microservices using Coyote state machines programming model for fully asynchronous, non-blocking computation. The team also wrote detailed functional specifications—as well as models of external services—to allow for exhaustive testing of concurrent behaviors and failures. More than 100,000 lines of code were written in Coyote.


### Discoveries: Coyote’s key advantages

- The team discovered a previously unheard-of end-to-end test coverage. Coyote removed reliance on overly complex, and often inadequate, unit tests for each small component.
- They frequently found bugs with the Coyote tester in a matter of minutes—bugs that would have taken days to discover with stress testing. 
- They saved dev time: Coyote’s design and testing agility required only one developer month for adding a new feature supporting low-priority VMs. This is in sharp contrast with the six developer months required by legacy code.
- They could develop features entirely in Coyote’s test environment. When dropped into production, the features worked immediately. 
- Coyote gave developers a significant confidence boost to do full failover and concurrency testing at each check-in, right on their desktops as the code was written. 

