// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tasks;

namespace Microsoft.Coyote.Tests.Common.Tasks
{
    public class SharedEntry
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
