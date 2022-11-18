// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Runtime;
using SystemGenerics = System.Collections.Generic;
using SystemInterlocked = System.Threading.Interlocked;

namespace Microsoft.Coyote.Rewriting.Types.Collections.Generic
{
#pragma warning disable CA1000 // Do not declare static members on generic types
    /// <summary>
    /// Provides methods for creating generic lists that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class List<T>
    {
        /// <summary>
        /// Creates a new list instance that is empty and has the default initial capacity.
        /// </summary>
        public static SystemGenerics.List<T> Create() =>
            CoyoteRuntime.IsExecutionControlled ?
            new Wrapper() :
            new SystemGenerics.List<T>();

        /// <summary>
        /// Creates a new list instance that is empty and has the specified initial capacity.
        /// </summary>
        public static SystemGenerics.List<T> Create(int capacity) =>
            CoyoteRuntime.IsExecutionControlled ?
            new Wrapper(capacity) :
            new SystemGenerics.List<T>(capacity);

        /// <summary>
        /// Creates a new list instance that contains elements copied from the specified collection
        /// and has sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        public static SystemGenerics.List<T> Create(SystemGenerics.IEnumerable<T> collection) =>
            CoyoteRuntime.IsExecutionControlled ?
            new Wrapper(collection) :
            new SystemGenerics.List<T>(collection);

        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static T get_Item(SystemGenerics.List<T> instance, int index)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance[index];
        }

