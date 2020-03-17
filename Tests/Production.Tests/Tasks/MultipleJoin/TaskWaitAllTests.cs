// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Production.Tests.Tasks
{
    public class TaskWaitAllTests : BaseProductionTest
    {
        public TaskWaitAllTests(ITestOutputHelper output)
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
        public void TestWaitAllWithTwoSynchronousTasks()
        {
            SharedEntry entry = new SharedEntry();
            Task task1 = WriteAsync(entry, 5);
            Task task2 = WriteAsync(entry, 3);
            Task.WaitAll(task1, task2);
            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.True(entry.Value == 5 || entry.Value == 3, $"Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoAsynchronousTasks()
        {
            SharedEntry entry = new SharedEntry();
            Task task1 = WriteWithDelayAsync(entry, 3);
            Task task2 = WriteWithDelayAsync(entry, 5);
            Task.WaitAll(task1, task2);
            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.True(entry.Value == 5 || entry.Value == 3, $"Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoParallelTasks()
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

            Task.WaitAll(task1, task2);

            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
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
        public void TestWaitAllWithTwoSynchronousTaskResults()
        {
            Task<int> task1 = GetWriteResultAsync(5);
            Task<int> task2 = GetWriteResultAsync(3);
            Task.WaitAll(task1, task2);
            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.Equal(5, task1.Result);
            Assert.Equal(3, task2.Result);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoAsynchronousTaskResults()
        {
            Task<int> task1 = GetWriteResultWithDelayAsync(5);
            Task<int> task2 = GetWriteResultWithDelayAsync(3);
            Task.WaitAll(task1, task2);
            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.Equal(5, task1.Result);
            Assert.Equal(3, task2.Result);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoParallelSynchronousTaskResults()
        {
            Task<int> task1 = Task.Run(async () =>
            {
                return await GetWriteResultAsync(5);
            });

            Task<int> task2 = Task.Run(async () =>
            {
                return await GetWriteResultAsync(3);
            });

            Task.WaitAll(task1, task2);

            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.Equal(5, task1.Result);
            Assert.Equal(3, task2.Result);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoParallelAsynchronousTaskResults()
        {
            Task<int> task1 = Task.Run(async () =>
            {
                return await GetWriteResultWithDelayAsync(5);
            });

            Task<int> task2 = Task.Run(async () =>
            {
                return await GetWriteResultWithDelayAsync(3);
            });

            Task.WaitAll(task1, task2);

            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.Equal(5, task1.Result);
            Assert.Equal(3, task2.Result);
        }
    }
}
