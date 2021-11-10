// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Microsoft.Coyote.Interception
{
    /// <summary>
    /// Provides methods for controlling <see cref="ConcurrentQueue{T}"/> during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ControlledConcurrentQueue
    {
        /// <summary>
        /// Gets the number of elements contained in the <see cref="ConcurrentQueue{T}"/>.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static int get_Count<T>(ConcurrentQueue<T> concurrentQueue)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentQueue.Count;
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="ConcurrentQueue{T}"/> is empty.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static bool get_IsEmpty<T>(ConcurrentQueue<T> concurrentQueue)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentQueue.IsEmpty;
        }

#if !NETSTANDARD2_0 && !NETFRAMEWORK
        /// <summary>
        /// Removes all objects from the <see cref="ConcurrentQueue{T}"/>.
        /// </summary>
        public static void Clear<T>(ConcurrentQueue<T> concurrentQueue)
        {
            ConcurrentCollectionHelper.Interleave();
            concurrentQueue.Clear();
        }
#endif

        /// <summary>
        /// Copies the <see cref="ConcurrentQueue{T}"/> elements to an existing one-dimensional <see cref="Array"/>,
        /// starting at the specified array index.
        /// </summary>
        public static void CopyTo<T>(ConcurrentQueue<T> concurrentQueue, T[] array, int index)
        {
            ConcurrentCollectionHelper.Interleave();
            concurrentQueue.CopyTo(array, index);
        }

        /// <summary>
        /// Adds an object to the end of the <see cref="ConcurrentQueue{T}"/>.
        /// </summary>
        public static void Enqueue<T>(ConcurrentQueue<T> concurrentQueue, T item)
        {
            ConcurrentCollectionHelper.Interleave();
            concurrentQueue.Enqueue(item);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the  <see cref="ConcurrentQueue{T}"/>.
        /// </summary>
        public static IEnumerator<T> GetEnumerator<T>(ConcurrentQueue<T> concurrentQueue)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentQueue.GetEnumerator();
        }

        /// <summary>
        /// Copies the elements stored in the <see cref="ConcurrentQueue{T}"/> to a new <see cref="Array"/>.
        /// </summary>
        public static T[] ToArray<T>(ConcurrentQueue<T> concurrentQueue)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentQueue.ToArray();
        }

        /// <summary>
        /// Tries to remove and return the object at the beginning of the <see cref="ConcurrentQueue{T}"/>.
        /// </summary>
        public static bool TryDequeue<T>(ConcurrentQueue<T> concurrentQueue, out T result)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentQueue.TryDequeue(out result);
        }

        /// <summary>
        /// Tries to return an object from the beginning of the <see cref="ConcurrentQueue{T}"/> without removing it.
        /// </summary>
        public static bool TryPeek<T>(ConcurrentQueue<T> concurrentQueue, out T result)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentQueue.TryPeek(out result);
        }
    }
}
