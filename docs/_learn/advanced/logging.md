---
layout: reference
title: Logging
section: learn
permalink: /learn/advanced/logging
---

## Logging

The Coyote runtime provides two levels of logging:
- `IMachineRuntimeLog` interface logs high level state machine activity.
- `ILogger` is a low level logging interface responsible for formatting text output.

The default implementation of `IMachineRuntimeLog` is `RuntimeLogWriter`
which formats all events as text writing them out using the lower level `ILogger`
interface.  The default `ILogger` is the`ConsoleLogger` which is used to write
output to the `System.Console`.

You can provide your own implementation of `IMachineRuntimeLog` and/or
`ILogger` in order to gain full control over what is logged and how.
For an interesting example of this see the `GraphMachineRuntimeLog` class
which implements `IMachineRuntimeLog` and generates a directed graph representing
all state transitions that happened during the execution of your state machines.
See [activity coverage](/Coyote/learn/tools/coverage) for example graph output.
The `coyote` tester uses this when you specify `--graph` or `--coverage activity`
command line options.

The `--verbose` command line option can also affect the default logging behavior.
When `--verbose` is specified all log output is written to the `System.Console`.
This can result in a lot of output especially if the test is performing many iterations.
It is usually more useful to only capture the output of the one failing iteration in a given
test run and this is done by the testing runtime automatically when `--versbose` is not set.
In the latter case, all logging is redirected into an `InMemoryLogger` and only when a bug
is found is that in-memory log written to a log file on disk.

## Customizing the IMachineRuntimeLog

You can implement your own `IMachineRuntimeLog` (as is done by `GraphMachineRuntimeLog`)
or you can subclass `RuntimeLogWriter`.  The following is an example subclass that overrides
two of the public methods, and two of the protected methods:

```c#
internal class CustomLogWriter : RuntimeLogWriter
{
  /* Callbacks on runtime events */

  public override void OnEnqueue(ActorId actorId, string eventName)
  {
    // Override to change the behavior. Base method logs an OnEnqueue runtime event
    // using the base FormatOnEnqueueLogMessage formatting method.
  }

  public override void OnSend(ActorId targetActorId, ActorId senderId, string senderStateName, string eventName,
      Guid opGroupId, bool isTargetHalted)
  {
    // Override to change the behavior. Base method logs an OnSend runtime event
    // using the base FormatOnSendLogMessage formatting method.
  }

  // More methods that can be overridden.

  /* Methods for formatting log messages */

  protected override string FormatOnEnqueueLogMessage(ActorId actorId, string eventName)
  {
    // Override to change the text to be logged.
  }

  protected override string FormatOnSendLogMessage(ActorId targetActorId, ActorId senderId, string senderStateName,
    string eventName, Guid opGroupId, bool isTargetHalted)
  {
    // Override to change the text to be logged.
  }

  // More methods that can be overridden.
}
```

You can then provide your new implementation using the following `IMachineRuntime` method:
```c#
using (RuntimeLogWriter old = runtime.SetLogWriter(new CustomLogWriter()))
{
}
```
The above method replaces the previously installed log writer, installs the specified one, and returns the previously installed one. The runtime will set the previously installed `ILogger` on your new `IMachineRuntimeLog`, so you do not need to do that.

You can also chain `IMachineRuntimeLog` objects in case you have loggers that are doing very
different things.  This can be done by using the `Next` method on `IMachineRuntimeLog`.  This
creates a linked list of `IMachineRuntimeLog` objects where each `IMachineRuntimeLog` object
in the list will delegate to the next.

Note that `RuntimeLogWriter` is disposable, so be sure to dispose it if you call SetLogWriter
and you do not link the old object into the linked list using `Next` (as shown in the example above).

## Using and replacing the logger

The `ILogger` interface is responsible for writing log messages using the `Write` and `WriteLine` methods. The `IsVerbose` property will be set to `true` when `--verbose` flag is provided to the `coyote` tester.
```c#
public interface ILogger : IDisposable
{
  bool IsVerbose { get; set; }

  void Write(string value);
  void Write(string format, object arg0);
  void Write(string format, object arg0, object arg1);
  void Write(string format, object arg0, object arg1, object arg2);
  void Write(string format, params object[] args);

  void WriteLine(string value);
  void WriteLine(string format, object arg0);
  void WriteLine(string format, object arg0, object arg1);
  void WriteLine(string format, object arg0, object arg1, object arg2);
  void WriteLine(string format, params object[] args);
}
```

The current ILogger can be accessed via the following property which you can find on  `IMachineRuntime`, `StateMachine`, `Monitor` and `IMachineRuntimeLog`:
```c#
ILogger Logger { get; }
```

It is possible to replace the default logger with a custom one that implements the `ILogger` and `IDisposable` interfaces. For example, you could write the following `CustomLogger` that uses a `StringBuilder` for writing the log:

```c#
public class CustomLogger : ILogger
{
  private StringBuilder StringBuilder;

  public bool IsVerbose { get; set; } = false;

  public CustomLogger(bool isVerbose)
  {
    this.StringBuilder = new StringBuilder();
    this.IsVerbose = isVerbose;
  }

  public void Write(string value) => this.StringBuilder.Append(value);

  public void Write(string format, object arg0) =>
    this.StringBuilder.AppendFormat(format, arg0.ToString());

  public void Write(string format, object arg0, object arg1) =>
    this.StringBuilder.AppendFormat(format, arg0.ToString(), arg1.ToString());

  public void Write(string format, object arg0, object arg1, object arg2) =>
    this.StringBuilder.AppendFormat(format, arg0.ToString(), arg1.ToString(), arg2.ToString());

  public void Write(string format, params object[] args) =>
    this.StringBuilder.AppendFormat(format, args);

  public void WriteLine(string value) =>
    this.StringBuilder.AppendLine(value);

  public void WriteLine(string format, object arg0)
  {
    this.StringBuilder.AppendFormat(format, arg0.ToString());
    this.StringBuilder.AppendLine();
  }

  public void WriteLine(string format, object arg0, object arg1)
  {
    this.StringBuilder.AppendFormat(format, arg0.ToString(), arg1.ToString());
    this.StringBuilder.AppendLine();
  }

  public void WriteLine(string format, object arg0, object arg1, object arg2)
  {
    this.StringBuilder.AppendFormat(format, arg0.ToString(), arg1.ToString(), arg2.ToString());
    this.StringBuilder.AppendLine();
  }

  public void WriteLine(string format, params object[] args)
  {
    this.StringBuilder.AppendFormat(format, args);
    this.StringBuilder.AppendLine();
  }

  public override string ToString() => this.StringBuilder.ToString();

  public void Dispose()
  {
    this.StringBuilder = null;
  }
}
```

You could use this log level interface to intercept all logging messages and
send them to an Azure Log table, or over a TCP socket.

To replace the default logger, call the following `IMachineRuntime` method:
```c#
using (ILogger old = runtime.SetLogger(new CustomLogger()))
{
}
```
The above method replaces the previously installed logger with the specified one and returns the previously installed logger.

Note that `SetLogger` is _not_ calling `Dispose` on the previously installed logger. This allows the logger to be accessed and used after being removed from the Coyote runtime, so it is your responsibility to call Dispose, as shown in the example above.
