// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Coyote.SharedObjects
{
    /// <summary>
    /// Implements a shared dictionary to be used in production.
    /// </summary>
    internal sealed class ProductionSharedDictionary<TKey, TValue> : ISharedDictionary<TKey, TValue>
    {
        /// <summary>
        /// The dictionary.
        /// </summary>
        private readonly ConcurrentDictionary<TKey, TValue> Dictionary;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductionSharedDictionary{TKey, TValue}"/> class.
        /// </summary>
        internal ProductionSharedDictionary()
        {
            this.Dictionary = new ConcurrentDictionary<TKey, TValue>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductionSharedDictionary{TKey, TValue}"/> class.
        /// </summary>
        internal ProductionSharedDictionary(IEqualityComparer<TKey> comparer)
        {
            this.Dictionary = new ConcurrentDictionary<TKey, TValue>(comparer);
        }

        /// <summary>
        /// Adds a new key to the dictionary, if it doesn't already exist in the dictionary.
        /// </summary>
        public bool TryAdd(TKey key, TValue value) => this.Dictionary.TryAdd(key, value);

        /// <summary>
        /// Updates the value for an existing key in the dictionary, if that key has a specific value.
        /// </summary>
        public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue) =>
            this.Dictionary.TryUpdate(key, newValue, comparisonValue);

        /// <summary>
        /// Attempts to get the value associated with the specified key.
        /// </summary>
        public bool TryGetValue(TKey key, out TValue value) => this.Dictionary.TryGetValue(key, out value);

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        public TValue this[TKey key]
        {
            get => this.Dictionary[key];

            set
            {
                this.Dictionary[key] = value;
            }
        }

        /// <summary>
        /// Removes the specified key from the dictionary.
        /// </summary>
        public bool TryRemove(TKey key, out TValue value) => this.Dictionary.TryRemove(key, out value);

        /// <summary>
        /// Gets the number of elements in the dictionary.
        /// </summary>
        public int Count
        {
            get => this.Dictionary.Count;
        }
    }
}
