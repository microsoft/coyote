// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

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
