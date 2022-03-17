## Monitors

This is an abstract implementation of a [failure detector](https://en.wikipedia.org/wiki/Failure_detector) in Coyote.
See the [tutorial](https://microsoft.github.io/coyote/samples/actors/failure-detector).

The aim of this sample is to showcase the testing capabilities of Coyote, and features such as nondeterministic timers
and monitors (used to specify global safety and liveness properties).

This program implements a failure detection protocol. A failure detector state machine is given a list of machines,
each of which represents a daemon running at a computing node in a distributed system. The failure detector sends each
machine in the list a 'Ping' event and determines whether the machine has failed if it does not respond with a 'Pong'
event within a certain time period.

## How to test

The sample contains a hard to find nondeterministic bug (injected on purpose).
The `coyote` testing tool can detect it after thousands of testing iterations.

To test for the bug execute the following command:
```
coyote test Monitors.dll -i 500000 --max-steps 200
```
To find this bug more quickly, add the following command line option `--sch-pct 10` to use the priority-based probabilistic
scheduling strategy which can probabilistically control how many context switches will be explored during each test.
