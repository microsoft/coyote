## Program non-determinism

Programs that are deterministic are also easy to test. Intuitively, a program is deterministic
whenever it executes in the same manner if provided the same inputs. Most software, however, isn't
deterministic. Let's begin by understanding this non-determinism and how it makes your life as
developers more difficult.

The most common form of non-determinism stems from concurrency. Consider the following procedure:

```c#
// Shared variable x.
int x = 0;

int foo()
{
   // Concurrent operations on x.
   var t1 = Task.Run(() => { x = 1; });
   var t2 = Task.Run(() => { x = 2; });

   // Join all.
   Task.WaitAll(t1, t2);
}
```

Executing `foo` can result in `x == 1` or `x == 2`. The exact value of `x` depends on the order in
which the tasks are executed. This is non-determinism because the scheduling of tasks is not in your
control. Adding synchronization helps reduce the amount of non-determinism but doesn't completely
take it away.

The above example showed a classical _data race_. Actually, it is a _low level_ data race between
individual memory accesses. Even _high level_ races such as the order in which messages arrive and
get processed also contributes to non-determinism.

Non-determinism arises from many other sources as well. Timeouts are a good example. The use of
timers prevail in many distributed systems. Consider a program that wishes to offload some
computation to a remote computer. It will typically send a message over the network, and then start
a timer with some pre-determined timeout value. If the response arrives successfully from the remote
machine, the timer is canceled and all is good. If the timer fires before the response arrives, then
the sender may wish to invoke a recovery action. Which of these two cases actually happen in
practice is, again, not in the programmer's control. Timers, thus, are another source of
non-determinism in the program.

Other similar examples include failures. When designing software services that execute in the cloud,
you must anticipate the possibility of hardware or software failures that cause the hosting VM to
reboot. In such cases the fallback is to restart the VM (or spawn a new one) to take over the failed
process. This is also non-determinism because the failure (and restart) can happen at any time.
Calling into an external service that may return one of many error codes is also non-determinism;
the exact return value is again not in your control when you call the external service.

The trouble with non-determinism is that you must defend against all the ways in which the
non-determinism unfolds at runtime. For example, no matter what scheduling happens between
concurrent operations or when failures happen, your program must still work. This imposes a cognitive
burden on your design and makes it harder to write correct code. Once the design and code are in
place, even testing is hard because how do you get coverage of these non-deterministic activities?
Stress testing techniques hope that with enough load on the system, the corner cases will get
covered. Coyote offers a more principled approach, namely [concurrency unit testing](concurrency-unit-testing.md)
that has proven to be very effective in practice, and [widely adopted in Azure](../case-studies/azure-batch-service.md).
