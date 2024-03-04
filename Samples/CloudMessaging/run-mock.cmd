cd %~dp0
dotnet ..\..\bin\net8.0\coyote.dll test /../bin/net8.0/Raft.Mocking.dll -i 1000 -ms 500 -graph-bug
