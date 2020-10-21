// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
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
                await Task.Yield();
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestAsynchronousTaskYield()
        {
            this.Test(async () =>
            {
                await Task.Run(async () =>
                {
                    await Task.Yield();
                });

                await Task.Yield();
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestParallelTaskYield()
        {
            this.Test(async () =>
            {
                Task task = Task.Run(async () =>
                {
                    await Task.Yield();
                });

                await Task.Yield();
                await task;
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestTwoParallelTasksYield()
        {
            this.Test(async () =>
            {
                Task task1 = Task.Run(async () =>
                {
                    await Task.Yield();
                });

                Task task2 = Task.Run(async () =>
                {
                    await Task.Yield();
                });

                await Task.Yield();
                await Task.WhenAll(task1, task2);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestTwoParallelTasksWriteWithYield()
        {
            this.Test(async () =>
            {
                int entry = 0;

                async Task WriteAsync(int value)
                {
                    await Task.Yield();
                    entry = value;
                    if (this.IsSystematicTest)
                    {
                        Specification.Assert(entry == value, "Value is {0} instead of '{1}'.", entry, value);
                    }
                }

                Task task1 = Task.Run(async () =>
                {
                    await WriteAsync(3);
                });

                Task task2 = Task.Run(async () =>
                {
                    await WriteAsync(5);
                });

                await Task.Yield();
                await Task.WhenAll(task1, task2);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestTwoParallelTasksWriteWithYieldFail()
        {
            this.TestWithError(async () =>
            {
                int entry = 0;

                async Task WriteAsync(int value)
                {
                    entry = value;
                    await Task.Yield();
                    Specification.Assert(entry == value, "Found unexpected value {0} after write.", entry);
                }

                Task task1 = WriteAsync(3);
                Task task2 = WriteAsync(5);
                await Task.WhenAll(task1, task2);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Found unexpected value 5 after write.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestTwoAsynchronousTasksWriteWithYieldFail()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();

                async Task WriteAsync(int value)
                {
                    await Task.Yield();
                    entry.Value = value;
                }

                Task task1 = WriteAsync(3);
                Task task2 = WriteAsync(5);
                await Task.WhenAll(task1, task2);
                this.AssertSharedEntryValue(entry, 5, 3);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private static async Task WriteWithYieldAsync(SharedEntry entry, int value)
        {
            await Task.Yield();
            entry.Value = value;
        }

        private static async Task InvokeWriteWithYieldAsync(SharedEntry entry, int value)
        {
            await WriteWithYieldAsync(entry, value);
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInNestedYield()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Task task = InvokeWriteWithYieldAsync(entry, 3);
                entry.Value = 5;
                await task;
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInNestedYields()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                Task task1 = InvokeWriteWithYieldAsync(entry, 3);
                Task task2 = InvokeWriteWithYieldAsync(entry, 5);
                await Task.WhenAll(task1, task2);
                this.AssertSharedEntryValue(entry, 5, 3);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
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
                Func<int, Task> invokeWriteWithYieldAsync = async value =>
#pragma warning restore IDE0039 // Use local function
                {
                    await WriteWithYieldAsync(entry, value);
                };

                Task task1 = invokeWriteWithYieldAsync(3);
                Task task2 = invokeWriteWithYieldAsync(5);
                await Task.WhenAll(task1, task2);
                this.AssertSharedEntryValue(entry, 5, 3);
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
        public void TestInterleavingsInLocalFunctionYields()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                async Task InvokeWriteWithYieldAsync(int value)
                {
                    await WriteWithYieldAsync(entry, value);
                }

                Task task1 = InvokeWriteWithYieldAsync(3);
                Task task2 = InvokeWriteWithYieldAsync(5);
                await Task.WhenAll(task1, task2);
                this.AssertSharedEntryValue(entry, 5, 3);
            },
            configuration: GetConfiguration().WithTestingIterations(300),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private static Task InvokeParallelWriteWithYieldAsync(SharedEntry entry)
        {
            return Task.Run(async () =>
            {
                Task task1 = WriteWithYieldAsync(entry, 3);
                Task task2 = WriteWithYieldAsync(entry, 5);
                await Task.WhenAll(task1, task2);
            });
        }

        [Fact(Timeout = 5000)]
        public void TestInterleavingsInNestedParallelYields()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await InvokeParallelWriteWithYieldAsync(entry);
                this.AssertSharedEntryValue(entry, 5, 3);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
    }
}
