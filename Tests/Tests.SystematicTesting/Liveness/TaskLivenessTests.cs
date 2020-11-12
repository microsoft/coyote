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

                        await Task.Delay(10);
                    }
                });

                await Specification.WhenTrue(() =>
                {
                    if (entry.Value is 1)
                    {
                        return Task.FromResult(true);
                    }

                    return Task.FromResult(false);
                },
                () => entry.Value,
                TimeSpan.FromMilliseconds(10));

                await task;

                Specification.Assert(entry.Value is 1, $"Unexpected value {entry.Value}.");
            },
            configuration: GetConfiguration().WithTestingIterations(200));
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
                        await Task.Delay(10);
                    }
                });

                await Specification.WhenTrue(() =>
                {
                    if (entry.Value is 1)
                    {
                        return Task.FromResult(true);
                    }

                    return Task.FromResult(false);
                },
                () => entry.Value,
                TimeSpan.FromMilliseconds(10));

                await task;

                Specification.Assert(entry.Value is 1, $"Unexpected value {entry.Value}.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            errorChecker: (e) =>
            {
                Assert.StartsWith("Found liveness bug at the end of program execution.", e);
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestTaskLivenessPropertyAsync()
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

                await Specification.WhenTrue(async () =>
                {
                    if (entry.Value is 1)
                    {
                        return true;
                    }

                    await Task.Delay(10);
                    return false;
                },
                () => entry.Value,
                TimeSpan.FromMilliseconds(10));

                await task;

                Specification.Assert(entry.Value is 1, $"Unexpected value {entry.Value}.");
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestTaskLivenessPropertyAsyncFailure()
        {
            this.TestWithError(async () =>
            {
                SharedEntry entry = new SharedEntry();

                var task = Task.Run(async () =>
                {
                    for (int i = 0; i < 10; i++)
                    {
                        await Task.Delay(10);
                    }
                });

                await Specification.WhenTrue(async () =>
                {
                    if (entry.Value is 1)
                    {
                        return true;
                    }

                    await Task.Delay(10);
                    return false;
                },
                () => entry.Value,
                TimeSpan.FromMilliseconds(10));

                await task;

                Specification.Assert(entry.Value is 1, $"Unexpected value {entry.Value}.");
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            errorChecker: (e) =>
            {
                Assert.StartsWith("Found liveness bug at the end of program execution.", e);
            },
            replay: true);
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
                        await Task.Delay(10);
                        entry.Value++;
                    }
                });

                await Specification.WhenTrue(async () =>
                {
                    if (entry.Value is 10)
                    {
                        return true;
                    }

                    await Task.Delay(10);
                    return false;
                },
                () => entry.Value,
                TimeSpan.FromMilliseconds(10));

                await task;

                Specification.Assert(entry.Value is 10, $"Unexpected value {entry.Value}.");
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
                        await Task.Delay(10);
                        entry.Value--;
                    }
                });

                await Specification.WhenTrue(async () =>
                {
                    if (entry.Value is 10)
                    {
                        return true;
                    }

                    await Task.Delay(10);
                    return false;
                },
                () => entry.Value,
                TimeSpan.FromMilliseconds(10));

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
        public void TestTaskLivenessPropertyWithException()
        {
            this.TestWithException<NotSupportedException>(async () =>
            {
                await Specification.WhenTrue(() =>
                {
                    throw new NotSupportedException();
                },
                () => 0,
                TimeSpan.FromMilliseconds(10));
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestTaskLivenessPropertyWithHashMethodException()
        {
            this.TestWithException<NotSupportedException>(async () =>
            {
                await Specification.WhenTrue(() =>
                {
                    return Task.FromResult(false);
                },
                () => throw new NotSupportedException(),
                TimeSpan.FromMilliseconds(10));
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            replay: true);
        }
    }
}
