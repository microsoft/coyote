## Binary rewriting for systematic testing

To enable systematic testing of unmodified programs, Coyote performs _binary rewriting_ of managed
.NET assemblies. This process loads one or more of your assemblies and rewrites them for systematic
testing (for production just use the original unmodified assemblies). This can be easily done in a
post-build task. The rewritten code maintains exact semantics with the production version (so you
don't need to worry about false bugs), but has stubs and hooks injected that allow Coyote to take
control of concurrent execution and various sources of nondeterminism in a program.

To learn how to run the binary rewriter of Coyote before testing your application read
[here](../get-started/using-coyote.md), as well as check out our tutorial on [writing your first
concurrency unit test](../tutorials/first-concurrency-unit-test.md).

## Quality of life improvements through rewriting

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
