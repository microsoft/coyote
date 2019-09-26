// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class TaskYieldTest : BaseTest
    {
        public TaskYieldTest(ITestOutputHelper output)
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
    }
}
