## Overview of Coyote samples

All Coyote samples are available on [GitHub](http://github.com/microsoft/coyote-samples). The repo
is organized in two sets of samples.

The first set shows how you can use Coyote to systematically test unmodified C# task-based programs:

- [AccountManager](http://github.com/microsoft/coyote-samples/tree/main/AccountManager):
  demonstrates how to write a simple task-based C# application to create, get and delete account
  records in a backend NoSQL database and then systematically test this application using Coyote to
  find a race condition. Read the accompanying two-parts tutorial available
  [here](../tutorials/first-concurrency-unit-test.md) and
  [here](../tutorials/test-concurrent-operations.md).
- [Coffee Machine Failover](http://github.com/microsoft/coyote-samples/tree/main/CoffeeMachineTasks): demonstrates
  how to systematically test the failover logic in your task-based applications. Read the
  accompanying tutorial available [here](../tutorials/test-failover.md).
- [BoundedBuffer](http://github.com/microsoft/coyote-samples/tree/main/BoundedBuffer): demonstrates
  how to use `coyote rewrite` to find deadlocks in unmodified C# code. Read more about this sample
  [here](tasks/bounded-buffer.md).

The second set shows how you can use the more advanced
[actor](https://microsoft.github.io/coyote/concepts/actors/overview/) programming model of
Coyote to build reliable applications and services:

- [HelloWorldActors](http://github.com/microsoft/coyote-samples/tree/main/HelloWorldActors):
  demonstrates how to write a simple Coyote application using actors, and then run and
  systematically test it. Read the accompanying tutorial available
  [here](../tutorials/actors/hello-world.md).
- [CloudMessaging](http://github.com/microsoft/coyote-samples/tree/main/CloudMessaging):
  demonstrates how to write a Coyote application that contains components that communicate with each
  other using the [Azure Service Bus](https://azure.microsoft.com/en-us/services/service-bus/) cloud
  messaging queue. ead the accompanying two-parts tutorial available
  [here](../tutorials/actors/raft-azure.md) and [here](../tutorials/actors/raft-mocking.md).
- [Timers in Actors](http://github.com/microsoft/coyote-samples/tree/main/Timers): demonstrates how
  to use the timer API of the Coyote actor programming model.
- [Coffee Machine Failover](http://github.com/microsoft/coyote-samples/tree/main/CoffeeMachineActors): demonstrates
  how to systematically test the failover logic in your Coyote actor applications. Read the
  accompanying tutorial available [here](../tutorials/actors/test-failover.md).
- [Robot Navigator Failover](http://github.com/microsoft/coyote-samples/tree/main/DrinksServingRobotActors):
  demonstrates how to systematically test the failover logic in your Coyote actors applications.
  Read more about this sample [here](actors/failover-robot-navigator.md).

### Building the samples

To build the samples, clone the [Coyote samples repo](http://github.com/microsoft/coyote-samples),
then use the following `PowerShell` command line from a Visual Studio 2019 Developer Command Prompt:

```plain
powershell -f build.ps1
```

In your local [Coyote samples](http://github.com/microsoft/coyote-samples) repo you can find the
compiled binaries in the `bin` folder. You can use the `coyote` tool to automatically test these
samples and find bugs. Read how to use the tool [here](../get-started/using-coyote.md).
