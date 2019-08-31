FailureDetector
===============
This is an abstract implementation of a [failure detector](https://en.wikipedia.org/wiki/Failure_detector) in Coyote.

The aim of this sample is to showcase the testing capabilities of Coyote, and features such as nondeterministic timers and monitors (used to specify global safety and liveness properties).

## How to test

The sample contains a hard to find nondeterministic bug (injected on purpose). The Coyote tester can detect it after thousands of testing iterations.

To test for the bug execute the following command:
```
CoyoteTester.exe /test:FailureDetector.exe /i:500000 /max-steps:200
```
The deterministically explores an execution path that triggers the bug use the option `/sch-seed:121`. Using this seed the bug will get triggered in the 2420th testing iteration.
