// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    /// <summary>
    /// This is an implementation of the concurrency bug described in
    /// Extreme Programming Challenge 14:
    ///
    /// https://wiki.c2.com/?ExtremeProgrammingChallengeFourteenTheBug.
    /// </summary>
    public class ExtremeProgrammingChallenge14Tests : BaseBugFindingTest
    {
        public ExtremeProgrammingChallenge14Tests(ITestOutputHelper output)
            : base(output)
        {
        }

        internal class BoundedBuffer
        {
            private readonly object syncObject = new object();
            private readonly object[] buffer = new object[1];
            private int putAt;
            private int takeAt;
            private int occupied;
            private readonly bool pulseAll;

            public BoundedBuffer(bool pulseAll)
            {
                this.pulseAll = pulseAll;
            }

            public void Put(object x)
            {
                lock (this.syncObject)
                {
                    while (this.occupied == this.buffer.Length)
                    {
                        Monitor.Wait(this.syncObject);
                    }

                    ++this.occupied;
                    this.putAt %= this.buffer.Length;
                    this.buffer[this.putAt++] = x;

                    if (this.pulseAll)
                    {
                        Monitor.PulseAll(this.syncObject);
                    }
                    else
                    {
                        Monitor.Pulse(this.syncObject);
                    }
                }
            }

            public object Take()
            {
                object result = null;
                lock (this.syncObject)
                {
                    while (this.occupied is 0)
                    {
                        Monitor.Wait(this.syncObject);
                    }

                    --this.occupied;
                    this.takeAt %= this.buffer.Length;
                    result = this.buffer[this.takeAt++];

                    if (this.pulseAll)
                    {
                        Monitor.PulseAll(this.syncObject);
                    }
                    else
                    {
                        Monitor.Pulse(this.syncObject);
                    }
                }

                return result;
            }
        }

        private static void Reader(BoundedBuffer buffer)
        {
            for (int i = 0; i < 10; i++)
            {
                buffer.Take();
            }
        }

        private static void Writer(BoundedBuffer buffer)
        {
            for (int i = 0; i < 20; i++)
            {
                buffer.Put("hello " + i);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestChallenge14WithDeadlock()
        {
            this.TestWithError(() =>
            {
                BoundedBuffer buffer = new BoundedBuffer(false);
                var tasks = new List<Task>
                {
                    Task.Run(() => Reader(buffer)),
                    Task.Run(() => Reader(buffer)),
                    Task.Run(() => Writer(buffer))
                };

                Task.WaitAll(tasks.ToArray());
            },
            configuration: this.GetConfiguration().WithTestingIterations(100)
                .WithLockAccessRaceCheckingEnabled(true),
            errorChecker: (e) =>
            {
                Assert.StartsWith("Deadlock detected.", e);
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestChallenge14WithFix()
        {
            this.Test(() =>
            {
                BoundedBuffer buffer = new BoundedBuffer(true);
                var tasks = new List<Task>
                {
                    Task.Run(() => Reader(buffer)),
                    Task.Run(() => Reader(buffer)),
                    Task.Run(() => Writer(buffer))
                };

                Task.WaitAll(tasks.ToArray());
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }
    }
}
