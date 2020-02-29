---
layout: reference
title: Logging
section: learn
permalink: /learn/advanced/logging
---

## Logging

The Coyote runtime provides two levels of logging:
- The `IActorRuntimeLog` interface logs high level `Actor` and `StateMachine` activity.
- The `System.IO.TextWriter` interface is used for low level logging formatted as text.

The default `Logger` is the`ConsoleLogger` which is used to write
output to the `System.Console`.

The runtime also provides the `ActorRuntimeLogTextFormatter` base class which is responsible
for formatting all runtime log events as text and writing them out using the installed `TextWriter`.

You can provide your own implementation of `IActorRuntimeLog`, `ActorRuntimeLogTextFormatter`
and/or `System.IO.TextWriter` in order to gain full control over what is logged and how.
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
  // Callbacks on runtime events

  public void OnCreateActor(ActorId id, ActorId creator)
  {
    // Override to change the behavior.
  }

  public void OnEnqueueEvent(ActorId id, Event e)
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

## Customizing the ActorRuntimeLogTextFormatter

You can modify the format of text log messages by providing your own `ActorRuntimeLogTextFormatter`.
You can subclass the default `ActorRuntimeLogTextFormatter` implementation to override its default behavior.
The following is an example of how to do this:

```c#
internal class CustomLogFormatter : ActorRuntimeLogTextFormatter
{
  // Methods for formatting log messages

  public override void OnCreateActor(ActorId id, ActorId creator)
  {
    // Override to change the text to be logged.
    this.Logger.WriteLine("Hello!");
  }

  public override void OnEnqueueEvent(ActorId id, Event e)
  {
    // Override to conditionally hide certain events from the log.
    if (!(e is SecretEvent))
    {
      base.OnEnqueueEvent(id, e);
    }
  }

  // More methods that can be overridden.
}
```

You can then replace the default `ActorRuntimeLogTextFormatter` with your new implementation using the following `IActorRuntime` method:
```c#
runtime.RegisterLog(new CustomLogFormatter());
```

The above method replaces the previously installed `ActorRuntimeLogTextFormatter` with the specified one.

## Using and replacing the TextWriter

The `System.IO.TextWriter` is responsible for writing text messages using the `Write` and `WriteLine` methods.
The current `TextWriter` can be accessed via the `Logger` property on the following `IActorRuntime`, `Actor`, `StateMachine`, `Monitor` and `IActorRuntimeLog`:
```c#
TextWriter Logger { get; }
```

It is possible to replace the default logger with a custom one.  The following example captures all log output in a `StringBuilder`:

```c#
    public class CustomLogger : TextWriter
    {
        private StringBuilder StringBuilder;

        public CustomLogger()
        {
            this.StringBuilder = new StringBuilder();
        }

        public override Encoding Encoding => Encoding.Unicode;

        public override void Write(string value)
        {
            this.StringBuilder.Append(value);
        }

        public override void WriteLine(string value)
        {
            this.StringBuilder.AppendLine(value);
        }

        public override string ToString()
        {
            return this.StringBuilder.ToString();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.StringBuilder.Clear();
                this.StringBuilder = null;
            }

            base.Dispose(disposing);
        }
    }
```

To replace the default logger, call the following `IActorRuntime` method:

```c#
using (var oldLogger = runtime.SetLogger(new CustomLogger()))
{
}
```

The above method replaces the previously installed logger with the specified one and returns the previously installed logger.

Note that `SetLogger` does not `Dispose` the previously installed logger. This allows the logger to be accessed and
used after being removed from the Coyote runtime, so it is your responsibility to call Dispose, which can be done with a
`using` block.

You could write a custom `Logger` to intercept all logging messages and send them to an Azure Log table, or over a TCP socket.

