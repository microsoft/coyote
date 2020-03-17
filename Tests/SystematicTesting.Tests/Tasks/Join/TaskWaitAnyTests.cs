// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class TaskWaitAnyTests : BaseSystematicTest
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
        public void TestWaitAnyWithTwoSynchronousTasks()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();
                Task task1 = WriteAsync(entry, 5);
                Task task2 = WriteAsync(entry, 3);
                int index = Task.WaitAny(task1, task2);
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
                Task task1 = WriteWithDelayAsync(entry, 3);
                Task task2 = WriteWithDelayAsync(entry, 5);
                int index = Task.WaitAny(task1, task2);
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

                Task task1 = Task.Run(async () =>
                {
                    await WriteAsync(entry, 3);
                });

                Task task2 = Task.Run(async () =>
                {
                    await WriteAsync(entry, 5);
                });

                int index = Task.WaitAny(task1, task2);

                Specification.Assert(index == 0 || index == 1, $"Index is {index}.");
                Specification.Assert(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
                Specification.Assert(task1.IsCompleted && task2.IsCompleted, "One task has not completed.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "One task has not completed.",
            replay: true);
        }

        private static async Task<int> GetWriteResultAsync(SharedEntry entry, int value)
        {
            entry.Value = value;
            await Task.CompletedTask;
            return entry.Value;
        }

        private static async Task<int> GetWriteResultWithDelayAsync(SharedEntry entry, int value)
        {
            entry.Value = value;
            await Task.Delay(1);
            return entry.Value;
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAnyWithTwoSynchronousTaskWithResults()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();
                Task<int> task1 = GetWriteResultAsync(entry, 5);
                Task<int> task2 = GetWriteResultAsync(entry, 3);
                int index = Task.WaitAny(task1, task2);
                SystemTasks.Task<int> result = index == 0 ? task1.UncontrolledTask : task2.UncontrolledTask;
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
                Task<int> task1 = GetWriteResultWithDelayAsync(entry, 5);
                Task<int> task2 = GetWriteResultWithDelayAsync(entry, 3);
                int index = Task.WaitAny(task1, task2);
                SystemTasks.Task<int> result = index == 0 ? task1.UncontrolledTask : task2.UncontrolledTask;
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

                Task<int> task1 = Task.Run(async () =>
                {
                    return await GetWriteResultAsync(entry, 5);
                });

                Task<int> task2 = Task.Run(async () =>
                {
                    return await GetWriteResultAsync(entry, 3);
                });

                int index = Task.WaitAny(task1, task2);
                SystemTasks.Task<int> result = index == 0 ? task1.UncontrolledTask : task2.UncontrolledTask;

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

                Task<int> task1 = Task.Run(async () =>
                {
                    return await GetWriteResultWithDelayAsync(entry, 5);
                });

                Task<int> task2 = Task.Run(async () =>
                {
                    return await GetWriteResultWithDelayAsync(entry, 3);
                });

                int index = Task.WaitAny(task1, task2);
                SystemTasks.Task<int> result = index == 0 ? task1.UncontrolledTask : task2.UncontrolledTask;

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
