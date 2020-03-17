// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class TaskWaitAllTests : BaseSystematicTest
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
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();
                Task task1 = WriteAsync(entry, 5);
                Task task2 = WriteAsync(entry, 3);
                Task.WaitAll(task1, task2);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoAsynchronousTasks()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();
                Task task1 = WriteWithDelayAsync(entry, 3);
                Task task2 = WriteWithDelayAsync(entry, 5);
                Task.WaitAll(task1, task2);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoParallelTasks()
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

                Task.WaitAll(task1, task2);

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
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
        public void TestWaitAllWithTwoSynchronousTaskWithResults()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();
                Task<int> task1 = GetWriteResultAsync(entry, 5);
                Task<int> task2 = GetWriteResultAsync(entry, 3);
                Task.WaitAll(task1, task2);
                Specification.Assert(task1.Result == 5 && task2.Result == 3, "Found unexpected value.");
                Specification.Assert(task1.Result == task2.Result, "Results are not equal.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Results are not equal.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoAsynchronousTaskWithResults()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();
                Task<int> task1 = GetWriteResultWithDelayAsync(entry, 5);
                Task<int> task2 = GetWriteResultWithDelayAsync(entry, 3);
                Task.WaitAll(task1, task2);
                Specification.Assert(task1.Result == 5 && task2.Result == 3, "Found unexpected value.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Found unexpected value.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoParallelSynchronousTaskWithResults()
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

                Task.WaitAll(task1, task2);

                Specification.Assert(task1.Result == 5, $"The first task result is {task1.Result} instead of 5.");
                Specification.Assert(task2.Result == 3, $"The second task result is {task2.Result} instead of 3.");
                Specification.Assert(task1.Result == task2.Result, "Results are not equal.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Results are not equal.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoParallelAsynchronousTaskWithResults()
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

                Task.WaitAll(task1, task2);

                Specification.Assert(task1.Result == 5 && task2.Result == 3, "Found unexpected value.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Found unexpected value.",
            replay: true);
        }
    }
}
