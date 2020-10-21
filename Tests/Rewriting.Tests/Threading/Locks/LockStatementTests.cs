// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;
using Monitor = System.Threading.Monitor;

namespace Microsoft.Coyote.Rewriting.Tests.Threading
{
    public class LockStatementTests : BaseRewritingTest
    {
        private readonly object SyncObject1 = new object();
        private string Value;

        public LockStatementTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestSimpleLock()
        {
            this.Test(() =>
            {
                lock (this.SyncObject1)
                {
                    this.Value = "1";
                    this.TestReentrancy();
                }

                var expected = "2";
                Specification.Assert(this.Value == expected, "Value is {0} instead of {1}.", this.Value, expected);
            });
        }

        private void TestReentrancy()
        {
            lock (this.SyncObject1)
            {
                this.Value = "2";
            }
        }

        [Fact(Timeout = 5000)]
        public void TestWaitPulse()
        {
            this.Test(async () =>
            {
                var t1 = Task.Run(this.TakeTask);
                var t2 = Task.Run(this.PutTask);

                await Task.WhenAll(t1, t2);

                var expected = "taken";
                Specification.Assert(this.Value == expected, "Value is {0} instead of {1}.", this.Value, expected);
            });
        }

        private void TakeTask()
        {
            lock (this.SyncObject1)
            {
                if (this.Value != "put")
                {
                    Monitor.Wait(this.SyncObject1);
                }

                this.Value = "taken";
            }
        }

        private void PutTask()
        {
            lock (this.SyncObject1)
            {
                this.Value = "put";
                Monitor.Pulse(this.SyncObject1);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestMonitorWithLockTaken()
        {
            this.Test(() =>
            {
                object obj = new object();
                bool lockTaken = false;
                Monitor.TryEnter(obj, ref lockTaken);
                if (lockTaken)
                {
                    Monitor.Exit(obj);
                }

                Specification.Assert(lockTaken, "lockTaken is false");
            },
            GetConfiguration());
        }
    }
}
