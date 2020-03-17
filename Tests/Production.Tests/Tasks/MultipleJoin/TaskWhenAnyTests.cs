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
    public class TaskWhenAnyTests : BaseProductionTest
    {
        public TaskWhenAnyTests(ITestOutputHelper output)
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
        public async SystemTasks.Task TestWhenAnyWithTwoSynchronousTasks()
        {
            SharedEntry entry = new SharedEntry();
            Task task1 = WriteAsync(entry, 5);
            Task task2 = WriteAsync(entry, 3);
            Task result = await Task.WhenAny(task1, task2);
            Assert.True(result.IsCompleted, "No task has completed.");
            Assert.True(entry.Value == 5 || entry.Value == 3, "Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestWhenAnyWithTwoAsynchronousTasks()
        {
            SharedEntry entry = new SharedEntry();
            Task task1 = WriteWithDelayAsync(entry, 3);
            Task task2 = WriteWithDelayAsync(entry, 5);
            Task result = await Task.WhenAny(task1, task2);
            Assert.True(result.IsCompleted, "No task has completed.");
            Assert.True(entry.Value == 5 || entry.Value == 3, "Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestWhenAnyWithTwoParallelTasks()
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

            Task result = await Task.WhenAny(task1, task2);

            Assert.True(result.IsCompleted, "No task has completed.");
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
        public async SystemTasks.Task TestWhenAnyWithTwoSynchronousTaskResults()
        {
            Task<int> task1 = GetWriteResultAsync(5);
            Task<int> task2 = GetWriteResultAsync(3);
            Task<int> result = await Task.WhenAny(task1, task2);
            Assert.True(result.IsCompleted, "No task has completed.");
            Assert.True(
                (result.Id == task1.Id && result.Result == 5) ||
                (result.Id == task2.Id && result.Result == 3),
                "Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestWhenAnyWithTwoAsynchronousTaskResults()
        {
            Task<int> task1 = GetWriteResultWithDelayAsync(5);
            Task<int> task2 = GetWriteResultWithDelayAsync(3);
            Task<int> result = await Task.WhenAny(task1, task2);
            Assert.True(result.IsCompleted, "No task has completed.");
            Assert.True(
                (result.Id == task1.Id && result.Result == 5) ||
                (result.Id == task2.Id && result.Result == 3),
                "Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestWhenAnyWithTwoParallelSynchronousTaskResults()
        {
            Task<int> task1 = Task.Run(async () =>
            {
                return await GetWriteResultAsync(5);
            });

            Task<int> task2 = Task.Run(async () =>
            {
                return await GetWriteResultAsync(3);
            });

            Task<int> result = await Task.WhenAny(task1, task2);

            Assert.True(result.IsCompleted, "No task has completed.");
            Assert.True(
                (result.Id == task1.Id && result.Result == 5) ||
                (result.Id == task2.Id && result.Result == 3),
                "Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestWhenAnyWithTwoParallelAsynchronousTaskResults()
        {
            Task<int> task1 = Task.Run(async () =>
            {
                return await GetWriteResultWithDelayAsync(5);
            });

            Task<int> task2 = Task.Run(async () =>
            {
                return await GetWriteResultWithDelayAsync(3);
            });

            Task<int> result = await Task.WhenAny(task1, task2);

            Assert.True(result.IsCompleted, "No task has completed.");
            Assert.True(
                (result.Id == task1.Id && result.Result == 5) ||
                (result.Id == task2.Id && result.Result == 3),
                "Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public async SystemTasks.Task TestWhenAnyWithException()
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

            Task result = await Task.WhenAny(task1, task2);

            Assert.True(result.IsFaulted, "No task has faulted.");
            Assert.IsType<InvalidOperationException>(result.Exception.InnerException);
        }
    }
}
