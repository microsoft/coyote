// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Threading;
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
    public class TaskWaitTests : BaseProductionTest
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
            configuration: GetConfiguration().WithTestingIterations(200));
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
            configuration: GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitWithTimeout()
        {
            // TODO: we do not yet support timeouts in testing, so we will improve this test later,
            // for now we just want to make sure it executes under binary rewriting.
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry
                {
                    Value = 5
                };

                Task task = Task.Run(() =>
                {
                    entry.Value = 3;
                });

                task.Wait(10);
                await task;

                Specification.Assert(entry.Value == 3, "Value is {0} instead of 3.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestWaitWithCancellationToken()
        {
            // TODO: we do not yet support cancelation in testing, so we will improve this test later,
            // for now we just want to make sure it executes under binary rewriting.
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry
                {
                    Value = 5
                };

                var tokenSource = new CancellationTokenSource();
                Task task = Task.Run(() =>
                {
                    entry.Value = 3;
                });

                task.Wait(tokenSource.Token);
                await task;

                Specification.Assert(entry.Value == 3, "Value is {0} instead of 3.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestWaitWithTimeoutAndCancellationToken()
        {
            // TODO: we do not yet support timeout and cancelation in testing, so we will improve this test later,
            // for now we just want to make sure it executes under binary rewriting.
            this.Test(async () =>
            {
                SharedEntry entry = new SharedEntry
                {
                    Value = 5
                };

                var tokenSource = new CancellationTokenSource();
                Task task = Task.Run(() =>
                {
                    entry.Value = 3;
                });

                task.Wait(10, tokenSource.Token);
                await task;

                Specification.Assert(entry.Value == 3, "Value is {0} instead of 3.", entry.Value);
            },
            configuration: GetConfiguration().WithTestingIterations(200));
        }
    }
}
