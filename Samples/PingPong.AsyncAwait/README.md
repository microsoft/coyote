PingPong
========
This is a simple implementation of a ping-pong application in C# uins the Coyote library.

A network environment machine (which is basically a test harness) creates a server and a client machine. The server and client machine then start exchanging ping and pong events for a number of turns.

The aim of this sample is to show how to write basic Coyote programs. We provide 4 different versions of the same program:
- A version written using C# and the Coyote library.
- A version that is mixed-mode (uses both high-level syntax and the C# library, this is based on partial classes).
- A version that shows how to install a custom logger for testing.

## How to test

To test the produced executable use the following command:
```
CoyoteTester.exe /test:PingPong.exe /i:100
```
With the above command, the Coyote tester will systematically test the program for 100 testing iterations.

Note that this program is pretty simple: there are no bugs to be found, and the execution is pretty much deterministic. Please check our other samples for more advanced examples.