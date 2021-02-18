## Deadlock in bounded-buffer

Concurrent programming can be tricky. This tutorial shows a classic example of a deadlock and how
Coyote can help you find and understand that deadlock. More details about this program and how
Coyote can find this deadlock is found in this [blog
article](https://cloudblogs.microsoft.com/opensource/2020/07/14/extreme-programming-meets-systematic-testing-using-coyote/).

## What you will need

To run the `BoundedBuffer` example, you will need to:

- Install [Visual Studio 2019](https://visualstudio.microsoft.com/downloads/).
- Install the [.NET 5.0 version of the coyote tool](../get-started/install.md).
- Clone the [Coyote Samples git repo](http://github.com/microsoft/coyote-samples).
- Be familiar with the `coyote test` tool. See [Testing](../tools/testing.md).

## Build the samples

Build the `coyote-samples` repo by running the following command:

```plain
powershell -f build.ps1
```

## Run the failover coffee machine application

Now you can run the `BoundedBuffer` application in a mode that should trigger the deadlock most of
the time:

```plain
./bin/net5.0/BoundedBuffer.exe -m
```

And you can run it with a fix for the deadlock as follows:

```plain
./bin/net5.0/BoundedBuffer.exe -f
```

### Can you find the deadlock bug in BoundedBuffer class?

The BoundedBuffer is a producer consumer queue where the `Take` method blocks if the buffer is empty
and the `Put` method blocks if the buffer has reached it's maximum allowed capacity and while the
code looks correct it contains a nasty deadlock bug.

Writing concurrent code is tricky and with the popular `async/await` feature in the C# Programming
Language it is now extremely easy to write concurrent code.

But how do we test concurrency in our code? Testing tools and frameworks have not kept up with the
pace of language innovation and so this is where `Coyote` comes to the rescue. When you use Coyote
to test your programs you will find concurrency bugs that are very hard to find any other way.

Coyote rewrites your `async tasks` in a way that allows the [coyote test](../tools/testing.md) tool
to control all the concurrency and locking in your program and this allows it to find bugs using
intelligent [systematic testing](../core/systematic-testing.md).

For example, if you take the `BoundedBuffer.dll` from the above sample you can do the following:

```
coyote rewrite BoundedBuffer.dll
coyote test BoundedBuffer.dll -m TestBoundedBufferMinimalDeadlock --iterations 100
```

This will report a deadlock error because Coyote has deadlock detection during testing. You will get
a log file explaining all this, and more importantly you will also get a trace file that can be used
to replay the bug in your debugger in a way that is 100% reproducible.

Concurrency bugs tend to be the kind of bugs that keep people up late at night pulling their hair
out because they are often not easily reproduced in any sort of predictable manner.

Coyote solves this problem giving you an environment where concurrency bugs can be systematically
found and reliably reproduced in a debugger -- allowing developers to fully understand them and fix
the core problem.
