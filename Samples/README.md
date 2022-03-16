# Coyote Samples

This directory contains two sets of Coyote samples.

The first set of samples shows how you can use Coyote to systematically test unmodified C#
task-based applications and services:

- [AccountManager](./AccountManager): demonstrates how to write a simple task-based C# application
  to create, get and delete account records in a backend NoSQL database and then systematically test
  this application using Coyote to find a race condition.
- [ImageGalleryAspNet](./WebApps/ImageGalleryAspNet): demonstrates how to use Coyote to test an ASP.NET Core
  service.
- [Coffee Machine Failover](./CoffeeMachineTasks): demonstrates how to systematically test
  the failover logic in your task-based applications.
- [BoundedBuffer](./BoundedBuffer): demonstrates how to use `coyote rewrite` to find deadlocks in
  unmodified C# code.

The second set of samples shows how you can use the Coyote
[actor](https://microsoft.github.io/coyote/concepts/actors/overview/) programming model
to build reliable applications and services:

- [HelloWorldActors](./HelloWorldActors): demonstrates how to write a simple Coyote application
  using actors, and then run and systematically test it.
- [CloudMessaging](./CloudMessaging): demonstrates how to write a Coyote application that contains
  components that communicate with each other using the [Azure Service
  Bus](https://azure.microsoft.com/en-us/services/service-bus/) cloud messaging queue.
- [Coffee Machine Failover](./CoffeeMachineActors): demonstrates how to systematically test
  the failover logic in your Coyote actor applications.
- [Robot Navigator Failover](./DrinksServingRobotActors): demonstrates how to
  systematically test the failover logic in your Coyote actors applications.
- [Timers in Actors](./Timers): demonstrates how to use the timer API of the Coyote actor
  programming model.

## Get started

To build and run the samples, you will need to:

- Install the [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet).
- Install the [.NET 6.0 version of the coyote
  tool](https://microsoft.github.io/coyote/get-started/install/).

Once you are ready, clone this repository and build the samples by running the following script in
`powershell`:
```
./Samples/Scripts/build.ps1
```

Then, follow the instructions in each sample.
