// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common.Threading;
using Microsoft.Coyote.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Threading.Tasks
{
    public class TaskDelayTests : BaseTest
    {
        public TaskDelayTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private static async ControlledTask WriteWithLoopAndDelayAsync(SharedEntry entry, int value, int delay)
        {
            for (int i = 0; i < 2; i++)
            {
                entry.Value = value + i;
                await ControlledTask.Delay(delay);
            }
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInLoopWithSynchronousDelays()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();

                ControlledTask[] tasks = new ControlledTask[2];
                for (int i = 0; i < 2; i++)
                {
                    tasks[i] = WriteWithLoopAndDelayAsync(entry, i, 0);
                }

                await ControlledTask.WhenAll(tasks);

                Specification.Assert(entry.Value == 2, "Value is '{0}' instead of 2.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInLoopWithAsynchronousDelays()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();

                ControlledTask[] tasks = new ControlledTask[2];
                for (int i = 0; i < 2; i++)
                {
                    tasks[i] = WriteWithLoopAndDelayAsync(entry, i, 1);
                }

                await ControlledTask.WhenAll(tasks);

                Specification.Assert(entry.Value == 2, "Value is {0} instead of 2.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 1 instead of 2.",
            replay: true);
        }

        private static async ControlledTask WriteWithDelayAsync(SharedEntry entry, int value, int delay, bool repeat = false)
        {
            await ControlledTask.Delay(delay);
            ControlledTask task = null;
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

        private static async ControlledTask InvokeWriteWithDelayAsync(SharedEntry entry, int value, int delay, bool repeat = false)
        {
            await WriteWithDelayAsync(entry, value, delay, repeat);
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInNestedSynchronousDelay()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                ControlledTask task = InvokeWriteWithDelayAsync(entry, 3, 0);
                entry.Value = 5;
                await task;
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInNestedAsynchronousDelay()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                ControlledTask task = InvokeWriteWithDelayAsync(entry, 3, 1);
                entry.Value = 5;
                await task;
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInNestedSynchronousDelays()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                ControlledTask task1 = InvokeWriteWithDelayAsync(entry, 3, 0);
                ControlledTask task2 = InvokeWriteWithDelayAsync(entry, 5, 0);
                await ControlledTask.WhenAll(task1, task2);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInNestedAsynchronousDelays()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                ControlledTask task1 = InvokeWriteWithDelayAsync(entry, 3, 1);
                ControlledTask task2 = InvokeWriteWithDelayAsync(entry, 5, 1);
                await ControlledTask.WhenAll(task1, task2);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInRepeatedNestedSynchronousDelays()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                ControlledTask task1 = InvokeWriteWithDelayAsync(entry, 3, 0, true);
                ControlledTask task2 = InvokeWriteWithDelayAsync(entry, 5, 0, true);
                await ControlledTask.WhenAll(task1, task2);
                Specification.Assert(entry.Value != 3, "Value is 3.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInRepeatedNestedAsynchronousDelays()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                ControlledTask task1 = InvokeWriteWithDelayAsync(entry, 3, 1, true);
                ControlledTask task2 = InvokeWriteWithDelayAsync(entry, 5, 1, true);
                await ControlledTask.WhenAll(task1, task2);
                Specification.Assert(entry.Value != 3, "Value is 3.");
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
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
                Func<int, int, ControlledTask> invokeWriteWithDelayAsync = async (value, delay) =>
#pragma warning restore IDE0039 // Use local function
                {
                    await WriteWithDelayAsync(entry, value, delay);
                };

                ControlledTask task1 = invokeWriteWithDelayAsync(3, 0);
                ControlledTask task2 = invokeWriteWithDelayAsync(5, 0);
                await ControlledTask.WhenAll(task1, task2);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInLambdaAsynchronousDelays()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
#pragma warning disable IDE0039 // Use local function
                Func<int, int, ControlledTask> invokeWriteWithDelayAsync = async (value, delay) =>
#pragma warning restore IDE0039 // Use local function
                {
                    await WriteWithDelayAsync(entry, value, delay);
                };

                ControlledTask task1 = invokeWriteWithDelayAsync(3, 1);
                ControlledTask task2 = invokeWriteWithDelayAsync(5, 1);
                await ControlledTask.WhenAll(task1, task2);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInLocalFunctionSynchronousDelays()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();
                async ControlledTask invokeWriteWithDelayAsync(int value, int delay)
                {
                    await WriteWithDelayAsync(entry, value, delay);
                }

                ControlledTask task1 = invokeWriteWithDelayAsync(3, 0);
                ControlledTask task2 = invokeWriteWithDelayAsync(5, 0);
                await ControlledTask.WhenAll(task1, task2);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInLocalFunctionAsynchronousDelays()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                async ControlledTask invokeWriteWithDelayAsync(int value, int delay)
                {
                    await WriteWithDelayAsync(entry, value, delay);
                }

                ControlledTask task1 = invokeWriteWithDelayAsync(3, 1);
                ControlledTask task2 = invokeWriteWithDelayAsync(5, 1);
                await ControlledTask.WhenAll(task1, task2);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private static ControlledTask InvokeParallelWriteWithDelayAsync(SharedEntry entry, int delay)
        {
            return ControlledTask.Run(async () =>
            {
                ControlledTask task1 = WriteWithDelayAsync(entry, 3, delay);
                ControlledTask task2 = WriteWithDelayAsync(entry, 5, delay);
                await ControlledTask.WhenAll(task1, task2);
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
            configuration: GetConfiguration().WithNumberOfIterations(200));
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
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
    }
}
