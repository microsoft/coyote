// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class TaskAwaitTests : BaseSystematicTest
    {
        public TaskAwaitTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private static async Task WriteAsync(SharedEntry entry, int value)
        {
            await Task.CompletedTask;
            entry.Value = value;
        }

        private static async Task WriteWithDelayAsync(SharedEntry entry, int value)
        {
            await Task.Delay(1);
            entry.Value = value;
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitSynchronousTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await WriteAsync(entry, 5);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitSynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await WriteAsync(entry, 3);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitAsynchronousTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await WriteWithDelayAsync(entry, 5);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitAsynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await WriteWithDelayAsync(entry, 3);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private static async Task NestedWriteAsync(SharedEntry entry, int value)
        {
            await Task.CompletedTask;
            await WriteAsync(entry, value);
        }

        private static async Task NestedWriteWithDelayAsync(SharedEntry entry, int value)
        {
            await Task.Delay(1);
            await WriteWithDelayAsync(entry, value);
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedSynchronousTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await NestedWriteAsync(entry, 5);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedSynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await NestedWriteAsync(entry, 3);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedAsynchronousTask()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await NestedWriteWithDelayAsync(entry, 5);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedAsynchronousTaskFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await NestedWriteWithDelayAsync(entry, 3);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private static async Task<int> GetWriteResultAsync(SharedEntry entry, int value)
        {
            await Task.CompletedTask;
            entry.Value = value;
            return entry.Value;
        }

        private static async Task<int> GetWriteResultWithDelayAsync(SharedEntry entry, int value)
        {
            await Task.Delay(1);
            entry.Value = value;
            return entry.Value;
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitSynchronousTaskWithResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await GetWriteResultAsync(entry, 5);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitSynchronousTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await GetWriteResultAsync(entry, 3);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitAsynchronousTaskWithResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await GetWriteResultWithDelayAsync(entry, 5);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitAsynchronousTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await GetWriteResultWithDelayAsync(entry, 3);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private static async Task<int> NestedGetWriteResultAsync(SharedEntry entry, int value)
        {
            await Task.CompletedTask;
            return await GetWriteResultAsync(entry, value);
        }

        private static async Task<int> NestedGetWriteResultWithDelayAsync(SharedEntry entry, int value)
        {
            await Task.Delay(1);
            return await GetWriteResultWithDelayAsync(entry, value);
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedSynchronousTaskWithResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await NestedGetWriteResultAsync(entry, 5);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedSynchronousTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await NestedGetWriteResultAsync(entry, 3);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedAsynchronousTaskWithResult()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await NestedGetWriteResultWithDelayAsync(entry, 5);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAwaitNestedAsynchronousTaskWithResultFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                int value = await NestedGetWriteResultWithDelayAsync(entry, 3);
                Specification.Assert(value == 5, "Value is {0} instead of 5.", value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
    }
}
