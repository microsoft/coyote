// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
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
        public void TestWaitTaskWithTimeout()
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
        public void TestWaitTaskWithCancellationToken()
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
        public void TestWaitTaskWithTimeoutAndCancellationToken()
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

        [Fact(Timeout = 5000)]
        public void TestWaitTaskWithException()
        {
            this.TestWithError(() =>
            {
                var task = Task.Run(() =>
                {
                    ThrowException<InvalidOperationException>();
                });

                AggregateException exception = null;

                try
                {
                    task.Wait();
                }
                catch (Exception ex)
                {
                    exception = ex as AggregateException;
                }

                Specification.Assert(exception != null, "Expected an `AggregateException`.");

                Exception innerException = exception.InnerExceptions.FirstOrDefault();
                Specification.Assert(innerException is InvalidOperationException,
                    $"The inner exception is `{innerException.GetType()}`.");

                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithTestingIterations(1),
            expectedError: "Reached test assertion.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAsyncTaskWithException()
        {
            this.TestWithError(() =>
            {
                var task = Task.Run(async () =>
                {
                    await Task.Delay(1);
                    ThrowException<InvalidOperationException>();
                });

                AggregateException exception = null;

                try
                {
                    task.Wait();
                }
                catch (Exception ex)
                {
                    exception = ex as AggregateException;
                }

                Specification.Assert(exception != null, "Expected an `AggregateException`.");

                Exception innerException = exception.InnerExceptions.FirstOrDefault();
                Specification.Assert(innerException is InvalidOperationException,
                    $"The inner exception is `{innerException.GetType()}`.");

                Specification.Assert(false, "Reached test assertion.");
            },
            configuration: GetConfiguration().WithTestingIterations(1),
            expectedError: "Reached test assertion.",
            replay: true);
        }
    }
}
