// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;
using Microsoft.Coyote.Machines;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote
{
    /// <summary>
    /// Provides static methods that are useful for writing specifications
    /// and interacting with the systematic testing engine.
    /// </summary>
    public static class Specification
    {
        /// <summary>
        /// Checks if the predicate holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool predicate, string s, object arg0) =>
            CoyoteRuntime.Provider.Current.Assert(predicate, s, arg0);

        /// <summary>
        /// Checks if the predicate holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool predicate, string s, object arg0, object arg1) =>
            CoyoteRuntime.Provider.Current.Assert(predicate, s, arg0, arg1);

        /// <summary>
        /// Checks if the predicate holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool predicate, string s, object arg0, object arg1, object arg2) =>
            CoyoteRuntime.Provider.Current.Assert(predicate, s, arg0, arg1, arg2);

        /// <summary>
        /// Checks if the predicate holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool predicate, string s, params object[] args) =>
            CoyoteRuntime.Provider.Current.Assert(predicate, s, args);

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled during analysis or testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ChooseRandomBoolean() => CoyoteRuntime.Provider.Current.GetNondeterministicBooleanChoice(null, 2);

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled during analysis or testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ChooseRandomBoolean(int maxValue) => CoyoteRuntime.Provider.Current.GetNondeterministicBooleanChoice(null, maxValue);

        /// <summary>
        /// Returns a nondeterministic integer, that can be controlled during analysis or testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ChooseRandomInteger(int maxValue) => CoyoteRuntime.Provider.Current.GetNondeterministicIntegerChoice(null, maxValue);

        /// <summary>
        /// Registers a new safety or liveness monitor.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterMonitor<T>()
            where T : Monitor =>
            CoyoteRuntime.Provider.Current.RegisterMonitor(typeof(T));

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Monitor<T>(Event e)
            where T : Monitor =>
            CoyoteRuntime.Provider.Current.Monitor(typeof(T), null, e);
    }
}
