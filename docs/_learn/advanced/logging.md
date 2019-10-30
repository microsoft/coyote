---
layout: reference
title: Logging
section: learn
permalink: /learn/advanced/logging
---

## Logging

The Coyote runtime uses the `RuntimeLogWriter` to log all runtime messages. The `RuntimeLogWriter` uses
the `ILogger` interface for logging. By default, log output is written to the `Console` (when verbosity
is enabled). This behavior can be overriden by installing a custom `ILogger`. Further,
`RuntimeLogWriter` can be subclassed to change the behavior of how the runtime logs messages. During
testing, and if verbosity is disabled, the log output is automatically redirected to an in-memory
`ILogger` (which dumps it to a readable trace file when a bug is found).

## Using and replacing the logger

The `ILogger` interface is responsible for writing log messages using the `Write` and `WriteLine`
methods. The `IsVerbose` property is `true` when verbosity is enabled, and `false` when disabled.

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

The runtime logger can be accessed via the following `IMachineRuntime`, `StateMachine` or `Monitor` property:

```c#
ILogger Logger { get; }
```

It is possible to replace the default logger with a custom one that implements the `ILogger` and
`IDisposable` interfaces. For example someone could write the following `CustomLogger` that uses a
`StringBuilder` for writing the log:

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

To replace the default logger, call the following `IMachineRuntime` method:

```c#
ILogger SetLogger(ILogger logger);
```
The above method replaces the previously installed logger on the `RuntimeLogWriter`, installs the
specified one and returns the previously installed logger.

Note that `SetLogger` is _not_ calling `Dispose` on the previously installed logger. This allows the
logger to be accessed and used after being removed from the Coyote runtime.

## Customizing the runtime log writer

Although typically you would not want to modify the default implementation of the `RuntimeLogWriter`,
you have the flexibility to subclass it and (partially) override the default methods to define a custom
implementation that suits your needs.

To do this, first subclass `RuntimeLogWriter` and override the methods that you are interested in:

```c#
internal class CustomLogWriter : RuntimeLogWriter
{
  /* Callbacks on runtime events */

  public override void OnEnqueue(MachineId machineId, string eventName)
  {
    // Override to change the behavior. Base method logs an OnEnqueue runtime event.
    // using the base FormatOnEnqueueLogMessage formatting method.
  }

  public override void OnSend(MachineId targetMachineId, MachineId senderId, string senderStateName, string eventName,
      Guid opGroupId, bool isTargetHalted)
  {
    // Override to change the behavior. Base method logs an OnSend runtime event.
    // using the base FormatOnSendLogMessage formatting method.
  }

  // More methods that can be overriden.

  /* Methods for formatting log messages */

  protected override string FormatOnEnqueueLogMessage(MachineId machineId, string eventName)
  {
    // Override to change the text to be logged.
  }

  protected override string FormatOnSendLogMessage(MachineId targetMachineId, MachineId senderId, string senderStateName,
    string eventName, Guid opGroupId, bool isTargetHalted)
  {
    // Override to change the text to be logged.
  }

  // More methods that can be overriden.
}
```

Finally, set the new implementation using the following `IMachineRuntime` method:

```c#
RuntimeLogWriter SetLogWriter(RuntimeLogWriter logWriter);
```

The above method replaces the previously installed log writer, installs the specified one, and returns
the previously installed one. The runtime is going to set the previously installed `ILogger` on the new
`RuntimeLogWriter`, so you do not need to reset them.
