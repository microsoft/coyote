// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Production.Tests.Tasks
{
    public class TaskWaitAnyTests : BaseProductionTest
    {
        public TaskWaitAnyTests(ITestOutputHelper output)
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
        public void TestWhenAnyWithTwoSynchronousTasks()
        {
            SharedEntry entry = new SharedEntry();
            Task task1 = WriteAsync(entry, 5);
            Task task2 = WriteAsync(entry, 3);
            Task.WaitAny(task1, task2);
            Assert.True(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
            Assert.True(entry.Value == 5 || entry.Value == 3, $"Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAnyWithTwoAsynchronousTasks()
        {
            SharedEntry entry = new SharedEntry();
            Task task1 = WriteWithDelayAsync(entry, 3);
            Task task2 = WriteWithDelayAsync(entry, 5);
            Task.WaitAny(task1, task2);
            Assert.True(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
            Assert.True(entry.Value == 5 || entry.Value == 3, $"Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAnyWithTwoParallelTasks()
        {
            SharedEntry entry = new SharedEntry();

            Task task1 = Task.Run(async () =>
            {
                await WriteAsync(entry, 3);
            });

            Task task2 = Task.Run(async () =>
            {
                await WriteAsync(entry, 5);
            });

            Task.WaitAny(task1, task2);

            Assert.True(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
            Assert.True(entry.Value == 5 || entry.Value == 3, $"Found unexpected value.");
        }

        private static async Task<int> GetWriteResultAsync(int value)
        {
            await Task.CompletedTask;
            return value;
        }

        private static async Task<int> GetWriteResultWithDelayAsync(int value)
        {
            await Task.Delay(1);
            return value;
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAnyWithTwoSynchronousTaskResults()
        {
            Task<int> task1 = GetWriteResultAsync(5);
            Task<int> task2 = GetWriteResultAsync(3);
            int index = Task.WaitAny(task1, task2);
            Assert.True(index >= 0, "Index is negative.");
            Assert.True(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
            Assert.True((index == 0 && task1.Result == 5) || (index == 1 && task2.Result == 3), $"Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAnyWithTwoAsynchronousTaskResults()
        {
            Task<int> task1 = GetWriteResultWithDelayAsync(5);
            Task<int> task2 = GetWriteResultWithDelayAsync(3);
            int index = Task.WaitAny(task1, task2);
            Assert.True(index >= 0, "Index is negative.");
            Assert.True(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
            Assert.True((index == 0 && task1.Result == 5) || (index == 1 && task2.Result == 3), $"Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAnyWithTwoParallelSynchronousTaskResults()
        {
            Task<int> task1 = Task.Run(async () =>
            {
                return await GetWriteResultAsync(5);
            });

            Task<int> task2 = Task.Run(async () =>
            {
                return await GetWriteResultAsync(3);
            });

            int index = Task.WaitAny(task1, task2);

            Assert.True(index >= 0, "Index is negative.");
            Assert.True(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
            Assert.True((index == 0 && task1.Result == 5) || (index == 1 && task2.Result == 3), $"Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAnyWithTwoParallelAsynchronousTaskResults()
        {
            Task<int> task1 = Task.Run(async () =>
            {
                return await GetWriteResultWithDelayAsync(5);
            });

            Task<int> task2 = Task.Run(async () =>
            {
                return await GetWriteResultWithDelayAsync(3);
            });

            int index = Task.WaitAny(task1, task2);

            Assert.True(index >= 0, "Index is negative.");
            Assert.True(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
            Assert.True((index == 0 && task1.Result == 5) || (index == 1 && task2.Result == 3), $"Found unexpected value.");
        }
    }
}
