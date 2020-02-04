// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote
{
    /// <summary>
    /// Provides static methods for generating random values. During testing,
    /// the random value generator is controlled by the tester, allowing the
    /// choices to be systematically explored.
    /// </summary>
    /// <remarks>
    /// See <see href="/coyote/learn/core/non-determinism" >Program non-determinism</see>
    /// for more information.
    /// </remarks>
    public static class ControlledRandomValueGenerator
    {
        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled during testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetNextBoolean() => CoyoteRuntime.Current.GetNondeterministicBooleanChoice(null, 2);

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled during testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetNextBoolean(int maxValue) => CoyoteRuntime.Current.GetNondeterministicBooleanChoice(null, maxValue);

        /// <summary>
        /// Returns a nondeterministic integer, that can be controlled during testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetNextInteger(int maxValue) => CoyoteRuntime.Current.GetNondeterministicIntegerChoice(null, maxValue);
    }
}
