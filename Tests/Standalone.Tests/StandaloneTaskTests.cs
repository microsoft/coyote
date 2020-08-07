// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Xunit;

namespace Standalone.Tests
{
    public class StandaloneTaskTests
    {
        [Fact(Timeout=5000)]
        public async Task SimpleTaskTest()
        {
            int result = 0;
            await Task.Run(async () =>
            {
                await Task.Delay(10);
                result = 10;
            });

            Assert.True(result == 10, "result should be 10");
        }
    }
}
