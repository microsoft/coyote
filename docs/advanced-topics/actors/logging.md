## Logging

The Coyote runtime provides a `Microsoft.Coyote.IO.ILogger` interface for logging so that your program output can be
captured and included in `coyote` test tool output logs.  A default `Logger` is provided and can be
accessed like this:

| Programming Model        | Accessing the Logger                                                                                                    |
| ------------------------ | ----------------------------------------------------------------------------------------------------------------------- |
| `Task` based program     | `Microsoft.Coyote.Actors.RuntimeFactory.Create().Logger`                                                                |
| `Actor` based program    | `Microsoft.Coyote.Runtime.RuntimeFactory.Create().Logger` <br/> `Actor.Logger`, `StateMachine.Logger`, `Monitor.Logger` |

The default `Logger` is a `ConsoleLogger` which is used to write output to the `System.Console`.
You can provide your own implementation of `Microsoft.Coyote.IO.ILogger` by setting the `Logger` property on the
`ICoyoteRuntime` or `IActorRuntime`.

The `IActorRuntime` also provides a higher level logging interface called [`IActorRuntimeLog`](#iactorruntimelog) for
logging `Actor` and `StateMachine` activity.

## Example of custom ILogger

It is possible to replace the default logger with a custom one. The following example captures all log output in a `StringBuilder`:

```c#
public class CustomLogger : ILogger
{
    private StringBuilder StringBuilder;

    public TextWriter TextWriter => throw new NotImplementedException();

    public CustomLogger()
    {
        this.StringBuilder = new StringBuilder();
    }

    public void Write(string value)
    {
        this.Write(LogSeverity.Informational, value);
    }

    public void WriteLine(string value)
    {
        this.WriteLine(LogSeverity.Informational, value);
    }

    public void Write(string format, params object[] args)
    {
        this.Write(LogSeverity.Informational, format, args);
    }

    public void WriteLine(string format, params object[] args)
    {
        this.WriteLine(LogSeverity.Informational, format, args);
    }

    public void Write(LogSeverity severity, string format, object[] args)
    {
        this.Write(severity, string.Format(format, args));
    }

    public void WriteLine(LogSeverity severity, string format, object[] args)
    {
        this.WriteLine(severity, string.Format(format, args));
    }

    public void Write(LogSeverity severity, string value)
    {
        switch (severity)
        {
            case LogSeverity.Informational:
                this.StringBuilder.Append("<info>" + value);
                break;
            case LogSeverity.Warning:
                this.StringBuilder.Append("<warning>" + value);
                break;
            case LogSeverity.Error:
                this.StringBuilder.Append("<error>" + value);
                break;
            case LogSeverity.Important:
                this.StringBuilder.Append("<important>" + value);
                break;
        }
    }

    public void WriteLine(LogSeverity severity, string value)
    {
        this.Write(severity, value);
        this.StringBuilder.AppendLine();
    }

    public override string ToString()
    {
        return this.StringBuilder.ToString();
    }

    public void Dispose()
    {
        // todo
    }
}
```

To replace the default logger, call the following `IActorRuntime` method:

```c#
runtime.Logger = new CustomLogger();
```

The above method replaces the previously installed logger with the specified one and returns the
previously installed logger.

Note that the old `Logger` might be disposable, so if you care about disposing the old logger at
the same time you may need to write this instead:

```c#
using (var oldLogger = runtime.Logger)
{
   runtime.Logger = new CustomLogger();
}
```

You could write a custom `ILogger` to intercept all logging messages and send them to an Azure Log
table, or over a TCP socket.

## IActorRuntimeLog

The default `IActorRuntimeLog` implementation is the `ActorRuntimeLogTextFormatter` base class which
is responsible for formatting all `Actor` and `StateMachine` activity as text and writing that out
using the installed `Logger`.

You can add your own implementation of `IActorRuntimeLog` or `ActorRuntimeLogTextFormatter` using
the `RegisterLog` method on `IActorRuntime`.  This is additive so you can have the default
`ActorRuntimeLogTextFormatter` and another logger running at the same time.  For example, see the
`ActorRuntimeLogGraphBuilder` class which implements `IActorRuntimeLog` and generates a directed
graph representing all activities that happened during the execution of your actors. See [activity
coverage](../../tools/coverage.md) for an example graph output. The `coyote` test tool sets this up for
you when you specify `--graph` or `--coverage activity` command line options.

The `--verbosity` command line option can also affect the default logging behavior. When
`--verbosity` is specified all log output is written to the `System.Console` by default, but you
can specify different levels of output using `--verbosity quiet` to get no output, `--verbosity
minimal` to see only error messages and `--verbosity normal` to get errors and warnings. This can
produce a lot of output especially if you run many testing iterations. It is usually more useful to
only capture the output of the one failing iteration in a log file and this is done automatically
by the testing runtime when `--verbosity` is not specified.

See [IActorRuntimeLog API documentation](../../ref/Microsoft.Coyote.Actors/IActorRuntimeLog.md).

## Example of a custom IActorRuntimeLog

You can also implement your own `IActorRuntimeLog`. The following is an example of how to do this:

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
different things. The runtime will invoke each callback for every registered `IActorRuntimeLog`.

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

You can then replace the default `ActorRuntimeLogTextFormatter` with your new implementation using
the following `IActorRuntime` method:

```c#
runtime.RegisterLog(new CustomLogFormatter());
```

The above method replaces the previously installed `ActorRuntimeLogTextFormatter` with the specified
one.

See [ActorRuntimeLogTextFormatter](../../ref/Microsoft.Coyote.Actors/ActorRuntimeLogTextFormatter.md)
documentation.
