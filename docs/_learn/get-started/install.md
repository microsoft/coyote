---
title: Get started with Coyote
permalink: /learn/get-started/install
layout: reference
section: learn
navsection: install
template: basic
---

## Installing the NuGet package

<div>
<a href="https://www.nuget.org/packages/Microsoft.Coyote/" class="btn btn-primary mt-20 mr-30" target="_blank">Install NuGet package</a>
<br/>
<br/>
</div>

The Coyote libraries and tools can be easily installed by adding the `Microsoft.Coyote` [NuGet
package](https://www.nuget.org/packages/Microsoft.Coyote/) to your C# project. You can then
immediately start programming the Coyote API as shown in the
[samples](http://github.com/microsoft/coyote-samples).

## Installing the .NET core 3.1 coyote tool

You can also install the `dotnet tool` named `coyote` for .NET Core 3.1 using the following command:

```
dotnet tool install --global Microsoft.Coyote.CLI
```

Now you can run the `coyote test` tool without having to build Coyote from source.  The dotnet tool install can also install coyote to a `--local` folder if you prefer that.

You can remove the global `coyote` tool by running the following command:

```
dotnet tool uninstall --global Microsoft.Coyote.CLI
```

## Build from source

<a href="http://github.com/microsoft/coyote" class="btn btn-primary mt-20" target="_blank">Build from source</a>

### Prerequisites

- [Visual Studio 2019](https://docs.microsoft.com/en-us/visualstudio/install/install-visual-studio)
- [.NET Core SDK 3.1.201](https://dotnet.microsoft.com/download/dotnet-core)
- [.NET Framework SDK 4.6, 4.7 and 4.8](https://dotnet.microsoft.com/download/dotnet-framework), as Coyote builds binaries for 4.6, 4.7 and 4.8.

- Optional: add the [DGML editor](../tools/dgml) feature of Visual Studio 2019.

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

If you did not build the coyote source code then you can find the coyote tool inside the NuGet
package location here:

```
set COYOTE_PATH=d:\git\coyote-samples\packages\microsoft.coyote\<version>\lib\net46\
set COYOTE_PATH=d:\git\coyote-samples\packages\microsoft.coyote\<version>\lib\net47\
set COYOTE_PATH=d:\git\coyote-samples\packages\microsoft.coyote\<version>\lib\net48\
```

where `<version>` is the coyote version number matching what you installed.
If you built local source the the version needs to match the version in `Common\version.props`.
Then based on what you decide you can now add this to your PATH environment:

```
set PATH=%PATH%;%COYOTE_PATH%
```

Now type `coyote --help` to see if it is working.

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

### Building the samples

Clone the [Coyote samples repo](http://github.com/microsoft/coyote-samples), then use the following
`PowerShell` command line from a Visual Studio 2019 Developer Command Prompt:

```
powershell -f build.ps1
```
