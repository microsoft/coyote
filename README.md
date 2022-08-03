# `Coyote`

[![NuGet](https://img.shields.io/nuget/v/Microsoft.Coyote.svg)](https://www.nuget.org/packages/Microsoft.Coyote/)
[![Follow on Twitter](https://img.shields.io/twitter/follow/coyote_dev?style=social&logo=twitter)](https://twitter.com/intent/follow?screen_name=coyote_dev)

![Build and Test CI](https://github.com/microsoft/coyote/actions/workflows/test-coyote.yml/badge.svg?branch=main)
![CodeQL](https://github.com/microsoft/coyote/actions/workflows/codeql-analysis.yml/badge.svg?branch=main)

Coyote is a library and tool for testing concurrent C# code and deterministically reproducing bugs.

Using Coyote, you easily test the *concurrency* and other *nondeterminism* in your C# code, by
writing what we call a *concurrency unit test*. These look like your regular unit tests, but can
reliably test concurrent workloads (such as actors, tasks, or concurrent requests to ASP.NET
controllers). In regular unit tests, you would typically avoid concurrency due to flakiness, but
with Coyote you are encouraged to embrace concurrency in your tests to find bugs.

Coyote is used by many teams in [Azure](https://azure.microsoft.com/) to test their distributed
systems and services, and has has found hundreds of concurrency-related bugs before deploying code
in production and affecting users. In the words of an Azure service architect:
> Coyote found several issues early in the dev process, this sort of issues that would usually bleed
> through into production and become very expensive to fix later.

Coyote is made with :heart: by Microsoft Research.

## How it works?

Consider the following simple test:
```csharp
[Fact]
public async Task TestTask()
{
  int value = 0;
  Task task = Task.Run(() =>
  {
    value = 1;
  });

  Assert.Equal(0, value);
  await task;
}
```

This test will pass most of the time because the assertion will typically execute before the task
starts, but there is one schedule where the task starts fast enough to set `value` to `1` causing
the assertion to fail. Of course, this is a very naive example and the bug is obvious, but you could
imagine much more complicated race conditions that are hidden in complex execution paths.

The way Coyote works, is that you first convert the above test to a concurrency unit test using the
Coyote `TestingEngine` API:
```csharp
using Microsoft.Coyote.SystematicTesting;

[Fact]
public async Task CoyoteTestTask()
{
  var configuration = Configuration.Create().WithTestingIterations(10);
  var engine = TestingEngine.Create(configuration, TestTask);
  engine.Run();
}
```

Next, you run the `coyote rewrite` CLI (typically as a post-build task) to automatically rewrite the
IL of your test and production binaries. This allows Coyote to inject hooks that take control of the
concurrent execution during testing.

You then run the concurrent unit test from your favorite unit testing framework (such as
[xUnit](https://xunit.net/) above). Coyote will take over and repeatedly execute the test from
beginning to the end for N iterations (in the above example N was configured to `10`). Under the
hood, Coyote uses intelligent search strategies to explore all kinds of execution paths that might
hide a bug in each iteration.

The awesome thing is that once a bug is found, Coyote gives you a trace that you can use to reliably
*reproduce* the bug as many times as you want, making debugging and fixing the issue significantly
easier.

## Get started

Getting started with Coyote is easy! Check out
[https://microsoft.github.io/coyote](https://microsoft.github.io/coyote/) for tutorials,
documentation, how-tos, samples and more information about the project. Enjoy!

If you are a Microsoft employee, please consider joining the internal-only [Friends of Coyote Teams
channel](https://teams.microsoft.com/l/channel/19%3a1fe966b4fdc544bca648d89bf25c3c56%40thread.tacv2/General?groupId=7a6d8afc-c23d-4e5d-b9cb-9124118c0220&tenantId=72f988bf-86f1-41af-91ab-2d7cd011db47),
to be part of our community and learn from each other. Otherwise, please feel free to start a
[discussion](https://github.com/microsoft/coyote/discussions) with us or open an
[issue](https://github.com/microsoft/coyote/issues) on GitHub, thank you!

## Contributing

This project welcomes contributions and suggestions. Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide a
CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repositories using our CLA.

## Code of Conduct

This project has adopted the [Microsoft Open Source Code of
Conduct](https://opensource.microsoft.com/codeofconduct/). For more information see the [Code of
Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or contact
[opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.
