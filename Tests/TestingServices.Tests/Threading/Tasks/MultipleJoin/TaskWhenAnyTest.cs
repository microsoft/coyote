// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Threading.Tasks
{
    public class TaskWhenAnyTest : BaseTest
    {
        public TaskWhenAnyTest(ITestOutputHelper output)
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
        public void TestWhenAnyWithTwoSynchronousTasks()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                ControlledTask task1 = WriteAsync(entry, 5);
                ControlledTask task2 = WriteAsync(entry, 3);
                ControlledTask result = await ControlledTask.WhenAny(task1, task2);
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
                ControlledTask task1 = WriteWithDelayAsync(entry, 3);
                ControlledTask task2 = WriteWithDelayAsync(entry, 5);
                ControlledTask result = await ControlledTask.WhenAny(task1, task2);
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

                ControlledTask task1 = ControlledTask.Run(async () =>
                {
                    await WriteAsync(entry, 3);
                });

                ControlledTask task2 = ControlledTask.Run(async () =>
                {
                    await WriteAsync(entry, 5);
                });

                ControlledTask result = await ControlledTask.WhenAny(task1, task2);

                Specification.Assert(result.IsCompleted, "No task has completed.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
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
        public void TestWhenAnyWithTwoSynchronousTaskWithResults()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                ControlledTask<int> task1 = GetWriteResultAsync(entry, 5);
                ControlledTask<int> task2 = GetWriteResultAsync(entry, 3);
                ControlledTask<int> result = await ControlledTask.WhenAny(task1, task2);
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
                ControlledTask<int> task1 = GetWriteResultWithDelayAsync(entry, 5);
                ControlledTask<int> task2 = GetWriteResultWithDelayAsync(entry, 3);
                ControlledTask<int> result = await ControlledTask.WhenAny(task1, task2);
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

                ControlledTask<int> task1 = ControlledTask.Run(async () =>
                {
                    return await GetWriteResultAsync(entry, 5);
                });

                ControlledTask<int> task2 = ControlledTask.Run(async () =>
                {
                    return await GetWriteResultAsync(entry, 3);
                });

                ControlledTask<int> result = await ControlledTask.WhenAny(task1, task2);

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

                ControlledTask<int> task1 = ControlledTask.Run(async () =>
                {
                    return await GetWriteResultWithDelayAsync(entry, 5);
                });

                ControlledTask<int> task2 = ControlledTask.Run(async () =>
                {
                    return await GetWriteResultWithDelayAsync(entry, 3);
                });

                ControlledTask<int> result = await ControlledTask.WhenAny(task1, task2);

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

                ControlledTask task1 = ControlledTask.Run(async () =>
                {
                    await WriteAsync(entry, 3);
                    throw new InvalidOperationException();
                });

                ControlledTask task2 = ControlledTask.Run(async () =>
                {
                    await WriteAsync(entry, 5);
                    throw new NotSupportedException();
                });

                ControlledTask result = await ControlledTask.WhenAny(task1, task2);

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
