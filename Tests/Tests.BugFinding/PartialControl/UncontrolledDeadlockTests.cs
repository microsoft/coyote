// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using System.Threading.Tasks;
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
                SemaphoreSlim semaphore = new SemaphoreSlim(1);
                Task task = Task.Run(async () =>
                {
                    semaphore.Wait(100);
                    await Task.Delay(1);
                    semaphore.Release();
                });

                semaphore.Wait(100);
                await Task.Delay(1);
                semaphore.Release();
                await task;
            },
            configuration: this.GetConfiguration()
                .WithPartiallyControlledConcurrencyAllowed()
                .WithDeadlockTimeout(100)
                .WithTestingIterations(10),
            errorChecker: (e) =>
            {
                Assert.StartsWith($"Potential deadlock detected. Because a deadlock detection timeout", e);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestUncontrolledDeadlockReportedAsNoBug()
        {
            this.Test(async () =>
            {
                SemaphoreSlim semaphore = new SemaphoreSlim(1);
                Task task = Task.Run(async () =>
                {
                    semaphore.Wait(100);
                    await Task.Delay(1);
                    semaphore.Release();
                });

                semaphore.Wait(100);
                await Task.Delay(1);
                semaphore.Release();
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