        /// <summary>
        /// Sets the element at the specified index.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static void set_Item(SystemGenerics.List<T> instance, int index, T value)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance[index] = value;
        }

        /// <summary>
        /// Returns the number of elements contained in the list.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static int get_Count(SystemGenerics.List<T> instance)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.Count;
        }

        /// <summary>
        /// Gets the total number of elements the internal data structure can hold without resizing.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static int get_Capacity(SystemGenerics.List<T> instance)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.Capacity;
        }

        /// <summary>
        /// Sets the total number of elements the internal data structure can hold without resizing.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static void set_Capacity(SystemGenerics.List<T> instance, int value)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.Capacity = value;
        }

        /// <summary>
        /// Adds an object to the end of the list.
        /// </summary>
        public static void Add(SystemGenerics.List<T> instance, T item)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.Add(item);
        }

        /// <summary>
        /// Adds the elements of the specified collection to the end of the list.
        /// </summary>
        public static void AddRange(SystemGenerics.List<T> instance, SystemGenerics.IEnumerable<T> collection)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.AddRange(collection);
        }

        /// <summary>
        /// Searches the entire sorted list for an element using the default
        /// comparer and returns the zero-based index of the element.
        /// </summary>
        public static void BinarySearch(SystemGenerics.List<T> instance, T item)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            instance.BinarySearch(item);
        }

        /// <summary>
        /// Searches the entire sorted list for an element using the specified
        /// comparer and returns the zero-based index of the element.
        /// </summary>
        public static void BinarySearch(SystemGenerics.List<T> instance, T item, SystemGenerics.IComparer<T> comparer)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            instance.BinarySearch(item, comparer);
        }

        /// <summary>
        /// Searches a range of elements in the sorted list for an element using the
        /// specified comparer and returns the zero-based index of the element.
        /// </summary>
        public static void BinarySearch(SystemGenerics.List<T> instance, int index, int count, T item, SystemGenerics.IComparer<T> comparer)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            instance.BinarySearch(index, count, item, comparer);
        }

        /// <summary>
        /// Removes all elements from the list.
        /// </summary>
        public static void Clear(SystemGenerics.List<T> instance)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.Clear();
        }

        /// <summary>
        /// Determines whether an element is in the list.
        /// </summary>
        public static bool Contains(SystemGenerics.List<T> instance, T item)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.Contains(item);
        }

        /// <summary>
        /// Converts the elements in the current list to another type,
        /// and returns a list containing the converted elements.
        /// </summary>
        public static SystemGenerics.List<TOutput> ConvertAll<TOutput>(
            SystemGenerics.List<T> instance, Converter<T, TOutput> converter)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.ConvertAll(converter);
        }

        /// <summary>
        /// Copies the entire list to a compatible one-dimensional array,
        /// starting at the beginning of the target array.
        /// </summary>
        public static void CopyTo(SystemGenerics.List<T> instance, T[] array)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            instance.CopyTo(array);
        }

        /// <summary>
        /// Copies the entire list to a compatible one-dimensional array,
        /// starting at the specified index of the target array.
        /// </summary>
        public static void CopyTo(SystemGenerics.List<T> instance, T[] array, int arrayIndex)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            instance.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Copies a range of elements from the list to a compatible one-dimensional array,
        /// starting at the specified index of the target array.
        /// </summary>
        public static void CopyTo(SystemGenerics.List<T> instance, int index, T[] array, int arrayIndex, int count)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            instance.CopyTo(index, array, arrayIndex, count);
        }

        /// <summary>
        /// Determines whether the list contains elements that match the
        /// conditions defined by the specified predicate.
        /// </summary>
        public static bool Exists(SystemGenerics.List<T> instance, Predicate<T> match)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.Exists(match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the first occurrence within the entire list.
        /// </summary>
        public static T Find(SystemGenerics.List<T> instance, Predicate<T> match)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.Find(match);
        }

        /// <summary>
        /// Retrieves all the elements that match the conditions defined by the specified predicate.
        /// </summary>
        public static SystemGenerics.List<T> FindAll(SystemGenerics.List<T> instance, Predicate<T> match)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.FindAll(match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the first occurrence within the
        /// range of elements in the list that starts at the specified index and contains
        /// the specified number of elements.
        /// </summary>
        public static int FindIndex(SystemGenerics.List<T> instance, int startIndex, int count, Predicate<T> match)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.FindIndex(startIndex, count, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate,
        /// and returns the zero-based index of the first occurrence within the range of elements
        /// in the list that extends from the specified index to the last element.
        /// </summary>
        public static int FindIndex(SystemGenerics.List<T> instance, int startIndex, Predicate<T> match)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.FindIndex(startIndex, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the first occurrence within the
        /// entire list.
        /// </summary>
        public static int FindIndex(SystemGenerics.List<T> instance, Predicate<T> match)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.FindIndex(match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the last occurrence within the entire list.
        /// </summary>
        public static T FindLast(SystemGenerics.List<T> instance, Predicate<T> match)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.FindLast(match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the last occurrence within the
        /// range of elements in the list that contains the specified
        /// number of elements and ends at the specified index.
        /// </summary>
        public static int FindLastIndex(SystemGenerics.List<T> instance, int startIndex, int count, Predicate<T> match)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.FindLastIndex(startIndex, count, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the last occurrence within the
        /// range of elements in the list that extends from the first
        /// element to the specified index.
        /// </summary>
        public static int FindLastIndex(SystemGenerics.List<T> instance, int startIndex, Predicate<T> match)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.FindLastIndex(startIndex, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the last occurrence within the
        /// entire list.
        /// </summary>
        public static int FindLastIndex(SystemGenerics.List<T> instance, Predicate<T> match)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.FindLastIndex(match);
        }

        /// <summary>
        /// Performs the specified action on each element of the list.
        /// </summary>
        public static void ForEach(SystemGenerics.List<T> instance, Action<T> action)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            instance.ForEach(action);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the list.
        /// </summary>
        public static SystemGenerics.List<T>.Enumerator GetEnumerator(SystemGenerics.List<T> instance)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.GetEnumerator();
        }

        /// <summary>
        /// Creates a shallow copy of a range of elements in the source list.
        /// </summary>
        public static SystemGenerics.List<T> GetRange(SystemGenerics.List<T> instance, int index, int count)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.GetRange(index, count);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first
        /// occurrence within the range of elements in the list that starts at the specified
        /// index and contains the specified number of elements.
        /// </summary>
        public static int IndexOf(SystemGenerics.List<T> instance, T item, int index, int count)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.IndexOf(item, index, count);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the
        /// first occurrence within the range of elements in the list that extends from
        /// the specified index to the last element.
        /// </summary>
        public static int IndexOf(SystemGenerics.List<T> instance, T item, int index)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.IndexOf(item, index);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first
        /// occurrence within the entire list.
        /// </summary>
        public static int IndexOf(SystemGenerics.List<T> instance, T item)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.IndexOf(item);
        }

        /// <summary>
        /// Inserts an element into the list at the specified index.
        /// </summary>
        public static void Insert(SystemGenerics.List<T> instance, int index, T item)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.Insert(index, item);
        }

        /// <summary>
        /// Inserts the elements of a collection into the list
        /// at the specified index.
        /// </summary>
        public static void InsertRange(SystemGenerics.List<T> instance, int index,
            SystemGenerics.IEnumerable<T> collection)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.InsertRange(index, collection);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the last
        /// occurrence within the entire list.
        /// </summary>
        public static int LastIndexOf(SystemGenerics.List<T> instance, T item)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.LastIndexOf(item);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the last
        /// occurrence within the range of elements in the list that extends from the first
        /// element to the specified index.
        /// </summary>
        public static int LastIndexOf(SystemGenerics.List<T> instance, T item, int index)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.LastIndexOf(item, index);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the last
        /// occurrence within the range of elements in the list that contains the specified
        /// number of elements and ends at the specified index.
        /// </summary>
        public static int LastIndexOf(SystemGenerics.List<T> instance, T item, int index, int count)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.LastIndexOf(item, index, count);
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the list.
        /// </summary>
        public static bool Remove(SystemGenerics.List<T> instance, T item)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            return instance.Remove(item);
        }

        /// <summary>
        /// Removes all the elements that match the conditions defined by the specified predicate.
        /// </summary>
        public static int RemoveAll(SystemGenerics.List<T> instance, Predicate<T> match)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            return instance.RemoveAll(match);
        }

        /// <summary>
        /// Removes the element at the specified index of the list.
        /// </summary>
        public static void RemoveAt(SystemGenerics.List<T> instance, int index)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.RemoveAt(index);
        }

        /// <summary>
        /// Removes a range of elements from the list.
        /// </summary>
        public static void RemoveRange(SystemGenerics.List<T> instance, int index, int count)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.RemoveRange(index, count);
        }

        /// <summary>
        /// Reverses the order of the elements in the specified range.
        /// </summary>
        public static void Reverse(SystemGenerics.List<T> instance, int index, int count)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.Reverse(index, count);
        }

        /// <summary>
        /// Reverses the order of the elements in the entire list.
        /// </summary>
        public static void Reverse(SystemGenerics.List<T> instance)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.Reverse();
        }

        /// <summary>
        /// Sorts the elements in the entire list using the specified <see cref="Comparison{T}"/>.
        /// </summary>
        public static void Sort(SystemGenerics.List<T> instance, Comparison<T> comparison)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.Sort(comparison);
        }

        /// <summary>
        /// Sorts the elements in a range of elements in list using the specified comparer.
        /// </summary>
        public static void Sort(SystemGenerics.List<T> instance, int index, int count,
            SystemGenerics.IComparer<T> comparer)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.Sort(index, count, comparer);
        }

        /// <summary>
        /// Sorts the elements in the entire list using the default comparer.
        /// </summary>
        public static void Sort(SystemGenerics.List<T> instance)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.Sort();
        }

        /// <summary>
        /// Sorts the elements in the entire list using the specified comparer.
        /// </summary>
        public static void Sort(SystemGenerics.List<T> instance, SystemGenerics.IComparer<T> comparer)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.Sort(comparer);
        }

        /// <summary>
        /// Copies the elements of the list to a new array.
        /// </summary>
        public static T[] ToArray(SystemGenerics.List<T> instance)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.ToArray();
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the list,
        /// if that number is less than a threshold value.
        /// </summary>
        public static void TrimExcess(SystemGenerics.List<T> instance)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.TrimExcess();
        }

        /// <summary>
        /// Determines whether every element in the list matches
        /// the conditions defined by the specified predicate.
        /// </summary>
        public static bool TrueForAll(SystemGenerics.List<T> instance, Predicate<T> match)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.TrueForAll(match);
        }

        /// <summary>
        /// Wraps a list so that it can be controlled during testing.
        /// </summary>
        private class Wrapper : SystemGenerics.List<T>
        {
            /// <summary>
            /// Count of read accesses to the list.
            /// </summary>
            private volatile int ReaderCount;

            /// <summary>
            /// Count of write accesses to the list.
            /// </summary>
            private volatile int WriterCount;

            /// <summary>
            /// Initializes a new instance of the <see cref="Wrapper"/> class.
            /// </summary>
            internal Wrapper()
                : base() => this.Setup();

            /// <summary>
            /// Initializes a new instance of the <see cref="Wrapper"/> class.
            /// </summary>
            internal Wrapper(SystemGenerics.IEnumerable<T> collection)
                : base(collection) => this.Setup();

            /// <summary>
            /// Initializes a new instance of the <see cref="Wrapper"/> class.
            /// </summary>
            internal Wrapper(int capacity)
                : base(capacity) => this.Setup();

            /// <summary>
            /// Setups the wrapper.
            /// </summary>
            private void Setup()
            {
                this.ReaderCount = 0;
                this.WriterCount = 0;
            }

            /// <summary>
            /// Checks for a data race.
            /// </summary>
            internal void CheckDataRace(bool isWriteAccess)
            {
                var runtime = CoyoteRuntime.Current;
                if (isWriteAccess)
                {
                    runtime.Assert(this.WriterCount is 0,
                        $"Found write/write data race on '{typeof(SystemGenerics.List<T>)}'.");
                    runtime.Assert(this.ReaderCount is 0,
                        $"Found read/write data race on '{typeof(SystemGenerics.List<T>)}'.");
                    SystemInterlocked.Increment(ref this.WriterCount);
                }
                else
                {
                    runtime.Assert(this.WriterCount is 0,
                        $"Found read/write data race on '{typeof(SystemGenerics.List<T>)}'.");
                    SystemInterlocked.Increment(ref this.ReaderCount);
                }

                if (runtime.SchedulingPolicy != SchedulingPolicy.None &&
                    runtime.TryGetExecutingOperation(out ControlledOperation current))
                {
                    if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving)
                    {
                        runtime.ScheduleNextOperation(current, SchedulingPointType.Default);
                    }
                    else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                    {
                        runtime.DelayOperation(current);
                    }
                }

                if (isWriteAccess)
                {
                    SystemInterlocked.Decrement(ref this.WriterCount);
                }
                else
                {
                    SystemInterlocked.Decrement(ref this.ReaderCount);
                }
            }
        }
    }
#pragma warning restore CA1000 // Do not declare static members on generic types
}
