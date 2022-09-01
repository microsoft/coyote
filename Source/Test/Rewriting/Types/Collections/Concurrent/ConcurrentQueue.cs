// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;

using SystemConcurrent = System.Collections.Concurrent;
using SystemGenerics = System.Collections.Generic;

namespace Microsoft.Coyote.Rewriting.Types.Collections.Concurrent
{
#pragma warning disable CA1000 // Do not declare static members on generic types
    /// <summary>
    /// Provides methods for controlling a concurrent queue during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ConcurrentQueue<T>
    {
        /// <summary>
        /// Gets the number of elements contained in the concurrent queue.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static int get_Count(SystemConcurrent.ConcurrentQueue<T> instance)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            SchedulingPoint.Default();
            return instance.Count;
        }

        /// <summary>
        /// Gets a value that indicates whether the concurrent queue is empty.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static bool get_IsEmpty(SystemConcurrent.ConcurrentQueue<T> instance)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            SchedulingPoint.Default();
            return instance.IsEmpty;
        }

#if NET || NETCOREAPP3_1
        /// <summary>
        /// Removes all objects from the concurrent queue.
        /// </summary>
        public static void Clear(SystemConcurrent.ConcurrentQueue<T> instance)
        {
            SchedulingPoint.Default();
            instance.Clear();
        }
#endif

        /// <summary>
        /// Copies the concurrent queue elements to an existing one-dimensional array,
        /// starting at the specified array index.
        /// </summary>
        public static void CopyTo(SystemConcurrent.ConcurrentQueue<T> instance, T[] array, int index)
        {
            SchedulingPoint.Default();
            instance.CopyTo(array, index);
        }

        /// <summary>
        /// Adds an object to the end of the concurrent queue.
        /// </summary>
        public static void Enqueue(SystemConcurrent.ConcurrentQueue<T> instance, T item)
        {
            SchedulingPoint.Default();
            instance.Enqueue(item);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the  concurrent queue.
        /// </summary>
        public static SystemGenerics.IEnumerator<T> GetEnumerator(SystemConcurrent.ConcurrentQueue<T> instance)
        {
            SchedulingPoint.Default();
            return instance.GetEnumerator();
        }

        /// <summary>
        /// Copies the elements stored in the concurrent queue to a new array.
        /// </summary>
        public static T[] ToArray(SystemConcurrent.ConcurrentQueue<T> instance)
        {
            SchedulingPoint.Default();
            return instance.ToArray();
        }

        /// <summary>
        /// Tries to remove and return the object at the beginning of the concurrent queue.
        /// </summary>
        public static bool TryDequeue(SystemConcurrent.ConcurrentQueue<T> instance, out T result)
        {
            SchedulingPoint.Default();
            return instance.TryDequeue(out result);
        }

        /// <summary>
        /// Tries to return an object from the beginning of the concurrent queue without removing it.
        /// </summary>
        public static bool TryPeek(SystemConcurrent.ConcurrentQueue<T> instance, out T result)
        {
            SchedulingPoint.Default();
            return instance.TryPeek(out result);
        }
    }
#pragma warning restore CA1000 // Do not declare static members on generic types
}
