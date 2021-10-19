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
            this.TestWithError(async () =>
            {
                await AsyncProvider.DelayAsync(100);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100),
            errorChecker: (e) =>
            {
                string expectedMethodName = $"{typeof(AsyncProvider).FullName}.{nameof(AsyncProvider.DelayAsync)}";
                Assert.StartsWith($"Invoking '{expectedMethodName}' returned task", e);
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestDetectedUncontrolledAwaiter()
        {
            this.Test(async () =>
            {
                await new UncontrolledTaskAwaiter();
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestDetectedUncontrolledGenericAwaiter()
        {
            this.Test(async () =>
            {
                await new UncontrolledTaskAwaiter<int>();
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }
    }
}
