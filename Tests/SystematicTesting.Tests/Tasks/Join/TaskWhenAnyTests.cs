// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class TaskWhenAnyTests : BaseSystematicTest
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
        public void TestWhenAnyWithTwoSynchronousTasks()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Task task1 = WriteAsync(entry, 5);
                Task task2 = WriteAsync(entry, 3);
                Task result = await Task.WhenAny(task1, task2);
                Specification.Assert(result.IsCompleted, "No task has completed.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAnyWithTwoAsynchronousTasks()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Task task1 = WriteWithDelayAsync(entry, 3);
                Task task2 = WriteWithDelayAsync(entry, 5);
                Task result = await Task.WhenAny(task1, task2);
                Specification.Assert(result.IsCompleted, "No task has completed.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAnyWithTwoParallelTasks()
        {
            this.TestWithError(async () =>
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

                Specification.Assert(result.IsCompleted, "No task has completed.");
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
        public void TestWhenAnyWithTwoSynchronousTaskWithResults()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Task<int> task1 = GetWriteResultAsync(entry, 5);
                Task<int> task2 = GetWriteResultAsync(entry, 3);
                Task<int> result = await Task.WhenAny(task1, task2);
                Specification.Assert(result.IsCompleted, "One task has not completed.");
                Specification.Assert(
                    (result.Id == task1.Id && result.Result == 5) ||
                    (result.Id == task2.Id && result.Result == 3),
                    "Found unexpected value.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAnyWithTwoAsynchronousTaskWithResults()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Task<int> task1 = GetWriteResultWithDelayAsync(entry, 5);
                Task<int> task2 = GetWriteResultWithDelayAsync(entry, 3);
                Task<int> result = await Task.WhenAny(task1, task2);
                Specification.Assert(result.IsCompleted, "One task has not completed.");
                Specification.Assert(
                    (result.Id == task1.Id && result.Result == 5) ||
                    (result.Id == task2.Id && result.Result == 3),
                    "Found unexpected value.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Found unexpected value.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAnyWithTwoParallelSynchronousTaskWithResults()
        {
            this.TestWithError(async () =>
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

                Task<int> result = await Task.WhenAny(task1, task2);

                Specification.Assert(result.IsCompleted, "One task has not completed.");
                Specification.Assert(
                    (result.Id == task1.Id && result.Result == 5) ||
                    (result.Id == task2.Id && result.Result == 3),
                    "Found unexpected value.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAnyWithTwoParallelAsynchronousTaskWithResults()
        {
            this.TestWithError(async () =>
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

                Task<int> result = await Task.WhenAny(task1, task2);

                Specification.Assert(result.IsCompleted, "One task has not completed.");
                Specification.Assert(
                    (result.Id == task1.Id && result.Result == 5) ||
                    (result.Id == task2.Id && result.Result == 3),
                    "Found unexpected value.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Found unexpected value.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAnyWithException()
        {
            this.TestWithError(async () =>
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
                    throw new NotSupportedException();
                });

                Task result = await Task.WhenAny(task1, task2);

                Specification.Assert(result.IsFaulted, "No task has faulted.");
                Specification.Assert(
                    (task1.IsFaulted && task1.Exception.InnerException.GetType() == typeof(InvalidOperationException)) ||
                    (task2.IsFaulted && task2.Exception.InnerException.GetType() == typeof(NotSupportedException)),
                    "The exception is not of the expected type.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
    }
}
