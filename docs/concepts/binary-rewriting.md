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

### Configuration

If you have multiple binaries to rewrite, then you should provide a JSON rewriting configuration
file, which looks like this example:

```json
{
  "AssembliesPath": "bin/net6.0",
  "OutputPath": "bin/net6.0/rewritten",
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
because of two issues: (1) Coyote today does not support the universe of concurrency APIs in C#
(primarily focuses on the mainstream [task-asynchronous programming
model](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/)), and (2)
state (schedule) space explosion.

Regarding (1), Coyote is focused on [asynchronous
task-based](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/async/))
concurrency (basically common things like `Task` objects and `async`/`await`). So if an external
library (or some "low-level" dependency DLL) is written with "lower-level" threading APIs (such as
explicitly spawning threads and waiting on synchronization primitives such as a `WaitHandle`) or
uses custom concurrency semantics (for example via a custom `TaskScheduler` or custom threadpools),
and you decide to rewrite these DLLs, then Coyote will either (a) not be able to intercept these
concurrency mechanisms properly (if the C# APIs is not supported by Coyote yet) which can end up
regressing exploration, or (b) be able to intercept them but the state (schedule) space in your test
will explode (more on this below). The good news is that using these "low-level" APIs is uncommon in
_most_ user applications/services, but of course some frameworks/library dependencies do use them.

Regarding (2), the more concurrent code you instrument, the more scheduling decisions Coyote has to
explore in every test iteration. This exponentially increases how much time you need to test to
cover the same code surface of your application. This is known as state space explosion. Since
Coyote explores under a test "budget" (such as number of test iterations) the bigger the state space
to explore, the less efficient Coyote will be. Ideally, you just want to focus on testing your own
concurrent code, and not the code of 3rd party frameworks/libraries (which you assume is correct!).
For this reason, its recommended instead of rewriting every single dependency, to just rewrite DLLs
that you (and your team) owns. This basically means to focus rewriting the test DLL as well as your
production code DLLs, assuming these DLLs only use tasks, `async`/`await` and these kind of
"high-level" concurrency primitives. Think about this as "component-wise" testing.

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
