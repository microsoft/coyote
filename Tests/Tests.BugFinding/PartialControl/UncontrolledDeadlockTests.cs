// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Tests.Common.Threads;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class UncontrolledDeadlockTests : BaseBugFindingTest
    {
        public UncontrolledDeadlockTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledDeadlock()
        {
            this.TestWithError(async () =>
            {
                var handle = new ManualResetEventStub(true);
                Task task = Task.Run(async () =>
                {
                    handle.WaitOne();
                    await Task.Delay(1);
                    handle.Set();
                    handle.Reset();
                });

                handle.WaitOne();
                await Task.Delay(1);
                handle.Set();
                handle.Reset();
                await task;
            },
            configuration: this.GetConfiguration()
                .WithPartiallyControlledConcurrencyAllowed()
                .WithDeadlockTimeout(10)
                .WithTestingIterations(100),
            errorChecker: (e) =>
            {
                Assert.StartsWith("Potential deadlock or hang detected. The periodic deadlock detection monitor", e);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledDeadlockReportedAsNoBug()
        {
            this.Test(async () =>
            {
                var handle = new ManualResetEventStub(true);
                Task task = Task.Run(async () =>
                {
                    handle.WaitOne();
                    await Task.Delay(1);
                    handle.Set();
                    handle.Reset();
                });

                handle.WaitOne();
                await Task.Delay(1);
                handle.Set();
                handle.Reset();
                await task;
            },
            configuration: this.GetConfiguration()
                .WithPartiallyControlledConcurrencyAllowed()
                .WithPotentialDeadlocksReportedAsBugs(false)
                .WithDeadlockTimeout(10)
                .WithTestingIterations(10));
        }
    }
}
