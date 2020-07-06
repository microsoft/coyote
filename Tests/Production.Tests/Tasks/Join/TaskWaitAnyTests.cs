// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if BINARY_REWRITE
using System.Threading.Tasks;
#else
using Microsoft.Coyote.Tasks;
#endif
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;
using SystemTasks = System.Threading.Tasks;

#if BINARY_REWRITE
namespace Microsoft.Coyote.BinaryRewriting.Tests.Tasks
#else
namespace Microsoft.Coyote.Production.Tests.Tasks
#endif
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
            configuration: GetConfiguration().WithTestingIterations(200),
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
            configuration: GetConfiguration().WithTestingIterations(200),
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
            configuration: GetConfiguration().WithTestingIterations(200),
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
                SystemTasks.Task<int> result = index == 0 ? GetUncontrolledTask(task1) : GetUncontrolledTask(task2);
                Specification.Assert(index == 0 || index == 1, $"Index is {index}.");
                Specification.Assert(result.Result == 5 || result.Result == 3, "Found unexpected value.");
                Specification.Assert((task1.IsCompleted && !task2.IsCompleted) || (!task1.IsCompleted && task2.IsCompleted),
                    "Both task have completed.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
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
                SystemTasks.Task<int> result = index == 0 ? GetUncontrolledTask(task1) : GetUncontrolledTask(task2);
                Specification.Assert(index == 0 || index == 1, $"Index is {index}.");
                Specification.Assert(result.Result == 5 || result.Result == 3, "Found unexpected value.");
                Specification.Assert(task1.IsCompleted && task2.IsCompleted, "One task has not completed.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
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
                SystemTasks.Task<int> result = index == 0 ? GetUncontrolledTask(task1) : GetUncontrolledTask(task2);

                Specification.Assert(index == 0 || index == 1, $"Index is {index}.");
                Specification.Assert(result.Result == 5 || result.Result == 3, "Found unexpected value.");
                Specification.Assert(task1.IsCompleted && task2.IsCompleted, "One task has not completed.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
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
                SystemTasks.Task<int> result = index == 0 ? GetUncontrolledTask(task1) : GetUncontrolledTask(task2);

                Specification.Assert(index == 0 || index == 1, $"Index is {index}.");
                Specification.Assert(result.Result == 5 || result.Result == 3, "Found unexpected value.");
                Specification.Assert(task1.IsCompleted && task2.IsCompleted, "One task has not completed.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "One task has not completed.",
            replay: true);
        }
    }
}
