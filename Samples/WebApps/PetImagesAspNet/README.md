# PetImages ASP.NET Web Application

This directory contains the sample code behind the "Exploring Project Coyote" interactive demo that
is available in the [Microsoft
Innovation](https://innovation.microsoft.com/en-us/exploring-project-coyote) website.

## Get started

To build the sample run:
```
dotnet build
```

The above command will also automatically rewrite the sample using Coyote via a post-build task. See
the `PetImages.Tests.csproj` file for how this this is done.

To run the tests with Coyote do:
```
dotnet test .\PetImages.Tests\PetImages.Tests.csproj --no-build
```
