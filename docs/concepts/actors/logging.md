## Logging

The Coyote runtime provides a `Microsoft.Coyote.Logging.ILogger` interface for logging so that your
program output can be captured and included in `coyote` test tool output logs. The installed
`ILogger` can be accessed by the `Logger` property on the `IActorRuntime` interface or the `Actor`,
`StateMachine` and `Monitor` types.

The default implementation of the `ILogger` writes to the console when setting the `--console`
option in the `coyote` tool (or the `Configuration.WithConsoleLoggingEnabled()` configuration when
using the `TestingEngine` API).

The Coyote logging infrastructure decides when to log messages using the specified `VerbosityLevel`
and the individual `LogSeverity` of messages getting logged with the `ILogger.Write` and
`ILogger.WriteLine` methods (by default the `LogSeverity` of messages is set to `LogSeverity.Info`).
As long as the `LogSeverity` is equal or higher than the `VerbosityLevel` then the message will be
logged. By default, the `VerbosityLevel` is set to `None`, which means that no messages are logged,
but this can be easily customized using the `--verbosity` (or `-v`) option in the `coyote` tool (or
the `Configuration.WithVerbosityEnabled()` configuration when using the `TestingEngine` API)

Setting `--verbosity` in the command line will set the `VerbosityLevel` to `VerbosityLevel.Info`
which logs all messages with `LogSeverity.Info` and higher. Other allowed verbosity values are
`error`, `warning`, `info`, `debug` and `exhaustive`. For example, choosing `--verbosity debug` will
log all messages with `LogSeverity.Debug` and higher.

## Installing up a custom logger

You can easily install your own logger by implementing the `ILogger` interface and replacing the
default logger by setting the `Logger` property on the `IActorRuntime`.

The following is an example of a custom `ILogger` implementation that captures all log output in a
`StringBuilder`:

```csharp
using System.Text;
using Microsoft.Coyote.Logging;

class CustomLogger : ILogger
{
    private readonly StringBuilder Builder;
    private readonly VerbosityLevel VerbosityLevel;
    private readonly object Lock;

    public MemoryLogger(VerbosityLevel level)
    {
        this.Builder = new StringBuilder();
        this.VerbosityLevel = level;
        this.Lock = new object();
    }

    public void Write(string value) => this.Write(LogSeverity.Info, value);
    public void Write(string format, object arg0) =>
        this.Write(LogSeverity.Info, format, arg0);
    public void Write(string format, object arg0, object arg1) =>
        this.Write(LogSeverity.Info, format, arg0, arg1);
    public void Write(string format, object arg0, object arg1, object arg2) =>
        this.Write(LogSeverity.Info, format, arg0, arg1, arg2);
    public void Write(string format, params object[] args) =>
        this.Write(LogSeverity.Info, string.Format(format, args));

    public void Write(LogSeverity severity, string value)
    {
        if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
        {
            lock (this.Lock)
            {
                this.Builder.Append(value);
            }
        }
    }

    public void Write(LogSeverity severity, string format, object arg0)
    {
        if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
        {
            lock (this.Lock)
            {
                this.Builder.AppendFormat(format, arg0);
            }
        }
    }

    public void Write(LogSeverity severity, string format, object arg0, object arg1)
    {
        if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
        {
            lock (this.Lock)
            {
                this.Builder.AppendFormat(format, arg0, arg1);
            }
        }
    }

    public void Write(LogSeverity severity, string format, object arg0, object arg1, object arg2)
    {
        if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
        {
            lock (this.Lock)
            {
                this.Builder.AppendFormat(format, arg0, arg1, arg2);
            }
        }
    }

    public void Write(LogSeverity severity, string format, params object[] args)
    {
        if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
        {
            lock (this.Lock)
            {
                this.Builder.AppendFormat(format, args);
            }
        }
    }

    public void WriteLine(string value) => this.WriteLine(LogSeverity.Info, value);
    public void WriteLine(string format, object arg0) =>
        this.WriteLine(LogSeverity.Info, format, arg0);
    public void WriteLine(string format, object arg0, object arg1) =>
        this.WriteLine(LogSeverity.Info, format, arg0, arg1);
    public void WriteLine(string format, object arg0, object arg1, object arg2) =>
        this.WriteLine(LogSeverity.Info, format, arg0, arg1, arg2);
    public void WriteLine(string format, params object[] args) =>
        this.WriteLine(LogSeverity.Info, string.Format(format, args));

    public void WriteLine(LogSeverity severity, string value)
    {
        if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
        {
            lock (this.Lock)
            {
                this.Builder.AppendLine(value);
            }
        }
    }

    public void WriteLine(LogSeverity severity, string format, object arg0)
    {
        if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
        {
            lock (this.Lock)
            {
                this.Builder.AppendFormat(format, arg0);
                this.Builder.AppendLine();
            }
        }
    }

    public void WriteLine(LogSeverity severity, string format, object arg0, object arg1)
    {
        if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
        {
            lock (this.Lock)
            {
                this.Builder.AppendFormat(format, arg0, arg1);
                this.Builder.AppendLine();
            }
        }
    }

    public void WriteLine(LogSeverity severity, string format, object arg0, object arg1, object arg2)
    {
        if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
        {
            lock (this.Lock)
            {
                this.Builder.AppendFormat(format, arg0, arg1, arg2);
                this.Builder.AppendLine();
            }
        }
    }

    public void WriteLine(LogSeverity severity, string format, params object[] args)
    {
        if (LogWriter.IsVerbose(severity, this.VerbosityLevel))
        {
            lock (this.Lock)
            {
                this.Builder.AppendFormat(format, args);
                this.Builder.AppendLine();
            }
        }
    }

    public override string ToString()
    {
        lock (this.Lock)
        {
            return this.Builder.ToString();
        }
    }

    public void Dispose()
    {
        lock (this.Lock)
        {
            this.Builder.Clear();
        }
    }
}
```

