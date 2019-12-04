// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Threading.Tasks
{
    public class TaskWaitAnyTests : BaseTest
    {
        public TaskWaitAnyTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SharedEntry
        {
            public int Value = 0;
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
        public void TestWaitAnyWithTwoSynchronousTasks()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();
                ControlledTask task1 = WriteAsync(entry, 5);
                ControlledTask task2 = WriteAsync(entry, 3);
                int index = ControlledTask.WaitAny(task1, task2);
                Specification.Assert(index == 0 || index == 1, $"Index is {index}.");
                Specification.Assert(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
                Specification.Assert((task1.IsCompleted && !task2.IsCompleted) || (!task1.IsCompleted && task2.IsCompleted),
                    "Both task have completed.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Both task have completed.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAnyWithTwoAsynchronousTasks()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();
                ControlledTask task1 = WriteWithDelayAsync(entry, 3);
                ControlledTask task2 = WriteWithDelayAsync(entry, 5);
                int index = ControlledTask.WaitAny(task1, task2);
                Specification.Assert(index == 0 || index == 1, $"Index is {index}.");
                Specification.Assert(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
                Specification.Assert(task1.IsCompleted && task2.IsCompleted, "One task has not completed.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "One task has not completed.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAnyWithTwoParallelTasks()
        {
            this.TestWithError(() =>
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

                int index = ControlledTask.WaitAny(task1, task2);

                Specification.Assert(index == 0 || index == 1, $"Index is {index}.");
                Specification.Assert(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
                Specification.Assert(task1.IsCompleted && task2.IsCompleted, "One task has not completed.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "One task has not completed.",
            replay: true);
        }

        private static async ControlledTask<int> GetWriteResultAsync(SharedEntry entry, int value)
        {
            entry.Value = value;
            await ControlledTask.CompletedTask;
            return entry.Value;
        }

        private static async ControlledTask<int> GetWriteResultWithDelayAsync(SharedEntry entry, int value)
        {
            entry.Value = value;
            await ControlledTask.Delay(1);
            return entry.Value;
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAnyWithTwoSynchronousTaskWithResults()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();
                ControlledTask<int> task1 = GetWriteResultAsync(entry, 5);
                ControlledTask<int> task2 = GetWriteResultAsync(entry, 3);
                int index = ControlledTask.WaitAny(task1, task2);
                Task<int> result = index == 0 ? task1.AwaiterTask : task2.AwaiterTask;
                Specification.Assert(index == 0 || index == 1, $"Index is {index}.");
                Specification.Assert(result.Result == 5 || result.Result == 3, "Found unexpected value.");
                Specification.Assert((task1.IsCompleted && !task2.IsCompleted) || (!task1.IsCompleted && task2.IsCompleted),
                    "Both task have completed.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Both task have completed.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAnyWithTwoAsynchronousTaskWithResults()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();
                ControlledTask<int> task1 = GetWriteResultWithDelayAsync(entry, 5);
                ControlledTask<int> task2 = GetWriteResultWithDelayAsync(entry, 3);
                int index = ControlledTask.WaitAny(task1, task2);
                Task<int> result = index == 0 ? task1.AwaiterTask : task2.AwaiterTask;
                Specification.Assert(index == 0 || index == 1, $"Index is {index}.");
                Specification.Assert(result.Result == 5 || result.Result == 3, "Found unexpected value.");
                Specification.Assert(task1.IsCompleted && task2.IsCompleted, "One task has not completed.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "One task has not completed.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAnyWithTwoParallelSynchronousTaskWithResults()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();

                ControlledTask<int> task1 = ControlledTask.Run(async () =>
                {
                    return await GetWriteResultAsync(entry, 5);
                });

                ControlledTask<int> task2 = ControlledTask.Run(async () =>
                {
                    return await GetWriteResultAsync(entry, 3);
                });

                int index = ControlledTask.WaitAny(task1, task2);
                Task<int> result = index == 0 ? task1.AwaiterTask : task2.AwaiterTask;

                Specification.Assert(index == 0 || index == 1, $"Index is {index}.");
                Specification.Assert(result.Result == 5 || result.Result == 3, "Found unexpected value.");
                Specification.Assert(task1.IsCompleted && task2.IsCompleted, "One task has not completed.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "One task has not completed.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAnyWithTwoParallelAsynchronousTaskWithResults()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();

                ControlledTask<int> task1 = ControlledTask.Run(async () =>
                {
                    return await GetWriteResultWithDelayAsync(entry, 5);
                });

                ControlledTask<int> task2 = ControlledTask.Run(async () =>
                {
                    return await GetWriteResultWithDelayAsync(entry, 3);
                });

                int index = ControlledTask.WaitAny(task1, task2);
                Task<int> result = index == 0 ? task1.AwaiterTask : task2.AwaiterTask;

                Specification.Assert(index == 0 || index == 1, $"Index is {index}.");
                Specification.Assert(result.Result == 5 || result.Result == 3, "Found unexpected value.");
                Specification.Assert(task1.IsCompleted && task2.IsCompleted, "One task has not completed.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "One task has not completed.",
            replay: true);
        }
    }
}
