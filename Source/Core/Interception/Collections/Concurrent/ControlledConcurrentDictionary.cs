// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Interception
{
    /// <summary>
    /// Provides methods for creating concurrent dictionaries that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ControlledConcurrentDictionary
    {
        /// <summary>
        /// Gets the number of key/value pairs contained in the <see cref="ConcurrentDictionary{TKey, TValue}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static int get_Count<TKey, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            return concurrentDictionary.Count;
        }

        /// <summary>
        /// Checks if the <see cref="ConcurrentDictionary{TKey, TValue}"/> is empty.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static bool get_IsEmpty<TKey, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            return concurrentDictionary.IsEmpty;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static TValue get_Item<TKey, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary, TKey key)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            return concurrentDictionary[key];
        }

        /// <summary>
        /// Sets the value associated with the specified key.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static void set_Item<TKey, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary, TKey key, TValue value)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            concurrentDictionary[key] = value;
        }

        /// <summary>
        /// Gets a collection containing the keys in the <see cref="ConcurrentDictionary{TKey, TValue}"/>.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ICollection<TKey> get_Keys<TKey, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            return concurrentDictionary.Keys;
        }

        /// <summary>
        /// Gets a collection containing the values in the <see cref="ConcurrentDictionary{TKey, TValue}"/>.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ICollection<TValue> get_Values<TKey, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            return concurrentDictionary.Values;
        }

        /// <summary>
        /// Removes all keys and values from the <see cref="ConcurrentDictionary{TKey, TValue}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear<TKey, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary)
        {
            concurrentDictionary.Clear();
        }

        /// <summary>
        /// Attempts to add the specified key and value to the ConcurrentDictionary.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdd<TKey, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary, TKey key, TValue value)
        {
            bool result = concurrentDictionary.TryAdd(key, value);
            SchedulingPoint.Interleave();
            return result;
        }

        /// <summary>
        /// Attempts to remove and return the value that has the specified key from the ConcurrentDictionary.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryRemove<TKey, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary, TKey key, out TValue value)
        {
            return concurrentDictionary.TryRemove(key, out value);
        }
    }
}
