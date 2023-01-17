// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class ThreadYieldTests : BaseBugFindingTest
    {
        public ThreadYieldTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestThreadYield()
        {
            this.Test(() =>
            {
                Thread.Yield();
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestCooperativeThreadYield()
        {
            Configuration config = this.GetConfiguration().WithTestingIterations(10)
                .WithPartiallyControlledConcurrencyAllowed(true);
            this.Test(() =>
            {
                bool isDone = false;
                Task t1 = Task.Run(() =>
                {
                    isDone = true;
                });

                Task t2 = Task.Run(() =>
                {
                    while (!isDone)
                    {
                        Thread.Yield();
                    }
                });

                Task.WaitAll(t1, t2);
            },
            config);
        }
    }
}
