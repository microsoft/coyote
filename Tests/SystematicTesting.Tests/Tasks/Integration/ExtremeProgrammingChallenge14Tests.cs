// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    /// <summary>
    /// This is an implementation of the concurrency bug described in
    /// Extreme Programming Challenge 14:
    ///
    /// http://wiki.c2.com/?ExtremeProgrammingChallengeFourteenTheBug.
    /// </summary>
    public class ExtremeProgrammingChallenge14Tests : BaseTest
    {
        public ExtremeProgrammingChallenge14Tests(ITestOutputHelper output)
            : base(output)
        {
        }

        public override bool SystematicTest => true;

        internal class BoundedBuffer
        {
            private readonly bool pulseAll;

            public BoundedBuffer(bool pulseAll)
            {
                this.pulseAll = pulseAll;
            }

            public void Put(object x)
            {
                using (var monitor = SynchronizedBlock.Lock(this.syncObject))
                {
                    while (this.occupied == this.buffer.Length)
                    {
                        monitor.Wait();
                    }

                    ++this.occupied;
                    this.putAt %= this.buffer.Length;
                    this.buffer[this.putAt++] = x;

                    if (this.pulseAll)
                    {
                        monitor.PulseAll();
                    }
                    else
                    {
                        monitor.Pulse();
                    }
                }
            }

            public object Take()
            {
                object result = null;
                using (var monitor = SynchronizedBlock.Lock(this.syncObject))
                {
                    while (this.occupied == 0)
                    {
                        monitor.Wait();
                    }

                    --this.occupied;
                    this.takeAt %= this.buffer.Length;
                    result = this.buffer[this.takeAt++];

                    if (this.pulseAll)
                    {
                        monitor.PulseAll();
                    }
                    else
                    {
                        monitor.Pulse();
                    }
                }

                return result;
            }

            private readonly object syncObject = new object();
            private readonly object[] buffer = new object[1];
            private int putAt;
            private int takeAt;
            private int occupied;
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
            configuration: GetConfiguration().WithTestingIterations(100),
            errorChecker: (e) =>
            {
                Assert.True(e.StartsWith("Deadlock detected."), "Expected 'Deadlock detected', but found error: " + e);
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
            configuration: GetConfiguration().WithTestingIterations(100));
        }
    }
}
