## Installing Coyote

Coyote is a NuGet library and works on .NET Core which means it can be used on Windows, Linux and
macOS.

### Prerequisites
- [.NET 5.0 SDK](https://dotnet.microsoft.com/download/dotnet-core)

**Optional:**

- [Visual Studio 2019](https://docs.microsoft.com/en-us/visualstudio/install/install-visual-studio)
if you are on Windows.

- [Visual Studio Code](https://code.visualstudio.com/Download) is handy to have on other platforms.

- Add the [DGML editor](../how-to/generate-dgml.md) feature of Visual Studio 2019.

### Installing the NuGet package

<div>
<a href="https://www.nuget.org/packages/Microsoft.Coyote/" class="btn btn-primary mt-20 mr-30" target="_blank">Install Coyote package</a>
<a href="https://www.nuget.org/packages/Microsoft.Coyote.Test/" class="btn btn-primary mt-20 mr-30" target="_blank">Install Coyote Test package</a>
<br/>
<br/>
</div>

The Coyote libraries can be easily installed by adding the
[`Microsoft.Coyote`](https://www.nuget.org/packages/Microsoft.Coyote/) NuGet package and the
[`Microsoft.Coyote.Test`](https://www.nuget.org/packages/Microsoft.Coyote.Test/) NuGet package to
your C# project. You can then immediately start programming the Coyote API as shown in the
[samples](http://github.com/microsoft/coyote-samples).

You can manually add Coyote to your C# project by using:

```plain
dotnet add <yourproject>.csproj package Microsoft.Coyote
dotnet add <yourproject>.csproj package Microsoft.Coyote.Test
```

### Installing the .NET 5.0 Coyote tool

You can also install the `dotnet tool` named `coyote` for .NET 5.0 using the following command:

```plain
dotnet tool install --global Microsoft.Coyote.CLI
```

Now you can run the `coyote test` tool without having to build Coyote from source. Type `coyote
--help` to see if it is working. The dotnet tool install can also install coyote to a `--local`
folder if you prefer that.

You can update the global `coyote` tool by running the following command:

```plain
dotnet tool update --global Microsoft.Coyote.CLI
```

You can remove the global `coyote` tool by running the following command:

```plain
dotnet tool uninstall --global Microsoft.Coyote.CLI
```

**Note:** this command line tool is only for .NET Core. If you need a version of `coyote.exe` that
runs on .NET Framework, this is installed from the `Microsoft.Coyote.Test` package, and you can run
it from the bin folder of your Coyote application.

### Using the Coyote tool

To learn how to use the Coyote tool read [here](using-coyote.md).

### Troubleshooting

#### The element 'metadata' in namespace nuspec.xsd has invalid child element 'repository'...

If you get an error building the nuget package, you may need to download
a new version of `nuget.exe` from [https://www.nuget.org/downloads](https://www.nuget.org/downloads).
