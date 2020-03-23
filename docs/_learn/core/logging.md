---
layout: reference
title: Logging
section: learn
permalink: /learn/core/logging
---

## Logging

The Coyote runtime provides a `System.IO.TextWriter` for logging so that your program output can be
captured and included in `coyote` test tool output logs.  A default `Logger` is provided and can be
accessed like this:

| Programming Model        | Accessing the Logger     |
| :-------------    | :----------: | -----------: |
| `Task` based program    | `Microsoft.Coyote.Actors.RuntimeFactory.Create().Logger` |
| `Actor` based program | `Microsoft.Coyote.Runtime.RuntimeFactory.Create().Logger` <br/> `Actor.Logger`, `StateMachine.Logger`, `Monitor.Logger` |

The default `Logger` is a `ConsoleLogger` which is used to write output to the `System.Console`.
You can provide your own implementation of `System.IO.TextWriter` by calling the `SetLogger` method on the
`ICoyoteRuntime` or `IActorRuntime`.

The `IActorRuntime` also provides a higher level logging interface called [`IActorRuntimeLog`](/coyote/learn/core/logging#iactorruntimelog) for
logging `Actor` and `StateMachine` activity.

## Example of custom TextWriter

It is possible to replace the default logger with a custom one. The following example captures all log output in a `StringBuilder`:

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
  // disposes the old logger.
}
```

The above method replaces the previously installed logger with the specified one and returns the
previously installed logger.

Note that `SetLogger` does not `Dispose` the previously installed logger. This allows the logger to
be accessed and used after being removed from the Coyote runtime, so it is your responsibility to
call Dispose, which can be done with a `using` block.

You could write a custom `Logger` to intercept all logging messages and send them to an Azure Log
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
coverage](../tools/coverage) for an example graph output. The `coyote` test tool sets this up for
you when you specify `--graph` or `--coverage activity` command line options.

The `--verbose` command line option can also affect the default logging behavior. When `--verbose`
is specified all log output is written to the `System.Console`. This can result in a lot of output
especially if the test is performing many iterations. It is usually more useful to only capture the
output of the one failing iteration in a given test run and this is done by the testing runtime
automatically when `--verbose` is not set. In the latter case, all logging is redirected into an
`NullTextWriter` and only when a bug is found is the iteration run again with your real log writers
activated to capture the full log for that iteration.

See [IActorRuntimeLog API documentation](/coyote/learn/ref/Microsoft.Coyote.Actors/IActorRuntimeLogType).

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

See [ActorRuntimeLogTextFormatter](/coyote/learn/ref/Microsoft.Coyote.Actors/ActorRuntimeLogTextFormatterType)
documentation.
