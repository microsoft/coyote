// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote
{
    /// <summary>
    /// Provides static methods for generating random boolean or integer values.
    /// During testing, the random value generator is controlled by the tester,
    /// allowing the choices to be systematically explored.
    /// </summary>
    public static class RandomValueGenerator
    {
        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled during testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetNextBoolean() => CoyoteRuntime.Provider.Current.GetNondeterministicBooleanChoice(null, 2);

        /// <summary>
        /// Returns a nondeterministic boolean choice, that can be controlled during testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetNextBoolean(int maxValue) => CoyoteRuntime.Provider.Current.GetNondeterministicBooleanChoice(null, maxValue);

        /// <summary>
        /// Returns a nondeterministic integer, that can be controlled during testing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetNextInteger(int maxValue) => CoyoteRuntime.Provider.Current.GetNondeterministicIntegerChoice(null, maxValue);
    }
}
