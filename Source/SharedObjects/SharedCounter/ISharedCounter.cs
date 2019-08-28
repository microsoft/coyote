// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.Coyote.SharedObjects
{
    /// <summary>
    /// Interface of a shared counter.
    /// </summary>
    public interface ISharedCounter
    {
        /// <summary>
        /// Increments the shared counter.
        /// </summary>
        void Increment();

        /// <summary>
        /// Decrements the shared counter.
        /// </summary>
        void Decrement();

        /// <summary>
        /// Gets the current value of the shared counter.
        /// </summary>
        /// <returns>Current value</returns>
        int GetValue();

        /// <summary>
        /// Adds a value to the counter atomically.
        /// </summary>
        /// <param name="value">Value to add</param>
        /// <returns>The new value of the counter</returns>
        int Add(int value);

        /// <summary>
        /// Sets the counter to a value atomically.
        /// </summary>
        /// <param name="value">Value to set</param>
        /// <returns>The original value of the counter</returns>
        int Exchange(int value);

        /// <summary>
        /// Sets the counter to a value atomically if it is equal to a given value.
        /// </summary>
        /// <param name="value">Value to set</param>
        /// <param name="comparand">Value to compare against</param>
        /// <returns>The original value of the counter</returns>
        int CompareExchange(int value, int comparand);
    }
}
