---
title: Get started with Coyote
permalink: /learn/get-started/install
layout: reference
section: learn
navsection: install
template: basic
---

## Installing Coyote

### Prerequisites

- [Visual Studio 2019](https://docs.microsoft.com/en-us/visualstudio/install/install-visual-studio) if you are on Windows.
- [Visual Studio Code](https://code.visualstudio.com/Download) - is handy to have on other platforms.

**Optional**:
- [.NET Core SDK 3.1.201](https://dotnet.microsoft.com/download/dotnet-core) if you want to develop a .NET core app
that uses Coyote.
- [.NET Framework SDK 4.6, 4.7 and 4.8](https://dotnet.microsoft.com/download/dotnet-framework) if you want to
develop a .NET Framework app on windows, these are the .NET versions that Coyote supports.
- Add the [DGML editor](../tools/dgml) feature of Visual Studio 2019.


### Installing the NuGet package

<div>
<a href="https://www.nuget.org/packages/Microsoft.Coyote/" class="btn btn-primary mt-20 mr-30" target="_blank">Install NuGet package</a>
<br/>
<br/>
</div>

The Coyote libraries and tools can be easily installed by adding the `Microsoft.Coyote` [NuGet
package](https://www.nuget.org/packages/Microsoft.Coyote/) to your C# project. You can then
immediately start programming the Coyote API as shown in the
[samples](http://github.com/microsoft/coyote-samples).

### Installing the .NET core 3.1 coyote tool

You can also install the `dotnet tool` named `coyote` for .NET Core 3.1 using the following command:

```
dotnet tool install --global Microsoft.Coyote.CLI
```

Now you can run the `coyote test` tool without having to build Coyote from source.  The dotnet tool install can also install coyote to a `--local` folder if you prefer that.

You can remove the global `coyote` tool by running the following command:

```
dotnet tool uninstall --global Microsoft.Coyote.CLI
```

### Installing the Coyote command line tool for .NET Frameworks on Windows

If you prefer to use .NET 4.6, 4.7 or 4.8 instead of .NET Core 3.1, then you will need to add the
location of the tool to your `PATH` environment as follows.  First you must decide what .NET
platform you want to use.  Coyote supports .NET 4.6, 4.7 and 4.8.  The `coyote` tool that
you use must match the platform of the sample app you are testing.  Pick one of the following
locations:

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

### Building the samples

Clone the [Coyote samples repo](http://github.com/microsoft/coyote-samples), then use the following
`PowerShell` command line from a Visual Studio 2019 Developer Command Prompt:

```
powershell -f build.ps1
```

### Troubleshooting

#### The element 'metadata' in namespace nuspec.xsd has invalid child element 'repository'...

If you get an error building the nuget package, you may need to download
a new version of `nuget.exe` from [https://www.nuget.org/downloads](https://www.nuget.org/downloads).
