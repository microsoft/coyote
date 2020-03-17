// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Production.Tests.Tasks
{
    public class TaskConfigureAwaitFalseTests : BaseProductionTest
    {
        public TaskConfigureAwaitFalseTests(ITestOutputHelper output)
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
            await Task.Delay(1).ConfigureAwait(false);
            entry.Value = value;
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestAwaitSynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await WriteAsync(entry, 5).ConfigureAwait(false);
            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestAwaitAsynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await WriteWithDelayAsync(entry, 5).ConfigureAwait(false);
            Assert.Equal(5, entry.Value);
        }

        private static async Task NestedWriteAsync(SharedEntry entry, int value)
        {
            await Task.CompletedTask;
            await WriteAsync(entry, value).ConfigureAwait(false);
        }

        private static async Task NestedWriteWithDelayAsync(SharedEntry entry, int value)
        {
            await Task.Delay(1).ConfigureAwait(false);
            await WriteWithDelayAsync(entry, value).ConfigureAwait(false);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestAwaitNestedSynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await NestedWriteAsync(entry, 5).ConfigureAwait(false);
            Assert.Equal(5, entry.Value);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestAwaitNestedAsynchronousTask()
        {
            SharedEntry entry = new SharedEntry();
            await NestedWriteWithDelayAsync(entry, 5).ConfigureAwait(false);
            Assert.Equal(5, entry.Value);
        }

        private static async Task<int> GetWriteResultAsync(SharedEntry entry, int value)
        {
            await Task.CompletedTask;
            entry.Value = value;
            return entry.Value;
        }

        private static async Task<int> GetWriteResultWithDelayAsync(SharedEntry entry, int value)
        {
            await Task.Delay(1).ConfigureAwait(false);
            entry.Value = value;
            return entry.Value;
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestAwaitSynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await GetWriteResultAsync(entry, 5).ConfigureAwait(false);
            Assert.Equal(5, value);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestAwaitAsynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await GetWriteResultWithDelayAsync(entry, 5).ConfigureAwait(false);
            Assert.Equal(5, value);
        }

        private static async Task<int> NestedGetWriteResultAsync(SharedEntry entry, int value)
        {
            await Task.CompletedTask;
            return await GetWriteResultAsync(entry, value).ConfigureAwait(false);
        }

        private static async Task<int> NestedGetWriteResultWithDelayAsync(SharedEntry entry, int value)
        {
            await Task.Delay(1);
            return await GetWriteResultWithDelayAsync(entry, value).ConfigureAwait(false);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestAwaitNestedSynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await NestedGetWriteResultAsync(entry, 5).ConfigureAwait(false);
            Assert.Equal(5, value);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestAwaitNestedAsynchronousTaskResult()
        {
            SharedEntry entry = new SharedEntry();
            int value = await NestedGetWriteResultWithDelayAsync(entry, 5).ConfigureAwait(false);
            Assert.Equal(5, value);
        }
    }
}
