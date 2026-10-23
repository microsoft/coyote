// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class TaskWaitAllTests : BaseBugFindingTest
    {
        public TaskWaitAllTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private static async Task WriteAsync(SharedEntry entry, int value)
        {
            await Task.CompletedTask;
            entry.Value = value;
        }

        private static async Task WriteWithDelayAsync(SharedEntry entry, int value)
        {
            await Task.Delay(1);
            entry.Value = value;
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoSynchronousTasks()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();
                Task task1 = WriteAsync(entry, 5);
                Task task2 = WriteAsync(entry, 3);
                Task.WaitAll(task1, task2);
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoAsynchronousTasks()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();
                Task task1 = WriteWithDelayAsync(entry, 3);
                Task task2 = WriteWithDelayAsync(entry, 5);
                Task.WaitAll(task1, task2);
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoParallelTasks()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();

                Task task1 = Task.Run(async () =>
                {
                    await WriteAsync(entry, 3);
                });

                Task task2 = Task.Run(async () =>
                {
                    await WriteAsync(entry, 5);
                });

                Task.WaitAll(task1, task2);
                AssertSharedEntryValue(entry, 5);
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Value is 3 instead of 5.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoSynchronousTaskWithResults()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();
                Task<int> task1 = entry.GetWriteResultAsync(5);
                Task<int> task2 = entry.GetWriteResultAsync(3);
                Task.WaitAll(task1, task2);
                Specification.Assert(task1.Result == 5 && task2.Result is 3, "Found unexpected value.");
                Specification.Assert(task1.Result == task2.Result, "Results are not equal.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Results are not equal.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoAsynchronousTaskWithResults()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();
                Task<int> task1 = entry.GetWriteResultWithDelayAsync(5);
                Task<int> task2 = entry.GetWriteResultWithDelayAsync(3);
                Task.WaitAll(task1, task2);
                Specification.Assert(task1.Result == 5 && task2.Result is 3, "Found unexpected value.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Found unexpected value.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoParallelSynchronousTaskWithResults()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();

                Task<int> task1 = Task.Run(async () =>
                {
                    return await entry.GetWriteResultAsync(5);
                });

                Task<int> task2 = Task.Run(async () =>
                {
                    return await entry.GetWriteResultAsync(3);
                });

                Task.WaitAll(task1, task2);

                Specification.Assert(task1.Result == 5, $"The first task result is {task1.Result} instead of 5.");
                Specification.Assert(task2.Result is 3, $"The second task result is {task2.Result} instead of 3.");
                Specification.Assert(task1.Result == task2.Result, "Results are not equal.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Results are not equal.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithTwoParallelAsynchronousTaskWithResults()
        {
            this.TestWithError(() =>
            {
                SharedEntry entry = new SharedEntry();

                Task<int> task1 = Task.Run(async () =>
                {
                    return await entry.GetWriteResultWithDelayAsync(5);
                });

                Task<int> task2 = Task.Run(async () =>
                {
                    return await entry.GetWriteResultWithDelayAsync(3);
                });

                Task.WaitAll(task1, task2);

                Specification.Assert(task1.Result == 5 && task2.Result is 3, "Found unexpected value.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(200),
            expectedError: "Found unexpected value.",
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllDeadlock()
        {
            this.TestWithError(async () =>
            {
                // Test that `WaitAll` deadlocks because one of the tasks cannot complete until later.
                var tcs = new TaskCompletionSource<bool>();
                Task.WaitAll(tcs.Task, Task.Delay(1));
                tcs.SetResult(true);
                await tcs.Task;
            },
            errorChecker: (e) =>
            {
                Assert.StartsWith("Deadlock detected.", e);
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithResultsAndDeadlock()
        {
            this.TestWithError(async () =>
            {
                // Test that `WaitAll` deadlocks because one of the tasks cannot complete until later.
                var tcs = new TaskCompletionSource<bool>();
                Task.WaitAll(tcs.Task, Task.FromResult(true));
                tcs.SetResult(true);
                await tcs.Task;
            },
            errorChecker: (e) =>
            {
                Assert.StartsWith("Deadlock detected.", e);
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAllWithExceptionThrown()
        {
            this.TestWithException<InvalidOperationException>(() =>
            {
                Task[] tasks = new Task[1];
                tasks[0] = Task.Run(() =>
                {
                    throw new InvalidOperationException("Task failed.");
                });

                bool succeeded = false;
                Exception exception = null;
                try
                {
                    succeeded = Task.WaitAll(tasks, Timeout.Infinite);
                }
                catch (AggregateException e)
                {
                    exception = e.Flatten().InnerException;
                }

                Specification.Assert(!succeeded, "Waiting the task should not succeed.");
                Specification.Assert(tasks[0].Status is TaskStatus.Faulted, "The task is not faulted.");
                throw exception;
            },
            replay: true);
        }

#if NET10_0_OR_GREATER
        [Fact(Timeout = 5000)]
        public void TestWaitAllEnumerableWithAlreadyCanceledToken()
        {
            this.TestWithException<OperationCanceledException>(() =>
            {
                using var source = new CancellationTokenSource();
                source.Cancel();
                IEnumerable<Task> tasks = new[] { new TaskCompletionSource<bool>().Task };
                Task.WaitAll(tasks, source.Token);
            },
            replay: true);
        }
#endif

#if NET
        [Theory(Timeout = 10000)]
        [InlineData(false)]
#if NET10_0_OR_GREATER
        [InlineData(true)]
#endif
        public void TestWaitAllObservesCancellationWhileBlocked(bool isEnumerable)
        {
            string uncontrolled = WaitAllProvider.GetBlockedOutcome(isEnumerable, null, false);
            Assert.Equal(WaitAsyncProvider.WaitTokenCanceledOutcome, uncontrolled);

            this.Test(() =>
            {
                using var cancellation = new CancellationTokenSource();
                var pending = new TaskCompletionSource<int>();
                bool isWaitStarted = false;

                // There is no scheduling point between setting the flag and pausing the wait, so the
                // canceler can only cancel the token after the wait has started. Completing a task to
                // signal the canceler instead would introduce a scheduling point.
                Task canceler = Task.Run(async () =>
                {
                    while (!isWaitStarted)
                    {
                        await Task.Yield();
                    }

                    Specification.Assert(!cancellation.IsCancellationRequested, "The token was canceled early.");
                    cancellation.Cancel();
                });

                isWaitStarted = true;
                OperationCanceledException error = null;
                try
                {
                    WaitAll(new Task[] { pending.Task }, isEnumerable, cancellation.Token);
                }
                catch (OperationCanceledException ex)
                {
                    error = ex;
                }

                Specification.Assert(error != null, "The wait was not canceled.");
                Specification.Assert(error.CancellationToken == cancellation.Token,
                    "The wait was canceled with an unexpected token.");
                Specification.Assert(!pending.Task.IsCompleted, "The pending task completed.");
                canceler.Wait();
                Specification.Assert(canceler.Status is TaskStatus.RanToCompletion, "The canceler did not complete.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(20)
                .WithPartiallyControlledConcurrencyAllowed(false)
                .WithSystematicFuzzingFallbackEnabled(false));
        }

        [Theory(Timeout = 15000)]
        [InlineData(false, WaitAsyncSourceEvent.Result, false)]
        [InlineData(false, WaitAsyncSourceEvent.Result, true)]
        [InlineData(false, WaitAsyncSourceEvent.Fault, false)]
        [InlineData(false, WaitAsyncSourceEvent.Fault, true)]
        [InlineData(false, WaitAsyncSourceEvent.Cancellation, false)]
        [InlineData(false, WaitAsyncSourceEvent.Cancellation, true)]
#if NET10_0_OR_GREATER
        [InlineData(true, WaitAsyncSourceEvent.Result, false)]
        [InlineData(true, WaitAsyncSourceEvent.Result, true)]
        [InlineData(true, WaitAsyncSourceEvent.Fault, false)]
        [InlineData(true, WaitAsyncSourceEvent.Fault, true)]
        [InlineData(true, WaitAsyncSourceEvent.Cancellation, false)]
        [InlineData(true, WaitAsyncSourceEvent.Cancellation, true)]
#endif
        public void TestWaitAllPreservesFirstEventWhileBlocked(bool isEnumerable, WaitAsyncSourceEvent sourceEvent,
            bool isCancellationFirst)
        {
            // The uncontrolled wait is blocked when both events happen, so a completion that precedes
            // the cancellation completes the wait, whereas a cancellation that precedes the completion
            // can race with it, in which case only the canceled outcome is asserted. A canceled task
            // completes the wait with an aggregate exception, unless the wait observes the requested
            // cancellation first, which depends on when the blocked wait wakes up, so both are allowed.
            string[] possibleOutcomes = isCancellationFirst ?
                new[] { WaitAsyncProvider.WaitTokenCanceledOutcome } :
                sourceEvent switch
                {
                    WaitAsyncSourceEvent.Result => new[] { WaitAsyncProvider.CompletedOutcome },
                    WaitAsyncSourceEvent.Fault => new[] { $"aggregate({nameof(InvalidOperationException)})" },
                    _ => new[]
                    {
                        WaitAsyncProvider.WaitTokenCanceledOutcome,
                        $"aggregate({nameof(TaskCanceledException)})"
                    }
                };
            string uncontrolled = WaitAllProvider.GetBlockedOutcome(isEnumerable, sourceEvent, isCancellationFirst);
            if (!isCancellationFirst)
            {
                Assert.Contains(uncontrolled, possibleOutcomes);
            }

            this.Test(() =>
            {
                using var cancellation = new CancellationTokenSource();
                using var sourceCancellation = new CancellationTokenSource();
                var pending = new TaskCompletionSource<int>();
                bool isWaitStarted = false;
                Task completer = Task.Run(async () =>
                {
                    while (!isWaitStarted)
                    {
                        await Task.Yield();
                    }

                    if (isCancellationFirst)
                    {
                        cancellation.Cancel();
                        CompleteSource(pending, sourceEvent, sourceCancellation);
                    }
                    else
                    {
                        CompleteSource(pending, sourceEvent, sourceCancellation);
                        cancellation.Cancel();
                    }
                });

                isWaitStarted = true;
                string actual = GetOutcome(new Task[] { pending.Task }, isEnumerable, cancellation.Token);
                completer.Wait();
                Specification.Assert(Array.IndexOf(possibleOutcomes, actual) >= 0,
                    "Found outcome '{0}' instead of an expected outcome '{1}'.",
                    actual, string.Join("' or '", possibleOutcomes));
            },
            configuration: this.GetConfiguration().WithTestingIterations(20)
                .WithPartiallyControlledConcurrencyAllowed(false)
                .WithSystematicFuzzingFallbackEnabled(false));
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
#if NET10_0_OR_GREATER
        [InlineData(true)]
#endif
        public void TestWaitAllWithCompletedTasksAndAlreadyCanceledToken(bool isEnumerable)
        {
            using var uncontrolledCancellation = new CancellationTokenSource();
            uncontrolledCancellation.Cancel();
            string uncontrolled = WaitAllProvider.GetOutcome(
                new[] { Task.CompletedTask }, isEnumerable, uncontrolledCancellation.Token);
            string uncontrolledEmpty = WaitAllProvider.GetOutcome(
                Array.Empty<Task>(), isEnumerable, uncontrolledCancellation.Token);

            this.Test(() =>
            {
                using var cancellation = new CancellationTokenSource();
                cancellation.Cancel();
                string actual = GetOutcome(new[] { Task.CompletedTask }, isEnumerable, cancellation.Token);
                Specification.Assert(actual == uncontrolled,
                    "Found outcome '{0}' instead of the uncontrolled outcome '{1}'.", actual, uncontrolled);
                string actualEmpty = GetOutcome(Array.Empty<Task>(), isEnumerable, cancellation.Token);
                Specification.Assert(actualEmpty == uncontrolledEmpty,
                    "Found outcome '{0}' instead of the uncontrolled outcome '{1}'.", actualEmpty, uncontrolledEmpty);
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
#if NET10_0_OR_GREATER
        [InlineData(true)]
#endif
        public void TestWaitAllWithCancelableTokenAndControlledCompletion(bool isEnumerable)
        {
            this.Test(() =>
            {
                using var cancellation = new CancellationTokenSource();
                var pending = new TaskCompletionSource<int>();
                Task producer = Task.Run(() => pending.SetResult(WaitAsyncProvider.ExpectedResult));
                WaitAll(new Task[] { pending.Task, producer }, isEnumerable, cancellation.Token);
                Specification.Assert(pending.Task.IsCompleted, "The pending task did not complete.");
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
#if NET10_0_OR_GREATER
        [InlineData(true)]
#endif
        public void TestWaitAllWithNeverCanceledTokenDeadlock(bool isEnumerable)
        {
            this.TestWithError(() =>
            {
                using var cancellation = new CancellationTokenSource();
                var pending = new TaskCompletionSource<int>();
                WaitAll(new Task[] { pending.Task }, isEnumerable, cancellation.Token);
            },
            errorChecker: (e) =>
            {
                Assert.StartsWith("Deadlock detected.", e);
            },
            replay: true);
        }

        private static void WaitAll(Task[] tasks, bool isEnumerable, CancellationToken cancellationToken)
        {
#if NET10_0_OR_GREATER
            if (isEnumerable)
            {
                Task.WaitAll((IEnumerable<Task>)tasks, cancellationToken);
                return;
            }
#else
            Assert.False(isEnumerable, "The enumerable overload requires .NET 10.");
#endif

            Task.WaitAll(tasks, cancellationToken);
        }

        private static string GetOutcome(Task[] tasks, bool isEnumerable, CancellationToken cancellationToken)
        {
            try
            {
                WaitAll(tasks, isEnumerable, cancellationToken);
                return WaitAsyncProvider.CompletedOutcome;
            }
            catch (Exception ex) when (!(ex is ThreadInterruptedException))
            {
                return WaitAllProvider.GetExceptionOutcome(ex, cancellationToken);
            }
        }

        private static void CompleteSource(TaskCompletionSource<int> source, WaitAsyncSourceEvent sourceEvent,
            CancellationTokenSource sourceCancellation)
        {
            switch (sourceEvent)
            {
                case WaitAsyncSourceEvent.Result:
                    source.SetResult(WaitAsyncProvider.ExpectedResult);
                    break;
                case WaitAsyncSourceEvent.Fault:
                    source.SetException(new InvalidOperationException(WaitAsyncProvider.ExpectedFaultMessage));
                    break;
                default:
                    sourceCancellation.Cancel();
                    source.SetCanceled(sourceCancellation.Token);
                    break;
            }
        }
#endif
    }
}
