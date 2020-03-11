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

The Coyote framework can be easily installed by adding the `Microsoft.Coyote` [NuGet
package](https://www.nuget.org/packages/Microsoft.Coyote/) to your C# project. You can then
immediately start programming the Coyote API as shown in the
[samples](http://github.com/microsoft/coyote-samples).

## Build from source

<a href="http://github.com/microsoft/coyote" class="btn btn-primary mt-20" target="_blank">Build from source</a>

### Prerequisites

- [Visual Studio 2019](https://docs.microsoft.com/en-us/visualstudio/install/install-visual-studio)
- [.NET Core SDK 2.2.402](https://dotnet.microsoft.com/download/dotnet-core)
- [.NET Framework SDK 4.6 and 4.7](https://dotnet.microsoft.com/download/dotnet-framework), as Coyote builds binaries for both 4.6 and 4.7.

- Optional: add the [DGML editor](../tools/dgml) feature of Visual Studio 2019.

### Building the Coyote project

Clone the [Coyote repo](http://github.com/microsoft/coyote), then open `Coyote.sln` and build.

You can also use the following `PowerShell` command line from a Visual Studio 2019 Developer Command
Prompt:

```
powershell -f Scripts/build.ps1
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

### Building the samples

Clone the [Coyote samples repo](http://github.com/microsoft/coyote-samples), then use the following
`PowerShell` command line from a Visual Studio 2019 Developer Command Prompt:

```
powershell -f build.ps1
```

### Using a local NuGet package

The samples use the published Microsoft.Coyote NuGet package by default. If you want the samples to
use the same Coyote bits you built from the Coyote repo, then edit the NuGet.config file and
uncomment the following line:
```xml
<add key="Coyote" value="../Coyote/bin/nuget"/>
```

Then in the Coyote project run this `PowerShell` command line from a Visual Studio 2019 Developer
Command Prompt:

```
powershell -f .\Scripts\create-nuget-packages.ps1
```

Now you can rebuild the samples and so long as the `Common\version.props` file contains the same
version in both `coyote` and `coyote-samples` then it will pick up your newly created NuGet package
and use that. Note: this can make debugging into the Coyote runtime possible.

Now you are ready to [start using Coyote](/coyote/learn/get-started/using-coyote).
