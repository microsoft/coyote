---
layout: reference
title: Distributed Testing
section: learn
permalink: /learn/tools/distributed-testing
---

## Distributed Test

You can also use the `--parallel` option to run the tests on different machines to get more scale and
more parallelism. Add the `--wait-for-testing-processes` option to tell the test coordinator to wait
for the test processes to launch rather than launching them locally. You will also want to use
`--testing-scheduler-ipaddress` to specify a different ip endpoint other than the default `127.0.0.1:0`
so that the remote machines can find your test coordinator. You may need to open the port that you
specify using your firewall settings. The following is an example which starts the server with the
expectation that there will be 5 remote parallel tests:

```
.\bin\net46\Coyote.exe test D:\git\Coyote\Samples\bin\net46\Raft.exe -i 100 --max-steps 100 --parallel 5 --wait-for-testing-processes --testing-scheduler-ipaddress 10.159.2.43:5050 -v
```

This outputs a message containing the command line needed to launch each remote test. At this point
the server waits for these 5 tests to call back.

Copy the generated command line to one or more remote machines and change the last argument
`--testing-process-id 0` so that the id varies from 0 to 5 (matching the `--parallel 5` argument).
The easiest way to do this is to save the command line in a file named `runtest.cmd` replacing the
id with a command line parameter (%1):
```
d:\git\Coyote\bin\net46\Coyote.dll test  d:\git\Coyote\Samples\bin\net46\Raft.exe -i 100 --max-steps 100 -sch-random -sch-seed:288 --timeout-delay 1 --run-as-parallel-testing-task --testing-scheduler-endpoint CoyoteTestScheduler.4723bb92-c413-4ecb-8e8a-22eb2ba22234 --testing-scheduler-ipaddress 10.159.2.43:5050 --testing-process-id %1
```

You can add the following if you want to see the individual test outputs:
```
pause
exit
```
And remove the `pause` when you want the TestProcesses to clean themselves up automatically. Then
create another `run.cmd` containing this:
```
start runtest 0
start runtest 1
start runtest 2
start runtest 3
start runtest 4
```

Now type `run.cmd` to launch the 5 test instances. You will see them connect with the server and
when a test finds a bug it will send back it's trace files so you should see this output on the
server:

```
... Task 4 found a bug.
... Saved trace report: D:\git\Coyote\Samples\bin\net46\Output\Raft.dll\CoyoteOutput\Raft_4_1.txt
... Saved trace report: D:\git\Coyote\Samples\bin\net46\Output\Raft.dll\CoyotOutput\Raft_4_1.pstrace
... Saved trace report: D:\git\Coyote\Samples\bin\net46\Output\Raft.dll\CoyoteOutput\Raft_4_1.schedule
```