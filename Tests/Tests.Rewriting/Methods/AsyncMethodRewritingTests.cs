// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class AsyncMethodRewritingTests : BaseRewritingTest
    {
        public AsyncMethodRewritingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public async Task TestRewritingAsyncMethod()
        {
            await Task.CompletedTask;
        }

        [Fact(Timeout = 5000)]
        public async Task TestRewritingGenericAsyncMethod()
        {
            Assert.Equal(1, await GetValueAsync());
        }

        private static async Task<int> GetValueAsync()
        {
            return await Task.FromResult(1);
        }
    }
}
