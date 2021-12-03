// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using SystemConcurrent = System.Collections.Concurrent;
using SystemGenerics = System.Collections.Generic;

namespace Microsoft.Coyote.Rewriting.Types.Collections.Concurrent
{
    /// <summary>
    /// Provides methods for controlling a concurrent bag during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ControlledConcurrentBag
    {
        /// <summary>
        /// Gets the number of elements contained in the concurrent bag.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static int get_Count<T>(SystemConcurrent.ConcurrentBag<T> concurrentBag)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            Helper.Interleave();
            return concurrentBag.Count;
        }

        /// <summary>
        /// Gets a value that indicates whether the concurrent bag is empty.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static bool get_IsEmpty<T>(SystemConcurrent.ConcurrentBag<T> concurrentBag)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            Helper.Interleave();
            return concurrentBag.IsEmpty;
        }

        /// <summary>
        /// Adds an object to the concurrent bag.
        /// </summary>
        public static void Add<T>(SystemConcurrent.ConcurrentBag<T> concurrentBag, T item)
        {
            Helper.Interleave();
            concurrentBag.Add(item);
        }

#if NET || NETCOREAPP3_1
        /// <summary>
        /// Removes all objects from the concurrent bag.
        /// </summary>
        public static void Clear<T>(SystemConcurrent.ConcurrentBag<T> concurrentBag)
        {
            Helper.Interleave();
            concurrentBag.Clear();
        }
#endif

        /// <summary>
        /// Copies the concurrent bag elements to an existing one-dimensional array,
        /// starting at the specified array index.
        /// </summary>
        public static void CopyTo<T>(SystemConcurrent.ConcurrentBag<T> concurrentBag, T[] array, int index)
        {
            Helper.Interleave();
            concurrentBag.CopyTo(array, index);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the  concurrent bag.
        /// </summary>
        public static SystemGenerics.IEnumerator<T> GetEnumerator<T>(SystemConcurrent.ConcurrentBag<T> concurrentBag)
        {
            Helper.Interleave();
            return concurrentBag.GetEnumerator();
        }

        /// <summary>
        /// Copies the elements stored in the concurrent bag to a new array.
        /// </summary>
        public static T[] ToArray<T>(SystemConcurrent.ConcurrentBag<T> concurrentBag)
        {
            Helper.Interleave();
            return concurrentBag.ToArray();
        }

        /// <summary>
        /// Tries to return an object from the beginning of the concurrent bag without removing it.
        /// </summary>
        public static bool TryPeek<T>(SystemConcurrent.ConcurrentBag<T> concurrentBag, out T result)
        {
            Helper.Interleave();
            return concurrentBag.TryPeek(out result);
        }

        /// <summary>
        /// Attempts to remove and return an object from the concurrent bag.
        /// </summary>
        public static bool TryTake<T>(SystemConcurrent.ConcurrentBag<T> concurrentBag, out T result)
        {
            Helper.Interleave();
            return concurrentBag.TryTake(out result);
        }
    }
}
