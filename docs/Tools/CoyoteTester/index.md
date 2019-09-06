---
layout: reference
title: CoyoteTester
section: learn
permalink: /Tools/CoyoteTester/
---

## CoyoteTester

CoyoteTester is used to find bugs in your async state machines.

**Usage**:** type "CoyoteTester -?" to get help.

## Example Inline Test

```
.\bin\net46\CoyoteTester.exe -test:.\Samples\bin\net46\Raft.exe -i:100 -max-steps:100
```

This will thoroughly test the "Raft.exe" state machine until it reaches max-steps or finds a bug.
If it finds a bug you will see this output:

```
... Task 0 found a bug.
... Emitting task 0 traces:
..... Writing .\Samples\bin\net46\Output\Raft.exe\CoyoteTesterOutput\Raft_0_2.txt
..... Writing .\Samples\bin\net46\Output\Raft.exe\CoyoteTesterOutput\Raft_0_2.pstrace
..... Writing .\Samples\bin\net46\Output\Raft.exe\CoyoteTesterOutput\Raft_0_2.schedule
```

And those trace files record the path leading up to the bug.

This test runs in the CoyoteTester.exe process.

## Example Parallel Test

You can also run multiple instances of Raft.exe in separate CoyoteTester processes using parallel testing.
The first CoyoteTester turns into a distributed test coordinator.  The following is an example:

```
.\bin\net46\CoyoteTester.exe -test:.\Samples\bin\net46\Raft.exe -i:100 -max-steps:100 -parallel:5
```

This runs 5 parallel instances, each instance will explore the program using different random seeds so
you see initial output like this when those tasks are launched:

```
... Task 0 is using 'Random' strategy (seed:802).
... Task 4 is using 'Random' strategy (seed:3494).
... Task 1 is using 'Random' strategy (seed:1475).
... Task 3 is using 'Random' strategy (seed:2821).
... Task 2 is using 'Random' strategy (seed:2148).
```

Then if one of the tasks finds a bug, that bug will be captured and all other tasks will be stopped.
You can add `-explore` which will allow each parallel test to keep running until they all finds bugs
or reach `-max-steps`.


## Example Distributed Test

You can also use the `-parallel` option to run the tests on different machines to get more scale and
more parallelism.  Add the `-wait-for-testing-processes` option to tell the test coordinator to wait
for the test processes to launch rather than launching them locally. You will also want to use
`-testing-scheduler-ipaddress` to specify a different ip endpoint other than the default `127.0.0.1:0`
so that the remote machines can find your test coordinator.  You may need to open the port that you
specify using your firewall settings.  The following is an example which starts the server with the
expectation that there will be 5 remote parallel tests:

```
.\bin\net46\CoyoteTester.exe -test:D:\git\Coyote\Samples\bin\net46\Raft.exe -i:100 -max-steps:100 -parallel:5 -wait-for-testing-processes -testing-scheduler-ipaddress:10.159.2.43:5050 -v
```

This outputs a message containing the command line needed to launch each remote test.  At this point
the server waits for these 5 tests to call back.

Copy the generated command line to one or more remote machines and change the last argument
`/testing-process-id:0` so that the id varies from 0 to 5 (matching the -parallel:[x] argument).
The easiest way to do this is to save the command line in a file named `runtest.cmd` replacing the
id with a command line parameter (%1):
```
dotnet d:\git\Coyote\bin\netcoreapp2.1\CoyoteTester.dll /test:d:\git\Coyote\Samples\bin\netcoreapp2.1\Raft.dll /i:100 /timeout:0 /max-steps:100 /sch:Random /sch-seed:288 /timeout-delay:1 /run-as-parallel-testing-task /testing-scheduler-endpoint:CoyoteTestScheduler.4723bb92-c413-4ecb-8e8a-22eb2ba22234 /testing-scheduler-ipaddress:10.159.2.43:5050 /testing-process-id:%1
```

You can add the following if you want to see the individual test outputs:
```
pause
exit
```
And remove the `pause` when you want the TestProcesses to clean themselves up automatically.  Then
create another `run.cmd` containing this:
```
start runtest 0
start runtest 1
start runtest 2
start runtest 3
start runtest 4
```

Now type `run.cmd` to launch the 5 test instances.  You will see them connect with the server and
when a test finds a bug it will send back it's trace files so you should see this output on the
server:

```
... Task 4 found a bug.
... Saved trace report: D:\git\Coyote\Samples\bin\netcoreapp2.1\Output\Raft.dll\CoyoteTesterOutput\Raft_4_1.txt
... Saved trace report: D:\git\Coyote\Samples\bin\netcoreapp2.1\Output\Raft.dll\CoyoteTesterOutput\Raft_4_1.pstrace
... Saved trace report: D:\git\Coyote\Samples\bin\netcoreapp2.1\Output\Raft.dll\CoyoteTesterOutput\Raft_4_1.schedule
```
