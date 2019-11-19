---
layout: reference
title: Logging
section: learn
permalink: /learn/advanced/logging
---

## Logging

The Coyote runtime provides two levels of logging:
- The `IActorRuntimeLog` interface logs high level actor activity.
- The `ILogger` is a low level logging interface responsible for formatting text output.

The default `ILogger` is the`ConsoleLogger` which is used to write
output to the `System.Console`.

The runtime also provides the `IActorRuntimeLogFormatter` interface which is responsible
for formatting all runtime log events as text and writing them out using the installed `ILogger`.
The default `IActorRuntimeLogFormatter` is implemented by `ActorRuntimeLogFormatter`.

You can provide your own implementation of `IActorRuntimeLog`, `IActorRuntimeLogFormatter`
and/or `ILogger` in order to gain full control over what is logged and how.
For an interesting example of this see the `ActorRuntimeLogGraphBuilder` class
which implements `IActorRuntimeLog` and generates a directed graph representing
all activities that happened during the execution of your actors.
See [activity coverage](../tools/coverage.md) for an example graph output.
The `coyote` tester uses this when you specify `--graph` or `--coverage activity`
command line options.

The `--verbose` command line option can also affect the default logging behavior.
When `--verbose` is specified all log output is written to the `System.Console`.
This can result in a lot of output especially if the test is performing many iterations.
It is usually more useful to only capture the output of the one failing iteration in a given
test run and this is done by the testing runtime automatically when `--verbose` is not set.
In the latter case, all logging is redirected into an `InMemoryLogger` and only when a bug
is found is that in-memory log written to a log file on disk.

## Registering a custom IActorRuntimeLog

You can implement your own `IActorRuntimeLog` (as done by `ActorRuntimeLogGraphBuilder`).
The following is an example of how to do this:

```c#
internal class CustomLogWriter : IActorRuntimeLog
{
  /* Callbacks on runtime events */

  public void OnCreateActor(ActorId id, ActorId creator)
  {
    // Override to change the behavior.
  }

  public void OnEnqueueEvent(ActorId id, string eventName)
  {
    // Override to change the behavior.
  }

  // More methods to implement.
}
```

You can then register your new implementation using the following `IActorRuntime` method:
```c#
runtime.RegisterLog(new CustomLogWriter());
```
You can register multiple `IActorRuntimeLog` objects in case you have loggers that are doing very
different things. The runtime will invoke the callback for each registered `IActorRuntimeLog`.

## Customizing the IActorRuntimeLogFormatter

You can modify the format of log messages by providing your own `IActorRuntimeLogFormatter`.
You can either create a new type that implements this interface, or subclass the
`ActorRuntimeLogFormatter` implementation to override its default behavior.
The following is an example of how to do this:

```c#
internal class CustomLogFormatter : ActorRuntimeLogFormatter
{
  /* Methods for formatting log messages */

  public override bool GetCreateActorLog(ActorId id, ActorId creator, out string text)
  {
    // Override to change the text to be logged.
    // Set the 'text' parameter to provide the text to be logged.
    // Return true to log the text, or false to ignore it.
  }

  public override bool GetEnqueueEventLog(ActorId id, string eventName, out string text)
  {
    // Override to change the text to be logged.
    // Set the 'text' parameter to provide the text to be logged.
    // Return true to log the text, or false to ignore it.
  }

  // More methods that can be overridden.
}
```

You can then replace the default `IActorRuntimeLogFormatter` with your new implementation using the following `IActorRuntime` method:
```c#
IActorRuntimeLogFormatter old = runtime.SetLogFormatter(new CustomLogFormatter());
```
The above method replaces the previously installed log formatter, installs the specified one, and returns the previously installed one.

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

The current ILogger can be accessed via the following property which you can find on `IActorRuntime`, `Actor`, `StateMachine`, `Monitor` and `IActorRuntimeLog`:
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

To replace the default logger, call the following `IActorRuntime` method:
```c#
using (ILogger old = runtime.SetLogger(new CustomLogger()))
{
}
```
The above method replaces the previously installed logger with the specified one and returns the previously installed logger.

Note that `SetLogger` is _not_ calling `Dispose` on the previously installed logger. This allows the logger to be accessed and used after being removed from the Coyote runtime, so it is your responsibility to call Dispose, as shown in the example above.
