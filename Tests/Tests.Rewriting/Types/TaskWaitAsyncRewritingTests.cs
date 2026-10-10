// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET8_0_OR_GREATER
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Rewriting.Tests
{
    public class TaskWaitAsyncRewritingTests : BaseRewritingTest
    {
        public TaskWaitAsyncRewritingTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingNonGenericTaskWaitAsyncOverloads()
        {
            this.Test(async () =>
            {
                using var source = new CancellationTokenSource();
                Task completed = Task.CompletedTask;
                await completed.WaitAsync(source.Token);
                await completed.WaitAsync(TimeSpan.Zero);
                await completed.WaitAsync(TimeSpan.Zero, source.Token);
                await completed.WaitAsync(TimeSpan.Zero, TimeProvider.System);
                await completed.WaitAsync(TimeSpan.Zero, TimeProvider.System, source.Token);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingGenericTaskWaitAsyncOverloads()
        {
            this.Test(async () =>
            {
                using var source = new CancellationTokenSource();
                Task<int> completed = Task.FromResult(37);
                Assert.Equal(37, await completed.WaitAsync(source.Token));
                Assert.Equal(37, await completed.WaitAsync(TimeSpan.Zero));
                Assert.Equal(37, await completed.WaitAsync(TimeSpan.Zero, source.Token));
                Assert.Equal(37, await completed.WaitAsync(TimeSpan.Zero, TimeProvider.System));
                Assert.Equal(37, await completed.WaitAsync(TimeSpan.Zero, TimeProvider.System, source.Token));
            });
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWaitAsyncTimeoutAndCancellation()
        {
            this.Test(async () =>
            {
                var pending = new TaskCompletionSource<bool>();
                await Assert.ThrowsAsync<TimeoutException>(() => pending.Task.WaitAsync(TimeSpan.Zero));

                using var source = new CancellationTokenSource();
                source.Cancel();
                await Assert.ThrowsAnyAsync<OperationCanceledException>(() => pending.Task.WaitAsync(source.Token));
                Assert.Throws<ArgumentOutOfRangeException>(() =>
                {
                    _ = pending.Task.WaitAsync(TimeSpan.FromMilliseconds(-2), TimeProvider.System);
                });
            });
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWaitAsyncPropagatesCompletionAndFaults()
        {
            this.Test(async () =>
            {
                Assert.Equal(17, await Task.FromResult(17).WaitAsync(
                    TimeSpan.FromMilliseconds(10), TimeProvider.System));

                var error = new InvalidOperationException("expected");
                await Assert.ThrowsAsync<InvalidOperationException>(() =>
                    Task.FromException(error).WaitAsync(TimeSpan.FromMilliseconds(1), TimeProvider.System));
            });
        }

        [Fact(Timeout = 5000)]
        public void TestRewritingTaskWaitAsyncPreservesFirstCompletionEvent()
        {
            this.Test(async () =>
            {
                using var cts = new CancellationTokenSource();
                var source = new TaskCompletionSource<int>();
                Task<int> wait = source.Task.WaitAsync(cts.Token);
                source.SetResult(42);
                cts.Cancel();
                Assert.Equal(42, await wait);

                using var nonGenericCts = new CancellationTokenSource();
                var nonGenericSource = new TaskCompletionSource();
                Task nonGenericWait = nonGenericSource.Task.WaitAsync(nonGenericCts.Token);
                nonGenericSource.SetResult();
                nonGenericCts.Cancel();
                await nonGenericWait;

                using var reverseCts = new CancellationTokenSource();
                var reverseSource = new TaskCompletionSource<int>();
                Task<int> reverseWait = reverseSource.Task.WaitAsync(reverseCts.Token);
                reverseCts.Cancel();
                reverseSource.SetResult(42);
                OperationCanceledException canceled = null;
                try
                {
                    await reverseWait;
                }
                catch (OperationCanceledException ex)
                {
                    canceled = ex;
                }

                Assert.NotNull(canceled);
                Assert.Equal(reverseCts.Token, canceled.CancellationToken);
            });
        }
    }
}
#endif
