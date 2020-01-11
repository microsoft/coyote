---
layout: reference
section: learn
title: Raft Example
permalink: /learn/tutorials/raft-mocking
---

## Raft Mocking Example

In the [previous example](raft-azure) you created an Azure example that uses Coyote
which performs messaging using an Azure Service Bus.  This is a great way to build
a reliable business application.  But there is overhead in using an enterprise scale
service bus, which limits our ability to test the state machine.

In this example you will `mock` the Azure Service Bus which allows the [Coyote tester](/coyote/learn/tools/testing)
to perform many per tests per second and thereby find bugs in the example core more quickly.

## What you will need

You will also need to:
- Install [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/).
- Build the [Coyote project](/coyote/learn/get-started/install) and in the build output find the netcoreapp2.2 version `coyote.dll`.
This will live in a path like this: `c:\git\coyote\bin\netcoreapp2.2\coyote.dll`.  Set this path in a new environment variable named `coyote`
- Clone the [Coyote Samples git repo](http://github.com/microsoft/coyote-samples).


## Build the Samples

Build the `coyote-samples` repo by running the following command:

```
powershell -f build.ps1
```

## Run the Raft.Mocking application

Now you can run the Raft.Mocking application

```shell
dotnet %coyote% test Mocking/bin/netcoreapp2.2/Raft.Mocking.dll -i 1000 -ms 200
```

Notice this application is able to run 200 steps per iteration and a thousand iterations pretty quickly, much faster than if
all those messages were going to Azure and back.

You can now play with other test parameters like `--graph` to see a [DGML Graph](https://en.wikipedia.org/wiki/DGML) of all the messages sent during the test.
You can browse these graphs using Visual Studio.  See [Dgml Editor setup](/coyote/learn/get-started/install).

See the [animating state machine demo](/coyote/learn/programming-models/actors/state-machine-demo) which shows what the [systematic testing](/learn/core/systematic-testing) looks like on this application.

## Design

Add some pretty pictures of the design, where the Mocking plugs in,
point out how the original app was designed with a messaging interface to make mocking easy, etc, etc.