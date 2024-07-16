// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Runtime;

using SystemConcurrent = System.Collections.Concurrent;
using SystemGenerics = System.Collections.Generic;

namespace Microsoft.Coyote.Rewriting.Types.Collections.Concurrent
{
#pragma warning disable CA1000 // Do not declare static members on generic types
    /// <summary>
    /// Provides methods for controlling a concurrent dictionary during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ConcurrentDictionary<TKey, TValue>
    {
        /// <summary>
        /// Gets the number of key/value pairs contained in the concurrent dictionary.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static int get_Count(SystemConcurrent.ConcurrentDictionary<TKey, TValue> instance)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            ExploreInterleaving();
            return instance.Count;
        }

        /// <summary>
        /// Gets a value that indicates whether the concurrent dictionary is empty.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static bool get_IsEmpty(SystemConcurrent.ConcurrentDictionary<TKey, TValue> instance)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            ExploreInterleaving();
            return instance.IsEmpty;
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static TValue get_Item(SystemConcurrent.ConcurrentDictionary<TKey, TValue> instance,
            TKey key)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            ExploreInterleaving();
            return instance[key];
        }

        /// <summary>
        /// Sets the value associated with the specified key.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static void set_Item(SystemConcurrent.ConcurrentDictionary<TKey, TValue> instance,
            TKey key, TValue value)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            ExploreInterleaving();
            instance[key] = value;
        }

        /// <summary>
        /// Gets a collection containing the keys in the concurrent dictionary.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static SystemGenerics.ICollection<TKey> get_Keys(
            SystemConcurrent.ConcurrentDictionary<TKey, TValue> instance)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            ExploreInterleaving();
            return instance.Keys;
        }

        /// <summary>
        /// Gets a collection containing the values in the concurrent dictionary.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static SystemGenerics.ICollection<TValue> get_Values(
            SystemConcurrent.ConcurrentDictionary<TKey, TValue> instance)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            ExploreInterleaving();
            return instance.Values;
        }

        /// <summary>
        /// Adds a key/value pair to the concurrent dictionary if the key does not
        /// already exist, or updates a key/value pair in the concurrent dictionary
        /// if the key already exists.
        /// </summary>
        public static TValue AddOrUpdate(SystemConcurrent.ConcurrentDictionary<TKey, TValue> instance,
            TKey key, Func<TKey, TValue> addValueFactory, Func<TKey, TValue, TValue> updateValueFactory)
        {
            ExploreInterleaving();
            return instance.AddOrUpdate(key, addValueFactory, updateValueFactory);
        }

        /// <summary>
        /// Adds a key/value pair to the concurrent dictionary if the key does not
        /// already exist, or updates a key/value pair in the concurrent dictionary
        /// if the key already exists.
        /// </summary>
        public static TValue AddOrUpdate(SystemConcurrent.ConcurrentDictionary<TKey, TValue> instance,
            TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
        {
            ExploreInterleaving();
            return instance.AddOrUpdate(key, addValue, updateValueFactory);
        }

#if NET
        /// <summary>
        /// Adds a key/value pair to the concurrent dictionary if the key does not
        /// already exist, or updates a key/value pair in the concurrent dictionary
        /// if the key already exists.
        /// </summary>
        public static TValue AddOrUpdate<TArg>(SystemConcurrent.ConcurrentDictionary<TKey, TValue> instance,
            TKey key, Func<TKey, TArg, TValue> addValueFactory, Func<TKey, TValue, TArg, TValue> updateValueFactory,
            TArg factoryArgument)
        {
            ExploreInterleaving();
            return instance.AddOrUpdate(key, addValueFactory, updateValueFactory, factoryArgument);
        }
#endif

        /// <summary>
        /// Removes all keys and values from the concurrent dictionary.
        /// </summary>
        public static void Clear(SystemConcurrent.ConcurrentDictionary<TKey, TValue> instance)
        {
            ExploreInterleaving();
            instance.Clear();
        }

        /// <summary>
        /// Determines whether the concurrent dictionary contains the specified key.
        /// </summary>
        public static bool ContainsKey(SystemConcurrent.ConcurrentDictionary<TKey, TValue> instance,
            TKey key)
        {
            ExploreInterleaving();
            return instance.ContainsKey(key);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the concurrent dictionary.
        /// </summary>
        public static SystemGenerics.IEnumerator<SystemGenerics.KeyValuePair<TKey, TValue>> GetEnumerator(
            SystemConcurrent.ConcurrentDictionary<TKey, TValue> instance)
        {
            ExploreInterleaving();
            return instance.GetEnumerator();
        }

        /// <summary>
        /// Adds a key/value pair to the concurrent dictionary if the key does not already exist.
        /// Returns the new value, or the existing value if the key already exists.
        /// </summary>
        public static TValue GetOrAdd(SystemConcurrent.ConcurrentDictionary<TKey, TValue> instance,
            TKey key, Func<TKey, TValue> valueFactory)
        {
            ExploreInterleaving();
            return instance.GetOrAdd(key, valueFactory);
        }

        /// <summary>
        /// Adds a key/value pair to the concurrent dictionary if the key does not already exist.
        /// Returns the new value, or the existing value if the key already exists.
        /// </summary>
        public static TValue GetOrAdd(SystemConcurrent.ConcurrentDictionary<TKey, TValue> instance,
            TKey key, TValue value)
        {
            ExploreInterleaving();
            return instance.GetOrAdd(key, value);
        }

#if NET
        /// <summary>
        /// Adds a key/value pair to the concurrent dictionary if the key does not already exist.
        /// Returns the new value, or the existing value if the key already exists.
        /// </summary>
        public static TValue GetOrAdd<TArg>(SystemConcurrent.ConcurrentDictionary<TKey, TValue> instance,
            TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
        {
            ExploreInterleaving();
            return instance.GetOrAdd(key, valueFactory, factoryArgument);
        }
#endif

        /// <summary>
        /// Copies the key and value pairs stored in the concurrent dictionary to a new array.
        /// </summary>
        public static SystemGenerics.KeyValuePair<TKey, TValue>[] ToArray(
            SystemConcurrent.ConcurrentDictionary<TKey, TValue> instance)
        {
            ExploreInterleaving();
            return instance.ToArray();
        }

        /// <summary>
        /// Attempts to add the specified key and value to the concurrent dictionary.
        /// </summary>
        public static bool TryAdd(SystemConcurrent.ConcurrentDictionary<TKey, TValue> instance,
            TKey key, TValue value)
        {
            ExploreInterleaving();
            return instance.TryAdd(key, value);
        }

        /// <summary>
        /// Attempts to get the value associated with the specified key
        /// from the concurrent dictionary.
        /// </summary>
        public static bool TryGetValue(SystemConcurrent.ConcurrentDictionary<TKey, TValue> instance,
            TKey key, out TValue value)
        {
            ExploreInterleaving();
            return instance.TryGetValue(key, out value);
        }

        /// <summary>
        /// Attempts to remove and return the value that has the specified key
        /// from the concurrent dictionary.
        /// </summary>
        public static bool TryRemove(SystemConcurrent.ConcurrentDictionary<TKey, TValue> instance,
            TKey key, out TValue value)
        {
            ExploreInterleaving();
            return instance.TryRemove(key, out value);
        }

#if NET
        /// <summary>
        /// Attempts to remove and return the value that has the specified key
        /// from the concurrent dictionary.
        /// </summary>
        public static bool TryRemove(SystemConcurrent.ConcurrentDictionary<TKey, TValue> instance,
            SystemGenerics.KeyValuePair<TKey, TValue> item)
        {
            ExploreInterleaving();
            return instance.TryRemove(item);
        }
#endif

        /// <summary>
        /// Updates the value associated with key to newValue if the existing value with key is equal to comparisonValue.
        /// </summary>
        public static bool TryUpdate(SystemConcurrent.ConcurrentDictionary<TKey, TValue> instance,
            TKey key, TValue newValue, TValue comparisonValue)
        {
            ExploreInterleaving();
            return instance.TryUpdate(key, newValue, comparisonValue);
        }

        /// <summary>
        /// Asks the runtime to explore a possible interleaving.
        /// </summary>
        private static void ExploreInterleaving()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.Configuration.IsCollectionAccessRaceCheckingEnabled &&
                runtime.SchedulingPolicy != SchedulingPolicy.None &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
                {
                    runtime.ScheduleNextOperation(current, SchedulingPointType.Default);
                }
                else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                {
                    runtime.DelayOperation(current);
                }
            }
        }
    }
#pragma warning restore CA1000 // Do not declare static members on generic types
}
