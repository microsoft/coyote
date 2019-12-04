// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests.Threading.Tasks
{
    public class TaskWhenAllTests : BaseTest
    {
        public TaskWhenAllTests(ITestOutputHelper output)
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
        public async Task TestWhenAllWithTwoSynchronousTasks()
        {
            SharedEntry entry = new SharedEntry();
            ControlledTask task1 = WriteAsync(entry, 5);
            ControlledTask task2 = WriteAsync(entry, 3);
            await ControlledTask.WhenAll(task1, task2);
            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.True(entry.Value == 5 || entry.Value == 3, "Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public async Task TestWhenAllWithTwoAsynchronousTasks()
        {
            SharedEntry entry = new SharedEntry();
            ControlledTask task1 = WriteWithDelayAsync(entry, 3);
            ControlledTask task2 = WriteWithDelayAsync(entry, 5);
            await ControlledTask.WhenAll(task1, task2);
            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.True(entry.Value == 5 || entry.Value == 3, "Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public async Task TestWhenAllWithTwoParallelTasks()
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

            await ControlledTask.WhenAll(task1, task2);

            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.True(entry.Value == 5 || entry.Value == 3, "Found unexpected value.");
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
        public async Task TestWhenAllWithTwoSynchronousTaskResults()
        {
            ControlledTask<int> task1 = GetWriteResultAsync(5);
            ControlledTask<int> task2 = GetWriteResultAsync(3);
            int[] results = await ControlledTask.WhenAll(task1, task2);
            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.Equal(2, results.Length);
            Assert.Equal(5, results[0]);
            Assert.Equal(3, results[1]);
        }

        [Fact(Timeout = 5000)]
        public async Task TestWhenAllWithTwoAsynchronousTaskResults()
        {
            ControlledTask<int> task1 = GetWriteResultWithDelayAsync(5);
            ControlledTask<int> task2 = GetWriteResultWithDelayAsync(3);
            int[] results = await ControlledTask.WhenAll(task1, task2);
            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.Equal(2, results.Length);
            Assert.Equal(5, results[0]);
            Assert.Equal(3, results[1]);
        }

        [Fact(Timeout = 5000)]
        public async Task TestWhenAllWithTwoParallelSynchronousTaskResults()
        {
            ControlledTask<int> task1 = ControlledTask.Run(async () =>
            {
                return await GetWriteResultAsync(5);
            });

            ControlledTask<int> task2 = ControlledTask.Run(async () =>
            {
                return await GetWriteResultAsync(3);
            });

            int[] results = await ControlledTask.WhenAll(task1, task2);

            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.Equal(2, results.Length);
            Assert.Equal(5, results[0]);
            Assert.Equal(3, results[1]);
        }

        [Fact(Timeout = 5000)]
        public async Task TestWhenAllWithTwoParallelAsynchronousTaskResults()
        {
            ControlledTask<int> task1 = ControlledTask.Run(async () =>
            {
                return await GetWriteResultWithDelayAsync(5);
            });

            ControlledTask<int> task2 = ControlledTask.Run(async () =>
            {
                return await GetWriteResultWithDelayAsync(3);
            });

            int[] results = await ControlledTask.WhenAll(task1, task2);

            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.Equal(2, results.Length);
            Assert.Equal(5, results[0]);
            Assert.Equal(3, results[1]);
        }

        [Fact(Timeout = 5000)]
        public async Task TestWhenAllWithException()
        {
            SharedEntry entry = new SharedEntry();

            ControlledTask task1 = ControlledTask.Run(async () =>
            {
                await WriteAsync(entry, 3);
                throw new InvalidOperationException();
            });

            ControlledTask task2 = ControlledTask.Run(async () =>
            {
                await WriteAsync(entry, 5);
                throw new InvalidOperationException();
            });

            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await ControlledTask.WhenAll(task1, task2);
            });
        }
    }
}
