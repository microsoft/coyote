// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using SystemConcurrent = System.Collections.Concurrent;
using SystemGenerics = System.Collections.Generic;

namespace Microsoft.Coyote.Rewriting.Types.Collections.Concurrent
{
    /// <summary>
    /// Provides methods for controlling a concurrent stack during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ControlledConcurrentStack
    {
        /// <summary>
        /// Gets the number of elements contained in the concurrent stack.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static int get_Count<T>(SystemConcurrent.ConcurrentStack<T> concurrentStack)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            Helper.Interleave();
            return concurrentStack.Count;
        }

        /// <summary>
        /// Gets a value that indicates whether the concurrent stack is empty.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static bool get_IsEmpty<T>(SystemConcurrent.ConcurrentStack<T> concurrentStack)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            Helper.Interleave();
            return concurrentStack.IsEmpty;
        }

        /// <summary>
        /// Removes all objects from the concurrent stack.
        /// </summary>
        public static void Clear<T>(SystemConcurrent.ConcurrentStack<T> concurrentStack)
        {
            Helper.Interleave();
            concurrentStack.Clear();
        }

        /// <summary>
        /// Copies the concurrent stack elements to an existing one-dimensional array,
        /// starting at the specified array index.
        /// </summary>
        public static void CopyTo<T>(SystemConcurrent.ConcurrentStack<T> concurrentStack, T[] array, int index)
        {
            Helper.Interleave();
            concurrentStack.CopyTo(array, index);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the  concurrent stack.
        /// </summary>
        public static SystemGenerics.IEnumerator<T> GetEnumerator<T>(SystemConcurrent.ConcurrentStack<T> concurrentStack)
        {
            Helper.Interleave();
            return concurrentStack.GetEnumerator();
        }

        /// <summary>
        /// Inserts an object at the top of the concurrent stack.
        /// </summary>
        public static void Push<T>(SystemConcurrent.ConcurrentStack<T> concurrentStack, T item)
        {
            Helper.Interleave();
            concurrentStack.Push(item);
        }

        /// <summary>
        /// Inserts multiple objects at the top of the concurrent stack atomically.
        /// </summary>
        public static void PushRange<T>(SystemConcurrent.ConcurrentStack<T> concurrentStack, T[] items)
        {
            Helper.Interleave();
            concurrentStack.PushRange(items);
        }

        /// <summary>
        /// Inserts multiple objects at the top of the concurrent stack atomically.
        /// </summary>
        public static void PushRange<T>(SystemConcurrent.ConcurrentStack<T> concurrentStack, T[] items, int startIndex, int count)
        {
            Helper.Interleave();
            concurrentStack.PushRange(items, startIndex, count);
        }

        /// <summary>
        /// Copies the elements stored in the concurrent stack to a new array.
        /// </summary>
        public static T[] ToArray<T>(SystemConcurrent.ConcurrentStack<T> concurrentStack)
        {
            Helper.Interleave();
            return concurrentStack.ToArray();
        }

        /// <summary>
        /// Attempts to return an object from the top of the concurrent stack without removing it.
        /// </summary>
        public static bool TryPeek<T>(SystemConcurrent.ConcurrentStack<T> concurrentStack, out T result)
        {
            Helper.Interleave();
            return concurrentStack.TryPeek(out result);
        }

        /// <summary>
        /// Attempt to pop and return the object at the top of the concurrent stack.
        /// </summary>
        public static bool TryPop<T>(SystemConcurrent.ConcurrentStack<T> concurrentStack, out T result)
        {
            Helper.Interleave();
            return concurrentStack.TryPop(out result);
        }

        /// <summary>
        /// Attempts to pop and return multiple objects from the top of the concurrent stack atomically.
        /// </summary>
        public static int TryPopRange<T>(SystemConcurrent.ConcurrentStack<T> concurrentStack, T[] items, int startIndex, int count)
        {
            Helper.Interleave();
            return concurrentStack.TryPopRange(items, startIndex, count);
        }

        /// <summary>
        /// Attempts to pop and return multiple objects from the top of the concurrent stack atomically.
        /// </summary>
        public static int TryPopRange<T>(SystemConcurrent.ConcurrentStack<T> concurrentStack, T[] items)
        {
            Helper.Interleave();
            return concurrentStack.TryPopRange(items);
        }
    }
}
