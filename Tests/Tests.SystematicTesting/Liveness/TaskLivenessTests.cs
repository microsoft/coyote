// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class TaskLivenessTests : BaseSystematicTest
    {
        public TaskLivenessTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestTaskLivenessProperty()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();

                var task = Task.Run(async () =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        if (i is 9)
                        {
                            entry.Value = 1;
                        }

                        await Task.Yield();
                    }
                });

                while (true)
                {
                    if (entry.Value is 1)
                    {
                        break;
                    }

                    await Task.Delay(10);
                }

                await task;

                Specification.Assert(entry.Value is 1, $"Unexpected value {entry.Value}.");
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestTaskLivenessPropertyLongRunning()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();

                var task = Task.Run(async () =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        await Task.Yield();
                        entry.Value++;
                    }
                });

                while (true)
                {
                    if (entry.Value is 10)
                    {
                        break;
                    }

                    await Task.Delay(10);
                }

                await task;

                Specification.Assert(entry.Value is 10, $"Unexpected value {entry.Value}.");
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestTaskLivenessPropertyWithDoubleDelay()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();

                var task = Task.Run(async () =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        if (i is 9)
                        {
                            entry.Value = 1;
                        }

                        await Task.Delay(10);
                    }
                });

                while (true)
                {
                    if (entry.Value is 1)
                    {
                        break;
                    }

                    await Task.Delay(10);
                }

                await task;

                Specification.Assert(entry.Value is 1, $"Unexpected value {entry.Value}.");
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestTaskLivenessPropertyLongRunningFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();

                var task = Task.Run(async () =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        await Task.Yield();
                        entry.Value--;
                    }
                });

                while (true)
                {
                    if (entry.Value is 10)
                    {
                        break;
                    }

                    await Task.Delay(10);
                }

                await task;

                Specification.Assert(entry.Value is 10, $"Unexpected value {entry.Value}.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            errorChecker: (e) =>
            {
                Assert.StartsWith("Found liveness bug at the end of program execution.", e);
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestTaskLivenessPropertyFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();

                var task = Task.Run(async () =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        await Task.Yield();
                        entry.Value--;
                    }
                });

                while (true)
                {
                    if (entry.Value is 10)
                    {
                        break;
                    }

                    await Task.Delay(500);
                }

                await task;

                Specification.Assert(entry.Value is 10, $"Unexpected value {entry.Value}.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            errorChecker: (e) =>
            {
                Assert.StartsWith("Found liveness bug at the end of program execution.", e);
            },
            replay: true);
        }
    }
}
