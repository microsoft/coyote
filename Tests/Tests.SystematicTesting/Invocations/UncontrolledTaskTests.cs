// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Invocations
{
    public class UncontrolledTaskTests : BaseSystematicTest
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
            configuration: GetConfiguration().WithTestingIterations(100),
            errorChecker: (e) =>
            {
                string expectedMethodName = $"{typeof(AsyncProvider).FullName}.{nameof(AsyncProvider.DelayAsync)}";
                Assert.StartsWith($"Method '{expectedMethodName}' returned an uncontrolled task", e);
            },
            replay: true);
        }
    }
}
