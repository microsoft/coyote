// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class ThreadPoolTests : BaseBugFindingTest
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

        private static async Task ThrowExceptionAsync()
        {
            await Task.Delay(1);
            throw new InvalidOperationException();
        }

        [Fact(Skip="todo", Timeout = 5000)]
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
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Skip="todo", Timeout = 5000)]
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
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Skip="todo", Timeout = 5000)]
        public void TestQueueUserWorkItemWithException()
        {
            this.TestWithError(async () =>
            {
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    ThrowException<InvalidOperationException>();
                }, null);

                await Task.Delay(10);
            },
            configuration: this.GetConfiguration().WithTestingIterations(10),
            errorChecker: (e) =>
            {
                Assert.StartsWith("Unhandled exception 'System.InvalidOperationException'", e);
            },
            replay: true);
        }

        [Fact(Skip="todo", Timeout = 5000)]
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
            configuration: this.GetConfiguration().WithTestingIterations(10),
            replay: true);
        }

        [Fact(Skip="todo", Timeout = 5000)]
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
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Skip="todo", Timeout = 5000)]
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
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Skip="todo", Timeout = 5000)]
        public void TestUnsafeQueueUserWorkItemWithException()
        {
            this.TestWithError(() =>
            {
                ThreadPool.UnsafeQueueUserWorkItem(_ =>
                {
                    ThrowException<InvalidOperationException>();
                }, null);
            },
            configuration: this.GetConfiguration().WithTestingIterations(10),
            errorChecker: (e) =>
            {
                Assert.StartsWith("Unhandled exception 'System.InvalidOperationException'", e);
            },
            replay: true);
        }

        [Fact(Skip="todo", Timeout = 5000)]
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
            configuration: this.GetConfiguration().WithTestingIterations(10),
            replay: true);
        }
    }
}
