---
title: Get started with Coyote
permalink: /learn/get-started/install
layout: reference
section: learn
navsection: install
template: basic
---

## Installing Coyote

Coyote is a NuGet library and works on .NET Core which means it can be used on Windows, Linux and
macOS.

### Prerequisites
- [.NET Core SDK 3.1.201](https://dotnet.microsoft.com/download/dotnet-core)

**Optional:**
- [Visual Studio 2019](https://docs.microsoft.com/en-us/visualstudio/install/install-visual-studio)
if you are on Windows.
- [Visual Studio Code](https://code.visualstudio.com/Download) is handy to have on other platforms.
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

You can manually add Coyote to your C# project by using:

```
dotnet add <yourproject>.csproj package Microsoft.Coyote
```

### Installing the .NET Core 3.1 coyote tool

You can also install the `dotnet tool` named `coyote` for .NET Core 3.1 using the following command:

```
dotnet tool install --global Microsoft.Coyote.CLI
```

Now you can run the `coyote test` tool without having to build Coyote from source. Type `coyote
--help` to see if it is working. The dotnet tool install can also install coyote to a `--local`
folder if you prefer that.

You can remove the global `coyote` tool by running the following command:

```
dotnet tool uninstall --global Microsoft.Coyote.CLI
```

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
