// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET8_0_OR_GREATER
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests.SystematicFuzzing
{
    public class TaskWaitAsyncTests : BaseBugFindingTest
    {
        public TaskWaitAsyncTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private protected override SchedulingPolicy SchedulingPolicy => SchedulingPolicy.Fuzzing;

        protected override Configuration GetConfiguration()
        {
            return base.GetConfiguration().WithSystematicFuzzingEnabled();
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAsyncWithLongTimeoutAndConcurrentCompletion()
        {
            // Fuzzing executes in real time, so a timeout that cannot expire during the test
            // must not be replaced by a fuzzed delay that expires immediately.
            this.Test(async () =>
            {
                var tcs = new TaskCompletionSource<bool>();
                Task producer = Task.Run(() => tcs.SetResult(true));
                await tcs.Task.WaitAsync(TimeSpan.FromMinutes(10));
                await producer;

                var resultTcs = new TaskCompletionSource<int>();
                Task resultProducer = Task.Run(() => resultTcs.SetResult(7));
                Assert.Equal(7, await resultTcs.Task.WaitAsync(TimeSpan.FromMinutes(10), TimeProvider.System));
                await resultProducer;
            },
            configuration: this.GetConfiguration().WithTestingIterations(50));
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAsyncWithZeroTimeoutAndIncompleteTask()
        {
            this.Test(async () =>
            {
                var tcs = new TaskCompletionSource<bool>();
                await Assert.ThrowsAsync<TimeoutException>(() => tcs.Task.WaitAsync(TimeSpan.Zero));

                var resultTcs = new TaskCompletionSource<int>();
                await Assert.ThrowsAsync<TimeoutException>(() => resultTcs.Task.WaitAsync(TimeSpan.Zero));
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAsyncReturnsSourceResultWhenCanceledAfterSourceCompletes()
        {
            this.Test(async () =>
            {
                using var cts = new CancellationTokenSource();
                var source = new TaskCompletionSource<int>();
                Task<int> wait = source.Task.WaitAsync(cts.Token);
                source.SetResult(42);
                cts.Cancel();
                Assert.Equal(42, await wait);

                using var reverseCts = new CancellationTokenSource();
                var reverseSource = new TaskCompletionSource();
                Task reverseWait = reverseSource.Task.WaitAsync(reverseCts.Token);
                reverseCts.Cancel();
                reverseSource.SetResult();
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
            },
            configuration: this.GetConfiguration().WithTestingIterations(50));
        }
    }
}
#endif
