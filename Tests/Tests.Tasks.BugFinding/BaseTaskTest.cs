// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Tasks.BugFinding.Tests
{
    public abstract class BaseTaskTest : BaseTest
    {
        public BaseTaskTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private protected override SchedulingPolicy SchedulingPolicy => SchedulingPolicy.Systematic;

        protected static void AssertSharedEntryValue(SharedEntry entry, int expected) =>
            Specification.Assert(entry.Value == expected, "Value is {0} instead of {1}.", entry.Value, expected);

        protected class SharedEntry
        {
            public volatile int Value = 0;

            public async Task<int> GetWriteResultAsync(int value)
            {
                this.Value = value;
                await Task.CompletedTask;
                return this.Value;
            }

            public async Task<int> GetWriteResultWithDelayAsync(int value)
            {
                this.Value = value;
                await Task.Delay(1);
                return this.Value;
            }
        }
    }
}
