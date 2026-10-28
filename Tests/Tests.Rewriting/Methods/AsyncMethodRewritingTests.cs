// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
            Assert.Equal(1, await InvokeGenericTaskAsync());
        }

        [Fact(Timeout = 5000)]
        public async Task TestRewritingAsyncValueTaskMethod()
        {
            await InvokeValueTaskAsync();
        }

        [Fact(Timeout = 5000)]
        public async Task TestRewritingGenericAsyncValueTaskMethod()
        {
            Assert.Equal(1, await InvokeGenericValueTaskAsync());
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task TestRewritingAsyncValueTaskException(bool isGeneric)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                if (isGeneric)
                {
                    await InvokeGenericValueTaskAsync(fail: true);
                }
                else
                {
                    await InvokeValueTaskAsync(fail: true);
                }
            });
        }

        private static async Task<int> InvokeGenericTaskAsync() => await Task.FromResult(1);

        private static async ValueTask InvokeValueTaskAsync(bool fail = false)
        {
            await Task.Yield();
            if (fail)
            {
                throw new InvalidOperationException();
            }
        }

        private static async ValueTask<int> InvokeGenericValueTaskAsync(bool fail = false)
        {
            await Task.Yield();
            if (fail)
            {
                throw new InvalidOperationException();
            }

            return 1;
        }
    }
}
