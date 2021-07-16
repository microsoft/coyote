// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Microsoft.Coyote.Interception
{
    /// <summary>
    /// Static implementation of the Properties and Methods of <see cref="ConcurrentStack{T}"/> that coyote uses for testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ControlledConcurrentStack
    {
        /// <summary>
        /// Gets the number of elements contained in the <see cref="ConcurrentStack{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static int get_Count<T>(ConcurrentStack<T> concurrentStack)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentStack.Count;
        }

        /// <summary>
        /// Gets a value that indicates whether the <see cref="ConcurrentStack{T}"/> is empty.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static bool get_IsEmpty<T>(ConcurrentStack<T> concurrentStack)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentStack.IsEmpty;
        }

        /// <summary>
        /// Removes all objects from the <see cref="ConcurrentStack{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear<T>(ConcurrentStack<T> concurrentStack)
        {
            ConcurrentCollectionHelper.Interleave();
            concurrentStack.Clear();
        }

        /// <summary>
        /// Copies the <see cref="ConcurrentStack{T}"/> elements to an existing one-dimensional <see cref="Array"/>,
        /// starting at the specified array index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(ConcurrentStack<T> concurrentStack, T[] array, int index)
        {
            ConcurrentCollectionHelper.Interleave();
            concurrentStack.CopyTo(array, index);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the  <see cref="ConcurrentStack{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerator<T> GetEnumerator<T>(ConcurrentStack<T> concurrentStack)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentStack.GetEnumerator();
        }

        /// <summary>
        /// Inserts an object at the top of the <see cref="ConcurrentStack{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Push<T>(ConcurrentStack<T> concurrentStack, T item)
        {
            ConcurrentCollectionHelper.Interleave();
            concurrentStack.Push(item);
        }

        /// <summary>
        /// Inserts multiple objects at the top of the <see cref="ConcurrentStack{T}"/> atomically.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PushRange<T>(ConcurrentStack<T> concurrentStack, T[] items)
        {
            ConcurrentCollectionHelper.Interleave();
            concurrentStack.PushRange(items);
        }

        /// <summary>
        /// Inserts multiple objects at the top of the <see cref="ConcurrentStack{T}"/> atomically.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void PushRange<T>(ConcurrentStack<T> concurrentStack, T[] items, int startIndex, int count)
        {
            ConcurrentCollectionHelper.Interleave();
            concurrentStack.PushRange(items, startIndex, count);
        }

        /// <summary>
        /// Copies the elements stored in the <see cref="ConcurrentStack{T}"/> to a new <see cref="Array"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArray<T>(ConcurrentStack<T> concurrentStack)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentStack.ToArray();
        }

        /// <summary>
        /// Attempts to return an object from the top of the <see cref="ConcurrentStack{T}"/> without removing it.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryPeek<T>(ConcurrentStack<T> concurrentStack, out T result)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentStack.TryPeek(out result);
        }

        /// <summary>
        /// Attempst to pop and return the object at the top of the <see cref="ConcurrentStack{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryPop<T>(ConcurrentStack<T> concurrentStack, out T result)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrentStack.TryPop(out result);
        }

        /// <summary>
        /// Attempts to pop and return multiple objects from the top of the <see cref="ConcurrentStack{T}"/> atomically.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TryPopRange<T>(ConcurrentStack<T> concurrenStack, T[] items, int startIndex, int count)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrenStack.TryPopRange(items, startIndex, count);
        }

        /// <summary>
        /// Attempts to pop and return multiple objects from the top of the <see cref="ConcurrentStack{T}"/> atomically.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TryPopRange<T>(ConcurrentStack<T> concurrenStack, T[] items)
        {
            ConcurrentCollectionHelper.Interleave();
            return concurrenStack.TryPopRange(items);
        }
    }
}
