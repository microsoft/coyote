// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if BINARY_REWRITE
using System.Threading.Tasks;
#else
using Microsoft.Coyote.Tasks;
#endif
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

#if BINARY_REWRITE
namespace Microsoft.Coyote.BinaryRewriting.Tests.Tasks
#else
namespace Microsoft.Coyote.Production.Tests.Tasks
#endif
{
    public class TaskResultTests : BaseProductionTest
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

                Task<int> task = Task.Run(() =>
                {
                    entry.Value = 3;
                    return 7;
                });

                int result = task.Result;
                entry.Value = 5;

                Specification.Assert(result == 7, "Result is {0} instead of 7.", result);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestParallelTaskResultAfterWrite()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();

                Task<int> task = Task.Run(() =>
                {
                    entry.Value = 3;
                    return 7;
                });

                entry.Value = 5;
                int result = task.Result;

                Specification.Assert(result == 7, "Result is {0} instead of 7.", result);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private static async Task WriteAsync(SharedEntry entry, int value)
        {
            await Task.CompletedTask;
            entry.Value = value;
        }

        [Fact(Timeout = 5000)]
        public void TestParallelTaskResultWithSynchronousInvocationAfterWrite()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();

                Task<int> task = Task.Run(async () =>
                {
                    await WriteAsync(entry, 3);
                    return 7;
                });

                entry.Value = 5;
                int result = task.Result;

                Specification.Assert(result == 7, "Result is {0} instead of 7.", result);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        private static async Task WriteWithDelayAsync(SharedEntry entry, int value)
        {
            await Task.Delay(1);
            entry.Value = value;
        }

        [Fact(Timeout = 5000)]
        public void TestParallelTaskResultWithAsynchronousInvocationAfterWrite()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();

                Task<int> task = Task.Run(async () =>
                {
                    await WriteWithDelayAsync(entry, 3);
                    return 7;
                });

                entry.Value = 5;
                int result = task.Result;

                Specification.Assert(result == 7, "Result is {0} instead of 7.", result);
                Specification.Assert(entry.Value == 5, "Value is {0} instead of 5.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }
    }
}
