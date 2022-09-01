// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;

using SystemConcurrent = System.Collections.Concurrent;
using SystemGenerics = System.Collections.Generic;

namespace Microsoft.Coyote.Rewriting.Types.Collections.Concurrent
{
#pragma warning disable CA1000 // Do not declare static members on generic types
    /// <summary>
    /// Provides methods for controlling a concurrent bag during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ConcurrentBag<T>
    {
        /// <summary>
        /// Gets the number of elements contained in the concurrent bag.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static int get_Count(SystemConcurrent.ConcurrentBag<T> instance)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            SchedulingPoint.Default();
            return instance.Count;
        }

        /// <summary>
        /// Gets a value that indicates whether the concurrent bag is empty.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static bool get_IsEmpty(SystemConcurrent.ConcurrentBag<T> instance)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            SchedulingPoint.Default();
            return instance.IsEmpty;
        }

        /// <summary>
        /// Adds an object to the concurrent bag.
        /// </summary>
        public static void Add(SystemConcurrent.ConcurrentBag<T> instance, T item)
        {
            SchedulingPoint.Default();
            instance.Add(item);
        }

#if NET || NETCOREAPP3_1
        /// <summary>
        /// Removes all objects from the concurrent bag.
        /// </summary>
        public static void Clear(SystemConcurrent.ConcurrentBag<T> instance)
        {
            SchedulingPoint.Default();
            instance.Clear();
        }
#endif

        /// <summary>
        /// Copies the concurrent bag elements to an existing one-dimensional array,
        /// starting at the specified array index.
        /// </summary>
        public static void CopyTo(SystemConcurrent.ConcurrentBag<T> instance, T[] array, int index)
        {
            SchedulingPoint.Default();
            instance.CopyTo(array, index);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the  concurrent bag.
        /// </summary>
        public static SystemGenerics.IEnumerator<T> GetEnumerator(SystemConcurrent.ConcurrentBag<T> instance)
        {
            SchedulingPoint.Default();
            return instance.GetEnumerator();
        }

        /// <summary>
        /// Copies the elements stored in the concurrent bag to a new array.
        /// </summary>
        public static T[] ToArray(SystemConcurrent.ConcurrentBag<T> instance)
        {
            SchedulingPoint.Default();
            return instance.ToArray();
        }

        /// <summary>
        /// Tries to return an object from the beginning of the concurrent bag without removing it.
        /// </summary>
        public static bool TryPeek(SystemConcurrent.ConcurrentBag<T> instance, out T result)
        {
            SchedulingPoint.Default();
            return instance.TryPeek(out result);
        }

        /// <summary>
        /// Attempts to remove and return an object from the concurrent bag.
        /// </summary>
        public static bool TryTake(SystemConcurrent.ConcurrentBag<T> instance, out T result)
        {
            SchedulingPoint.Default();
            return instance.TryTake(out result);
        }
    }
#pragma warning restore CA1000 // Do not declare static members on generic types
}
