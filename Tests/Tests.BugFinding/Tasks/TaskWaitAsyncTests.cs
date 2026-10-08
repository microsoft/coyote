// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET8_0_OR_GREATER
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.Tests.Common.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Coyote.BugFinding.Tests
{
    public class TaskWaitAsyncTests : BaseBugFindingTest
    {
        public TaskWaitAsyncTests(ITestOutputHelper output)
            : base(output)
        {
        }

        private static readonly TimeSpan FiniteTimeout = TimeSpan.FromMilliseconds(10);
        private static readonly TimeSpan LongTimeout = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan RuntimeSupportedLargeTimeout = TimeSpan.FromDays(30);
        private static readonly TimeSpan InvalidTimeout = TimeSpan.FromMilliseconds(-2);

        [Fact(Timeout = 5000)]
        public void TestWaitAsyncAcceptsRuntimeSupportedLargeTimeout()
        {
            using var uncontrolled = new CancellationTokenSource();
            string outcome = WaitAsyncProvider.GetOutcome(
                WaitAsyncProvider.CreateCompletedTask(), RuntimeSupportedLargeTimeout, uncontrolled.Token);
            string resultOutcome = WaitAsyncProvider.GetResultOutcome(
                WaitAsyncProvider.CreateCompletedResultTask(), RuntimeSupportedLargeTimeout, uncontrolled.Token);
            string timeProviderOutcome = WaitAsyncProvider.GetOutcomeWithTimeProvider(
                WaitAsyncProvider.CreateCompletedTask(), RuntimeSupportedLargeTimeout, uncontrolled.Token);
            string resultTimeProviderOutcome = WaitAsyncProvider.GetResultOutcomeWithTimeProvider(
                WaitAsyncProvider.CreateCompletedResultTask(), RuntimeSupportedLargeTimeout, uncontrolled.Token);
            Assert.Equal(WaitAsyncProvider.CompletedOutcome, outcome);
            Assert.Equal(WaitAsyncProvider.GetCompletedOutcome(WaitAsyncProvider.ExpectedResult), resultOutcome);
            Assert.Equal(WaitAsyncProvider.CompletedOutcome, timeProviderOutcome);
            Assert.Equal(WaitAsyncProvider.GetCompletedOutcome(WaitAsyncProvider.ExpectedResult),
                resultTimeProviderOutcome);

            this.Test(async () =>
            {
                using var source = new CancellationTokenSource();
                await AssertOutcomeAsync(outcome, ct =>
                    Task.CompletedTask.WaitAsync(RuntimeSupportedLargeTimeout, ct), source.Token);
                await AssertResultOutcomeAsync(resultOutcome, ct =>
                    Task.FromResult(WaitAsyncProvider.ExpectedResult).WaitAsync(
                        RuntimeSupportedLargeTimeout, ct), source.Token);
                await AssertOutcomeAsync(timeProviderOutcome, ct =>
                    Task.CompletedTask.WaitAsync(RuntimeSupportedLargeTimeout, TimeProvider.System, ct),
                    source.Token);
                await AssertResultOutcomeAsync(resultTimeProviderOutcome, ct =>
                    Task.FromResult(WaitAsyncProvider.ExpectedResult).WaitAsync(
                        RuntimeSupportedLargeTimeout, TimeProvider.System, ct), source.Token);
            },
            configuration: this.GetConfiguration().WithTestingIterations(1));
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAsyncWithAlreadyCanceledTokenAndIncompleteTask()
        {
            using var uncontrolled = new CancellationTokenSource();
            uncontrolled.Cancel();

            // The uncontrolled overloads deterministically prefer cancellation over the timeout.
            string zero = WaitAsyncProvider.GetOutcome(
                WaitAsyncProvider.CreatePendingTask(), TimeSpan.Zero, uncontrolled.Token);
            string finite = WaitAsyncProvider.GetOutcome(
                WaitAsyncProvider.CreatePendingTask(), FiniteTimeout, uncontrolled.Token);
            string infinite = WaitAsyncProvider.GetOutcome(
                WaitAsyncProvider.CreatePendingTask(), Timeout.InfiniteTimeSpan, uncontrolled.Token);
            string token = WaitAsyncProvider.GetOutcome(
                WaitAsyncProvider.CreatePendingTask(), uncontrolled.Token);
            string timeProvider = WaitAsyncProvider.GetOutcomeWithTimeProvider(
                WaitAsyncProvider.CreatePendingTask(), TimeSpan.Zero, uncontrolled.Token);
            Assert.Equal(WaitAsyncProvider.WaitTokenCanceledOutcome, zero);
            Assert.Equal(WaitAsyncProvider.WaitTokenCanceledOutcome, finite);
            Assert.Equal(WaitAsyncProvider.WaitTokenCanceledOutcome, infinite);
            Assert.Equal(WaitAsyncProvider.WaitTokenCanceledOutcome, token);
            Assert.Equal(WaitAsyncProvider.WaitTokenCanceledOutcome, timeProvider);

            this.Test(async () =>
            {
                using var source = new CancellationTokenSource();
                source.Cancel();

                await AssertOutcomeAsync(zero, ct =>
                    new TaskCompletionSource<bool>().Task.WaitAsync(TimeSpan.Zero, ct), source.Token);
                await AssertOutcomeAsync(finite, ct =>
                    new TaskCompletionSource<bool>().Task.WaitAsync(FiniteTimeout, ct), source.Token);
                await AssertOutcomeAsync(infinite, ct =>
                    new TaskCompletionSource<bool>().Task.WaitAsync(Timeout.InfiniteTimeSpan, ct), source.Token);
                await AssertOutcomeAsync(token, ct =>
                    new TaskCompletionSource<bool>().Task.WaitAsync(ct), source.Token);
                await AssertOutcomeAsync(timeProvider, ct =>
                    new TaskCompletionSource<bool>().Task.WaitAsync(TimeSpan.Zero, TimeProvider.System, ct),
                    source.Token);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAsyncWithAlreadyCanceledTokenAndIncompleteGenericTask()
        {
            using var uncontrolled = new CancellationTokenSource();
            uncontrolled.Cancel();

            string zero = WaitAsyncProvider.GetResultOutcome(
                WaitAsyncProvider.CreatePendingResultTask(), TimeSpan.Zero, uncontrolled.Token);
            string finite = WaitAsyncProvider.GetResultOutcome(
                WaitAsyncProvider.CreatePendingResultTask(), FiniteTimeout, uncontrolled.Token);
            string infinite = WaitAsyncProvider.GetResultOutcome(
                WaitAsyncProvider.CreatePendingResultTask(), Timeout.InfiniteTimeSpan, uncontrolled.Token);
            string token = WaitAsyncProvider.GetResultOutcome(
                WaitAsyncProvider.CreatePendingResultTask(), uncontrolled.Token);
            string timeProvider = WaitAsyncProvider.GetResultOutcomeWithTimeProvider(
                WaitAsyncProvider.CreatePendingResultTask(), TimeSpan.Zero, uncontrolled.Token);
            Assert.Equal(WaitAsyncProvider.WaitTokenCanceledOutcome, zero);
            Assert.Equal(WaitAsyncProvider.WaitTokenCanceledOutcome, finite);
            Assert.Equal(WaitAsyncProvider.WaitTokenCanceledOutcome, infinite);
            Assert.Equal(WaitAsyncProvider.WaitTokenCanceledOutcome, token);
            Assert.Equal(WaitAsyncProvider.WaitTokenCanceledOutcome, timeProvider);

            this.Test(async () =>
            {
                using var source = new CancellationTokenSource();
                source.Cancel();

                await AssertResultOutcomeAsync(zero, ct =>
                    new TaskCompletionSource<int>().Task.WaitAsync(TimeSpan.Zero, ct), source.Token);
                await AssertResultOutcomeAsync(finite, ct =>
                    new TaskCompletionSource<int>().Task.WaitAsync(FiniteTimeout, ct), source.Token);
                await AssertResultOutcomeAsync(infinite, ct =>
                    new TaskCompletionSource<int>().Task.WaitAsync(Timeout.InfiniteTimeSpan, ct), source.Token);
                await AssertResultOutcomeAsync(token, ct =>
                    new TaskCompletionSource<int>().Task.WaitAsync(ct), source.Token);
                await AssertResultOutcomeAsync(timeProvider, ct =>
                    new TaskCompletionSource<int>().Task.WaitAsync(TimeSpan.Zero, TimeProvider.System, ct),
                    source.Token);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAsyncWithAlreadyCanceledTokenAndCompletedTask()
        {
            using var uncontrolled = new CancellationTokenSource();
            uncontrolled.Cancel();
            using var uncontrolledSource = new CancellationTokenSource();
            uncontrolledSource.Cancel();

            // An already completed task takes precedence over the already canceled token.
            string completed = WaitAsyncProvider.GetOutcome(
                WaitAsyncProvider.CreateCompletedTask(), TimeSpan.Zero, uncontrolled.Token);
            string faulted = WaitAsyncProvider.GetOutcome(
                WaitAsyncProvider.CreateFaultedTask(), TimeSpan.Zero, uncontrolled.Token);
            string canceled = WaitAsyncProvider.GetOutcome(
                WaitAsyncProvider.CreateCanceledTask(uncontrolledSource.Token), TimeSpan.Zero, uncontrolled.Token);
            string completedWithFiniteTimeout = WaitAsyncProvider.GetOutcome(
                WaitAsyncProvider.CreateCompletedTask(), FiniteTimeout, uncontrolled.Token);
            Assert.Equal(WaitAsyncProvider.CompletedOutcome, completed);
            Assert.Equal($"fault({nameof(InvalidOperationException)})", faulted);
            Assert.Equal(WaitAsyncProvider.SourceTokenCanceledOutcome, canceled);
            Assert.Equal(WaitAsyncProvider.CompletedOutcome, completedWithFiniteTimeout);

            this.Test(async () =>
            {
                using var source = new CancellationTokenSource();
                source.Cancel();
                using var otherSource = new CancellationTokenSource();
                otherSource.Cancel();

                await AssertOutcomeAsync(completed, ct =>
                    Task.CompletedTask.WaitAsync(TimeSpan.Zero, ct), source.Token);
                await AssertOutcomeAsync(faulted, ct => Task.FromException(new InvalidOperationException(
                    WaitAsyncProvider.ExpectedFaultMessage)).WaitAsync(TimeSpan.Zero, ct), source.Token);
                await AssertOutcomeAsync(canceled, ct =>
                    Task.FromCanceled(otherSource.Token).WaitAsync(TimeSpan.Zero, ct), source.Token);
                await AssertOutcomeAsync(completedWithFiniteTimeout, ct =>
                    Task.CompletedTask.WaitAsync(FiniteTimeout, ct), source.Token);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAsyncWithAlreadyCanceledTokenAndCompletedGenericTask()
        {
            using var uncontrolled = new CancellationTokenSource();
            uncontrolled.Cancel();
            using var uncontrolledSource = new CancellationTokenSource();
            uncontrolledSource.Cancel();

            string completed = WaitAsyncProvider.GetResultOutcome(
                WaitAsyncProvider.CreateCompletedResultTask(), TimeSpan.Zero, uncontrolled.Token);
            string faulted = WaitAsyncProvider.GetResultOutcome(
                WaitAsyncProvider.CreateFaultedResultTask(), TimeSpan.Zero, uncontrolled.Token);
            string canceled = WaitAsyncProvider.GetResultOutcome(
                WaitAsyncProvider.CreateCanceledResultTask(uncontrolledSource.Token), TimeSpan.Zero,
                uncontrolled.Token);
            string completedWithFiniteTimeout = WaitAsyncProvider.GetResultOutcome(
                WaitAsyncProvider.CreateCompletedResultTask(), FiniteTimeout, uncontrolled.Token);
            Assert.Equal(WaitAsyncProvider.GetCompletedOutcome(WaitAsyncProvider.ExpectedResult), completed);
            Assert.Equal($"fault({nameof(InvalidOperationException)})", faulted);
            Assert.Equal(WaitAsyncProvider.SourceTokenCanceledOutcome, canceled);
            Assert.Equal(WaitAsyncProvider.GetCompletedOutcome(WaitAsyncProvider.ExpectedResult),
                completedWithFiniteTimeout);

            this.Test(async () =>
            {
                using var source = new CancellationTokenSource();
                source.Cancel();
                using var otherSource = new CancellationTokenSource();
                otherSource.Cancel();

                await AssertResultOutcomeAsync(completed, ct =>
                    Task.FromResult(WaitAsyncProvider.ExpectedResult).WaitAsync(TimeSpan.Zero, ct), source.Token);
                await AssertResultOutcomeAsync(faulted, ct => Task.FromException<int>(
                    new InvalidOperationException(WaitAsyncProvider.ExpectedFaultMessage)).WaitAsync(
                    TimeSpan.Zero, ct), source.Token);
                await AssertResultOutcomeAsync(canceled, ct =>
                    Task.FromCanceled<int>(otherSource.Token).WaitAsync(TimeSpan.Zero, ct), source.Token);
                await AssertResultOutcomeAsync(completedWithFiniteTimeout, ct =>
                    Task.FromResult(WaitAsyncProvider.ExpectedResult).WaitAsync(FiniteTimeout, ct), source.Token);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAsyncTimesOutWithUncanceledToken()
        {
            using var uncontrolled = new CancellationTokenSource();

            // A token that is not canceled must not suppress the timeout.
            string outcome = WaitAsyncProvider.GetOutcome(
                WaitAsyncProvider.CreatePendingTask(), TimeSpan.Zero, uncontrolled.Token);
            string resultOutcome = WaitAsyncProvider.GetResultOutcome(
                WaitAsyncProvider.CreatePendingResultTask(), TimeSpan.Zero, uncontrolled.Token);
            Assert.Equal(WaitAsyncProvider.TimedOutOutcome, outcome);
            Assert.Equal(WaitAsyncProvider.TimedOutOutcome, resultOutcome);

            this.Test(async () =>
            {
                using var source = new CancellationTokenSource();
                await AssertOutcomeAsync(outcome, ct =>
                    new TaskCompletionSource<bool>().Task.WaitAsync(TimeSpan.Zero, ct), source.Token);
                await AssertResultOutcomeAsync(resultOutcome, ct =>
                    new TaskCompletionSource<int>().Task.WaitAsync(TimeSpan.Zero, ct), source.Token);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAsyncValidatesTimeoutBeforeCancellation()
        {
            using var uncontrolled = new CancellationTokenSource();
            uncontrolled.Cancel();

            // The timeout is validated before the already canceled token is observed.
            string outcome = WaitAsyncProvider.GetOutcome(
                WaitAsyncProvider.CreatePendingTask(), InvalidTimeout, uncontrolled.Token);
            string resultOutcome = WaitAsyncProvider.GetResultOutcome(
                WaitAsyncProvider.CreatePendingResultTask(), InvalidTimeout, uncontrolled.Token);
            Assert.Equal($"fault({nameof(ArgumentOutOfRangeException)})", outcome);
            Assert.Equal($"fault({nameof(ArgumentOutOfRangeException)})", resultOutcome);

            this.Test(async () =>
            {
                using var source = new CancellationTokenSource();
                source.Cancel();

                await AssertOutcomeAsync(outcome, ct =>
                    new TaskCompletionSource<bool>().Task.WaitAsync(InvalidTimeout, ct), source.Token);
                await AssertResultOutcomeAsync(resultOutcome, ct =>
                    new TaskCompletionSource<int>().Task.WaitAsync(InvalidTimeout, ct), source.Token);
            },
            configuration: this.GetConfiguration().WithTestingIterations(10));
        }

        [Theory(Timeout = 5000)]
        [InlineData(false)]
        [InlineData(true)]
        public void TestWaitAsyncWithAsynchronousSourceContinuations(bool isGeneric)
        {
            this.Test(() =>
            {
                using var cancellation = new CancellationTokenSource();
                var source = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                if (isGeneric)
                {
                    Task<int> wait = source.Task.WaitAsync(cancellation.Token);
                    source.SetResult(WaitAsyncProvider.ExpectedResult);
                    Assert.Equal(WaitAsyncProvider.ExpectedResult, wait.Result);
                }
                else
                {
                    Task task = source.Task;
                    Task wait = task.WaitAsync(cancellation.Token);
                    source.SetResult(WaitAsyncProvider.ExpectedResult);
                    wait.GetAwaiter().GetResult();
                }
            },
            configuration: this.GetConfiguration().WithTestingIterations(200)
                .WithPartiallyControlledConcurrencyAllowed(false)
                .WithSystematicFuzzingFallbackEnabled(false));
        }

        [Theory(Timeout = 5000)]
        [InlineData(false, false)]
        [InlineData(false, true)]
        [InlineData(true, false)]
        [InlineData(true, true)]
        public void TestWaitAsyncPropagatesAsynchronousSourceFailure(bool isGeneric, bool isCanceled)
        {
            this.Test(() =>
            {
                using var cancellation = new CancellationTokenSource();
                using var sourceCancellation = new CancellationTokenSource();
                var source = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
                Task wait = isGeneric ?
                    source.Task.WaitAsync(cancellation.Token) :
                    ((Task)source.Task).WaitAsync(cancellation.Token);
                if (isCanceled)
                {
                    sourceCancellation.Cancel();
                    Assert.True(source.TrySetCanceled(sourceCancellation.Token));
                    var error = Assert.ThrowsAny<OperationCanceledException>(() => wait.GetAwaiter().GetResult());
                    Assert.Equal(sourceCancellation.Token, error.CancellationToken);
                    Assert.True(wait.IsCanceled);
                }
                else
                {
                    var error = new InvalidOperationException(WaitAsyncProvider.ExpectedFaultMessage);
                    source.SetException(new Exception[] { error, new ApplicationException("second fault") });
                    Assert.Same(error, Assert.Throws<InvalidOperationException>(() => wait.GetAwaiter().GetResult()));
                    Assert.True(wait.IsFaulted);
                    Assert.Equal(2, wait.Exception.InnerExceptions.Count);
                }
            },
            configuration: this.GetConfiguration().WithTestingIterations(100)
                .WithPartiallyControlledConcurrencyAllowed(false)
                .WithSystematicFuzzingFallbackEnabled(false));
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAsyncWithLongTimeoutAndControlledCompletion()
        {
            // A finite timeout must not be able to win a race against a controlled operation
            // that completes the task, no matter which schedule is explored.
            this.Test(async () =>
            {
                using var source = new CancellationTokenSource();
                await AssertControlledCompletionAsync(task => task.WaitAsync(LongTimeout));
                await AssertControlledCompletionAsync(task => task.WaitAsync(LongTimeout, source.Token));
                await AssertControlledCompletionAsync(task => task.WaitAsync(LongTimeout, TimeProvider.System));
                await AssertControlledCompletionAsync(task =>
                    task.WaitAsync(LongTimeout, TimeProvider.System, source.Token));
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAsyncWithLongTimeoutAndControlledGenericCompletion()
        {
            this.Test(async () =>
            {
                using var source = new CancellationTokenSource();
                await AssertControlledResultCompletionAsync(task => task.WaitAsync(LongTimeout));
                await AssertControlledResultCompletionAsync(task => task.WaitAsync(LongTimeout, source.Token));
                await AssertControlledResultCompletionAsync(task => task.WaitAsync(LongTimeout, TimeProvider.System));
                await AssertControlledResultCompletionAsync(task =>
                    task.WaitAsync(LongTimeout, TimeProvider.System, source.Token));
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAsyncWithShortTimeoutAndControlledCompletion()
        {
            // The size of the timeout must not change the outcome during systematic testing.
            this.Test(async () =>
            {
                await AssertControlledCompletionAsync(task => task.WaitAsync(FiniteTimeout));
                await AssertControlledResultCompletionAsync(task => task.WaitAsync(FiniteTimeout));
            },
            configuration: this.GetConfiguration().WithTestingIterations(200));
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAsyncWithFiniteTimeoutAndControlledCancellation()
        {
            // Cancellation must still be able to terminate a wait with a finite timeout.
            this.Test(async () =>
            {
                using var source = new CancellationTokenSource();
                Task canceler = Task.Run(() => source.Cancel());
                await AssertOutcomeAsync(WaitAsyncProvider.WaitTokenCanceledOutcome, ct =>
                    new TaskCompletionSource<bool>().Task.WaitAsync(LongTimeout, ct), source.Token);
                await canceler;
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAsyncWithZeroTimeoutAndIncompleteTask()
        {
            using var uncontrolled = new CancellationTokenSource();

            // A zero timeout expires before the task gets any chance to complete.
            string outcome = WaitAsyncProvider.GetOutcome(
                WaitAsyncProvider.CreatePendingTask(), TimeSpan.Zero, uncontrolled.Token);
            string timeProviderOutcome = WaitAsyncProvider.GetOutcomeWithTimeProvider(
                WaitAsyncProvider.CreatePendingTask(), TimeSpan.Zero, uncontrolled.Token);
            string resultOutcome = WaitAsyncProvider.GetResultOutcome(
                WaitAsyncProvider.CreatePendingResultTask(), TimeSpan.Zero, uncontrolled.Token);
            string resultTimeProviderOutcome = WaitAsyncProvider.GetResultOutcomeWithTimeProvider(
                WaitAsyncProvider.CreatePendingResultTask(), TimeSpan.Zero, uncontrolled.Token);
            Assert.Equal(WaitAsyncProvider.TimedOutOutcome, outcome);
            Assert.Equal(WaitAsyncProvider.TimedOutOutcome, timeProviderOutcome);
            Assert.Equal(WaitAsyncProvider.TimedOutOutcome, resultOutcome);
            Assert.Equal(WaitAsyncProvider.TimedOutOutcome, resultTimeProviderOutcome);

            this.Test(async () =>
            {
                using var source = new CancellationTokenSource();

                await AssertOutcomeAsync(outcome, _ =>
                    new TaskCompletionSource<bool>().Task.WaitAsync(TimeSpan.Zero), source.Token);
                await AssertOutcomeAsync(timeProviderOutcome, ct =>
                    new TaskCompletionSource<bool>().Task.WaitAsync(TimeSpan.Zero, TimeProvider.System, ct),
                    source.Token);
                await AssertResultOutcomeAsync(resultOutcome, _ =>
                    new TaskCompletionSource<int>().Task.WaitAsync(TimeSpan.Zero), source.Token);
                await AssertResultOutcomeAsync(resultTimeProviderOutcome, ct =>
                    new TaskCompletionSource<int>().Task.WaitAsync(TimeSpan.Zero, TimeProvider.System, ct),
                    source.Token);
            },
            configuration: this.GetConfiguration().WithTestingIterations(100));
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAsyncWithUncompletableTask()
        {
            // A wait that no controlled operation can complete is reported as a deadlock,
            // instead of hanging the test or spuriously timing out.
            this.TestWithError(async () =>
            {
                var tcs = new TaskCompletionSource<bool>();
                await tcs.Task.WaitAsync(FiniteTimeout);
            },
            errorChecker: (e) =>
            {
                Assert.StartsWith("Deadlock detected.", e);
            },
            replay: true);
        }

        [Fact(Timeout = 5000)]
        public void TestWaitAsyncWithUncompletableGenericTaskAndCancellationToken()
        {
            this.TestWithError(async () =>
            {
                using var source = new CancellationTokenSource();
                var tcs = new TaskCompletionSource<int>();
                await tcs.Task.WaitAsync(LongTimeout, TimeProvider.System, source.Token);
            },
            errorChecker: (e) =>
            {
                Assert.StartsWith("Deadlock detected.", e);
            },
            replay: true);
        }

        /// <summary>
        /// Asserts that a wait for a task that is completed by another controlled operation
        /// completes, instead of spuriously timing out.
        /// </summary>
        private static async Task AssertControlledCompletionAsync(Func<Task, Task> wait)
        {
            var tcs = new TaskCompletionSource<bool>();
            Task producer = Task.Run(() => tcs.SetResult(true));
            string actual = await GetOutcomeAsync(_ => wait(tcs.Task), default);
            Specification.Assert(actual == WaitAsyncProvider.CompletedOutcome,
                "Found outcome '{0}' instead of the expected outcome '{1}'.",
                actual, WaitAsyncProvider.CompletedOutcome);
            await producer;
        }

        /// <summary>
        /// Asserts that a wait for a generic task that is completed by another controlled
        /// operation completes, instead of spuriously timing out.
        /// </summary>
        private static async Task AssertControlledResultCompletionAsync(Func<Task<int>, Task<int>> wait)
        {
            var tcs = new TaskCompletionSource<int>();
            Task producer = Task.Run(() => tcs.SetResult(WaitAsyncProvider.ExpectedResult));
            string actual = await GetResultOutcomeAsync(_ => wait(tcs.Task), default);
            string expected = WaitAsyncProvider.GetCompletedOutcome(WaitAsyncProvider.ExpectedResult);
            Specification.Assert(actual == expected,
                "Found outcome '{0}' instead of the expected outcome '{1}'.", actual, expected);
            await producer;
        }

        private static async Task AssertOutcomeAsync(string expected, Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken)
        {
            string actual = await GetOutcomeAsync(operation, cancellationToken);
            Specification.Assert(actual == expected,
                "Found outcome '{0}' instead of the uncontrolled outcome '{1}'.", actual, expected);
        }

        private static async Task AssertResultOutcomeAsync(string expected,
            Func<CancellationToken, Task<int>> operation, CancellationToken cancellationToken)
        {
            string actual = await GetResultOutcomeAsync(operation, cancellationToken);
            Specification.Assert(actual == expected,
                "Found outcome '{0}' instead of the uncontrolled outcome '{1}'.", actual, expected);
        }

        private static async Task<string> GetOutcomeAsync(Func<CancellationToken, Task> operation,
            CancellationToken cancellationToken)
        {
            try
            {
                await operation(cancellationToken);
                return WaitAsyncProvider.CompletedOutcome;
            }
            catch (Exception ex) when (!(ex is ThreadInterruptedException))
            {
                return WaitAsyncProvider.GetExceptionOutcome(ex, cancellationToken);
            }
        }

        private static async Task<string> GetResultOutcomeAsync(Func<CancellationToken, Task<int>> operation,
            CancellationToken cancellationToken)
        {
            try
            {
                return WaitAsyncProvider.GetCompletedOutcome(await operation(cancellationToken));
            }
            catch (Exception ex) when (!(ex is ThreadInterruptedException))
            {
                return WaitAsyncProvider.GetExceptionOutcome(ex, cancellationToken);
            }
        }
    }
}
#endif
