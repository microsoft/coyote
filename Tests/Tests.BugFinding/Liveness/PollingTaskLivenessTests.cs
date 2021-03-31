// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class PollingTaskLivenessTests : BaseBugFindingTest
    {
        public PollingTaskLivenessTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestPollingTaskLivenessProperty()
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
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestPollingTaskLivenessPropertyWithDoubleDelay()
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
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Fact(Timeout = 5000)]
        public void TestPollingTaskLivenessPropertyFailure()
        {
            this.TestWithError(async () =>
            {
                var pollingTask = Task.Run(async () =>
                {
                    while (true)
                    {
                        await Task.Delay(10);
                    }
                });

                Specification.IsEventuallyCompletedSuccessfully(pollingTask);
                await pollingTask;
            },
            configuration: this.GetConfiguration().WithMaxSchedulingSteps(100),
            errorChecker: (e) =>
            {
                Assert.StartsWith("Found potential liveness bug at the end of program execution.", e);
            },
            replay: true);
        }
    }
}
