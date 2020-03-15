// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tasks;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.SystematicTesting.Tests.Tasks
{
    public class TaskResultTests : BaseSystematicTest
    {
        public TaskResultTests(ITestOutputHelper output)
            : base(output)
        {
        }

        [Fact(Timeout = 5000)]
        public void TestParallelTaskResultBeforeWrite()
        {
            this.Test(() =>
            {
                SharedEntry entry = new SharedEntry();

                ControlledTask<int> task = ControlledTask.Run(() =>
                {
                    entry.Value = 3;
                    return 7;
                });

                int result = task.Result;
                entry.Value = 5;

                Specification.Assert(result == 7, "Result is {0} instead of 7.", result);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestParallelTaskResultAfterWrite()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();

                ControlledTask<int> task = ControlledTask.Run(() =>
                {
                    entry.Value = 3;
                    return 7;
                });

                entry.Value = 5;
                int result = task.Result;

                Specification.Assert(result == 7, "Result is {0} instead of 7.", result);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private static async ControlledTask WriteAsync(SharedEntry entry, int value)
        {
            await ControlledTask.CompletedTask;
            entry.Value = value;
        }

        [Fact(Timeout = 5000)]
        public void TestParallelTaskResultWithSynchronousInvocationAfterWrite()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();

                ControlledTask<int> task = ControlledTask.Run(async () =>
                {
                    await WriteAsync(entry, 3);
                    return 7;
                });

                entry.Value = 5;
                int result = task.Result;

                Specification.Assert(result == 7, "Result is {0} instead of 7.", result);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private static async ControlledTask WriteWithDelayAsync(SharedEntry entry, int value)
        {
            await ControlledTask.Delay(1);
            entry.Value = value;
        }

        [Fact(Timeout = 5000)]
        public void TestParallelTaskResultWithAsynchronousInvocationAfterWrite()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();

                ControlledTask<int> task = ControlledTask.Run(async () =>
                {
                    await WriteWithDelayAsync(entry, 3);
                    return 7;
                });

                entry.Value = 5;
                int result = task.Result;

                Specification.Assert(result == 7, "Result is {0} instead of 7.", result);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithNumberOfIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
    }
}
