# Coyote

Can you find the deadlock bug in this BoundedBuffer class?

```c#
public class BoundedBuffer
{
    private readonly object SyncObject = new object();
    private readonly object[] Buffer;
    private int PutAt;
    private int TakeAt;
    private int Occupied;

    public BoundedBuffer(int bufferSize)
    {
        this.PulseAll = pulseAll;
        this.Buffer = new object[bufferSize];
    }

    public void Put(object x)
    {
        lock (this.SyncObject)
        {
            while (this.Occupied == this.Buffer.Length)
            {
                Monitor.Wait(this.SyncObject);
            }

            ++this.Occupied;
            this.PutAt %= this.Buffer.Length;
            this.Buffer[this.PutAt++] = x;
            Monitor.Pulse(this.SyncObject);
        }
    }

    public object Take()
    {
        object result = null;
        lock (this.SyncObject)
        {
            while (this.Occupied == 0)
            {
                Monitor.Wait(this.SyncObject);
            }

            --this.Occupied;
            this.TakeAt %= this.Buffer.Length;
            result = this.Buffer[this.TakeAt++];
            Monitor.Pulse(this.SyncObject);
        }

        return result;
    }
}
```

This is a producer consumer queue where the `Take` method blocks if the buffer is empty and the
`Put` method blocks if the buffer has reached it's maximum allowed capacity and while the code
looks correct it contains a nasty deadlock bug.

Writing concurrent code is tricky and with the popular `async/await` feature in the C# Programming
Language it is now extremely easy to write concurrent code.

But how do we test concurrency in our code? Testing tools and frameworks have not kept up with the
pace of language innovation and so this is where `Coyote` comes to the rescue. When you use Coyote
to test your programs you will find concurrency bugs that are very hard to find any other way.

Coyote rewrites your `async tasks` in a way that allows the [coyote test](tools/testing.md) tool to
control all the concurrency and locking in your program and this allows it to find bugs using
intelligent [systematic testing](core/systematic-testing.md).

For example, if you take the `BoundedBuffer.dll` from the above sample you can do the following:

```
coyote rewrite BoundedBuffer.dll
coyote test BoundedBuffer.dll -m TestBoundedBufferMinimalDeadlock --iterations 100
```

This will report a deadlock error because Coyote has deadlock detection during testing. You will
get a log file explaining all this, and more importantly you will also get a trace file that can be
used to replay the bug in your debugger in a way that is 100% reproducable.

Concurrency bugs tend to be the kind of bugs that keep people up late at night pulling their hair
out because they are often not easily reproduced in any sort of predictable manner.

Coyote solves this problem giving you an environment where concurrency bugs can be systematically
found and reliably reproduced in a debugger -- allowing developers to fully understand them and fix
the core problem.

## Fearless coding for concurrent software

As a result Coyote gives your team much more confidence in building mission-critical services that
also push the limits on high concurrency, maximizing throughput and minimizing operational costs.

With Coyote you can create highly reliable software in a way that is also highly productive.

These are some direct quotes from Azure Engineers that uses Coyote:

  * _We often found bugs with Coyote in a matter of minutes that would have taken days with stress testing._

  * _Coyote added agility and allowed progress at a much faster pace._

  * _Features were developed in a test environment to first pass the Coyote tester. When dropped in
  production, they simply worked from the start._

  * _Coyote gave developers a significant confidence boost by providing full failover and
  concurrency testing at each check-in, right on their desktops as the code was written._

[Learn more about Coyote](overview/what-is-coyote.md)

[Get started now, it is super easy](get-started/install.md)

[Read more about how Azure is using Coyote](case-studies/azure-batch-service.md)

[State machine demo](programming-models/actors/state-machine-demo/)

[Code on Github](https://github.com/microsoft/coyote/)

