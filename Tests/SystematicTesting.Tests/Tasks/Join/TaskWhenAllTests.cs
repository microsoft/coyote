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
    public class TaskWhenAllTests : BaseSystematicTest
    {
        public TaskWhenAllTests(ITestOutputHelper output)
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
        public void TestWhenAllWithTwoSynchronousTasks()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Task task1 = WriteAsync(entry, 5);
                Task task2 = WriteAsync(entry, 3);
                await Task.WhenAll(task1, task2);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithTwoAsynchronousTasks()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Task task1 = WriteWithDelayAsync(entry, 3);
                Task task2 = WriteWithDelayAsync(entry, 5);
                await Task.WhenAll(task1, task2);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithTwoParallelTasks()
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

                await Task.WhenAll(task1, task2);

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
        public void TestWhenAllWithTwoSynchronousTaskWithResults()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Task<int> task1 = GetWriteResultAsync(entry, 5);
                Task<int> task2 = GetWriteResultAsync(entry, 3);
                int[] results = await Task.WhenAll(task1, task2);
                Specification.Assert(results.Length == 2, "Result count is '{0}' instead of 2.", results.Length);
                Specification.Assert(results[0] == 5 && results[1] == 3, "Found unexpected value.");
                Specification.Assert(results[0] == results[1], "Results are not equal.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Results are not equal.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithTwoAsynchronousTaskWithResults()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Task<int> task1 = GetWriteResultWithDelayAsync(entry, 5);
                Task<int> task2 = GetWriteResultWithDelayAsync(entry, 3);
                int[] results = await Task.WhenAll(task1, task2);
                Specification.Assert(results.Length == 2, "Result count is '{0}' instead of 2.", results.Length);
                Specification.Assert(results[0] == 5 && results[1] == 3, "Found unexpected value.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Found unexpected value.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithTwoParallelSynchronousTaskWithResults()
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

                int[] results = await Task.WhenAll(task1, task2);

                Specification.Assert(results.Length == 2, "Result count is '{0}' instead of 2.", results.Length);
                Specification.Assert(results[0] == 5, $"The first task result is {results[0]} instead of 5.");
                Specification.Assert(results[1] == 3, $"The second task result is {results[1]} instead of 3.");
                Specification.Assert(results[0] == results[1], "Results are not equal.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Results are not equal.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithTwoParallelAsynchronousTaskWithResults()
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

                int[] results = await Task.WhenAll(task1, task2);

                Specification.Assert(results.Length == 2, "Result count is '{0}' instead of 2.", results.Length);
                Specification.Assert(results[0] == 5 && results[1] == 3, "Found unexpected value.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Found unexpected value.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithException()
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

                try
                {
                    await Task.WhenAll(task1, task2);
                }
                catch (AggregateException ex)
                {
                    Specification.Assert(ex.InnerExceptions.Count == 2, "Expected two exceptions.");
                    Specification.Assert(ex.InnerExceptions[0].InnerException.GetType() == typeof(InvalidOperationException),
                        "The first exception is not of the expected type.");
                    Specification.Assert(ex.InnerExceptions[1].InnerException.GetType() == typeof(NotSupportedException),
                        "The second exception is not of the expected type.");
                }

                Specification.Assert(task1.IsFaulted && task2.IsFaulted, "One task has not faulted.");
                Specification.Assert(task1.Exception.InnerException.GetType() == typeof(InvalidOperationException),
                    "The first task exception is not of the expected type.");
                Specification.Assert(task2.Exception.InnerException.GetType() == typeof(NotSupportedException),
                    "The second task exception is not of the expected type.");
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
    }
}
