// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SystemThreadState = System.Threading.ThreadState;

namespace Microsoft.Coyote.Tests.Common.Tasks
{
    /// <summary>
    /// Helper class that invokes the uncontrolled task wait-all overloads and classifies their outcome.
    /// </summary>
    /// <remarks>
    /// We do not rewrite this class in purpose, so that tests can compare the outcome of the
    /// controlled overloads against the outcome of the uncontrolled runtime overloads.
    /// </remarks>
    public static class WaitAllProvider
    {
        /// <summary>
        /// The time to let a blocked uncontrolled wait settle before completing or canceling it.
        /// </summary>
        private static readonly TimeSpan SettleTime = TimeSpan.FromMilliseconds(100);

        /// <summary>
        /// Invokes the array or, if available and requested, the enumerable wait-all overload.
        /// </summary>
        public static void WaitAll(Task[] tasks, bool isEnumerable, CancellationToken cancellationToken)
        {
#if NET10_0_OR_GREATER
            if (isEnumerable)
            {
                Task.WaitAll((IEnumerable<Task>)tasks, cancellationToken);
                return;
            }
#else
            if (isEnumerable)
            {
                throw new NotSupportedException("The enumerable overload requires .NET 10.");
            }
#endif

            Task.WaitAll(tasks, cancellationToken);
        }

        /// <summary>
        /// Returns the outcome of waiting for the specified tasks with the specified token.
        /// </summary>
        public static string GetOutcome(Task[] tasks, bool isEnumerable, CancellationToken cancellationToken) =>
            Classify(() => WaitAll(tasks, isEnumerable, cancellationToken), cancellationToken);

        /// <summary>
        /// Returns the outcome of a wait for a pending task that is blocked when the task completes
        /// with the specified event and the wait token is canceled in the specified order.
        /// </summary>
        /// <param name="isEnumerable">True to use the enumerable overload.</param>
        /// <param name="sourceEvent">The event that completes the pending task, or null to keep it pending.</param>
        /// <param name="isCancellationFirst">True to cancel the wait token before completing the task.</param>
        public static string GetBlockedOutcome(bool isEnumerable, WaitAsyncSourceEvent? sourceEvent,
            bool isCancellationFirst)
        {
            using var cancellation = new CancellationTokenSource();
            using var sourceCancellation = new CancellationTokenSource();
            var pending = new TaskCompletionSource<int>();
            string outcome = null;
            var waiter = new Thread(() => outcome = GetOutcome(
                new Task[] { pending.Task }, isEnumerable, cancellation.Token));
            waiter.Start();
            SpinWait.SpinUntil(() => (waiter.ThreadState & SystemThreadState.WaitSleepJoin) != 0);
            Thread.Sleep(SettleTime);

            if (isCancellationFirst || sourceEvent is null)
            {
                cancellation.Cancel();
            }

            switch (sourceEvent)
            {
                case WaitAsyncSourceEvent.Result:
                    pending.SetResult(WaitAsyncProvider.ExpectedResult);
                    break;
                case WaitAsyncSourceEvent.Fault:
                    pending.SetException(new InvalidOperationException(WaitAsyncProvider.ExpectedFaultMessage));
                    break;
                case WaitAsyncSourceEvent.Cancellation:
                    sourceCancellation.Cancel();
                    pending.SetCanceled(sourceCancellation.Token);
                    break;
            }

            if (!isCancellationFirst && sourceEvent != null)
            {
                cancellation.Cancel();
            }

            waiter.Join();
            return outcome;
        }

        /// <summary>
        /// Returns the outcome that corresponds to the specified exception thrown by a wait-all.
        /// </summary>
        public static string GetExceptionOutcome(Exception exception, CancellationToken cancellationToken) =>
            exception is AggregateException aggregate ?
                $"aggregate({aggregate.InnerException.GetType().Name})" :
                WaitAsyncProvider.GetExceptionOutcome(exception, cancellationToken);

        private static string Classify(Action operation, CancellationToken cancellationToken)
        {
            try
            {
                operation();
                return WaitAsyncProvider.CompletedOutcome;
            }
            catch (Exception ex)
            {
                return GetExceptionOutcome(ex, cancellationToken);
            }
        }
    }
}
#endif
