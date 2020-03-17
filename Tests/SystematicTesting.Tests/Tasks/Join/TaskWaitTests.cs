// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class TaskWaitTests : BaseSystematicTest
    {
        public TaskWaitTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestWaitParallelTaskBeforeWrite()
        {
            this.Test(() =>
            {
                SharedEntry entry = new SharedEntry();

                Task task = Task.Run(() =>
                {
                    entry.Value = 3;
                });

                task.Wait();
                entry.Value = 5;

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestWaitParallelTaskAfterWrite()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();

                Task task = Task.Run(() =>
                {
                    entry.Value = 3;
                });

                entry.Value = 5;
                task.Wait();

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private static async Task WriteAsync(SharedEntry entry, int value)
        {
            await Task.CompletedTask;
            entry.Value = value;
        }

        [Fact(Timeout = 5000)]
        public void TestWaitParallelTaskWithSynchronousInvocationAfterWrite()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();

                Task task = Task.Run(async () =>
                {
                    await WriteAsync(entry, 3);
                });

                entry.Value = 5;
                task.Wait();

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private static async Task WriteWithDelayAsync(SharedEntry entry, int value)
        {
            await Task.Delay(1);
            entry.Value = value;
        }

        [Fact(Timeout = 5000)]
        public void TestWaitParallelTaskWithAsynchronousInvocationAfterWrite()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();

                Task task = Task.Run(async () =>
                {
                    await WriteWithDelayAsync(entry, 3);
                });

                entry.Value = 5;
                task.Wait();

                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
    }
}
