// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.Coyote.Interception
{
    /// <summary>
    /// Static implementation of the Properties and Methods of <see cref="ConcurrentBag{T}"/> that coyote uses for testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ControlledConcurrentBag
    {
        /// <summary>
        /// Gets the number of elements contained in the <see cref="ConcurrentBag{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static int get_Count<T>(ConcurrentBag<T> concurrentBag)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentBag.Count;
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="ConcurrentBag{T}"/> is empty.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static bool get_IsEmpty<T>(ConcurrentBag<T> concurrentBag)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentBag.IsEmpty;
        }

        /// <summary>
        /// Adds an object to the <see cref="ConcurrentBag{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add<T>(ConcurrentBag<T> concurrentBag, T item)
        {
            ConcurrentCollectionHelper.Interleave();
            concurrentBag.Add(item);
        }

#if !NETSTANDARD2_0 && !NETFRAMEWORK
        /// <summary>
        /// Removes all objects from the <see cref="ConcurrentBag{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear<T>(ConcurrentBag<T> concurrentBag)
        {
            ConcurrentCollectionHelper.Interleave();
            concurrentBag.Clear();
        }
#endif

        /// <summary>
        /// Copies the <see cref="ConcurrentBag{T}"/> elements to an existing one-dimensional <see cref="Array"/>,
        /// starting at the specified array index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(ConcurrentBag<T> concurrentBag, T[] array, int index)
        {
            ConcurrentCollectionHelper.Interleave();
            concurrentBag.CopyTo(array, index);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the  <see cref="ConcurrentBag{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerator<T> GetEnumerator<T>(ConcurrentBag<T> concurrentBag)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentBag.GetEnumerator();
        }

        /// <summary>
        /// Copies the elements stored in the <see cref="ConcurrentBag{T}"/> to a new <see cref="Array"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArray<T>(ConcurrentBag<T> concurrentBag)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentBag.ToArray();
        }

        /// <summary>
        /// Tries to return an object from the beginning of the <see cref="ConcurrentBag{T}"/> without removing it.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryPeek<T>(ConcurrentBag<T> concurrentBag, out T result)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentBag.TryPeek(out result);
        }

        /// <summary>
        /// Attempts to remove and return an object from the <see cref="ConcurrentBag{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryTake<T>(ConcurrentBag<T> concurrentBag, out T result)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentBag.TryTake(out result);
        }
    }
}
