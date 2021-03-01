## Using Coyote

Assuming you have [installed Coyote](install.md), the best place to get started is by following
[this tutorial](../tutorials/first-concurrency-unit-test.md) to learn how to write your first
concurrency unit test, which is also available as a [video on
YouTube](https://youtu.be/wuKo-9iRm6o).

Once you are ready to dive in, you read the [core concepts](../concepts/non-determinism.md) behind
Coyote and how it can be used to [test the concurrency in your
code](../concepts/concurrency-unit-testing.md).

There are many tutorials and samples available for you to explore Coyote further! To build the
samples, clone the [Coyote samples repo](http://github.com/microsoft/coyote-samples), then use the
following `PowerShell` command line from a Visual Studio 2019 Developer Command Prompt:

```plain
powershell -f build.ps1
```

In your local [Coyote samples](http://github.com/microsoft/coyote-samples) repo you can find the
compiled binaries in the `bin` folder. You can use the `coyote` tool to automatically test these
samples and find bugs. Read how to use the tool [here](../tools/rewriting.md).

Optionally, you can try out the advanced [actor and state
machine](../advanced-topics/actors/overview.md) programming model of Coyote, which allows you to
build a highly-reliable system from scratch using lightweight concurrency primitives that are
battle-tested inside Azure.

**Note:** If you are upgrading to Coyote from P#, see [upgrading from P#](upgrade-from-psharp.md).
