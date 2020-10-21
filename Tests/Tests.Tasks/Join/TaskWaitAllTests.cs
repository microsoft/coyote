// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Tasks.Tests
{
    public class TaskWaitAllTests : BaseTaskTest
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
                Specification.Assert(entry.Value is 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
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
                Specification.Assert(entry.Value is 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private void AssertSharedEntryValue(SharedEntry entry, int expected, int other)
        {
            if (this.IsSystematicTest)
            {
                Specification.Assert(entry.Value == expected, "Value is {0} instead of {1}.", entry.Value, expected);
            }
            else
            {
                Specification.Assert(entry.Value == expected || entry.Value == other, "Unexpected value {0} in SharedEntry", entry.Value);
                Specification.Assert(false, "Value is {0} instead of {1}.", other, expected);
            }
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

                this.AssertSharedEntryValue(entry, 5, 3);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoSynchronousTaskWithResults()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();
                Task<int> task1 = entry.GetWriteResultAsync(5);
                Task<int> task2 = entry.GetWriteResultAsync(3);
                Task.WaitAll(task1, task2);
                Specification.Assert(task1.Result is 5 && task2.Result is 3, "Found unexpected value.");
                Specification.Assert(task1.Result == task2.Result, "Results are not equal.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Results are not equal.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoAsynchronousTaskWithResults()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();
                Task<int> task1 = entry.GetWriteResultWithDelayAsync(5);
                Task<int> task2 = entry.GetWriteResultWithDelayAsync(3);
                Task.WaitAll(task1, task2);
                Specification.Assert(task1.Result is 5 && task2.Result is 3, "Found unexpected value.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
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
                    return await entry.GetWriteResultAsync(5);
                });

                Task<int> task2 = Task.Run(async () =>
                {
                    return await entry.GetWriteResultAsync(3);
                });

                Task.WaitAll(task1, task2);

                if (this.IsSystematicTest)
                {
                    Specification.Assert(task1.Result is 5, $"The first task result is {task1.Result} instead of 5.");
                    Specification.Assert(task2.Result is 3, $"The second task result is {task2.Result} instead of 3.");
                    Specification.Assert(task1.Result == task2.Result, "Results are not equal.");
                }
                else
                {
                    // production version of this test we don't know which order the tasks execute and
                    // with statement level interleaving we can even end up with duplicate values.
                    Specification.Assert(false, "Results are not equal.");
                }
            },
            configuration: GetConfiguration().WithTestingIterations(200),
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
                    return await entry.GetWriteResultWithDelayAsync(5);
                });

                Task<int> task2 = Task.Run(async () =>
                {
                    return await entry.GetWriteResultWithDelayAsync(3);
                });

                Task.WaitAll(task1, task2);

                Specification.Assert(task1.Result is 5 && task2.Result is 3, "Found unexpected value.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Found unexpected value.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllDeadlock()
        {
            if (!this.IsSystematicTest)
            {
                // .NET cannot detect these deadlocks.
                return;
            }

            this.TestWithError(async () =>
            {
                // Test that `WaitAll` deadlocks because one of the tasks cannot complete until later.
                var tcs = TaskCompletionSource.Create<bool>();
                Task.WaitAll(tcs.Task, Task.Delay(1));
                tcs.SetResult(true);
                await tcs.Task;
            },
            errorChecker: (e) =>
            {
                Assert.True(e.StartsWith("Deadlock detected."), "Expected 'Deadlock detected', but found error: " + e);
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithResultsAndDeadlock()
        {
            if (!this.IsSystematicTest)
            {
                // .NET cannot detect these deadlocks.
                return;
            }

            this.TestWithError(async () =>
            {
                // Test that `WaitAll` deadlocks because one of the tasks cannot complete until later.
                var tcs = TaskCompletionSource.Create<bool>();
                Task.WaitAll(tcs.Task, Task.FromResult(true));
                tcs.SetResult(true);
                await tcs.Task;
            },
            errorChecker: (e) =>
            {
                Assert.True(e.StartsWith("Deadlock detected."), "Expected 'Deadlock detected', but found error: " + e);
            },
            replay: true);
        }
    }
}
