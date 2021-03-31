// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Random;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class TaskRandomIntegerTests : BaseBugFindingTest
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

                async Task WriteAsync()
                {
                    await Task.CompletedTask;
                    if (generator.NextInteger(5) is 0)
                    {
                        entry.Value = 3;
                    }
                    else
                    {
                        entry.Value = 5;
                    }
                }

                await WriteAsync();
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
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

                async Task WriteWithDelayAsync()
                {
                    await Task.Delay(1);
                    if (generator.NextInteger(5) is 0)
                    {
                        entry.Value = 3;
                    }
                    else
                    {
                        entry.Value = 5;
                    }
                }

                await WriteWithDelayAsync();
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
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

                await Task.Run(() =>
                {
                    if (generator.NextInteger(5) is 0)
                    {
                        entry.Value = 3;
                    }
                    else
                    {
                        entry.Value = 5;
                    }
                });

                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
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

                await Task.Run(async () =>
                {
                    await Task.CompletedTask;
                    if (generator.NextInteger(5) is 0)
                    {
                        entry.Value = 3;
                    }
                    else
                    {
                        entry.Value = 5;
                    }
                });

                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
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

                await Task.Run(async () =>
                {
                    await Task.Delay(1);
                    if (generator.NextInteger(5) is 0)
                    {
                        entry.Value = 3;
                    }
                    else
                    {
                        entry.Value = 5;
                    }
                });

                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
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

                await Task.Run(async () =>
                {
                    await Task.Run(async () =>
                    {
                        await Task.CompletedTask;
                        if (generator.NextInteger(5) is 0)
                        {
                            entry.Value = 3;
                        }
                        else
                        {
                            entry.Value = 5;
                        }
                    });
                });

                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
    }
}
