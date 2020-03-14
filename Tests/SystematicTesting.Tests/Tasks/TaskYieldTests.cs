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
    public class TaskYieldTests : BaseSystematicTest
    {
        public TaskYieldTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestTaskYield()
        {
            this.Test(async () =>
            {
                await ControlledTask.Yield();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAsynchronousTaskYield()
        {
            this.Test(async () =>
            {
                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.Yield();
                });

                await ControlledTask.Yield();
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestParallelTaskYield()
        {
            this.Test(async () =>
            {
                ControlledTask task = ControlledTask.Run(async () =>
                {
                    await ControlledTask.Yield();
                });

                await ControlledTask.Yield();
                await task;
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestTwoParallelTasksYield()
        {
            this.Test(async () =>
            {
                ControlledTask task1 = ControlledTask.Run(async () =>
                {
                    await ControlledTask.Yield();
                });

                ControlledTask task2 = ControlledTask.Run(async () =>
                {
                    await ControlledTask.Yield();
                });

                await ControlledTask.Yield();
                await ControlledTask.WhenAll(task1, task2);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestTwoParallelTasksWriteWithYield()
        {
            this.Test(async () =>
            {
                int entry = 0;

                async ControlledTask WriteAsync(int value)
                {
                    await ControlledTask.Yield();
                    entry = value;
                    Specification.Assert(entry == value, "Value is {0} instead of '{1}'.", entry, value);
                }

                ControlledTask task1 = ControlledTask.Run(async () =>
                {
                    await WriteAsync(3);
                });

                ControlledTask task2 = ControlledTask.Run(async () =>
                {
                    await WriteAsync(5);
                });

                await ControlledTask.Yield();
                await ControlledTask.WhenAll(task1, task2);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestTwoParallelTasksWriteWithYieldFail()
        {
            this.TestWithError(async () =>
            {
                int entry = 0;

                async ControlledTask WriteAsync(int value)
                {
                    entry = value;
                    await ControlledTask.Yield();
                    Specification.Assert(entry == value, "Found unexpected value '{0}' after write.", entry);
                }

                ControlledTask task1 = WriteAsync(3);
                ControlledTask task2 = WriteAsync(5);
                await ControlledTask.WhenAll(task1, task2);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Found unexpected value '' after write.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestTwoAsynchronousTasksWriteWithYieldFail()
        {
            this.TestWithError(async () =>
            {
                int entry = 0;

                async ControlledTask WriteAsync(int value)
                {
                    await ControlledTask.Yield();
                    entry = value;
                }

                ControlledTask task1 = WriteAsync(3);
                ControlledTask task2 = WriteAsync(5);
                await ControlledTask.WhenAll(task1, task2);
                Specification.Assert(entry == 5, "Value is {0} instead of 5.", entry);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private static async ControlledTask WriteWithYieldAsync(SharedEntry entry, int value)
        {
            await ControlledTask.Yield();
            entry.Value = value;
        }

        private static async ControlledTask InvokeWriteWithYieldAsync(SharedEntry entry, int value)
        {
            await WriteWithYieldAsync(entry, value);
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInNestedYield()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                ControlledTask task = InvokeWriteWithYieldAsync(entry, 3);
                entry.Value = 5;
                await task;
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInNestedYields()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                ControlledTask task1 = InvokeWriteWithYieldAsync(entry, 3);
                ControlledTask task2 = InvokeWriteWithYieldAsync(entry, 5);
                await ControlledTask.WhenAll(task1, task2);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInLambdaYields()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
#pragma warning disable IDE0039 // Use local function
                Func<int, ControlledTask> invokeWriteWithYieldAsync = async value =>
#pragma warning restore IDE0039 // Use local function
                {
                    await WriteWithYieldAsync(entry, value);
                };

                ControlledTask task1 = invokeWriteWithYieldAsync(3);
                ControlledTask task2 = invokeWriteWithYieldAsync(5);
                await ControlledTask.WhenAll(task1, task2);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInLocalFunctionYields()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                async ControlledTask invokeWriteWithYieldAsync(int value)
                {
                    await WriteWithYieldAsync(entry, value);
                }

                ControlledTask task1 = invokeWriteWithYieldAsync(3);
                ControlledTask task2 = invokeWriteWithYieldAsync(5);
                await ControlledTask.WhenAll(task1, task2);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private static ControlledTask InvokeParallelWriteWithYieldAsync(SharedEntry entry)
        {
            return ControlledTask.Run(async () =>
            {
                ControlledTask task1 = WriteWithYieldAsync(entry, 3);
                ControlledTask task2 = WriteWithYieldAsync(entry, 5);
                await ControlledTask.WhenAll(task1, task2);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInNestedParallelYields()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await InvokeParallelWriteWithYieldAsync(entry);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
    }
}
