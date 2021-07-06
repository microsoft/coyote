// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

namespace Microsoft.Coyote.Tasks.BugFinding.Tests
{
    public class TaskWaitAnyTests : BaseTaskTest
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
                Specification.Assert(index is 0 || index is 1, $"Index is {index}.");
                Specification.Assert(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
                Specification.Assert((task1.IsCompleted && !task2.IsCompleted) || (!task1.IsCompleted && task2.IsCompleted),
                    "Both task have completed.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
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
                Specification.Assert(index is 0 || index is 1, $"Index is {index}.");
                Specification.Assert(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
                AssertCompleted(task1, task2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
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

                Specification.Assert(index is 0 || index is 1, $"Index is {index}.");
                Specification.Assert(task1.IsCompleted || task2.IsCompleted, "No task has completed.");
                AssertCompleted(task1, task2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "One task has not completed.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAnyWithTwoSynchronousTaskWithResults()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();
                Task<int> task1 = entry.GetWriteResultAsync(5);
                Task<int> task2 = entry.GetWriteResultAsync(3);
                int index = Task.WaitAny(task1, task2);
                SystemTasks.Task<int> result = index is 0 ? task1.UncontrolledTask : task2.UncontrolledTask;
                Specification.Assert(index is 0 || index is 1, $"Index is {index}.");
                Specification.Assert(result.Result is 5 || result.Result is 3, "Found unexpected value.");
                Specification.Assert((task1.IsCompleted && !task2.IsCompleted) || (!task1.IsCompleted && task2.IsCompleted),
                    "Both task have completed.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Both task have completed.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAnyWithTwoAsynchronousTaskWithResults()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();
                Task<int> task1 = entry.GetWriteResultWithDelayAsync(5);
                Task<int> task2 = entry.GetWriteResultWithDelayAsync(3);
                int index = Task.WaitAny(task1, task2);
                SystemTasks.Task<int> result = index is 0 ? task1.UncontrolledTask : task2.UncontrolledTask;
                Specification.Assert(index is 0 || index is 1, $"Index is {index}.");
                Specification.Assert(result.Result is 5 || result.Result is 3, "Found unexpected value.");
                AssertCompleted(task1, task2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
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
                    return await entry.GetWriteResultAsync(5);
                });

                Task<int> task2 = Task.Run(async () =>
                {
                    return await entry.GetWriteResultAsync(3);
                });

                int index = Task.WaitAny(task1, task2);
                SystemTasks.Task<int> result = index is 0 ? task1.UncontrolledTask : task2.UncontrolledTask;

                Specification.Assert(index is 0 || index is 1, $"Index is {index}.");
                Specification.Assert(result.Result is 5 || result.Result is 3, "Found unexpected value.");
                AssertCompleted(task1, task2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
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
                    return await entry.GetWriteResultWithDelayAsync(5);
                });

                Task<int> task2 = Task.Run(async () =>
                {
                    return await entry.GetWriteResultWithDelayAsync(3);
                });

                int index = Task.WaitAny(task1, task2);
                SystemTasks.Task<int> result = index is 0 ? task1.UncontrolledTask : task2.UncontrolledTask;

                Specification.Assert(index is 0 || index is 1, $"Index is {index}.");
                Specification.Assert(result.Result is 5 || result.Result is 3, "Found unexpected value.");
                AssertCompleted(task1, task2);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "One task has not completed.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAnyWithIncompleteTask()
        {
            this.Test(async () =>
            {
                // Test that `WaitAny` can complete even if one of the tasks cannot complete until later.
                var tcs = TaskCompletionSource.Create<bool>();
                Task.WaitAny(tcs.Task, Task.Delay(1));
                tcs.SetResult(true);
                await tcs.Task;
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAnyWithIncompleteGenericTask()
        {
            this.Test(async () =>
            {
                // Test that `WaitAny` can complete even if one of the tasks cannot complete until later.
                var tcs = TaskCompletionSource.Create<bool>();
                Task.WaitAny(tcs.Task, Task.FromResult(true));
                tcs.SetResult(true);
                await tcs.Task;
            },
            configuration: this.GetConfiguration().WithTestingIterations(500));
        }

        private static void AssertCompleted(Task task1, Task task2) =>
            Specification.Assert(task1.IsCompleted && task2.IsCompleted, "One task has not completed.");
    }
}
