## Installing Coyote

Coyote is an open-source cross-platform .NET library and tool, which means it can be used on
Windows, Linux and macOS.

### Prerequisites

Install the [.NET SDK](https://dotnet.microsoft.com/download/dotnet) for one of the .NET target
frameworks supported by Coyote:

| Target Framework      | Operating System      |
| :-------------------: | :-------------------: |
| .NET 7.0              | Linux, macOS, Windows |
| .NET 6.0              | Linux, macOS, Windows |
| .NET Core 3.1         | Linux, macOS, Windows |
| .NET Standard 2.0     | Linux, macOS, Windows |
| .NET Framework 4.6.2  | Windows               |

Learn more about the .NET target frameworks
[here](https://learn.microsoft.com/en-us/dotnet/standard/frameworks) and .NET Standard
[here](https://learn.microsoft.com/en-us/dotnet/standard/net-standard). Coyote supports new .NET
target frameworks once they are released, and until they reach
[end-of-life](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core).

Additionally, you can **optionally** install:
- [Visual Studio Code](https://code.visualstudio.com/Download), which is cross-platform.
- [Visual Studio 2022](https://docs.microsoft.com/en-us/visualstudio/install/install-visual-studio)
if you are on Windows.
- The [DGML editor](../how-to/generate-dgml.md) feature of Visual Studio 2022.

### Installing the Coyote NuGet packages

The Coyote libraries can be easily installed by adding the
[`Microsoft.Coyote.Core`](https://www.nuget.org/packages/Microsoft.Coyote.Core/) NuGet package and
the [`Microsoft.Coyote.Test`](https://www.nuget.org/packages/Microsoft.Coyote.Test/) NuGet package
to your C# project. You can then immediately start programming the Coyote API as shown in the
[samples](https://github.com/microsoft/coyote/tree/main/Samples).

You can manually add Coyote to your C# project by using:
```plain
dotnet add <yourproject>.csproj package Microsoft.Coyote.Core
dotnet add <yourproject>.csproj package Microsoft.Coyote.Test
```

Alternatively, Coyote provides the
[`Microsoft.Coyote`](https://www.nuget.org/packages/Microsoft.Coyote/) NuGet meta-package, which
includes all the other Coyote packages. You can install this by using:
```plain
dotnet add <yourproject>.csproj package Microsoft.Coyote
```

### Installing the Coyote tool

You can install and use the cross-platform `coyote` command-line tool without having to build Coyote
from source. To use `coyote` from the command-line, you must first install it as a `dotnet tool`
using the following command:
```plain
dotnet tool install --global Microsoft.Coyote.CLI
```
Using the `--global` flag installs `coyote` for the current user. You can update the global `coyote`
tool by running the following command:
```plain
dotnet tool update --global Microsoft.Coyote.CLI
```
You can remove the global `coyote` tool by running the following command:
```plain
dotnet tool uninstall --global Microsoft.Coyote.CLI
```

Alternatively, to install the tool **locally** on a specific repo, so anyone who clones the repo
gets access to the same version that you use, you must first use the following command from the root
of that repo:
```
dotnet new tool-manifest
```

This creates a new `<path>/.config/dotnet-tools.json` file. (You can skip this step if you already
have such a .NET tool manifest file in your repo.) Now you can install the `coyote` tool using the
following command:
```bash
dotnet tool install --local Microsoft.Coyote.CLI
# You can invoke the tool from this directory using the following commands:
#  'dotnet tool run coyote' or 'dotnet coyote'.
# Tool 'microsoft.coyote.cli' (version '...') was successfully installed.
# Entry is added to the manifest file <path>/.config/dotnet-tools.json.
```

The `coyote` tool can now be version controlled in your repo, so that it can easily be shared with
other developers. The `<path>/.config/dotnet-tools.json` file will look like this:
```json
{
  "version": 1,
  "isRoot": true,
  "tools": {
    "microsoft.coyote.cli": {
      "version": "...",
      "commands": [
        "coyote"
      ]
    }
  }
}
```

Each time you clone your repo and want to restore the `coyote` tool, you can run:
```bash
dotnet tool restore
```

You can also edit the `<path>/.config/dotnet-tools.json` file to upgrade the version of `coyote` and
run the same `dotnet tool restore` command to upgrade the tool.

Learn more about .NET tools and how to manage them
[here](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools).

**Note:** the above command line tool is only for the cross-platform .NET target frameworks. If you
prefer to not install and manage `coyote` as a .NET tool (via the `dotnet tool` command), or you
need a version of `coyote.exe` that runs on .NET Framework for Windows, then you can instead download
the [`Microsoft.Coyote.Tool`](https://www.nuget.org/packages/Microsoft.Coyote.Tool/) NuGet package, which
includes the self-contained tool for all supported target frameworks. To install this package run:
```plain
dotnet add package Microsoft.Coyote.Tool
```
You will find the `coyote` executable in the `tools\<dotnet_target_framework>` directory of the
package, and you can run it from inside that directory.

### Using the Coyote tool

You can now start using the `coyote` command-line tool! Type `coyote --help` to see if it is
working. To learn how to use the Coyote tool read [here](using-coyote.md).

### Troubleshooting

#### The element 'metadata' in namespace nuspec.xsd has invalid child element 'repository'...

If you get an error building the nuget package, you may need to download
a new version of `nuget.exe` from [https://www.nuget.org/downloads](https://www.nuget.org/downloads).
