// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Threading.Tasks;
using Microsoft.Coyote.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests
{
    public class TaskAwaitTest : BaseTest
    {
        public TaskAwaitTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SharedEntry
        {
            public volatile int Value = 0;
        }

        private static async ControlledTask WriteAsync(SharedEntry entry, int value)
        {
            await ControlledTask.CompletedTask;
            entry.Value = value;
        }

        private static async ControlledTask WriteWithDelayAsync(SharedEntry entry, int value)
        {
            await ControlledTask.Delay(1);
            entry.Value = value;
        }

        [Fact(Timeout = 5000)]
        public async Task TestAwaitSynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await WriteAsync(entry, 5);
            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestAwaitAsynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await WriteWithDelayAsync(entry, 5);
            Assert.Equal(5, entry.Value);
        }

        private static async ControlledTask NestedWriteAsync(SharedEntry entry, int value)
        {
            await ControlledTask.CompletedTask;
            await WriteAsync(entry, value);
        }

        private static async ControlledTask NestedWriteWithDelayAsync(SharedEntry entry, int value)
        {
            await ControlledTask.Delay(1);
            await WriteWithDelayAsync(entry, value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestAwaitNestedSynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await NestedWriteAsync(entry, 5);
            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestAwaitNestedAsynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await NestedWriteWithDelayAsync(entry, 5);
            Assert.Equal(5, entry.Value);
        }

        private static async ControlledTask<int> GetWriteResultAsync(SharedEntry entry, int value)
        {
            await ControlledTask.CompletedTask;
            entry.Value = value;
            return entry.Value;
        }

        private static async ControlledTask<int> GetWriteResultWithDelayAsync(SharedEntry entry, int value)
        {
            await ControlledTask.Delay(1);
            entry.Value = value;
            return entry.Value;
        }

        [Fact(Timeout = 5000)]
        public async Task TestAwaitSynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await GetWriteResultAsync(entry, 5);
            Assert.Equal(5, value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestAwaitAsynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await GetWriteResultWithDelayAsync(entry, 5);
            Assert.Equal(5, value);
        }

        private static async ControlledTask<int> NestedGetWriteResultAsync(SharedEntry entry, int value)
        {
            await ControlledTask.CompletedTask;
            return await GetWriteResultAsync(entry, value);
        }

        private static async ControlledTask<int> NestedGetWriteResultWithDelayAsync(SharedEntry entry, int value)
        {
            await ControlledTask.Delay(1);
            return await GetWriteResultWithDelayAsync(entry, value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestAwaitNestedSynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await NestedGetWriteResultAsync(entry, 5);
            Assert.Equal(5, value);
        }

        [Fact(Timeout = 5000)]
        public async Task TestAwaitNestedAsynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await NestedGetWriteResultWithDelayAsync(entry, 5);
            Assert.Equal(5, value);
        }
    }
}
