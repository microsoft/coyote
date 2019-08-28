// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.Coyote.SharedObjects
{
    /// <summary>
    /// Interface of a shared dictionary.
    /// </summary>
    public interface ISharedDictionary<TKey, TValue>
    {
        /// <summary>
        /// Adds a new key to the dictionary, if it doesn’t already exist in the dictionary.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value</param>
        /// <returns>True or false depending on whether the new key/value pair was added.</returns>
        bool TryAdd(TKey key, TValue value);

        /// <summary>
        /// Updates the value for an existing key in the dictionary, if that key has a specific value.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="newValue">New value</param>
        /// <param name="comparisonValue">Old value</param>
        /// <returns>True if the value with key was equal to comparisonValue and was replaced with newValue; otherwise, false.</returns>
        bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue);

        /// <summary>
        /// Attempts to get the value associated with the specified key.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value associated with the key, or the default value if the key does not exist</param>
        /// <returns>True if the key was found; otherwise, false.</returns>
        bool TryGetValue(TKey key, out TValue value);

        /// <summary>
        /// Gets or sets the value associated with the specified key.
        /// </summary>
        /// <param name="key">Key</param>
        /// <returns>Value</returns>
        TValue this[TKey key] { get;  set;  }

        /// <summary>
        /// Removes the specified key from the dictionary.
        /// </summary>
        /// <param name="key">Key</param>
        /// <param name="value">Value associated with the key if present, or the default value otherwise.</param>
        /// <returns>True if the element is successfully removed; otherwise, false.</returns>
        bool TryRemove(TKey key, out TValue value);

        /// <summary>
        /// Gets the number of elements in the dictionary.
        /// </summary>
        /// <returns>Size</returns>
        int Count { get; }
    }
}
