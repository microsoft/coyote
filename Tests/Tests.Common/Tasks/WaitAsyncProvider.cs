// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Coyote.Tests.Common.Tasks
{
    /// <summary>
    /// Helper class that invokes the uncontrolled task wait overloads and classifies their outcome.
    /// </summary>
    /// <remarks>
    /// We do not rewrite this class in purpose, so that tests can compare the outcome of the
    /// controlled overloads against the outcome of the uncontrolled runtime overloads.
    /// </remarks>
    public static class WaitAsyncProvider
    {
        /// <summary>
        /// The result of a task that completes successfully with a result.
        /// </summary>
        public const int ExpectedResult = 7;

        /// <summary>
        /// The message of the exception thrown by a faulted task.
        /// </summary>
        public const string ExpectedFaultMessage = "expected fault";

        /// <summary>
        /// The outcome of a wait that completed successfully.
        /// </summary>
        public const string CompletedOutcome = "completed";

        /// <summary>
        /// The outcome of a wait that timed out.
        /// </summary>
        public const string TimedOutOutcome = "timeout";

        /// <summary>
        /// The outcome of a wait that was canceled with the token passed to the wait.
        /// </summary>
        public const string WaitTokenCanceledOutcome = "canceled(waitToken)";

        /// <summary>
        /// The outcome of a wait that was canceled with a token other than the one passed to the wait.
        /// </summary>
        public const string SourceTokenCanceledOutcome = "canceled(sourceToken)";

        /// <summary>
        /// Creates a task that never completes.
        /// </summary>
        public static Task CreatePendingTask() => new TaskCompletionSource<bool>().Task;

        /// <summary>
        /// Creates a generic task that never completes.
        /// </summary>
        public static Task<int> CreatePendingResultTask() => new TaskCompletionSource<int>().Task;

        /// <summary>
        /// Creates a task that has already completed successfully.
        /// </summary>
        public static Task CreateCompletedTask() => Task.CompletedTask;

        /// <summary>
        /// Creates a generic task that has already completed successfully.
        /// </summary>
        public static Task<int> CreateCompletedResultTask() => Task.FromResult(ExpectedResult);

        /// <summary>
        /// Creates a task that has already faulted.
        /// </summary>
        public static Task CreateFaultedTask() =>
            Task.FromException(new InvalidOperationException(ExpectedFaultMessage));

        /// <summary>
        /// Creates a generic task that has already faulted.
        /// </summary>
        public static Task<int> CreateFaultedResultTask() =>
            Task.FromException<int>(new InvalidOperationException(ExpectedFaultMessage));

        /// <summary>
        /// Creates a task that has already been canceled with the specified token.
        /// </summary>
        public static Task CreateCanceledTask(CancellationToken cancellationToken) =>
            Task.FromCanceled(cancellationToken);

        /// <summary>
        /// Creates a generic task that has already been canceled with the specified token.
        /// </summary>
        public static Task<int> CreateCanceledResultTask(CancellationToken cancellationToken) =>
            Task.FromCanceled<int>(cancellationToken);

        /// <summary>
        /// Returns the outcome of waiting for the specified task with the specified cancellation token.
        /// </summary>
        public static string GetOutcome(Task task, CancellationToken cancellationToken) =>
            Classify(() => task.WaitAsync(cancellationToken), cancellationToken);

        /// <summary>
        /// Returns the outcome of waiting for the specified task with the specified timeout and token.
        /// </summary>
        public static string GetOutcome(Task task, TimeSpan timeout, CancellationToken cancellationToken) =>
            Classify(() => task.WaitAsync(timeout, cancellationToken), cancellationToken);

        /// <summary>
        /// Returns the outcome of waiting for the specified generic task with the specified token.
        /// </summary>
        public static string GetResultOutcome<TResult>(Task<TResult> task, CancellationToken cancellationToken) =>
            ClassifyResult(() => task.WaitAsync(cancellationToken), cancellationToken);

        /// <summary>
        /// Returns the outcome of waiting for the specified generic task with the specified timeout and token.
        /// </summary>
        public static string GetResultOutcome<TResult>(Task<TResult> task, TimeSpan timeout,
            CancellationToken cancellationToken) =>
            ClassifyResult(() => task.WaitAsync(timeout, cancellationToken), cancellationToken);

#if NET8_0_OR_GREATER
        /// <summary>
        /// Returns the outcome of waiting for the specified task with the specified timeout, time
        /// provider and token.
        /// </summary>
        public static string GetOutcomeWithTimeProvider(Task task, TimeSpan timeout,
            CancellationToken cancellationToken) =>
            Classify(() => task.WaitAsync(timeout, TimeProvider.System, cancellationToken), cancellationToken);

        /// <summary>
        /// Returns the outcome of waiting for the specified generic task with the specified timeout,
        /// time provider and token.
        /// </summary>
        public static string GetResultOutcomeWithTimeProvider<TResult>(Task<TResult> task, TimeSpan timeout,
            CancellationToken cancellationToken) =>
            ClassifyResult(() => task.WaitAsync(timeout, TimeProvider.System, cancellationToken), cancellationToken);
#endif

        /// <summary>
        /// Returns the outcome of waiting for a task whose source completes with the specified event
        /// immediately before or after the wait token is canceled, without an intervening await.
        /// </summary>
        public static string GetFirstEventOutcome(bool isGeneric, WaitAsyncSourceEvent sourceEvent,
            bool isCancellationFirst, TaskCreationOptions creationOptions)
        {
            using var cancellation = new CancellationTokenSource();
            using var sourceCancellation = new CancellationTokenSource();
            var source = new TaskCompletionSource<int>(creationOptions);
            Task wait = isGeneric ?
                source.Task.WaitAsync(cancellation.Token) :
                ((Task)source.Task).WaitAsync(cancellation.Token);
            if (isCancellationFirst)
            {
                cancellation.Cancel();
                CompleteSource(source, sourceEvent, sourceCancellation);
            }
            else
            {
                CompleteSource(source, sourceEvent, sourceCancellation);
                cancellation.Cancel();
            }

            return isGeneric ?
                ClassifyResult(() => (Task<int>)wait, cancellation.Token) :
                Classify(() => wait, cancellation.Token);
        }

        /// <summary>
        /// Returns the outcome expected when the first of the source event and the wait token
        /// cancellation completes the wait, which the uncontrolled runtime guarantees only if
        /// the source does not run its continuations asynchronously.
        /// </summary>
        public static string GetExpectedFirstEventOutcome(bool isGeneric, WaitAsyncSourceEvent sourceEvent,
            bool isCancellationFirst) =>
            isCancellationFirst ? WaitTokenCanceledOutcome : sourceEvent switch
            {
                WaitAsyncSourceEvent.Result => isGeneric ? GetCompletedOutcome(ExpectedResult) : CompletedOutcome,
                WaitAsyncSourceEvent.Fault => $"fault({nameof(InvalidOperationException)})",
                _ => SourceTokenCanceledOutcome
            };

        /// <summary>
        /// Returns the outcome of a task that completed successfully with the specified result.
        /// </summary>
        public static string GetCompletedOutcome<TResult>(TResult result) => $"{CompletedOutcome}(result={result})";

        /// <summary>
        /// Returns the outcome that corresponds to the specified exception.
        /// </summary>
        public static string GetExceptionOutcome(Exception exception, CancellationToken cancellationToken) =>
            exception switch
            {
                OperationCanceledException ex => ex.CancellationToken == cancellationToken ?
                    WaitTokenCanceledOutcome : SourceTokenCanceledOutcome,
                TimeoutException => TimedOutOutcome,
                _ => $"fault({exception.GetType().Name})"
            };

        private static void CompleteSource(TaskCompletionSource<int> source, WaitAsyncSourceEvent sourceEvent,
            CancellationTokenSource sourceCancellation)
        {
            switch (sourceEvent)
            {
                case WaitAsyncSourceEvent.Result:
                    source.SetResult(ExpectedResult);
                    break;
                case WaitAsyncSourceEvent.Fault:
                    source.SetException(new InvalidOperationException(ExpectedFaultMessage));
                    break;
                default:
                    sourceCancellation.Cancel();
                    source.SetCanceled(sourceCancellation.Token);
                    break;
            }
        }

        private static string Classify(Func<Task> operation, CancellationToken cancellationToken)
        {
            try
            {
                operation().GetAwaiter().GetResult();
                return CompletedOutcome;
            }
            catch (Exception ex)
            {
                return GetExceptionOutcome(ex, cancellationToken);
            }
        }

        private static string ClassifyResult<TResult>(Func<Task<TResult>> operation,
            CancellationToken cancellationToken)
        {
            try
            {
                return GetCompletedOutcome(operation().GetAwaiter().GetResult());
            }
            catch (Exception ex)
            {
                return GetExceptionOutcome(ex, cancellationToken);
            }
        }
    }
}
#endif
