# `Coyote`

[![NuGet](https://img.shields.io/nuget/v/Microsoft.Coyote.svg)](https://www.nuget.org/packages/Microsoft.Coyote/)
[![Join the chat at https://gitter.im/Microsoft/coyote](https://badges.gitter.im/Microsoft/coyote.svg)](https://gitter.im/Microsoft/coyote?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
[![Follow on Twitter](https://img.shields.io/twitter/follow/coyote_dev?style=social&logo=twitter)](https://twitter.com/intent/follow?screen_name=coyote_dev)

![Build and Test CI](https://github.com/microsoft/coyote/actions/workflows/test-coyote.yml/badge.svg?branch=main)
![CodeQL](https://github.com/microsoft/coyote/actions/workflows/codeql-analysis.yml/badge.svg?branch=main)

Coyote is a testing library and tool that helps ensure that your C# code is free of annoying
concurrency bugs.

It gives you the ability to reliably *unit test the concurrency* and other sources of nondeterminism
(such as message re-orderings, timeouts and failures) in your C# code. In the heart of Coyote is a
scheduler that takes control (via binary rewriting) of your program's concurrent execution during
testing and is able to _systematically explore_ the concurrency and nondeterminism to find bugs. The
awesome thing is that once Coyote finds a bug it gives you the ability to fully _reproduce_ it as
many times as you want, making debugging and fixing the issue much easier.

Coyote is used by many teams in [Azure](https://azure.microsoft.com/) to test their distributed
systems and services, finding hundreds of concurrency-related bugs before they manifest in
production. In the words of an Azure service architect:
> Coyote found several issues early in the dev process, this sort of issues that would usually bleed
> through into production and become very expensive to fix later.

Coyote is made with :heart: by Microsoft Research.

## Get started

See [https://microsoft.github.io/coyote](https://microsoft.github.io/coyote/) for more information
about Coyote, documentation, tutorials, samples and how-tos. Enjoy!

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
