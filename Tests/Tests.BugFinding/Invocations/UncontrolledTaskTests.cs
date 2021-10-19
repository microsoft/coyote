// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
            configuration: this.GetConfiguration().WithTestingIterations(10),
            errorChecker: (e) =>
            {
                Assert.StartsWith($"Invoking 'AsyncProvider.DelayAsync' is not intercepted", e);
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
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestDetectedUncontrolledGenericAwaiter()
        {
            this.Test(async () =>
            {
                await new UncontrolledTaskAwaiter<int>();
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }
    }
}
