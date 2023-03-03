// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class ManualResetEventTests : BaseBugFindingTest
    {
        public ManualResetEventTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestWaitSetReset()
        {
            this.Test(() =>
            {
                using ManualResetEvent mre = new ManualResetEvent(false);
                bool result = mre.WaitOne(0);
                Specification.Assert(!result, "1st assertion failed.");
                result = mre.Set();
                Specification.Assert(result, "2nd assertion failed.");
                result = mre.WaitOne(0);
                Specification.Assert(result, "3rd assertion failed.");
                result = mre.WaitOne(0);
                Specification.Assert(result, "4rd assertion failed.");
                result = mre.Reset();
                Specification.Assert(result, "5th assertion failed.");
                result = mre.WaitOne(0);
                Specification.Assert(!result, "6th assertion failed.");
                result = mre.WaitOne(0);
                Specification.Assert(!result, "7th assertion failed.");
                result = mre.Set();
                Specification.Assert(result, "8th assertion failed.");
            });
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAlreadySignaled()
        {
            this.Test(() =>
            {
                using ManualResetEvent mre = new ManualResetEvent(true);
                bool result = mre.WaitOne();
                Specification.Assert(result, "Waiting the event failed.");
            });
        }

        [Fact(Timeout = 5000)]
        public void TestWaitDeadlock()
        {
            this.TestWithError(() =>
            {
                using ManualResetEvent mre = new ManualResetEvent(false);
                mre.WaitOne();
            },
            errorChecker: (e) =>
            {
                Assert.StartsWith("Deadlock detected.", e);
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestMultiThreadedWait()
        {
            this.Test(() =>
            {
                using ManualResetEvent mre = new ManualResetEvent(false);
                Thread t1 = new Thread(() =>
                {
                    mre.Set();
                });

                bool result = false;
                Thread t2 = new Thread(() =>
                {
                    result = mre.WaitOne();
                });

                t1.Start();
                t2.Start();

                t1.Join();
                t2.Join();

                Specification.Assert(result, "Waiting the event failed.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestTestMultiThreadedWaitDeadlock()
        {
            this.TestWithError(() =>
            {
                using ManualResetEvent mre = new ManualResetEvent(false);
                Thread t1 = new Thread(() =>
                {
                    for (int i = 0; i < 2; i++)
                    {
                        mre.Set();
                    }
                });

                Thread t2 = new Thread(() =>
                {
                    for (int i = 0; i < 2; i++)
                    {
                        mre.WaitOne();
                        mre.Reset();
                    }
                });

                t1.Start();
                t2.Start();

                t1.Join();
                t2.Join();
            },
            configuration: this.GetConfiguration().WithTestingIterations(10),
            errorChecker: (e) =>
            {
                Assert.StartsWith("Deadlock detected.", e);
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestPingPong()
        {
            this.Test(() =>
            {
                using ManualResetEvent mre1 = new ManualResetEvent(true);
                using ManualResetEvent mre2 = new ManualResetEvent(false);

                Thread t1 = new Thread(() =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        mre1.WaitOne();
                        mre1.Reset();
                        mre2.Set();
                    }
                });

                Thread t2 = new Thread(() =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        mre2.WaitOne();
                        mre2.Reset();
                        mre1.Set();
                    }
                });

                t1.Start();
                t2.Start();

                t1.Join();
                t2.Join();
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }
    }
}
