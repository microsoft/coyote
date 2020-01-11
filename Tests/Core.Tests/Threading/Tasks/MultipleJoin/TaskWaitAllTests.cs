// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Tests.Common.Threading;
using Microsoft.Coyote.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Core.Tests.Threading.Tasks
{
    public class TaskWaitAllTests : BaseTest
    {
        public TaskWaitAllTests(ITestOutputHelper output)
            : base(output)
        {
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
        public void TestWaitAllWithTwoSynchronousTasks()
        {
            SharedEntry entry = new SharedEntry();
            ControlledTask task1 = WriteAsync(entry, 5);
            ControlledTask task2 = WriteAsync(entry, 3);
            ControlledTask.WaitAll(task1, task2);
            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.True(entry.Value == 5 || entry.Value == 3, $"Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoAsynchronousTasks()
        {
            SharedEntry entry = new SharedEntry();
            ControlledTask task1 = WriteWithDelayAsync(entry, 3);
            ControlledTask task2 = WriteWithDelayAsync(entry, 5);
            ControlledTask.WaitAll(task1, task2);
            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.True(entry.Value == 5 || entry.Value == 3, $"Found unexpected value.");
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoParallelTasks()
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

            ControlledTask.WaitAll(task1, task2);

            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
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
        public void TestWaitAllWithTwoSynchronousTaskResults()
        {
            ControlledTask<int> task1 = GetWriteResultAsync(5);
            ControlledTask<int> task2 = GetWriteResultAsync(3);
            ControlledTask.WaitAll(task1, task2);
            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.Equal(5, task1.Result);
            Assert.Equal(3, task2.Result);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoAsynchronousTaskResults()
        {
            ControlledTask<int> task1 = GetWriteResultWithDelayAsync(5);
            ControlledTask<int> task2 = GetWriteResultWithDelayAsync(3);
            ControlledTask.WaitAll(task1, task2);
            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.Equal(5, task1.Result);
            Assert.Equal(3, task2.Result);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoParallelSynchronousTaskResults()
        {
            ControlledTask<int> task1 = ControlledTask.Run(async () =>
            {
                return await GetWriteResultAsync(5);
            });

            ControlledTask<int> task2 = ControlledTask.Run(async () =>
            {
                return await GetWriteResultAsync(3);
            });

            ControlledTask.WaitAll(task1, task2);

            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.Equal(5, task1.Result);
            Assert.Equal(3, task2.Result);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoParallelAsynchronousTaskResults()
        {
            ControlledTask<int> task1 = ControlledTask.Run(async () =>
            {
                return await GetWriteResultWithDelayAsync(5);
            });

            ControlledTask<int> task2 = ControlledTask.Run(async () =>
            {
                return await GetWriteResultWithDelayAsync(3);
            });

            ControlledTask.WaitAll(task1, task2);

            Assert.True(task1.IsCompleted);
            Assert.True(task2.IsCompleted);
            Assert.Equal(5, task1.Result);
            Assert.Equal(3, task2.Result);
        }
    }
}
