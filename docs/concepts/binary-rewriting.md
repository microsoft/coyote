## Binary rewriting for systematic testing

To enable systematic testing of unmodified programs, Coyote performs _binary rewriting_ of managed
.NET assemblies. This process loads one or more of your assemblies (`*.dll`, `*.exe`) and rewrites
them for systematic testing (for production just use the original unmodified assemblies). The
rewritten code maintains exact semantics with the production version (so you don't need to worry
about false bugs), but has stubs and hooks injected that allow Coyote to take control of concurrent
execution and various sources of nondeterminism in a program.

To invoke the rewriter use the following command:

```plain
coyote rewrite ${PATH}
```

`${PATH}` is the path to the assembly (`*.dll`, `*.exe`) to rewrite or to a [JSON rewriting
configuration file](#configuration) (`*.json`). For automation, this can be conveniently done in a
post-build task, like this:
```xml
<Target Name="CoyoteRewrite" AfterTargets="AfterBuild">
  <Exec Command="dotnet $(PathToCoyote)/coyote.dll rewrite ${PATH}"/>
</Target>
```

To learn how to test your application after rewriting your binaries with Coyote, read
[here](../get-started/using-coyote.md), as well as check out our tutorial on [writing your first
concurrency unit test](../tutorials/first-concurrency-unit-test.md).

### Choosing the right Coyote host

Coyote ships a host for each supported .NET version. Rewriting injects references to the runtime of
the host that performs it, so you must run the host that matches the .NET major version targeted by
the assembly: use the `net8.0` Coyote host to rewrite a `net8.0` assembly and the `net10.0` host to
rewrite a `net10.0` assembly. If the versions do not match, `coyote rewrite` reports an error naming
both versions and leaves the assembly unmodified.

### Configuration

If you have multiple binaries to rewrite, then you should provide a JSON rewriting configuration
file, which looks like this example:

```json
{
  "AssembliesPath": "bin/net8.0",
  "OutputPath": "bin/net8.0/rewritten",
  "Assemblies": [
    "BoundedBuffer.dll",
    "MyOtherLibrary.dll",
    "FooBar123.dll"
  ]
}
```

- `AssembliesPath` is the folder containing the original binaries.  This property is required.

- `OutputPath` allows you to specify a different location for the rewritten assemblies. The
`OutputPath` can be omitted in which case it is assumed to be the same as `AssembliesPath` and in
that case the original assemblies will be replaced.

- `Assemblies` is the list of specific assemblies in `AssembliesPath` to be rewritten. You must
  explicitly list all the assemblies to rewrite (pattern matching, `*` and `.` are not supported).

Then pass this JSON file on the command line: `coyote rewrite config.json`.

### Which DLLs to rewrite?

**TLDR:** The short answer (and our recommendation) is that ideally you should just rewrite your
test DLLs, as well as your production code DLLs (which means the code that you and your team owns),
and to not rewrite any external dependencies (which you assume are correct after all).

The reason behind this recommendation is that there are certain trade-offs when rewriting DLLs
because of two issues: Coyote today does not support every single concurrency API in C# (instead
mostly focuses on the popular [task-asynchronous programming
model](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/)); and
dealing with the infamous state (schedule) space explosion problem.

