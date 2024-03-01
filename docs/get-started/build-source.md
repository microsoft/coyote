## Build from source

If you plan to contribute a Pull Request to Coyote then you need to be able to build the source code
and run the tests.

<a href="http://github.com/microsoft/coyote" class="btn btn-primary mt-20" target="_blank">Clone
the github repo</a>

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet)

**Optional:**

- [Visual Studio 2022](https://docs.microsoft.com/en-us/visualstudio/install/install-visual-studio)
on Windows.
- [Visual Studio Code](https://code.visualstudio.com/Download) is handy to have on other platforms.

### Building the Coyote project

Clone the [Coyote repo](http://github.com/microsoft/coyote), then open `Coyote.sln` and build.

You can also use the following `PowerShell` command line from a Visual Studio 2022 Developer
Command Prompt:

```plain
powershell -f Scripts/build.ps1
```

### Building the NuGet packages

In the Coyote project run this `PowerShell` command line from a Visual Studio 2022 Developer
Command Prompt:

```plain
powershell -f Scripts/build.ps1 -nuget
```

### Installing the Coyote command line tool package

You can install the `coyote` tool from this locally built package using:

```plain
dotnet tool install --global --add-source ./bin/nuget Microsoft.Coyote.CLI
```

To update your version of the tool you will have to first uninstall the previous version using:

```plain
dotnet tool uninstall --global Microsoft.Coyote.CLI
```

Now you are ready to [start using Coyote](using-coyote.md).

### Running the tests

To run all available tests, execute the following `PowerShell` command line from a Visual Studio
2022 Developer Command Prompt:

```plain
powershell -f Scripts/run-tests.ps1
```

You can also run a specific category of tests by adding the `-test` option to specify the category
name, for example:

```plain
powershell -f Scripts/run-tests.ps1 -test core
```
