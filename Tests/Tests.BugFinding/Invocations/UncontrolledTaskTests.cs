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
                Assert.StartsWith($"Method '{expectedMethodName}' returned an uncontrolled task", e);
            },
            replay: true);
        }
    }
}
