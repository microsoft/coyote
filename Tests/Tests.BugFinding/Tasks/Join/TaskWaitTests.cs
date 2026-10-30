// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class TaskWaitTests : BaseBugFindingTest
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

                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
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

                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
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

                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
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

                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
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

                AssertSharedEntryValue(entry, 3);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
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

                AssertSharedEntryValue(entry, 3);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
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

                AssertSharedEntryValue(entry, 3);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Theory(Timeout = 10000)]
        [InlineData(0, false)]
        [InlineData(0, true)]
        [InlineData(1, false)]
        [InlineData(1, true)]
#if NET7_0_OR_GREATER
        [InlineData(2, false)]
        [InlineData(2, true)]
#endif
        public void TestWaitTaskObservesCancellation(int overload, bool isAlreadyCanceled)
        {
            this.Test(() =>
            {
                using var cancellation = new CancellationTokenSource();
                var pending = new TaskCompletionSource<int>();
                bool isWaitStarted = false;
                Task canceler = Task.CompletedTask;
                if (isAlreadyCanceled)
                {
                    cancellation.Cancel();
                }
                else
                {
                    canceler = Task.Run(async () =>
                    {
                        while (!isWaitStarted)
                        {
                            await Task.Yield();
                        }

                        cancellation.Cancel();
                    });
                }

                // No scheduling point separates this flag from entering the controlled wait.
                isWaitStarted = true;
                OperationCanceledException error = null;
                try
                {
                    WaitWithCancellation(pending.Task, overload, cancellation.Token);
                }
                catch (OperationCanceledException ex)
                {
                    error = ex;
                }

                Specification.Assert(error != null, "The wait was not canceled.");
                Specification.Assert(error.CancellationToken == cancellation.Token,
                    "The wait was canceled with an unexpected token.");
                Specification.Assert(!pending.Task.IsCompleted, "Canceling the wait completed the task.");
                canceler.Wait();
            },
            configuration: this.GetConfiguration().WithTestingIterations(20)
                .WithPartiallyControlledConcurrencyAllowed(false)
                .WithSystematicFuzzingFallbackEnabled(false));
        }

        [Theory(Timeout = 5000)]
        [InlineData(0)]
        [InlineData(1)]
#if NET7_0_OR_GREATER
        [InlineData(2)]
#endif
        public void TestWaitCompletedTaskWithCanceledWaitToken(int overload)
        {
            this.Test(() =>
            {
                using var cancellation = new CancellationTokenSource();
                cancellation.Cancel();
                bool completed = WaitWithCancellation(Task.CompletedTask, overload, cancellation.Token);
                Specification.Assert(completed, "The completed task did not complete the wait.");
            });
        }

        [Theory(Timeout = 5000)]
        [InlineData(0)]
        [InlineData(1)]
#if NET7_0_OR_GREATER
        [InlineData(2)]
#endif
        public void TestWaitTaskWithNeverCanceledWaitTokenDeadlock(int overload)
        {
            this.TestWithError(() =>
            {
                using var cancellation = new CancellationTokenSource();
                var pending = new TaskCompletionSource<int>();
                WaitWithCancellation(pending.Task, overload, cancellation.Token);
            },
            errorChecker: error => Assert.StartsWith("Deadlock detected.", error),
            replay: true);
        }

        private static bool WaitWithCancellation(Task task, int overload, CancellationToken cancellationToken)
        {
            switch (overload)
            {
                case 0:
                    task.Wait(cancellationToken);
                    return true;
                case 1:
                    return task.Wait(Timeout.Infinite, cancellationToken);
#if NET7_0_OR_GREATER
                case 2:
                    return task.Wait(Timeout.InfiniteTimeSpan, cancellationToken);
#endif
                default:
                    throw new ArgumentOutOfRangeException(nameof(overload));
            }
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
            configuration: this.GetConfiguration().WithTestingIterations(1),
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
            configuration: this.GetConfiguration().WithTestingIterations(1),
            expectedError: "Reached test assertion.",
            replay: true);
        }
    }
}
