// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemGenerics.List<T> Create() => new Mock();

        /// <summary>
        /// Creates a new list instance that is empty and has the specified initial capacity.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemGenerics.List<T> Create(int capacity) => new Mock(capacity);

        /// <summary>
        /// Creates a new list instance that contains elements copied from the specified collection
        /// and has sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemGenerics.List<T> Create(SystemGenerics.IEnumerable<T> collection) =>
            new Mock(collection);

        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static T get_Item(SystemGenerics.List<T> list, int index)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (list as Mock)?.CheckDataRace(false);
            return list[index];
        }

        /// <summary>
        /// Sets the element at the specified index.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static void set_Item(SystemGenerics.List<T> list, int index, T value)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (list as Mock)?.CheckDataRace(true);
            list[index] = value;
        }

        /// <summary>
        /// Returns the number of elements contained in the list.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static int get_Count(SystemGenerics.List<T> list)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (list as Mock)?.CheckDataRace(false);
            return list.Count;
        }

        /// <summary>
        /// Gets the total number of elements the internal data structure can hold without resizing.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static int get_Capacity(SystemGenerics.List<T> list)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (list as Mock)?.CheckDataRace(false);
            return list.Capacity;
        }

        /// <summary>
        /// Sets the total number of elements the internal data structure can hold without resizing.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static void set_Capacity(SystemGenerics.List<T> list, int value)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (list as Mock)?.CheckDataRace(true);
            list.Capacity = value;
        }

        /// <summary>
        /// Adds an object to the end of the list.
        /// </summary>
        public static void Add(SystemGenerics.List<T> list, T item)
        {
            (list as Mock)?.CheckDataRace(true);
            list.Add(item);
        }

        /// <summary>
        /// Adds the elements of the specified collection to the end of the list.
        /// </summary>
        public static void AddRange(SystemGenerics.List<T> list, SystemGenerics.IEnumerable<T> collection)
        {
            (list as Mock)?.CheckDataRace(true);
            list.AddRange(collection);
        }

        /// <summary>
        /// Searches the entire sorted list for an element using the default
        /// comparer and returns the zero-based index of the element.
        /// </summary>
        public static void BinarySearch(SystemGenerics.List<T> list, T item)
        {
            (list as Mock)?.CheckDataRace(false);
            list.BinarySearch(item);
        }

        /// <summary>
        /// Searches the entire sorted list for an element using the specified
        /// comparer and returns the zero-based index of the element.
        /// </summary>
        public static void BinarySearch(SystemGenerics.List<T> list, T item, SystemGenerics.IComparer<T> comparer)
        {
            (list as Mock)?.CheckDataRace(false);
            list.BinarySearch(item, comparer);
        }

        /// <summary>
        /// Searches a range of elements in the sorted list for an element using the
        /// specified comparer and returns the zero-based index of the element.
        /// </summary>
        public static void BinarySearch(SystemGenerics.List<T> list, int index, int count, T item, SystemGenerics.IComparer<T> comparer)
        {
            (list as Mock)?.CheckDataRace(false);
            list.BinarySearch(index, count, item, comparer);
        }

        /// <summary>
        /// Removes all elements from the list.
        /// </summary>
        public static void Clear(SystemGenerics.List<T> list)
        {
            (list as Mock)?.CheckDataRace(true);
            list.Clear();
        }

        /// <summary>
        /// Determines whether an element is in the list.
        /// </summary>
        public static bool Contains(SystemGenerics.List<T> list, T item)
        {
            (list as Mock)?.CheckDataRace(false);
            return list.Contains(item);
        }

        /// <summary>
        /// Converts the elements in the current list to another type,
        /// and returns a list containing the converted elements.
        /// </summary>
        public static SystemGenerics.List<TOutput> ConvertAll<TOutput>(
            SystemGenerics.List<T> list, Converter<T, TOutput> converter)
        {
            (list as Mock)?.CheckDataRace(false);
            return list.ConvertAll(converter);
        }

        /// <summary>
        /// Copies the entire list to a compatible one-dimensional array,
        /// starting at the beginning of the target array.
        /// </summary>
        public static void CopyTo(SystemGenerics.List<T> list, T[] array)
        {
            (list as Mock)?.CheckDataRace(false);
            list.CopyTo(array);
        }

        /// <summary>
        /// Copies the entire list to a compatible one-dimensional array,
        /// starting at the specified index of the target array.
        /// </summary>
        public static void CopyTo(SystemGenerics.List<T> list, T[] array, int arrayIndex)
        {
            (list as Mock)?.CheckDataRace(false);
            list.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Copies a range of elements from the list to a compatible one-dimensional array,
        /// starting at the specified index of the target array.
        /// </summary>
        public static void CopyTo(SystemGenerics.List<T> list, int index, T[] array, int arrayIndex, int count)
        {
            (list as Mock)?.CheckDataRace(false);
            list.CopyTo(index, array, arrayIndex, count);
        }

        /// <summary>
        /// Determines whether the list contains elements that match the
        /// conditions defined by the specified predicate.
        /// </summary>
        public static bool Exists(SystemGenerics.List<T> list, Predicate<T> match)
        {
            (list as Mock)?.CheckDataRace(false);
            return list.Exists(match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the first occurrence within the entire list.
        /// </summary>
        public static T Find(SystemGenerics.List<T> list, Predicate<T> match)
        {
            (list as Mock)?.CheckDataRace(false);
            return list.Find(match);
        }

        /// <summary>
        /// Retrieves all the elements that match the conditions defined by the specified predicate.
        /// </summary>
        public static SystemGenerics.List<T> FindAll(SystemGenerics.List<T> list, Predicate<T> match)
        {
            (list as Mock)?.CheckDataRace(false);
            return list.FindAll(match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the first occurrence within the
        /// range of elements in the list that starts at the specified index and contains
        /// the specified number of elements.
        /// </summary>
        public static int FindIndex(SystemGenerics.List<T> list, int startIndex, int count, Predicate<T> match)
        {
            (list as Mock)?.CheckDataRace(false);
            return list.FindIndex(startIndex, count, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified predicate,
        /// and returns the zero-based index of the first occurrence within the range of elements
        /// in the list that extends from the specified index to the last element.
        /// </summary>
        public static int FindIndex(SystemGenerics.List<T> list, int startIndex, Predicate<T> match)
        {
            (list as Mock)?.CheckDataRace(false);
            return list.FindIndex(startIndex, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the first occurrence within the
        /// entire list.
        /// </summary>
        public static int FindIndex(SystemGenerics.List<T> list, Predicate<T> match)
        {
            (list as Mock)?.CheckDataRace(false);
            return list.FindIndex(match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the last occurrence within the entire list.
        /// </summary>
        public static T FindLast(SystemGenerics.List<T> list, Predicate<T> match)
        {
            (list as Mock)?.CheckDataRace(false);
            return list.FindLast(match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the last occurrence within the
        /// range of elements in the list that contains the specified
        /// number of elements and ends at the specified index.
        /// </summary>
        public static int FindLastIndex(SystemGenerics.List<T> list, int startIndex, int count, Predicate<T> match)
        {
            (list as Mock)?.CheckDataRace(false);
            return list.FindLastIndex(startIndex, count, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the last occurrence within the
        /// range of elements in the list that extends from the first
        /// element to the specified index.
        /// </summary>
        public static int FindLastIndex(SystemGenerics.List<T> list, int startIndex, Predicate<T> match)
        {
            (list as Mock)?.CheckDataRace(false);
            return list.FindLastIndex(startIndex, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the last occurrence within the
        /// entire list.
        /// </summary>
        public static int FindLastIndex(SystemGenerics.List<T> list, Predicate<T> match)
        {
            (list as Mock)?.CheckDataRace(false);
            return list.FindLastIndex(match);
        }

        /// <summary>
        /// Performs the specified action on each element of the list.
        /// </summary>
        public static void ForEach(SystemGenerics.List<T> list, Action<T> action)
        {
            (list as Mock)?.CheckDataRace(false);
            list.ForEach(action);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the list.
        /// </summary>
        public static SystemGenerics.List<T>.Enumerator GetEnumerator(SystemGenerics.List<T> list)
        {
            (list as Mock)?.CheckDataRace(false);
            return list.GetEnumerator();
        }

        /// <summary>
        /// Creates a shallow copy of a range of elements in the source list.
        /// </summary>
        public static SystemGenerics.List<T> GetRange(SystemGenerics.List<T> list, int index, int count)
        {
            (list as Mock)?.CheckDataRace(false);
            return list.GetRange(index, count);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first
        /// occurrence within the range of elements in the list that starts at the specified
        /// index and contains the specified number of elements.
        /// </summary>
        public static int IndexOf(SystemGenerics.List<T> list, T item, int index, int count)
        {
            (list as Mock)?.CheckDataRace(false);
            return list.IndexOf(item, index, count);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the
        /// first occurrence within the range of elements in the list that extends from
        /// the specified index to the last element.
        /// </summary>
        public static int IndexOf(SystemGenerics.List<T> list, T item, int index)
        {
            (list as Mock)?.CheckDataRace(false);
            return list.IndexOf(item, index);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first
        /// occurrence within the entire list.
        /// </summary>
        public static int IndexOf(SystemGenerics.List<T> list, T item)
        {
            (list as Mock)?.CheckDataRace(false);
            return list.IndexOf(item);
        }

        /// <summary>
        /// Inserts an element into the list at the specified index.
        /// </summary>
        public static void Insert(SystemGenerics.List<T> list, int index, T item)
        {
            (list as Mock)?.CheckDataRace(true);
            list.Insert(index, item);
        }

        /// <summary>
        /// Inserts the elements of a collection into the list
        /// at the specified index.
        /// </summary>
        public static void InsertRange(SystemGenerics.List<T> list, int index,
            SystemGenerics.IEnumerable<T> collection)
        {
            (list as Mock)?.CheckDataRace(true);
            list.InsertRange(index, collection);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the last
        /// occurrence within the entire list.
        /// </summary>
        public static int LastIndexOf(SystemGenerics.List<T> list, T item)
        {
            (list as Mock)?.CheckDataRace(false);
            return list.LastIndexOf(item);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the last
        /// occurrence within the range of elements in the list that extends from the first
        /// element to the specified index.
        /// </summary>
        public static int LastIndexOf(SystemGenerics.List<T> list, T item, int index)
        {
            (list as Mock)?.CheckDataRace(false);
            return list.LastIndexOf(item, index);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the last
        /// occurrence within the range of elements in the list that contains the specified
        /// number of elements and ends at the specified index.
        /// </summary>
        public static int LastIndexOf(SystemGenerics.List<T> list, T item, int index, int count)
        {
            (list as Mock)?.CheckDataRace(false);
            return list.LastIndexOf(item, index, count);
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the list.
        /// </summary>
        public static bool Remove(SystemGenerics.List<T> list, T item)
        {
            (list as Mock)?.CheckDataRace(true);
            return list.Remove(item);
        }

        /// <summary>
        /// Removes all the elements that match the conditions defined by the specified predicate.
        /// </summary>
        public static int RemoveAll(SystemGenerics.List<T> list, Predicate<T> match)
        {
            (list as Mock)?.CheckDataRace(true);
            return list.RemoveAll(match);
        }

        /// <summary>
        /// Removes the element at the specified index of the list.
        /// </summary>
        public static void RemoveAt(SystemGenerics.List<T> list, int index)
        {
            (list as Mock)?.CheckDataRace(true);
            list.RemoveAt(index);
        }

        /// <summary>
        /// Removes a range of elements from the list.
        /// </summary>
        public static void RemoveRange(SystemGenerics.List<T> list, int index, int count)
        {
            (list as Mock)?.CheckDataRace(true);
            list.RemoveRange(index, count);
        }

        /// <summary>
        /// Reverses the order of the elements in the specified range.
        /// </summary>
        public static void Reverse(SystemGenerics.List<T> list, int index, int count)
        {
            (list as Mock)?.CheckDataRace(true);
            list.Reverse(index, count);
        }

        /// <summary>
        /// Reverses the order of the elements in the entire list.
        /// </summary>
        public static void Reverse(SystemGenerics.List<T> list)
        {
            (list as Mock)?.CheckDataRace(true);
            list.Reverse();
        }

        /// <summary>
        /// Sorts the elements in the entire list using the specified <see cref="Comparison{T}"/>.
        /// </summary>
        public static void Sort(SystemGenerics.List<T> list, Comparison<T> comparison)
        {
            (list as Mock)?.CheckDataRace(true);
            list.Sort(comparison);
        }

        /// <summary>
        /// Sorts the elements in a range of elements in list using the specified comparer.
        /// </summary>
        public static void Sort(SystemGenerics.List<T> list, int index, int count,
            SystemGenerics.IComparer<T> comparer)
        {
            (list as Mock)?.CheckDataRace(true);
            list.Sort(index, count, comparer);
        }

        /// <summary>
        /// Sorts the elements in the entire list using the default comparer.
        /// </summary>
        public static void Sort(SystemGenerics.List<T> list)
        {
            (list as Mock)?.CheckDataRace(true);
            list.Sort();
        }

        /// <summary>
        /// Sorts the elements in the entire list using the specified comparer.
        /// </summary>
        public static void Sort(SystemGenerics.List<T> list, SystemGenerics.IComparer<T> comparer)
        {
            (list as Mock)?.CheckDataRace(true);
            list.Sort(comparer);
        }

        /// <summary>
        /// Copies the elements of the list to a new array.
        /// </summary>
        public static T[] ToArray(SystemGenerics.List<T> list)
        {
            (list as Mock)?.CheckDataRace(false);
            return list.ToArray();
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the list,
        /// if that number is less than a threshold value.
        /// </summary>
        public static void TrimExcess(SystemGenerics.List<T> list)
        {
            (list as Mock)?.CheckDataRace(true);
            list.TrimExcess();
        }

        /// <summary>
        /// Determines whether every element in the list matches
        /// the conditions defined by the specified predicate.
        /// </summary>
        public static bool TrueForAll(SystemGenerics.List<T> list, Predicate<T> match)
        {
            (list as Mock)?.CheckDataRace(false);
            return list.TrueForAll(match);
        }

        /// <summary>
        /// Implements a list that can be controlled during testing.
        /// </summary>
        private class Mock : SystemGenerics.List<T>
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
            /// Initializes a new instance of the <see cref="Mock"/> class.
            /// </summary>
            internal Mock()
                : base() => this.Setup();

            /// <summary>
            /// Initializes a new instance of the <see cref="Mock"/> class.
            /// </summary>
            internal Mock(SystemGenerics.IEnumerable<T> collection)
                : base(collection) => this.Setup();

            /// <summary>
            /// Initializes a new instance of the <see cref="Mock"/> class.
            /// </summary>
            internal Mock(int capacity)
                : base(capacity) => this.Setup();

            /// <summary>
            /// Setups the mock.
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

                    if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
                    {
                        runtime.ScheduleNextOperation(SchedulingPointType.Interleave);
                    }
                    else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                    {
                        runtime.DelayOperation();
                    }

                    SystemInterlocked.Decrement(ref this.WriterCount);
                }
                else
                {
                    runtime.Assert(this.WriterCount is 0,
                        $"Found read/write data race on '{typeof(SystemGenerics.List<T>)}'.");
                    SystemInterlocked.Increment(ref this.ReaderCount);

                    if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
                    {
                        runtime.ScheduleNextOperation(SchedulingPointType.Interleave);
                    }
                    else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                    {
                        runtime.DelayOperation();
                    }

                    SystemInterlocked.Decrement(ref this.ReaderCount);
                }
            }
        }
    }
#pragma warning restore CA1000 // Do not declare static members on generic types
}
