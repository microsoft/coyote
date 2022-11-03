// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;
using Monitor = System.Threading.Monitor;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class LockStatementTests : BaseBugFindingTest
    {
        public LockStatementTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestLockUnlock()
        {
            this.Test(() =>
            {
                int value = 0;
                object sync = new object();
                lock (sync)
                {
                    value++;
                }

                lock (sync)
                {
                    value++;
                }

                int expected = 2;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestReentrantLock()
        {
            this.Test(() =>
            {
                int value = 0;
                object sync = new object();
                lock (sync)
                {
                    value++;
                    lock (sync)
                    {
                        value++;
                    }
                }

                int expected = 2;
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestWaitPulse()
        {
            this.Test(async () =>
            {
                string value = string.Empty;
                object sync = new object();
                var t1 = Task.Run(() =>
                {
                    lock (sync)
                    {
                        if (value != "put")
                        {
                            Monitor.Wait(sync);
                        }

                        value = "taken";
                    }
                });

                var t2 = Task.Run(() =>
                {
                    lock (sync)
                    {
                        value = "put";
                        Monitor.Pulse(sync);
                    }
                });

                await Task.WhenAll(t1, t2);

                var expected = "taken";
                Specification.Assert(value == expected, "Value is {0} instead of {1}.", value, expected);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestMonitorWithLockTaken()
        {
            this.Test(() =>
            {
                object sync = new object();
                bool lockTaken = false;
                Monitor.TryEnter(sync, ref lockTaken);
                if (lockTaken)
                {
                    Monitor.Exit(sync);
                }

                Specification.Assert(lockTaken, "lockTaken is false");
            },
            this.GetConfiguration());
        }
    }
}
