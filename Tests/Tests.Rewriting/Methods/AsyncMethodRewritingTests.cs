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
        public async Task<int> TestRewritingGenericAsyncMethod()
        {
            return await Task.FromResult(1);
        }
    }
}
