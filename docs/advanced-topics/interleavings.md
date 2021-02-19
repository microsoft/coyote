## Improving coverage of fine-grained interleavings

This page covers an advanced topic of programmatically controlling the interleavings that Coyote
covers during testing. Typically, this should not be needed: the "Controlled" APIs provided by
Coyote cover a lot of scenarios already. However, if you find yourself working with code that may
potentially have data races, or you are modeling other forms of synchronization not covered by
Coyote already, then you might have to help out the tester a bit more.

During [systematic testing](../concepts/concurrency-unit-testing.md), Coyote serializes the
concurrent execution of a program. Serialization means that only one task is running at any given
time. This helps remove the randomness that happens when the operating system schedules parallel
tasks and performs thread switching at unpredictable moments depending on your machine load. It also
makes executions reproducible because Coyote controls the scheduling. This is super useful for
debugging. Coyote, however, does not cover all possible interleavings by default. The interleavings
are complete (i.e., all possible behaviors can be covered by testing, given enough time) for
race-free programs. For programs with racy accesses, you have to help Coyote find more
interleavings.

Consider the following code:

```c#
int x = 0;

async Task A()
{
    var a = ++x;
    Console.WriteLine("A: " + a);
    await Task.CompletedTask;
}

async Task B()
{
    var b = ++x;
    Console.WriteLine("B: " + b);
    await Task.CompletedTask;
}

Task.Run(A);
Task.Run(B);
```

This code is setting up an obvious race condition on the variable `x`.  The production output of
this program can print `A: 1, B: 2` or `A: 2, B: 1` and sometimes even `A: 1, B: 1`.  This is
because the `x++` operator is not atomic, a thread switch can happen after the read of the value of
`x` and before the store of the incremented value.  When running the above code under `coyote test`,
the output will always be `A: 1, B: 2` or `A: 2, B:1`.  This is because `coyote test` runs the tasks
`A` and `B` until they either hit a _scheduling point_ or naturally terminate. None of the
statements in either methods is an automatically instrumented scheduling point, so Coyote fails to
test the data-race related concurrency above.

The `coyote test` tool executes tasks up to a _scheduling point_, and only then chooses to switch to
another task (or keeps running the same one) and execute it up to the next scheduling point and so
on. The default scheduling points are at Task API granularity (e.g., `Task.Run`, `Task.Delay`,
`await` or `Task.WhenAny` etc.). This is why the racy-behavior `A: 1, B: 1` is missed in the above
example: there is no scheduling point between the load of `x` and the store of its incremented
value.

## Instrumenting scheduling points to exercise racy behaviors

It is generally not safe for multiple tasks to perform non-atomic operations on the same variable at
the same time as this can create data race conditions. Ideally, you should avoid races using
synchronization. You can use a [Semaphore](semaphore.md), for instance, to protect the increment of
`x`. This rules out the `A: 1, B: 1` behavior and also makes coyote testing complete (given enough
time).

There may be situations where you cannot avoid the races, for instance when writing mocks for
testing. In that case, use `Task.ExploreContextSwitch` to introduce a custom scheduling point and
help coyote tester find sneaky bugs due to subtle data races within your code.

You can test this concurrency by introducing an explicit scheduling point using
`Task.ExploreContextSwitch`:

```c#
int x = 0;
int a = 0;
int b = 0;

async Task A()
{
    a = x + 1;
    Task.ExploreContextSwitch();
    x = a;
}

async Task B()
{
    b = x + 1;
    Task.ExploreContextSwitch();
    x = b;
}

public void RunTest()
{
    var t1 = Task.Run(A);
    var t2 = Task.Run(B);
    Task.WaitAll(t1, t2);
    Specification.Assert(a > 1 || b > 1, string.Format("a = {0} and b = {1}", a, b));
}
```

When you run this with `coyote test` the specification will sometimes fail with the Assert
`<ErrorLog> A: 1, B: 1`, which means `coyote test` can now fully explore behaviors of this program.

Note that `Task.ExploreContextSwitch` has no effect in production code, it only affects code running
under `coyote test`.

## Expressing asynchronous behavior in mocks

Mocks often don't have true asynchrony (they might just be dummy in-memory calls). In order to force
asynchronous execution when calling such mocks, you need to introduce a scheduling point in them.
One great way to do that is by using `Task.Yield`. This method, similar to `Task.Run`, has the
following two properties:

  1) As it's a scheduling point, it suspends the currently executing task and gives control back to
     Coyote so it can decide which task to schedule next.

  2) `Task.Yield` introduces a source of asynchrony in the system. The caller of the method that
     yields and the code after the yield can run asynchronously with respect to each other.

Similar to `Task.Run`, Coyote automatically instruments a scheduling point each time `Task.Yield` is
called.

The following example illustrates the difference between `Task.Yield` and
`Task.ExploreContextSwitch`. Consider two methods `FooAsync` and `BarAsync`, where the user calls
`FooAsync` which in turn calls `BarAsync`.

```c#
async Task BarAsync()
{
    await Task.Yield();
    Console.WriteLine("I am Bar!");
}

async Task FooAsync()
{
    BarAsync();
    Console.WriteLine("I am Foo!");
    await Task.CompletedTask;
}
```

If you call `FooAsync` above, then due to the asynchrony introduced by `Task.Yield`, there are two
possible outputs printed:

```plain
I am Foo!
I am Bar!
```

or

```plain
I am Bar!
I am Foo!
```

If you now replace `Task.Yield` with `Task.ExploreContextSwitch`, as in the following example:

```c#
async Task BarAsync()
{
    Task.ExploreContextSwitch();
    Console.WriteLine("I am Bar!");
    await Task.CompletedTask;
}

async Task FooAsync()
{
    BarAsync();
    Console.WriteLine("I am Foo!");
    await Task.CompletedTask;
}
```

Then, you only get one possible output:

```plain
I am Bar!
I am Foo!
```

This is because when you hit `Task.ExploreContextSwitch`, the control goes back to Coyote and it
looks to schedule another task, but since there is only _one_ task in the set of enabled tasks, it
is forced to schedule the same task again, which prints `I am Bar!` and then ends up printing `I am
Foo!`.

The reason there is only a single task in the above example, is because the `await
Task.CompletedTask` statements in `BarAsync` and `FooAsync` always run synchronously (the task is
already completed after all). Invoking `Task.ExploreContextSwitch` does not alter execution
semantics, so by itself cannot introduce concurrency where there is none.
