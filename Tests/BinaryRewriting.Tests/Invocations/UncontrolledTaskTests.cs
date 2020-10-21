﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BinaryRewriting.Tests.Invocations
{
    public class UncontrolledTaskTests : BaseRewritingTest
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
                await AsyncProvider.DelayAsync();
            },
            configuration: GetConfiguration().WithTestingIterations(100),
            errorChecker: (e) =>
            {
                Assert.True(e.StartsWith("Method 'Microsoft.Coyote.Tests.Common.Tasks.AsyncProvider.DelayAsync' returned an uncontrolled task"),
                    "Expected uncontrolled task from invoking the async method.");
            },
            replay: true);
        }
    }
}