Regarding the 1st issue, Coyote is focused on [asynchronous
task-based](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/)
concurrency (basically common things like `Task` objects and `async`/`await`). So if an external
library (or some "low-level" dependency DLL) is written with "lower-level" threading APIs (such as
explicitly spawning threads and waiting on synchronization primitives such as a `WaitHandle`) or
uses custom concurrency semantics (for example via a custom `TaskScheduler` or custom threadpools),
and you decide to rewrite these DLLs, then Coyote will either (a) not be able to intercept these
concurrency mechanisms properly (if the C# APIs is not supported by Coyote yet) which can end up
regressing exploration, or (b) be able to intercept them but the state (schedule) space in your test
will explode (more on this below). The good news is that using these "low-level" APIs is uncommon in
_most_ user applications/services, but of course some frameworks/library dependencies do use them.

Regarding the 2nd issue, the more concurrent code you instrument, the more scheduling decisions
Coyote must explore in every test iteration. This _exponentially_ increases how much time you need
to test to cover the same code surface of your application. This is known as state space explosion.
Since Coyote explores under a test "budget" (such as number of test iterations) the bigger the state
space to explore, the less efficient Coyote will be. Ideally, you just want to focus on testing your
own concurrent code, and not the code of 3rd party frameworks/libraries (which you assume is
correct!). For this reason, its recommended instead of rewriting every single dependency, to just
rewrite DLLs that you (and your team) owns. This basically means to focus rewriting the test DLL as
well as your production code DLLs, assuming these DLLs only use tasks, `async`/`await` and these
kind of "high-level" concurrency primitives. Think about this as "component-wise" testing.

Under the hood, Coyote deals with both of the above problems using a feature called
_partially-controlled exploration_. In this mode, which is enabled by default when testing a
partially-rewritten program (rewritten DLLs you own, and un-rewritten 3rd party DLLs), Coyote will
treat any un-rewritten DLLs as "pass-through", and their methods are invoked _atomically_. This
means that while Coyote sequentializes the program execution to explore different execution paths
and scheduling decisions (see [here](concurrency-unit-testing.md)), if it encounters a call to an
un-rewritten method (or unsupported C# system API), instead of giving up, or immediately scheduling
something else (resulting in lost coverage), Coyote will instead have a chance to wait for the
uncontrolled call to complete (with some tunable time bound, which is a heuristic inside Coyote).
This means that coverage wont regress in most cases.

Ideally, you want to mock important external dependencies (for example storage backends such as
CosmosDB) with some in-memory mock implementation to make your test fast and efficient, but at least
you can already get tests up and running without requiring to mock every single thing, making the
experience pay-as-you-go. And our plan is that as partially-controlled exploration improves over
time, you transparently also get better coverage without having to do much from your side.

### How timeouts are modeled

The controlled scheduler serializes your program and decides itself when each operation runs, so it
does not measure wall-clock time. Rewritten synchronization APIs that accept a timeout therefore do
not treat that timeout as a source of nondeterminism during systematic testing:

- A **finite non-zero** timeout is explored as if it was infinite, so the wait completes when the
  operation that it is waiting for completes. Racing the wait against its timeout would instead
  make every wait fail in some schedules, no matter how large the timeout is, reporting timeouts
  that the program is not expected to observe and hiding the bugs that happen after the wait
  succeeds. `Task.Wait`, `Task.WaitAll`, `Task.WaitAny`, `Task.WaitAsync`, `Monitor.Wait`,
  `SemaphoreSlim.Wait`, `SemaphoreSlim.WaitAsync`, `WaitHandle.WaitOne`, `WaitHandle.WaitAll`,
  `WaitHandle.WaitAny` and `Thread.Join` all follow this rule. The exception is `Lock.TryEnter`,
  which reports a finite non-zero timeout as unsupported rather than blocking indefinitely on a
  lock that its caller expects to give up on.
- If no operation can ever complete such a wait, then the runtime reports it as a **deadlock**,
  which is how it reports any other wait that cannot be satisfied.
- Where the API can return without giving any other operation a chance to run first, a **zero**
  timeout keeps its production meaning: `Task.WaitAsync` throws a `TimeoutException`, and
  `SemaphoreSlim.Wait`, `SemaphoreSlim.WaitAsync`, `WaitHandle.WaitOne` and `Lock.TryEnter` report
  that they did not acquire the resource.

This policy does not change how cancellation is observed. Where the API takes a cancellation token
that Coyote controls, such as `Task.WaitAsync`, a wait with a finite timeout still completes as
canceled when the token is canceled, and a token that is already canceled when the wait starts
takes precedence over the timeout, exactly as it does in production.

Systematic fuzzing is different: it executes the program on real threads and in real time, only
injecting delays in between operations. Timeouts there keep their wall-clock meaning and are
handled by the uncontrolled .NET runtime.

### How memory ordering is modeled

Systematic testing explores the interleavings of controlled operations, executing one operation at a
time and switching between them only at scheduling points. It does not explore weak-memory behaviors,
such as a processor or the JIT reordering memory accesses. Every schedule that Coyote explores is
sequentially consistent, so a bug that only manifests under a memory reordering can go unnoticed.

Within this boundary, the rewritten memory APIs behave as follows:

- `Volatile.Read`, `Volatile.Write` and the `Interlocked` operations access shared memory, so they are
  rewritten to introduce a scheduling point before the access, which lets another operation access
  the same memory in between. These scheduling points can be turned off with
  `Configuration.WithVolatileOperationRaceCheckingEnabled` and
  `Configuration.WithAtomicOperationRaceCheckingEnabled`.
- Memory barriers that do not access memory, namely `Thread.MemoryBarrier`, `Interlocked.MemoryBarrier`,
  `Interlocked.MemoryBarrierProcessWide` and the .NET 10 `Volatile.ReadBarrier` and
  `Volatile.WriteBarrier`, are not rewritten. They invoke the .NET runtime directly and do not introduce
  a scheduling point. This is not an uncontrolled synchronization operation: a barrier never blocks
  and never waits for another operation, so it cannot hide a dependency from the scheduler or cause a
  real deadlock. It only constrains the memory reorderings that Coyote does not explore anyway.

### Quality of life improvements through rewriting

Coyote will automatically rewrite certain parts of your test code (without changing the application
semantics) to improve the testing experience. For example:

During testing coyote needs to be able to terminate a test iteration at any time in order to support
the `--max-steps` command line argument. This termination is done using a special coyote
`ExecutionCancelledException`. The problem is when your code contains one of the following:

```csharp
} catch {
} catch (Exception) {
} catch (RuntimeException) {
```

These will inadvertently catch the special Coyote exception, which then stops `--max-steps` from
working. The recommended fix is to add a `when (!(e is Microsoft.Coyote.RuntimeException))` filter.
The good news is that `coyote rewrite` can take care of this for you automatically so you do not
need to modify any of your exception handlers.
