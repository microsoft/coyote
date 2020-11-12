// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Specifications
{
    /// <summary>
    /// Provides static methods that are useful for writing specifications
    /// and interacting with the systematic testing engine.
    /// </summary>
    /// <remarks>
    /// See <see href="/coyote/learn/core/specifications">Specifications Overview</see>
    /// for more information.
    /// </remarks>
    public static class Specification
    {
        /// <summary>
        /// Checks if the predicate holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool predicate, string s, object arg0) =>
            CoyoteRuntime.Current.Assert(predicate, s, arg0);

        /// <summary>
        /// Checks if the predicate holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool predicate, string s, object arg0, object arg1) =>
            CoyoteRuntime.Current.Assert(predicate, s, arg0, arg1);

        /// <summary>
        /// Checks if the predicate holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool predicate, string s, object arg0, object arg1, object arg2) =>
            CoyoteRuntime.Current.Assert(predicate, s, arg0, arg1, arg2);

        /// <summary>
        /// Checks if the predicate holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool predicate, string s, params object[] args) =>
            CoyoteRuntime.Current.Assert(predicate, s, args);

        /// <summary>
        /// Creates a <see cref="Task"/> that will complete when <paramref name="predicate"/> returns true. Invoking
        /// this method specifies a liveness property, which must be eventually satisfied during systematic testing,
        /// else the method throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        /// <param name="predicate">Function that specifies a liveness property that must be eventually satisfied.</param>
        /// <param name="getHashCode">
        /// Function that returns a hash used to track progress related to the liveness property. This is used only
        /// during systematic testing and must be lightweight as it is invoked frequently.
        /// </param>
        /// <param name="delay">
        /// The amount of time to delay each time func is invoked. This is not used during systematic testing.
        /// </param>
        /// <param name="cancellationToken">
        /// The cancellation token with which to complete the task. This is not used during systematic testing.
        /// </param>
        /// <returns>Task that represents the property to be satisfied asynchronously.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Task WhenTrue(Func<Task<bool>> predicate, Func<int> getHashCode, TimeSpan delay,
            CancellationToken cancellationToken = default) =>
            CoyoteRuntime.Current.AssertIsLivenessPropertySatisfied(predicate, getHashCode, delay, cancellationToken);

        /// <summary>
        /// Registers a new safety or liveness monitor.
        /// </summary>
        /// <typeparam name="T">Type of the monitor.</typeparam>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterMonitor<T>()
            where T : Monitor =>
            CoyoteRuntime.Current.RegisterMonitor<T>();

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        /// <typeparam name="T">Type of the monitor.</typeparam>
        /// <param name="e">Event to send to the monitor.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Monitor<T>(Event e)
            where T : Monitor =>
            CoyoteRuntime.Current.Monitor<T>(e);
    }
}
