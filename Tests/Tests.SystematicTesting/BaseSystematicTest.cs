// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Rewriting;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests
{
    public abstract class BaseSystematicTest : BaseTest
    {
        public BaseSystematicTest(ITestOutputHelper output)
            : base(output)
        {
        }

        protected override bool IsSystematicTest
        {
            get
            {
                var assembly = this.GetType().Assembly;
                bool result = RewritingEngine.IsAssemblyRewritten(assembly);
                Assert.True(result, $"Expected the '{assembly}' assembly to be rewritten.");
                return result;
            }
        }

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
                await Task.Delay(5);
                return this.Value;
            }
        }
    }
}
