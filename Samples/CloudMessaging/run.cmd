dotnet %~dp0\..\bin\net7.0\Raft.Azure.dll --connection-string %CONNECTION_STRING% --topic-name rafttopic --num-requests 5 --local-cluster-size 5 
