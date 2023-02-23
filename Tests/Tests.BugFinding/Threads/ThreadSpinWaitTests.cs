// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class ThreadSpinWaitTests : BaseBugFindingTest
    {
        public ThreadSpinWaitTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestThreadSpinWait()
        {
            this.Test(() =>
            {
                Thread.SpinWait(10);
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestCooperativeThreadSpinWait()
        {
            this.Test(() =>
            {
                bool isDone = false;
                Thread t1 = new Thread(() =>
                {
                    isDone = true;
                });

                Thread t2 = new Thread(() =>
                {
                    while (!isDone)
                    {
                        Thread.SpinWait(10);
                    }
                });

                t1.Start();
                t2.Start();

                t1.Join();
                t2.Join();

                Specification.Assert(isDone, "The expected condition was not satisfied.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }
    }
}
