## PetImagesAspNet

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

More details on this sample are coming soon.
