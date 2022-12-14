cd %~dp0
dotnet ..\..\bin\net7.0\coyote.dll test ../bin/net7.0/Raft.Nondeterminism.dll -i 1000 -ms 500 -graph-bug
