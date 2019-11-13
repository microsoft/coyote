// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.TestingServices.Tests.Threading.Tasks
{
    public class TaskDelayTest : BaseTest
    {
        public TaskDelayTest(ITestOutputHelper output)
            : base(output)
        {
        }

        private class SharedEntry
        {
            public int Value = 0;
        }

        private static async ControlledTask WriteWithDelayAsync(SharedEntry entry, int value, int delay)
        {
            for (int i = 0; i < 2; i++)
            {
                entry.Value = value + i;
                await ControlledTask.Delay(delay);
            }
        }

        [Fact(Timeout=5000)]
        public void TestInterleavingsInLoopWithSynchronousDelays()
        {
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry();

                ControlledTask[] tasks = new ControlledTask[2];
                for (int i = 0; i < 2; i++)
                {
                    tasks[i] = WriteWithDelayAsync(entry, i, 0);
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
                    tasks[i] = WriteWithDelayAsync(entry, i, 1);
                }

                await ControlledTask.WhenAll(tasks);

                Specification.Assert(entry.Value == 2, "Value is {0} instead of 2.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 1 instead of 2.",
            replay: true);
        }
    }
}
