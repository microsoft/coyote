// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class UncontrolledTaskTests : BaseBugFindingTest
    {
        public UncontrolledTaskTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestDetectedUncontrolledDelay()
        {
            this.Test(async () =>
            {
                await AsyncProvider.DelayAsync(100);
            },
            configuration: this.GetConfiguration()
                .WithPartiallyControlledConcurrencyAllowed()
                .WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestDetectedUncontrolledDelayWithNoPartialControl()
        {
            this.TestWithError(async () =>
            {
                await AsyncProvider.DelayAsync(100);
            },
            configuration: this.GetConfiguration().WithTestingIterations(10),
            errorChecker: (e) =>
            {
                var expectedMethodName = GetFullyQualifiedMethodName(typeof(AsyncProvider), nameof(AsyncProvider.DelayAsync));
                Assert.StartsWith($"Invoking '{expectedMethodName}' returned task", e);
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestDetectedUncontrolledTaskAwaiter()
        {
            this.Test(async () =>
            {
                await new UncontrolledTaskAwaiter();
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestDetectedUncontrolledGenericTaskAwaiter()
        {
            this.Test(async () =>
            {
                await new UncontrolledGenericTaskAwaiter();
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestDetectedUncontrolledTaskAwaiterWithGenericArgument()
        {
            this.Test(async () =>
            {
                await new UncontrolledTaskAwaiter<int>();
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }
    }
}
