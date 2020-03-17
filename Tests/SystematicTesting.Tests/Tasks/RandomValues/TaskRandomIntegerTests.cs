// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Random;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class TaskRandomIntegerTests : BaseSystematicTest
    {
        public TaskRandomIntegerTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestRandomIntegerInSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Generator generator = Generator.Create();
                SharedEntry entry = new SharedEntry();

                async ControlledTask WriteAsync()
                {
                    await ControlledTask.CompletedTask;
                    if (generator.NextInteger(5) == 0)
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
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRandomIntegerInAsynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Generator generator = Generator.Create();
                SharedEntry entry = new SharedEntry();

                async ControlledTask WriteWithDelayAsync()
                {
                    await ControlledTask.Delay(1);
                    if (generator.NextInteger(5) == 0)
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
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRandomIntegerInParallelTask()
        {
            this.TestWithError(async () =>
            {
                Generator generator = Generator.Create();
                SharedEntry entry = new SharedEntry();

                await ControlledTask.Run(() =>
                {
                    if (generator.NextInteger(5) == 0)
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
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRandomIntegerInParallelSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Generator generator = Generator.Create();
                SharedEntry entry = new SharedEntry();

                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.CompletedTask;
                    if (generator.NextInteger(5) == 0)
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
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRandomIntegerInParallelAsynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Generator generator = Generator.Create();
                SharedEntry entry = new SharedEntry();

                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.Delay(1);
                    if (generator.NextInteger(5) == 0)
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
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestRandomIntegerInNestedParallelSynchronousTask()
        {
            this.TestWithError(async () =>
            {
                Generator generator = Generator.Create();
                SharedEntry entry = new SharedEntry();

                await ControlledTask.Run(async () =>
                {
                    await ControlledTask.Run(async () =>
                    {
                        await ControlledTask.CompletedTask;
                        if (generator.NextInteger(5) == 0)
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
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
    }
}
