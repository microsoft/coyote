// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tests.Common;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Tasks.Tests
{
    public abstract class BaseTaskTest : BaseTest
    {
        public BaseTaskTest(ITestOutputHelper output)
            : base(output)
        {
        }

        protected override bool IsSystematicTest => true;

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
