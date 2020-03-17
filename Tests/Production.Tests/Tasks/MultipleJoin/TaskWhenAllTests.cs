// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Production.Tests.Tasks
{
    public class TaskWhenAllTests : BaseProductionTest
    {
        public TaskWhenAllTests(ITestOutputHelper output)
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
        public async SystemTasks.Task TestWhenAllWithTwoSynchronousTasks()
        {
            SharedEntry entry = new SharedEntry();
            Task task1 = WriteAsync(entry, 5);
            Task task2 = WriteAsync(entry, 3);
            await Task.WhenAll(task1, task2);
            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.True(entry.Value == 5 || entry.Value == 3, "Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestWhenAllWithTwoAsynchronousTasks()
        {
            SharedEntry entry = new SharedEntry();
            Task task1 = WriteWithDelayAsync(entry, 3);
            Task task2 = WriteWithDelayAsync(entry, 5);
            await Task.WhenAll(task1, task2);
            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.True(entry.Value == 5 || entry.Value == 3, "Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestWhenAllWithTwoParallelTasks()
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

            await Task.WhenAll(task1, task2);

            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.True(entry.Value == 5 || entry.Value == 3, "Found unexpected value.");
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
        public async SystemTasks.Task TestWhenAllWithTwoSynchronousTaskResults()
        {
            Task<int> task1 = GetWriteResultAsync(5);
            Task<int> task2 = GetWriteResultAsync(3);
            int[] results = await Task.WhenAll(task1, task2);
            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.Equal(2, results.Length);
            Assert.Equal(5, results[0]);
            Assert.Equal(3, results[1]);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestWhenAllWithTwoAsynchronousTaskResults()
        {
            Task<int> task1 = GetWriteResultWithDelayAsync(5);
            Task<int> task2 = GetWriteResultWithDelayAsync(3);
            int[] results = await Task.WhenAll(task1, task2);
            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.Equal(2, results.Length);
            Assert.Equal(5, results[0]);
            Assert.Equal(3, results[1]);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestWhenAllWithTwoParallelSynchronousTaskResults()
        {
            Task<int> task1 = Task.Run(async () =>
            {
                return await GetWriteResultAsync(5);
            });

            Task<int> task2 = Task.Run(async () =>
            {
                return await GetWriteResultAsync(3);
            });

            int[] results = await Task.WhenAll(task1, task2);

            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.Equal(2, results.Length);
            Assert.Equal(5, results[0]);
            Assert.Equal(3, results[1]);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestWhenAllWithTwoParallelAsynchronousTaskResults()
        {
            Task<int> task1 = Task.Run(async () =>
            {
                return await GetWriteResultWithDelayAsync(5);
            });

            Task<int> task2 = Task.Run(async () =>
            {
                return await GetWriteResultWithDelayAsync(3);
            });

            int[] results = await Task.WhenAll(task1, task2);

            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.Equal(2, results.Length);
            Assert.Equal(5, results[0]);
            Assert.Equal(3, results[1]);
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestWhenAllWithException()
        {
            SharedEntry entry = new SharedEntry();

            Task task1 = Task.Run(async () =>
            {
                await WriteAsync(entry, 3);
                throw new InvalidOperationException();
            });

            Task task2 = Task.Run(async () =>
            {
                await WriteAsync(entry, 5);
                throw new InvalidOperationException();
            });

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await Task.WhenAll(task1, task2);
            });
        }
    }
}
