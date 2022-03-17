## Overview of Coyote samples

All Coyote samples are available on [GitHub](https://github.com/microsoft/coyote/tree/main/Samples).
They are organized in two sets of samples.

The first set shows how you can use Coyote to systematically test unmodified C# task-based programs:

- [AccountManager](https://github.com/microsoft/coyote/tree/main/Samples/AccountManager):
  demonstrates how to write a simple task-based C# application to create, get and delete account
  records in a backend NoSQL database and then systematically test this application using Coyote to
  find a race condition. Read the accompanying two-parts tutorial available
  [here](../tutorials/first-concurrency-unit-test.md) and
  [here](../tutorials/test-concurrent-operations.md).
- [ImageGalleryAspNet](https://github.com/microsoft/coyote/tree/main/Samples/WebApps/ImageGalleryAspNet):
  demonstrates how to use Coyote to test an ASP.NET Core service. Read the accompanying tutorial
  available [here](../tutorials/testing-aspnet-service.md).
- [Coffee Machine Failover](https://github.com/microsoft/coyote/tree/main/Samples/CoffeeMachineTasks): demonstrates
  how to systematically test the failover logic in your task-based applications. Read the
  accompanying tutorial available [here](../tutorials/test-failover.md).
- [BoundedBuffer](https://github.com/microsoft/coyote/tree/main/Samples/BoundedBuffer): demonstrates
  how to use `coyote rewrite` to find deadlocks in unmodified C# code. Read more about this sample
  [here](tasks/bounded-buffer.md).

The second set shows how you can use the more advanced
[actor](https://microsoft.github.io/coyote/concepts/actors/overview/) programming model of
Coyote to build reliable applications and services:

- [HelloWorldActors](https://github.com/microsoft/coyote/tree/main/Samples/HelloWorldActors):
  demonstrates how to write a simple Coyote application using actors, and then run and
  systematically test it. Read the accompanying tutorial available
  [here](../tutorials/actors/hello-world.md).
- [CloudMessaging](https://github.com/microsoft/coyote/tree/main/Samples/CloudMessaging):
  demonstrates how to write a Coyote application that contains components that communicate with each
  other using the [Azure Service Bus](https://azure.microsoft.com/en-us/services/service-bus/) cloud
  messaging queue. ead the accompanying two-parts tutorial available
  [here](../tutorials/actors/raft-azure.md) and [here](../tutorials/actors/raft-mocking.md).
- [Timers in Actors](https://github.com/microsoft/coyote/tree/main/Samples/Timers): demonstrates how
  to use the timer API of the Coyote actor programming model.
- [Coffee Machine Failover](https://github.com/microsoft/coyote/tree/main/Samples/CoffeeMachineActors): demonstrates
  how to systematically test the failover logic in your Coyote actor applications. Read the
  accompanying tutorial available [here](../tutorials/actors/test-failover.md).
- [Robot Navigator Failover](https://github.com/microsoft/coyote/tree/main/Samples/DrinksServingRobotActors):
  demonstrates how to systematically test the failover logic in your Coyote actors applications.
  Read more about this sample [here](actors/failover-robot-navigator.md).

### Building the samples

Follow the instructions [here](https://github.com/microsoft/coyote/tree/main/Samples/README.md).
