// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using Microsoft.Coyote.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests
{
    public class TaskIntegerNondeterminismTest : BaseTest
    {
        public TaskIntegerNondeterminismTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SharedEntry
        {
            public int Value = 0;
        }

        [Fact(Timeout = 5000)]
        public void TestIntegerNondeterminismInSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                async ControlledTask WriteAsync()
                {
                    await ControlledTask.CompletedTask;
                    if (Specification.ChooseRandomInteger(5) == 0)
                    {
                        entry.Value = 3;
                    }
                    else
                    {
                        entry.Value = 5;
                    }
                }

                await WriteAsync();
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestIntegerNondeterminismInAsynchronousTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                async ControlledTask WriteWithDelayAsync()
                {
                    await ControlledTask.Delay(1);
                    if (Specification.ChooseRandomInteger(5) == 0)
                    {
                        entry.Value = 3;
                    }
                    else
                    {
                        entry.Value = 5;
                    }
                }

                await WriteWithDelayAsync();
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestIntegerNondeterminismInParallelTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await ControlledTask.Run(() =>
                {
                    if (Specification.ChooseRandomInteger(5) == 0)
                    {
                        entry.Value = 3;
                    }
                    else
                    {
                        entry.Value = 5;
                    }
                });

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestIntegerNondeterminismInParallelSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.CompletedTask;
                    if (Specification.ChooseRandomInteger(5) == 0)
                    {
                        entry.Value = 3;
                    }
                    else
                    {
                        entry.Value = 5;
                    }
                });

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestIntegerNondeterminismInParallelAsynchronousTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.Delay(1);
                    if (Specification.ChooseRandomInteger(5) == 0)
                    {
                        entry.Value = 3;
                    }
                    else
                    {
                        entry.Value = 5;
                    }
                });

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestIntegerNondeterminismInNestedParallelSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();
                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.Run(async () =>
                    {
                        await ControlledTask.CompletedTask;
                        if (Specification.ChooseRandomInteger(5) == 0)
                        {
                            entry.Value = 3;
                        }
                        else
                        {
                            entry.Value = 5;
                        }
                    });
                });

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(1000),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
    }
}
