// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

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
            ConcurrentCollectionHelper.Interleave();
            return concurrentDictionary.Count;
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="ConcurrentDictionary{TKey, TValue}"/> is empty.
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
            ConcurrentCollectionHelper.Interleave();
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
            ConcurrentCollectionHelper.Interleave();
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
            ConcurrentCollectionHelper.Interleave();
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
            ConcurrentCollectionHelper.Interleave();
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
            ConcurrentCollectionHelper.Interleave();
            return concurrentDictionary.Values;
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ConcurrentDictionary{TKey, TValue}"/> if the key does not
        /// already exist, or updates a key/value pair in the <see cref="ConcurrentDictionary{TKey, TValue}"/>
        /// if the key already exists.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue AddOrUpdate<TKey, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary, TKey key, Func<TKey, TValue> addValueFactory,
            Func<TKey, TValue, TValue> updateValueFactory)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentDictionary.AddOrUpdate(key, addValueFactory, updateValueFactory);
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ConcurrentDictionary{TKey, TValue}"/> if the key does not
        /// already exist, or updates a key/value pair in the <see cref="ConcurrentDictionary{TKey, TValue}"/>
        /// if the key already exists.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue AddOrUpdate<TKey, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary, TKey key, TValue addValue,
            Func<TKey, TValue, TValue> updateValueFactory)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentDictionary.AddOrUpdate(key, addValue, updateValueFactory);
        }

#if !NETSTANDARD2_0 && !NETFRAMEWORK
        /// <summary>
        /// Adds a key/value pair to the <see cref="ConcurrentDictionary{TKey, TValue}"/> if the key does not
        /// already exist, or updates a key/value pair in the <see cref="ConcurrentDictionary{TKey, TValue}"/>
        /// if the key already exists.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue AddOrUpdate<TKey, TArg, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary, TKey key, Func<TKey, TArg, TValue> addValueFactory,
            Func<TKey, TValue, TArg, TValue> updateValueFactory, TArg factoryArgument)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentDictionary.AddOrUpdate(key, addValueFactory, updateValueFactory, factoryArgument);
        }
#endif

        /// <summary>
        /// Removes all keys and values from the <see cref="ConcurrentDictionary{TKey, TValue}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear<TKey, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary)
        {
            ConcurrentCollectionHelper.Interleave();
            concurrentDictionary.Clear();
        }

        /// <summary>
        /// Determines whether the <see cref="ConcurrentDictionary{TKey, TValue}"/> contains the specified key.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsKey<TKey, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary, TKey key)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentDictionary.ContainsKey(key);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="ConcurrentDictionary{TKey, TValue}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator<TKey, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentDictionary.GetEnumerator();
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ConcurrentDictionary{TKey, TValue}"/> if the key does not already exist.
        /// Returns the new value, or the existing value if the key already exists.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue GetOrAdd<TKey, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary, TKey key, Func<TKey, TValue> valueFactory)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentDictionary.GetOrAdd(key, valueFactory);
        }

        /// <summary>
        /// Adds a key/value pair to the <see cref="ConcurrentDictionary{TKey, TValue}"/> if the key does not already exist.
        /// Returns the new value, or the existing value if the key already exists.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue GetOrAdd<TKey, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary, TKey key, TValue value)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentDictionary.GetOrAdd(key, value);
        }

#if !NETSTANDARD2_0 && !NETFRAMEWORK
        /// <summary>
        /// Adds a key/value pair to the <see cref="ConcurrentDictionary{TKey, TValue}"/> if the key does not already exist.
        /// Returns the new value, or the existing value if the key already exists.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TValue GetOrAdd<TKey, TArg, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary, TKey key, Func<TKey, TArg, TValue> valueFactory,
            TArg factoryArgument)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentDictionary.GetOrAdd(key, valueFactory, factoryArgument);
        }
#endif

        /// <summary>
        /// Copies the key and value pairs stored in the <see cref="ConcurrentDictionary{TKey, TValue}"/> to a new array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static KeyValuePair<TKey, TValue>[] ToArray<TKey, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentDictionary.ToArray();
        }

        /// <summary>
        /// Attempts to add the specified key and value to the <see cref="ConcurrentDictionary{TKey, TValue}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdd<TKey, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary, TKey key, TValue value)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentDictionary.TryAdd(key, value);
        }

        /// <summary>
        /// Attempts to get the value associated with the specified key from the <see cref="ConcurrentDictionary{TKey, TValue}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValue<TKey, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary, TKey key, out TValue value)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentDictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Attempts to remove and return the value that has the specified key from the <see cref="ConcurrentDictionary{TKey, TValue}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryRemove<TKey, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary, TKey key, out TValue value)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentDictionary.TryRemove(key, out value);
        }

#if !NETSTANDARD2_1 && !NETSTANDARD2_0 && !NETFRAMEWORK
        /// <summary>
        /// Attempts to remove and return the value that has the specified key from the <see cref="ConcurrentDictionary{TKey, TValue}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryRemove<TKey, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary, KeyValuePair<TKey, TValue> item)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentDictionary.TryRemove(item);
        }
#endif

        /// <summary>
        /// Updates the value associated with key to newValue if the existing value with key is equal to comparisonValue.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryUpdate<TKey, TValue>(ConcurrentDictionary<TKey, TValue> concurrentDictionary, TKey key, TValue newValue, TValue comparisonValue)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentDictionary.TryUpdate(key, newValue, comparisonValue);
        }
    }
}
