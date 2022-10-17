// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;

using SystemConcurrent = System.Collections.Concurrent;
using SystemGenerics = System.Collections.Generic;

namespace Microsoft.Coyote.Rewriting.Types.Collections.Concurrent
{
#pragma warning disable CA1000 // Do not declare static members on generic types
    /// <summary>
    /// Provides methods for controlling a concurrent stack during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ConcurrentStack<T>
    {
        /// <summary>
        /// Gets the number of elements contained in the concurrent stack.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static int get_Count(SystemConcurrent.ConcurrentStack<T> instance)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            Operation.ScheduleNext();
            return instance.Count;
        }

        /// <summary>
        /// Gets a value that indicates whether the concurrent stack is empty.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static bool get_IsEmpty(SystemConcurrent.ConcurrentStack<T> instance)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            Operation.ScheduleNext();
            return instance.IsEmpty;
        }

        /// <summary>
        /// Removes all objects from the concurrent stack.
        /// </summary>
        public static void Clear(SystemConcurrent.ConcurrentStack<T> instance)
        {
            Operation.ScheduleNext();
            instance.Clear();
        }

        /// <summary>
        /// Copies the concurrent stack elements to an existing one-dimensional array,
        /// starting at the specified array index.
        /// </summary>
        public static void CopyTo(SystemConcurrent.ConcurrentStack<T> instance, T[] array, int index)
        {
            Operation.ScheduleNext();
            instance.CopyTo(array, index);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the  concurrent stack.
        /// </summary>
        public static SystemGenerics.IEnumerator<T> GetEnumerator(SystemConcurrent.ConcurrentStack<T> instance)
        {
            Operation.ScheduleNext();
            return instance.GetEnumerator();
        }

        /// <summary>
        /// Inserts an object at the top of the concurrent stack.
        /// </summary>
        public static void Push(SystemConcurrent.ConcurrentStack<T> instance, T item)
        {
            Operation.ScheduleNext();
            instance.Push(item);
        }

        /// <summary>
        /// Inserts multiple objects at the top of the concurrent stack atomically.
        /// </summary>
        public static void PushRange(SystemConcurrent.ConcurrentStack<T> instance, T[] items)
        {
            Operation.ScheduleNext();
            instance.PushRange(items);
        }

        /// <summary>
        /// Inserts multiple objects at the top of the concurrent stack atomically.
        /// </summary>
        public static void PushRange(SystemConcurrent.ConcurrentStack<T> instance, T[] items, int startIndex, int count)
        {
            Operation.ScheduleNext();
            instance.PushRange(items, startIndex, count);
        }

        /// <summary>
        /// Copies the elements stored in the concurrent stack to a new array.
        /// </summary>
        public static T[] ToArray(SystemConcurrent.ConcurrentStack<T> instance)
        {
            Operation.ScheduleNext();
            return instance.ToArray();
        }

        /// <summary>
        /// Attempts to return an object from the top of the concurrent stack without removing it.
        /// </summary>
        public static bool TryPeek(SystemConcurrent.ConcurrentStack<T> instance, out T result)
        {
            Operation.ScheduleNext();
            return instance.TryPeek(out result);
        }

        /// <summary>
        /// Attempt to pop and return the object at the top of the concurrent stack.
        /// </summary>
        public static bool TryPop(SystemConcurrent.ConcurrentStack<T> instance, out T result)
        {
            Operation.ScheduleNext();
            return instance.TryPop(out result);
        }

        /// <summary>
        /// Attempts to pop and return multiple objects from the top of the concurrent stack atomically.
        /// </summary>
        public static int TryPopRange(SystemConcurrent.ConcurrentStack<T> instance, T[] items, int startIndex, int count)
        {
            Operation.ScheduleNext();
            return instance.TryPopRange(items, startIndex, count);
        }

        /// <summary>
        /// Attempts to pop and return multiple objects from the top of the concurrent stack atomically.
        /// </summary>
        public static int TryPopRange(SystemConcurrent.ConcurrentStack<T> instance, T[] items)
        {
            Operation.ScheduleNext();
            return instance.TryPopRange(items);
        }
    }
#pragma warning restore CA1000 // Do not declare static members on generic types
}
