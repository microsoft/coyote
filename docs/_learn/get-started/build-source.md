---
title: Build from source
permalink: /learn/get-started/build-source
layout: reference
section: learn
navsection: install
template: basic
---

## Build from source

If you plan to contribute a Pull Request to the Coyote repo then you need to be able to build the Coyote source code
and run the tests.

<a href="http://github.com/microsoft/coyote" class="btn btn-primary mt-20" target="_blank">Clone the github repo</a>

### Prerequisites

- [Visual Studio 2019](https://docs.microsoft.com/en-us/visualstudio/install/install-visual-studio) on Windows.
- [Visual Studio Code](https://code.visualstudio.com/Download) - is handy to have on other platforms.
- [.NET Core SDK 3.1.201](https://dotnet.microsoft.com/download/dotnet-core)
- [.NET Framework SDK 4.6, 4.7 and 4.8](https://dotnet.microsoft.com/download/dotnet-framework) on Windows.
These are optional, but you should test them before you submit a PR as Coyote CI build will build
and test binaries for all these .NET frameworks versions.

### Building the Coyote project

Clone the [Coyote repo](http://github.com/microsoft/coyote), then open `Coyote.sln` and build.

You can also use the following `PowerShell` command line from a Visual Studio 2019 Developer Command
Prompt:

```
powershell -f Scripts/build.ps1
```

### Building the NuGet packages

In the Coyote project run this `PowerShell` command line from a Visual Studio 2019 Developer
Command Prompt:

```
powershell -f .\Scripts\create-nuget-packages.ps1
```

### Installing the Coyote command line tool package

You can install the `coyote` tool from this locally built package using:

```
dotnet tool install --global --add-source ./bin/nuget Microsoft.Coyote.CLI
```

To update your version of the tool you will have to first uninstall the previous version using:

```
dotnet tool uninstall --global Microsoft.Coyote.CLI
```

### Using a local NuGet package

The samples use the published Microsoft.Coyote NuGet package by default. If you want the samples to
use the same Coyote bits you built from the Coyote repo, then edit the NuGet.config file and
uncomment the following line:
```xml
<add key="Coyote" value="../Coyote/bin/nuget"/>
```

Now you can rebuild the samples and so long as the `Common\version.props` file contains the same
version in both `coyote` and `coyote-samples` then it will pick up your newly created NuGet package
and use that. Note: this can make debugging into the Coyote runtime possible.

Now you are ready to [start using Coyote](/coyote/learn/get-started/using-coyote).

### Installing the Coyote command line tool for .NET Frameworks on Windows

If you prefer to use .NET 4.6, 4.7 or 4.8 instead of .NET Core 3.1, then you will need to add the
location of the tool to your `PATH` environment as follows.  First you must decide what .NET
platform you want to use.  Coyote supports .NET 4.6, 4.7 and 4.8.  The `coyote` tool that
you use must match the platform of the sample app you are testing.  Pick one of the following
locations:

```
set COYOTE_PATH=d:\git\coyote\bin\net46
set COYOTE_PATH=d:\git\coyote\bin\net47
set COYOTE_PATH=d:\git\coyote\bin\net48
```

Then run this to modify your local PATH environment:
```
set PATH=%PATH%;%COYOTE_PATH%
```

### Running the tests

To run all available tests, execute the following `PowerShell` command line from a Visual Studio
2019 Developer Command Prompt:

```
powershell -f Scripts/run-tests.ps1
```

You can also run a specific category of tests by adding the `-test` option to specify the category
name, for example:

```
powershell -f Scripts/run-tests.ps1 -test core
```
