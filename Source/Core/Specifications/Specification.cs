// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Coyote.Machines;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote
{
    /// <summary>
    /// Provides methods for writing specifications and interacting
    /// with the systematic testing engine.
    /// </summary>
    public static class Specification
    {
        /// <summary>
        /// The currently installed specification checker.
        /// </summary>
        internal static Checker CurrentChecker { get; set; } = Checker.Default;

        /// <summary>
        /// Checks if the predicate holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool predicate, string s, object arg0) =>
            CurrentChecker.Assert(predicate, s, arg0);

        /// <summary>
        /// Checks if the predicate holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool predicate, string s, object arg0, object arg1) =>
            CurrentChecker.Assert(predicate, s, arg0, arg1);

        /// <summary>
        /// Checks if the predicate holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool predicate, string s, object arg0, object arg1, object arg2) =>
            CurrentChecker.Assert(predicate, s, arg0, arg1, arg2);

        /// <summary>
        /// Checks if the predicate holds, and if not, throws an <see cref="AssertionFailureException"/> exception.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Assert(bool predicate, string s, params object[] args) =>
            CurrentChecker.Assert(predicate, s, args);

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled during analysis or testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ChooseRandomBoolean() => CurrentChecker.GetNondeterministicBooleanChoice(2);

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled during analysis or testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ChooseRandomBoolean(int maxValue) => CurrentChecker.GetNondeterministicBooleanChoice(maxValue);

        /// <summary>
        /// Returns a nondeterministic integer, that can be controlled during analysis or testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ChooseRandomInteger(int maxValue) => CurrentChecker.GetNondeterministicIntegerChoice(maxValue);

        /// <summary>
        /// Registers a new safety or liveness monitor.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RegisterMonitor<T>()
            where T : Monitor =>
            CurrentChecker.RegisterMonitor<T>();

        /// <summary>
        /// Invokes the specified monitor with the given event.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Monitor<T>(Event e)
            where T : Monitor =>
            CurrentChecker.Monitor<T>(e);

        /// <summary>
        /// Checks specifications for correctness.
        /// </summary>
        internal class Checker
        {
            /// <summary>
            /// The default <see cref="Specification"/> checker.
            /// </summary>
            public static Checker Default { get; } = new Checker();

            /// <summary>
            /// Initializes a new instance of the <see cref="Specification.Checker"/> class.
            /// </summary>
            internal Checker()
            {
            }

            /// <summary>
            /// Checks if the predicate holds, and if not, fails the assertion.
            /// </summary>
            internal virtual void Assert(bool predicate)
            {
                if (!predicate)
                {
                    throw new AssertionFailureException("Detected an assertion failure.");
                }
            }

            /// <summary>
            /// Checks if the predicate holds, and if not, fails the assertion.
            /// </summary>
            internal virtual void Assert(bool predicate, string s, object arg0)
            {
                if (!predicate)
                {
                    throw new AssertionFailureException(string.Format(CultureInfo.InvariantCulture, s, arg0.ToString()));
                }
            }

            /// <summary>
            /// Checks if the predicate holds, and if not, fails the assertion.
            /// </summary>
            internal virtual void Assert(bool predicate, string s, object arg0, object arg1)
            {
                if (!predicate)
                {
                    throw new AssertionFailureException(string.Format(CultureInfo.InvariantCulture, s, arg0.ToString(), arg1.ToString()));
                }
            }

            /// <summary>
            /// Checks if the predicate holds, and if not, fails the assertion.
            /// </summary>
            internal virtual void Assert(bool predicate, string s, object arg0, object arg1, object arg2)
            {
                if (!predicate)
                {
                    throw new AssertionFailureException(string.Format(CultureInfo.InvariantCulture, s, arg0.ToString(), arg1.ToString(), arg2.ToString()));
                }
            }

            /// <summary>
            /// Checks if the predicate holds, and if not, fails the assertion.
            /// </summary>
            internal virtual void Assert(bool predicate, string s, params object[] args)
            {
                if (!predicate)
                {
                    throw new AssertionFailureException(string.Format(CultureInfo.InvariantCulture, s, args));
                }
            }

            /// <summary>
            /// Returns a nondeterministic boolean choice, that can be
            /// controlled during analysis or testing.
            /// </summary>
            internal virtual bool GetNondeterministicBooleanChoice(int maxValue)
            {
                Random random = new Random(DateTime.Now.Millisecond);
                return random.Next(maxValue) == 0 ? true : false;
            }

            /// <summary>
            /// Returns a nondeterministic integer, that can be
            /// controlled during analysis or testing.
            /// </summary>
            internal virtual int GetNondeterministicIntegerChoice(int maxValue)
            {
                Random random = new Random(DateTime.Now.Millisecond);
                return random.Next(maxValue);
            }

            /// <summary>
            /// Registers a new safety or liveness monitor.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal virtual void RegisterMonitor<T>()
                where T : Monitor
            {
            }

            /// <summary>
            /// Invokes the specified monitor with the given event.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal virtual void Monitor<T>(Event e)
                where T : Monitor
            {
            }
        }
    }
}
