## Rewriting binaries

The [asynchronous tasks programming model](overview.md) shows how Coyote can control the concurrency
of asynchronous operations using the special Coyote Task types. You can choose to use this
programming model by modifying your code to use these Coyote Task types but this takes some effort
on your part. Instead, you can use the [coyote rewriting tool](../tools/rewriting) which will
automatically rewrite your binaries in a way that they can then be used in `coyote test` runs.

This automatic rewriting currently supports rewriting the following constructs so they are testable
with `coyote test`:

- `Task`, all methods except `ConfigureAwait` and `ContinueWith`
- C# lock statements and `Monitor` type.
- Exception handlers

Note that during testing coyote needs to be able to terminate a test iteration at any time in order
to support the `--max-steps` command line argument. This termination is done using a special coyote
`ExecutionCancelledException`. The problem is when your code contains one of the following:

```c#
} catch {
} catch (Exception) {
} catch (RuntimeException) {
```

these will inadvertently catch the special Coyote exception, which then stops `--max-steps` from
working. The recommended fix is to add a `when (!(e is Microsoft.Coyote.RuntimeException))`
filter as shown in [tester requirements](../tools/tester-requirements.md).

The good news is that `coyote rewrite` can take care of this for you automatically so you do not
need to modify any of your exception handlers.

## Not supported

The following concurrency constructs are not supported by `coyote rewrite`:

- `Parallel` helper class.
- `System.Collections.Current.BlockingCollection`
- `ThreadPool`
- `Thread`
- `ManualResetEvent`, `AutoResetEvent`, `SemaphoreSlim`, `Semaphore`, `SpinLock`, `SpinWait`, `ReaderWriterLock`, `Interlocked`, ...
- Essentially any form of concurrency control that is not listed in the supported list above
including native calls to Win32 API's that block the current thread or spawn new threads.
- [Task-like types](https://github.com/dotnet/roslyn/blob/master/docs/features/task-types.md)
- Any .NET library that is a mixed-mode assembly.
- Any .NET library type or method that uses any of the above, especially blocking IO like
`System.IO.Pipelines`, `System.IO.Channels`, etc.

As you can see this is currently a long list, that will hopefully get shorter over time
as development continues on this feature.
