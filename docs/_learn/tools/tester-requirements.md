---
layout: reference
title: Testing Coyote programs
section: learn
permalink: /learn/tools/tester-requirements
---

## Tester requirements

The Coyote tester is based on the idea of [systematic testing](../core/systematic-testing). This
means that it must understand the concurrency and nondeterminism in your test code. For this reason,
the following restrictions must be kept in mind when writing Coyote tests. Note that these
restrictions only apply to your test code (in order to run `coyote test`). There are no such
restrictions when running in production. If you find that your test must call some code that
potentially violates these restrictions, then you must mock the call for the purpose of the test.

### Stick to your programming model

The test must only have concurrency in the chosen Coyote programming model. The following code that
spawns both a controlled Coyote `Task` as well as an native .NET `Task` is going to confuse
the tester because it would not know how to control the scheduling of the latter.

```c#
using CoyoteTasks = Microsoft.Coyote.Tasks;
using SystemTasks = System.Threading.Tasks;

[Microsoft.Coyote.SystematicTesting.Test]
public static async CoyoteTasks.Task MyTest()
{
    // bad: do not do this.
    var t1 = CoyoteTasks.Task.Run(() => { foo(); });
    var t2 = SystemTasks.Task.Run(() => { bar(); });
    ...
}
```

The tester does its best to identify concurrency outside its control (e.g., the task `t2` above) and
complain so that you can debug your code, but it is not always able to do so.

The same holds for synchronization operations, e.g., acquiring or releasing locks via the `lock`
construct. In this case, the tester can deadlock if there is synchronization in the test that it
does not know about. In most cases, Coyote code would be free of these low-level synchronization
operations. If you must use a lock and you are using the Coyote Tasks programming model try the
Coyote `AsyncLock`. If you are calling 3rd party code that mixes locks and concurrency you may need
to mock that external code.

### Declare all nondeterminism

The tester needs to understand all nondeterminism in a test. So the test should not invoke code that
cannot be replayed by the tester. Consider the following code that performs a branch based on the
current time of the day.

```c#
if (DateTime.Now - prevTime > TimeSpan.FromSeconds(5)) { ... } else { ... }
```

This branch is not in the control of the tester, hence an attempt to replay a test iteration can
fail. In such a case, for the purpose of the test, the branch should instead be based on a call to
`Microsoft.Coyote.Random.Generator`, which can be recorded and replayed by the tester. This change has
the added bonus that it makes it easier for the tester to explore both sides of the branch. Of
course, do continue to use the time-based decision in production code.

```c#
bool branch = false;
if (isTest)
{
  // You should cache this generator at a higher level for better
  // performance.
  var generator = Microsoft.Coyote.Random.Generator.Create();
  branch = generator.NextBoolean();
} else {
  branch = DateTime.Now - prevTime > TimeSpan.FromSeconds(5);
}

if (branch) { ... } else { ... }
```

### Exceptions

The code executed by the test should not catch exceptions of the type
`Microsoft.Coyote.RuntimeException` (or any derived type). These exceptions are used by the
tester for multiple purposes. For instance, it is thrown when an assertion fails in a monitor, or
when a test iteration hits `max-steps`. In these cases, the test needs to fully unwind its stack.

The following code will confuse the tester because the catch block will catch Coyote exceptions that
the tester might throw when inside `RunTheTest` and then attempt to continue to the test, which the
tester does not expect.

```c#
[Microsoft.Coyote.SystematicTesting.Test]
public static async Microsoft.Coyote.Tasks.Task MyTest()
{
  try
  {
    RunTheTest();
  }
  catch (Exception e)
  {
    Cleanup();
  }
  SomeMoreTesting();
}
```

Essentially, what this means is that you should not have an unconditional `catch(Exception e)` in
your test code. The above code can be fixed by adding an exception filter as follows:

```c#
[Microsoft.Coyote.SystematicTesting.Test]
public static async Microsoft.Coyote.Tasks.Task MyTest()
{
  try
  {
    RunTheTest();
  }
  catch (Exception e) when (!(e is Microsoft.Coyote.RuntimeException))
  {
    Cleanup();
  }
  SomeMoreTesting();
}
```

This allows the special Coyote runtime exceptions to pass through this try/catch block so the Coyote
test engine can handle it and continue to run normally.
