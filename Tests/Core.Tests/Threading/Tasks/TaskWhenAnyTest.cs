// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests
{
    public class TaskWhenAnyTest : BaseTest
    {
        public TaskWhenAnyTest(ITestOutputHelper output)
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
        public async Task TestWhenAnyWithTwoSynchronousTasks()
        {
            SharedEntry entry = new SharedEntry();
            ControlledTask task1 = WriteAsync(entry, 5);
            ControlledTask task2 = WriteAsync(entry, 3);
            await ControlledTask.WhenAny(task1, task2);
            Assert.True(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
            Assert.True(entry.Value == 5 || entry.Value == 3, $"Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public async Task TestWhenAnyWithTwoAsynchronousTasks()
        {
            SharedEntry entry = new SharedEntry();
            ControlledTask task1 = WriteWithDelayAsync(entry, 3);
            ControlledTask task2 = WriteWithDelayAsync(entry, 5);
            await ControlledTask.WhenAny(task1, task2);
            Assert.True(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
            Assert.True(entry.Value == 5 || entry.Value == 3, $"Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public async Task TestWhenAnyWithTwoParallelTasks()
        {
            SharedEntry entry = new SharedEntry();

            ControlledTask task1 = ControlledTask.Run(async () =>
            {
                await WriteAsync(entry, 3);
            });

            ControlledTask task2 = ControlledTask.Run(async () =>
            {
                await WriteAsync(entry, 5);
            });

            await ControlledTask.WhenAny(task1, task2);

            Assert.True(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
            Assert.True(entry.Value == 5 || entry.Value == 3, $"Found unexpected value.");
        }

        private static async ControlledTask<int> GetWriteResultAsync(int value)
        {
            await ControlledTask.CompletedTask;
            return value;
        }

        private static async ControlledTask<int> GetWriteResultWithDelayAsync(int value)
        {
            await ControlledTask.Delay(1);
            return value;
        }

        [Fact(Timeout = 5000)]
        public async Task TestWhenAnyWithTwoSynchronousTaskResults()
        {
            ControlledTask<int> task1 = GetWriteResultAsync(5);
            ControlledTask<int> task2 = GetWriteResultAsync(3);
            Task<int> result = await ControlledTask.WhenAny(task1, task2);
            Assert.True(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
            Assert.True(result.Result == 5 || result.Result == 3, $"Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public async Task TestWhenAnyWithTwoAsynchronousTaskResults()
        {
            ControlledTask<int> task1 = GetWriteResultWithDelayAsync(5);
            ControlledTask<int> task2 = GetWriteResultWithDelayAsync(3);
            Task<int> result = await ControlledTask.WhenAny(task1, task2);
            Assert.True(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
            Assert.True(result.Result == 5 || result.Result == 3, $"Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public async Task TestWhenAnyWithTwoParallelSynchronousTaskResults()
        {
            ControlledTask<int> task1 = ControlledTask.Run(async () =>
            {
                return await GetWriteResultAsync(5);
            });

            ControlledTask<int> task2 = ControlledTask.Run(async () =>
            {
                return await GetWriteResultAsync(3);
            });

            Task<int> result = await ControlledTask.WhenAny(task1, task2);

            Assert.True(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
            Assert.True(result.Result == 5 || result.Result == 3, $"Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public async Task TestWhenAnyWithTwoParallelAsynchronousTaskResults()
        {
            ControlledTask<int> task1 = ControlledTask.Run(async () =>
            {
                return await GetWriteResultWithDelayAsync(5);
            });

            ControlledTask<int> task2 = ControlledTask.Run(async () =>
            {
                return await GetWriteResultWithDelayAsync(3);
            });

            Task<int> result = await ControlledTask.WhenAny(task1, task2);

            Assert.True(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
            Assert.True(result.Result == 5 || result.Result == 3, $"Found unexpected value.");
        }
    }
}
