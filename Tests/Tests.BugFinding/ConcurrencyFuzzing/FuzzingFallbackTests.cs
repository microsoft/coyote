// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests.SystematicFuzzing
{
    public class FuzzingFallbackTests : BaseBugFindingTest
    {
        public FuzzingFallbackTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestFuzzingFallbackAfterUncontrolledDelay()
        {
            var configuration = this.GetConfiguration().
                WithSystematicFuzzingFallbackEnabled(true).
                WithTestingIterations(10);
            this.Test(async () =>
            {
                // This would fail during SCT due to uncontrolled concurrency,
                // but the fuzzing fallback mechanism should prevent it.
                await AsyncProvider.DelayAsync(1);
            },
            configuration);
        }

        [Fact(Timeout = 5000)]
        public void TestFuzzingFallbackAfterUncontrolledInvocation()
        {
            var configuration = this.GetConfiguration().
                WithSystematicFuzzingFallbackEnabled(true).
                WithTestingIterations(10);
            this.Test(() =>
            {
                // This would fail during SCT due to uncontrolled concurrency,
                // but the fuzzing fallback mechanism should prevent it.
                Thread.Yield();
            },
            configuration);
        }
    }
}
