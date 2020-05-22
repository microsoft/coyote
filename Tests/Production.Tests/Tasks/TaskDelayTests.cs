// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.Production.Tests.Tasks
{
    public class TaskDelayTests : BaseTest
    {
        public TaskDelayTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private static async Task WriteWithLoopAndDelayAsync(SharedEntry entry, int value, int delay)
        {
            for (int i = 0; i < 2; i++)
            {
                entry.Value = value + i;
                await Task.Delay(delay);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInLoopWithSynchronousDelays()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();

                Task[] tasks = new Task[2];
                for (int i = 0; i < 2; i++)
                {
                    tasks[i] = WriteWithLoopAndDelayAsync(entry, i, 0);
                }

                await Task.WhenAll(tasks);

                Specification.Assert(entry.Value == 2, "Value is '{0}' instead of 2.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInLoopWithAsynchronousDelays()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();

                Task[] tasks = new Task[2];
                for (int i = 0; i < 2; i++)
                {
                    tasks[i] = WriteWithLoopAndDelayAsync(entry, i, 1);
                }

                await Task.WhenAll(tasks);

                Specification.Assert(entry.Value == 2, "Value is {0} instead of 2.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 1 instead of 2.",
            replay: true);
        }

        private static async Task WriteWithDelayAsync(SharedEntry entry, int value, int delay, bool repeat = false)
        {
            await Task.Delay(delay);
            Task task = null;
            if (repeat)
            {
                task = InvokeWriteWithDelayAsync(entry, value + 1, delay);
            }

            entry.Value = value;
            if (task != null)
            {
                await task;
            }
        }

        private static async Task InvokeWriteWithDelayAsync(SharedEntry entry, int value, int delay, bool repeat = false)
        {
            await WriteWithDelayAsync(entry, value, delay, repeat);
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInNestedSynchronousDelay()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Task task = InvokeWriteWithDelayAsync(entry, 3, 0);
                entry.Value = 5;
                await task;
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInNestedAsynchronousDelay()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Task task = InvokeWriteWithDelayAsync(entry, 3, 1);
                entry.Value = 5;
                await task;
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInNestedSynchronousDelays()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Task task1 = InvokeWriteWithDelayAsync(entry, 3, 0);
                Task task2 = InvokeWriteWithDelayAsync(entry, 5, 0);
                await Task.WhenAll(task1, task2);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInNestedAsynchronousDelays()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Task task1 = InvokeWriteWithDelayAsync(entry, 3, 1);
                Task task2 = InvokeWriteWithDelayAsync(entry, 5, 1);
                await Task.WhenAll(task1, task2);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInRepeatedNestedSynchronousDelays()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Task task1 = InvokeWriteWithDelayAsync(entry, 3, 0, true);
                Task task2 = InvokeWriteWithDelayAsync(entry, 5, 0, true);
                await Task.WhenAll(task1, task2);
                Specification.Assert(entry.Value != 3, "Value is 3.");
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInRepeatedNestedAsynchronousDelays()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Task task1 = InvokeWriteWithDelayAsync(entry, 3, 1, true);
                Task task2 = InvokeWriteWithDelayAsync(entry, 5, 1, true);
                await Task.WhenAll(task1, task2);
                Specification.Assert(entry.Value != 3, "Value is 3.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInLambdaSynchronousDelays()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
#pragma warning disable IDE0039 // Use local function
                Func<int, int, Task> invokeWriteWithDelayAsync = async (value, delay) =>
#pragma warning restore IDE0039 // Use local function
                {
                    await WriteWithDelayAsync(entry, value, delay);
                };

                Task task1 = invokeWriteWithDelayAsync(3, 0);
                Task task2 = invokeWriteWithDelayAsync(5, 0);
                await Task.WhenAll(task1, task2);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInLambdaAsynchronousDelays()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
#pragma warning disable IDE0039 // Use local function
                Func<int, int, Task> invokeWriteWithDelayAsync = async (value, delay) =>
#pragma warning restore IDE0039 // Use local function
                {
                    await WriteWithDelayAsync(entry, value, delay);
                };

                Task task1 = invokeWriteWithDelayAsync(3, 1);
                Task task2 = invokeWriteWithDelayAsync(5, 1);
                await Task.WhenAll(task1, task2);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInLocalFunctionSynchronousDelays()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                async Task InvokeWriteWithDelayAsync(int value, int delay)
                {
                    await WriteWithDelayAsync(entry, value, delay);
                }

                Task task1 = InvokeWriteWithDelayAsync(3, 0);
                Task task2 = InvokeWriteWithDelayAsync(5, 0);
                await Task.WhenAll(task1, task2);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInLocalFunctionAsynchronousDelays()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                async Task InvokeWriteWithDelayAsync(int value, int delay)
                {
                    await WriteWithDelayAsync(entry, value, delay);
                }

                Task task1 = InvokeWriteWithDelayAsync(3, 1);
                Task task2 = InvokeWriteWithDelayAsync(5, 1);
                await Task.WhenAll(task1, task2);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private static Task InvokeParallelWriteWithDelayAsync(SharedEntry entry, int delay)
        {
            return Task.Run(async () =>
            {
                Task task1 = WriteWithDelayAsync(entry, 3, delay);
                Task task2 = WriteWithDelayAsync(entry, 5, delay);
                await Task.WhenAll(task1, task2);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestParallelInterleavingsInNestedSynchronousDelays()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await InvokeParallelWriteWithDelayAsync(entry, 0);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestParallelInterleavingsInNestedAsynchronousDelays()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await InvokeParallelWriteWithDelayAsync(entry, 1);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
    }
}
