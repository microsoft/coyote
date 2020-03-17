// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Microsoft.Coyote.Runtime;
using Microsoft.Coyote.SystematicTesting;

namespace Microsoft.Coyote.Specifications
{
    /// <summary>
    /// Provides static methods that are useful for writing specifications
    /// and interacting with the systematic testing engine.
    /// </summary>
    /// <remarks>
    /// See <see href="/coyote/learn/specifications/overview">Specifications Overview</see> for more information.
    /// </remarks>
    public static class Specification
    {
        /// <summary>
        /// The currently executing runtime.
        /// </summary>
        private static CoyoteRuntime CurrentRuntime => CoyoteRuntime.IsExecutionControlled ?
            ControlledRuntime.Current : CoyoteRuntime.Default;

        /// <summary>
        /// Checks if the predicate holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool predicate, string s, object arg0) =>
            CurrentRuntime.Assert(predicate, s, arg0);

        /// <summary>
        /// Checks if the predicate holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool predicate, string s, object arg0, object arg1) =>
            CurrentRuntime.Assert(predicate, s, arg0, arg1);

        /// <summary>
        /// Checks if the predicate holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool predicate, string s, object arg0, object arg1, object arg2) =>
            CurrentRuntime.Assert(predicate, s, arg0, arg1, arg2);

        /// <summary>
        /// Checks if the predicate holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool predicate, string s, params object[] args) =>
            CurrentRuntime.Assert(predicate, s, args);

        /// <summary>
        /// Registers a new safety or liveness monitor.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterMonitor<T>()
            where T : Monitor =>
            CurrentRuntime.RegisterMonitor(typeof(T));

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Monitor<T>(Event e)
            where T : Monitor =>
            CurrentRuntime.Monitor(typeof(T), null, e);
    }
}
