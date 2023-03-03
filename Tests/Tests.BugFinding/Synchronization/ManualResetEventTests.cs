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
        public void TestSequentialWaitAlreadySignaled()
        {
            this.Test(() =>
            {
                ManualResetEvent mre = new ManualResetEvent(true);
                bool result = mre.WaitOne();
                Specification.Assert(result, "Waiting the event failed.");
            });
        }

        [Fact(Timeout = 5000)]
        public void TestSequentialWaitDeadlock()
        {
            this.TestWithError(() =>
            {
                ManualResetEvent mre = new ManualResetEvent(false);
                bool result = mre.WaitOne();
            },
            errorChecker: (e) =>
            {
                Assert.StartsWith("Deadlock detected.", e);
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAlreadySignaled()
        {
            this.Test(() =>
            {
                bool result = false;
                ManualResetEvent mre = new ManualResetEvent(false);
                Thread t1 = new Thread(() =>
                {
                    mre.Set();
                });

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
    }
}