To replace the default logger, call the following `IActorRuntime` method:

```csharp
runtime.Logger = new CustomLogger();
```

The above method replaces the previously installed logger with the specified one and returns the
previously installed logger.

Note that the old `ILogger` might be disposable, so if you care about disposing the old logger at
the same time you may need to write this instead:

```csharp
using (var oldLogger = runtime.Logger)
{
   runtime.Logger = new CustomLogger();
}
```

You could write a custom `ILogger` to intercept all logging messages and send them to your favorite
logging service in Azure or even over a TCP socket.

You can also use one of the built-in loggers available in the `Microsoft.Coyote.Logging` namespace,
such as the `MemoryLogger` which is a thread-safe logger that writes the log in memory (you can
access it as a `string` by invoking `MemoryLogger.ToString()`) or the `TextWriterLogger` which
allows you to run an existing `System.IO.TextWriter` into an `ILogger` implementation.

## Adding custom actor logging consumers

The `IActorRuntime` also provides a logging interface called `IActorRuntimeLog` that allows
consuming `Actor` and `StateMachine` activity and processing it in some custom way. When executing
actors, the runtime will call the `IActorRuntimeLog` interface to log various actions such as a new
`Actor` or `StateMachine` getting created or sending an `Event` to some actor.

You can implement your own `IActorRuntimeLog` consumer like this:

```csharp
internal class CustomRuntimeLog : IActorRuntimeLog
{
  public void OnCreateActor(ActorId id, ActorId creator)
  {
    // Add some custom logic.
  }

  public void OnEnqueueEvent(ActorId id, Event e)
  {
    // Add some custom logic.
  }

  // You can optionally override more actor logging methods.
}
```

You can then register the `CustomRuntimeLog` using the following `IActorRuntime` method:
```csharp
runtime.RegisterLog(new CustomRuntimeLog());
```

You can register multiple `IActorRuntimeLog` objects in case you have consumers that are doing very
different things. The runtime will invoke each callback for every registered `IActorRuntimeLog`.

For example, see the `ActorRuntimeLogGraphBuilder` class which implements `IActorRuntimeLog` and
generates a directed graph representing all activities that happened during the execution of your
actors. See [activity coverage](../../how-to/coverage.md) for an example graph output. The `coyote`
test tool sets this up for you when you specify `--graph` or `--coverage activity` command line
options.

See [IActorRuntimeLog API documentation](../../ref/Microsoft.Coyote.Actors/IActorRuntimeLog.md).

## Customizing the text formatting when logging actor activities

You can also use the same `IActorRuntimeLog` feature to customize the text formatting when the
installed `ILogger` logs actor activity.

The default actor text formatting implementation is provided by the `ActorRuntimeLogTextFormatter`
base class which implements the `IActorRuntimeLog` interface and is responsible for formatting all
`Actor` and `StateMachine` activity as text and logging it using the installed `Logger`.

You can add your own subclass of `ActorRuntimeLogTextFormatter` using the `RegisterLog` method on
`IActorRuntime`. However, unlike other `IActorRuntimeLog` consumers, only a single
`ActorRuntimeLogTextFormatter` can exist and adding a new one will replace the previous text
formatter.

The following is an example of how to do this:

```csharp
internal class CustomActorRuntimeLogTextFormatter : ActorRuntimeLogTextFormatter
{
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

  // You can optionally override more text formatting methods.
}
```

You can then replace the default `ActorRuntimeLogTextFormatter` with your new implementation using
the following `IActorRuntime` method:

```csharp
runtime.RegisterLog(new CustomActorRuntimeLogTextFormatter());
```

The above method replaces the previously installed `ActorRuntimeLogTextFormatter` with the specified
one.

See [ActorRuntimeLogTextFormatter](../../ref/Microsoft.Coyote.Actors/ActorRuntimeLogTextFormatter.md)
documentation.
