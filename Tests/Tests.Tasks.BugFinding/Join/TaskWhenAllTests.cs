// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Tasks.BugFinding.Tests
{
    public class TaskWhenAllTests : BaseTaskTest
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
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
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
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
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

                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithTwoSynchronousTaskWithResults()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Task<int> task1 = entry.GetWriteResultAsync(5);
                Task<int> task2 = entry.GetWriteResultAsync(3);
                int[] results = await Task.WhenAll(task1, task2);
                Specification.Assert(results.Length is 2, "Result count is '{0}' instead of 2.", results.Length);
                Specification.Assert(results[0] == 5 && results[1] is 3, "Found unexpected value.");
                Specification.Assert(results[0] == results[1], "Results are equal.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Results are equal.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithTwoAsynchronousTaskWithResults()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Task<int> task1 = entry.GetWriteResultWithDelayAsync(5);
                Task<int> task2 = entry.GetWriteResultWithDelayAsync(3);
                int[] results = await Task.WhenAll(task1, task2);
                Specification.Assert(results.Length is 2, "Result count is '{0}' instead of 2.", results.Length);
                Specification.Assert(results[0] == 5 && results[1] is 3, "Found unexpected value.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
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
                    return await entry.GetWriteResultAsync(5);
                });

                Task<int> task2 = Task.Run(async () =>
                {
                    return await entry.GetWriteResultAsync(3);
                });

                int[] results = await Task.WhenAll(task1, task2);

                Specification.Assert(results.Length is 2, "Result count is '{0}' instead of 2.", results.Length);
                Specification.Assert(results[0] == 5, $"The first task result is {results[0]} instead of 5.");
                Specification.Assert(results[1] is 3, $"The second task result is {results[1]} instead of 3.");
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(300),
            expectedError: "Reached test assertion.",
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
                    return await entry.GetWriteResultWithDelayAsync(5);
                });

                Task<int> task2 = Task.Run(async () =>
                {
                    return await entry.GetWriteResultWithDelayAsync(3);
                });

                int[] results = await Task.WhenAll(task1, task2);

                Specification.Assert(results.Length is 2, "Result count is '{0}' instead of 2.", results.Length);
                Specification.Assert(results[0] == 5 && results[1] is 3, "Found unexpected value.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Found unexpected value.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithAsyncCaller()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Func<Task> whenAll = async () =>
                {
                    List<Task> tasks = new List<Task>();
                    for (int i = 0; i < 2; i++)
                    {
                        tasks.Add(Task.Delay(1));
                    }

                    entry.Value = 3;
                    await Task.WhenAll(tasks);
                    entry.Value = 1;
                };

                var task = whenAll();
                AssertSharedEntryValue(entry, 1);
                await task;
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 1.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithResultAndAsyncCaller()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Func<Task> whenAll = async () =>
                {
                    List<Task<int>> tasks = new List<Task<int>>();
                    for (int i = 0; i < 2; i++)
                    {
                        tasks.Add(Task.Run(() => 1));
                    }

                    entry.Value = 3;
                    await Task.WhenAll(tasks);
                    entry.Value = 1;
                };

                var task = whenAll();
                AssertSharedEntryValue(entry, 1);
                await task;
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 1.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithException()
        {
            this.TestWithError(async () =>
            {
                Task task1 = Task.Run(async () =>
                {
                    await Task.CompletedTask;
                    ThrowException<InvalidOperationException>();
                });

                Task task2 = Task.Run(async () =>
                {
                    await Task.CompletedTask;
                    ThrowException<NotSupportedException>();
                });

                Exception exception = null;

                try
                {
                    await Task.WhenAll(task1, task2);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                Specification.Assert(exception != null, "Expected an `AggregateException`.");
                if (exception is AggregateException aex)
                {
                    Specification.Assert(aex.InnerExceptions.Any(e => e is InvalidOperationException),
                        "The exception is not of the expected type.");
                    Specification.Assert(aex.InnerExceptions.Any(e => e is NotSupportedException),
                        "The exception is not of the expected type.");
                }
                else
                {
                    Specification.Assert(exception is InvalidOperationException || exception is NotSupportedException,
                        "The exception is not of the expected type.");
                }

                Specification.Assert(task1.IsFaulted && task2.IsFaulted, "One task has not faulted.");
                Specification.Assert(task1.Exception.InnerException.GetType() == typeof(InvalidOperationException),
                    "The first task exception is not of the expected type.");
                Specification.Assert(task2.Exception.InnerException.GetType() == typeof(NotSupportedException),
                    "The second task exception is not of the expected type.");
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithResultsAndException()
        {
            this.TestWithError(async () =>
            {
                Task<int> task1 = Task.Run(async () =>
                {
                    await Task.CompletedTask;
                    ThrowException<InvalidOperationException>();
                    return 1;
                });

                Task<int> task2 = Task.Run(async () =>
                {
                    await Task.CompletedTask;
                    ThrowException<NotSupportedException>();
                    return 3;
                });

                Exception exception = null;

                try
                {
                    await Task.WhenAll(task1, task2);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }

                Specification.Assert(exception != null, "Expected an `AggregateException`.");
                if (exception is AggregateException aex)
                {
                    Specification.Assert(aex.InnerExceptions.Any(e => e is InvalidOperationException),
                        "The exception is not of the expected type.");
                    Specification.Assert(aex.InnerExceptions.Any(e => e is NotSupportedException),
                        "The exception is not of the expected type.");
                }
                else
                {
                    Specification.Assert(exception is InvalidOperationException || exception is NotSupportedException,
                        "The exception is not of the expected type.");
                }

                Specification.Assert(task1.IsFaulted && task2.IsFaulted, "One task has not faulted.");
                Specification.Assert(task1.Exception.InnerException.GetType() == typeof(InvalidOperationException),
                    "The first task exception is not of the expected type.");
                Specification.Assert(task2.Exception.InnerException.GetType() == typeof(NotSupportedException),
                    "The second task exception is not of the expected type.");
                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllDeadlock()
        {
            this.TestWithError(async () =>
            {
                // Test that `WhenAll` deadlocks because one of the tasks cannot complete until later.
                var tcs = TaskCompletionSource.Create<bool>();
                await Task.WhenAll(tcs.Task, Task.Delay(1));
                tcs.SetResult(true);
                await tcs.Task;
            },
            errorChecker: (e) =>
            {
                Assert.StartsWith("Deadlock detected.", e);
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWhenAllWithResultsAndDeadlock()
        {
            this.TestWithError(async () =>
            {
                // Test that `WhenAll` deadlocks because one of the tasks cannot complete until later.
                var tcs = TaskCompletionSource.Create<bool>();
                await Task.WhenAll(tcs.Task, Task.FromResult(true));
                tcs.SetResult(true);
                await tcs.Task;
            },
            errorChecker: (e) =>
            {
                Assert.StartsWith("Deadlock detected.", e);
            },
            replay: true);
        }
    }
}
