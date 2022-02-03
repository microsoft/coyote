// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class ValueTaskTests : BaseBugFindingTest
    {
        public ValueTaskTests(ITestOutputHelper output)
            : base(output)
        {
        }

#if NET
        private static async ValueTask WriteAsync(SharedEntry entry, int value)
        {
            await ValueTask.CompletedTask;
            entry.Value = value;
        }
#endif

        private static async ValueTask WriteWithDelayAsync(SharedEntry entry, int value)
        {
            await Task.Delay(1);
            entry.Value = value;
        }

#if NET
        [Fact(Timeout = 5000)]
        public void TestAwaitSynchronousValueTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await WriteAsync(entry, 5);
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitSynchronousValueTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await WriteAsync(entry, 3);
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
#endif

        [Fact(Timeout = 5000)]
        public void TestAwaitAsynchronousValueTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await WriteWithDelayAsync(entry, 5);
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitAsynchronousValueTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await WriteWithDelayAsync(entry, 3);
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

#if NET
        private static async ValueTask NestedWriteAsync(SharedEntry entry, int value)
        {
            await ValueTask.CompletedTask;
            await WriteAsync(entry, value);
        }
#endif

        private static async ValueTask NestedWriteWithDelayAsync(SharedEntry entry, int value)
        {
            await Task.Delay(1);
            await WriteWithDelayAsync(entry, value);
        }

#if NET
        [Fact(Timeout = 5000)]
        public void TestAwaitNestedSynchronousValueTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await NestedWriteAsync(entry, 5);
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedSynchronousValueTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await NestedWriteAsync(entry, 3);
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
#endif

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedAsynchronousValueTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await NestedWriteWithDelayAsync(entry, 5);
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedAsynchronousValueTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await NestedWriteWithDelayAsync(entry, 3);
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

#if NET
        [Fact(Timeout = 5000)]
        public void TestAwaitSynchronousValueTaskWithResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await entry.GetWriteResultAsync(5);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitSynchronousValueTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await entry.GetWriteResultAsync(3);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
#endif

        [Fact(Timeout = 5000)]
        public void TestAwaitAsynchronousValueTaskWithResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await entry.GetWriteResultWithDelayAsync(5);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitAsynchronousValueTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await entry.GetWriteResultWithDelayAsync(3);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

#if NET
        private static async ValueTask<int> NestedGetWriteResultAsync(SharedEntry entry, int value)
        {
            await ValueTask.CompletedTask;
            return await entry.GetWriteResultAsync(value);
        }
#endif

        private static async ValueTask<int> NestedGetWriteResultWithDelayAsync(SharedEntry entry, int value)
        {
            await Task.Delay(1);
            return await entry.GetWriteResultWithDelayAsync(value);
        }

#if NET
        [Fact(Timeout = 5000)]
        public void TestAwaitNestedSynchronousValueTaskWithResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await NestedGetWriteResultAsync(entry, 5);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedSynchronousValueTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await NestedGetWriteResultAsync(entry, 3);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
#endif

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedAsynchronousValueTaskWithResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await NestedGetWriteResultWithDelayAsync(entry, 5);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedAsynchronousValueTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await NestedGetWriteResultWithDelayAsync(entry, 3);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

#if NET
        private static async ValueTask<int> ConvertedGetWriteResultAsync(SharedEntry entry, int value) =>
            await NestedGetWriteResultAsync(entry, value).AsTask();
#endif

        private static async ValueTask<int> ConvertedGetWriteResultWithDelayAsync(SharedEntry entry, int value) =>
            await NestedGetWriteResultWithDelayAsync(entry, value).AsTask();

#if NET
        [Fact(Timeout = 5000)]
        public void TestAwaitConvertedSynchronousValueTaskWithResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = NestedGetWriteResultAsync(entry, 5).AsTask();
                await task;
                Specification.Assert(task.Result == 5, "Value is {0} instead of 5.", task.Result);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitConvertedSynchronousValueTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = NestedGetWriteResultAsync(entry, 3).AsTask();
                await task;
                Specification.Assert(task.Result == 5, "Value is {0} instead of 5.", task.Result);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
#endif

        [Fact(Timeout = 5000)]
        public void TestAwaitConvertedAsynchronousValueTaskWithResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = NestedGetWriteResultWithDelayAsync(entry, 5).AsTask();
                await task;
                Specification.Assert(task.Result == 5, "Value is {0} instead of 5.", task.Result);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitConvertedAsynchronousValueTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                var task = NestedGetWriteResultWithDelayAsync(entry, 3).AsTask();
                await task;
                Specification.Assert(task.Result == 5, "Value is {0} instead of 5.", task.Result);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
    }
}
