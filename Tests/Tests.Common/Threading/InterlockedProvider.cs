// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

#if NET9_0_OR_GREATER
using System;
using System.Threading;

namespace Microsoft.Coyote.Tests.Common.Threading
{
    /// <summary>
    /// Helper class that invokes the uncontrolled generic atomic operations and classifies their outcome.
    /// </summary>
    /// <remarks>
    /// We do not rewrite this class in purpose, so that tests can compare the outcome of the
    /// controlled operations against the outcome of the uncontrolled runtime operations.
    /// </remarks>
    public static class InterlockedProvider
    {
        /// <summary>
        /// Returns the outcome of exchanging the specified value with an explicit generic type argument.
        /// </summary>
        public static string GetExchangeOutcome<T>(T location, T value) =>
            GetOutcome(() => Interlocked.Exchange<T>(ref location, value), () => location);

        /// <summary>
        /// Returns the outcome of comparing and exchanging the specified value with an explicit
        /// generic type argument.
        /// </summary>
        public static string GetCompareExchangeOutcome<T>(T location, T value, T comparand) =>
            GetOutcome(() => Interlocked.CompareExchange<T>(ref location, value, comparand), () => location);

        /// <summary>
        /// Returns the outcome of an atomic operation that returns the original value.
        /// </summary>
        public static string GetOutcome<T>(Func<T> operation, Func<T> location)
        {
            try
            {
                T original = operation();
                return $"original={original}, location={location()}";
            }
            catch (Exception ex)
            {
                return $"{ex.GetType().FullName}: {ex.Message}";
            }
        }
    }
}
#endif
