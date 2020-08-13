// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BinaryRewriting.Tests.Tasks
{
    public class ThreadPoolTests : BaseProductionTest
    {
        public ThreadPoolTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private static void SetResult(TaskCompletionSource<int> tcs) => tcs.SetResult(7);

        private static async Task SetResultAsync(TaskCompletionSource<int> tcs)
        {
            await Task.Delay(1);
            tcs.SetResult(7);
        }

        private static void ThrowException() => throw new InvalidOperationException();

        private static async Task ThrowExceptionAsync()
        {
            await Task.Delay(1);
            throw new InvalidOperationException();
        }

        [Fact(Timeout = 5000)]
        public void TestQueueUserWorkItem()
        {
            this.Test(async () =>
            {
                var tcs = new TaskCompletionSource<int>();
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    SetResult(tcs);
                }, null);

                int result = await tcs.Task;
                Specification.Assert(result is 7, "Found unexpected value {0}.", result);
            },
            configuration: GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestQueueUserWorkItemAsync()
        {
            this.Test(async () =>
            {
                var tcs = new TaskCompletionSource<int>();
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    _ = SetResultAsync(tcs);
                }, null);

                int result = await tcs.Task;
                Specification.Assert(result is 7, "Found unexpected value {0}.", result);
            },
            configuration: GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestQueueUserWorkItemWithException()
        {
            if (!this.IsSystematicTest)
            {
                // production version of this test results in an unhandled exception.
                // bugbug: can we rewrite this test so it works both in production and systematic testing modes?
                // TestQueueUserWorkItemWithAsyncException makes a lot more sense to me.
                return;
            }

            this.TestWithError(async () =>
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    ThrowException();
                }, null);

                await Task.Delay(10);
            },
            configuration: GetConfiguration().WithTestingIterations(10),
            errorChecker: (e) =>
            {
                Assert.True(e.StartsWith("Unhandled exception. System.InvalidOperationException"),
                    "Expected 'InvalidOperationException', but found error: " + e);
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestQueueUserWorkItemWithAsyncException()
        {
            this.TestWithException<InvalidOperationException>(async () =>
            {
                var tcs = new TaskCompletionSource<Task>();
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    // The exception will be consumed, because this method cannot await the task,
                    // so we set the task returned by `ThrowExceptionAsync` to be awaited outside
                    // and propagate the exception.
                    var task = ThrowExceptionAsync();
                    tcs.SetResult(task);
                }, null);

                Task taskResult = await tcs.Task;
                await taskResult;
            },
            configuration: GetConfiguration().WithTestingIterations(10),
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestUnsafeQueueUserWorkItem()
        {
            this.Test(async () =>
            {
                var tcs = new TaskCompletionSource<int>();
                ThreadPool.UnsafeQueueUserWorkItem(_ =>
                {
                    SetResult(tcs);
                }, null);

                int result = await tcs.Task;
                Specification.Assert(result is 7, "Found unexpected value {0}.", result);
            },
            configuration: GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestUnsafeQueueUserWorkItemAsync()
        {
            this.Test(async () =>
            {
                var tcs = new TaskCompletionSource<int>();
                ThreadPool.UnsafeQueueUserWorkItem(_ =>
                {
                    _ = SetResultAsync(tcs);
                }, null);

                int result = await tcs.Task;
                Specification.Assert(result is 7, "Found unexpected value {0}.", result);
            },
            configuration: GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestUnsafeQueueUserWorkItemWithException()
        {
            if (!this.IsSystematicTest)
            {
                // production version of this test results in an unhandled exception.
                // bugbug: can we rewrite this test so it works both in production and systematic testing modes?
                // TestUnsafeQueueUserWorkItemWithAsyncException makes a lot more sense to me.
                return;
            }

            this.TestWithError(() =>
            {
                ThreadPool.UnsafeQueueUserWorkItem(_ =>
                {
                    ThrowException();
                }, null);
            },
            configuration: GetConfiguration().WithTestingIterations(10),
            errorChecker: (e) =>
            {
                Assert.True(e.StartsWith("Unhandled exception. System.InvalidOperationException"),
                    "Expected 'InvalidOperationException', but found error: " + e);
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestUnsafeQueueUserWorkItemWithAsyncException()
        {
            this.TestWithException<InvalidOperationException>(async () =>
            {
                var tcs = new TaskCompletionSource<Task>();
                ThreadPool.UnsafeQueueUserWorkItem(_ =>
                {
                    // The exception will be consumed, because this method cannot await the task,
                    // so we set the task returned by `ThrowExceptionAsync` to be awaited outside
                    // and propagate the exception.
                    var task = ThrowExceptionAsync();
                    tcs.SetResult(task);
                }, null);

                Task taskResult = await tcs.Task;
                await taskResult;
            },
            configuration: GetConfiguration().WithTestingIterations(10),
            replay: true);
        }
    }
}
