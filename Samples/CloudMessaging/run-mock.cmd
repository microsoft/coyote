cd %~dp0
dotnet ..\..\bin\net6.0\coyote.dll test /../bin/net6.0/Raft.Mocking.dll -i 1000 -ms 500 -graph-bug
