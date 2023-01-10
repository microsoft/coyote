dotnet build -c debug /p:BUILD_NET6=yes /p:BUILD_NETCORE31=yes /p:BUILD_ALL_SUPPORTED_NET_VERSIONS=yes
cp -r ./bin/netcoreapp3.1/* ../olp-core/binaries/
