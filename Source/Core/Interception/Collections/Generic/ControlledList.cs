// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Interception
{
    /// <summary>
    /// Provides methods for creating generic lists that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ControlledList
    {
        /// <summary>
        /// Creates a new instance of the <see cref="List{T}"/> clas that is empty and has the default initial capacity.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> Create<T>() => new Mock<T>();

        /// <summary>
        /// Creates a new instance of the <see cref="List{T}"/> clas that is empty and has the specified initial capacity.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> Create<T>(int capacity) => new Mock<T>(capacity);

        /// <summary>
        /// Creates a new instance of the <see cref="List{T}"/> class that contains elements copied from the specified
        /// collection and has sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> Create<T>(IEnumerable<T> collection) => new Mock<T>(collection);

        /// <summary>
        /// Gets the element at the specified index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static T get_Item<T>(List<T> list, int index)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list[index];
        }

        /// <summary>
        /// Sets the element at the specified index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static void set_Item<T>(List<T> list, int index, T value)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (list as Mock<T>)?.CheckDataRace(true);
            list[index] = value;
        }

        /// <summary>
        /// Returns the number of elements contained in the <see cref="List{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static int get_Count<T>(List<T> list)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.Count;
        }

        /// <summary>
        /// Gets the total number of elements the internal data structure can hold without resizing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static int get_Capacity<T>(List<T> list)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.Capacity;
        }

        /// <summary>
        /// Sets the total number of elements the internal data structure can hold without resizing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static void set_Capacity<T>(List<T> list, int value)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (list as Mock<T>)?.CheckDataRace(true);
            list.Capacity = value;
        }

        /// <summary>
        /// Adds an object to the end of the <see cref="List{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add<T>(List<T> list, T item)
        {
            (list as Mock<T>)?.CheckDataRace(true);
            list.Add(item);
        }

        /// <summary>
        /// Adds the elements of the specified collection to the end of the <see cref="List{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddRange<T>(List<T> list, IEnumerable<T> collection)
        {
            (list as Mock<T>)?.CheckDataRace(true);
            list.AddRange(collection);
        }

        /// <summary>
        /// Searches the entire sorted <see cref="List{T}"/> for an element using
        /// the default comparer and returns the zero-based index of the element.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BinarySearch<T>(List<T> list, T item)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            list.BinarySearch(item);
        }

        /// <summary>
        /// Searches the entire sorted <see cref="List{T}"/> for an element using
        /// the specified comparer and returns the zero-based index of the element.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BinarySearch<T>(List<T> list, T item, IComparer<T> comparer)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            list.BinarySearch(item, comparer);
        }

        /// <summary>
        /// Searches a range of elements in the sorted <see cref="List{T}"/> for an element using
        /// the specified comparer and returns the zero-based index of the element.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BinarySearch<T>(List<T> list, int index, int count, T item, IComparer<T> comparer)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            list.BinarySearch(index, count, item, comparer);
        }

        /// <summary>
        /// Removes all elements from the <see cref="List{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear<T>(List<T> list)
        {
            (list as Mock<T>)?.CheckDataRace(true);
            list.Clear();
        }

        /// <summary>
        /// Determines whether an element is in the <see cref="List{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T>(List<T> list, T item)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.Contains(item);
        }

        /// <summary>
        /// Converts the elements in the current <see cref="List{T}"/> to another
        /// type, and returns a list containing the converted elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TOutput> ConvertAll<T, TOutput>(List<T> list, Converter<T, TOutput> converter)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.ConvertAll(converter);
        }

        /// <summary>
        /// Copies the entire <see cref="List{T}"/> to a compatible one-dimensional
        /// array, starting at the beginning of the target array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(List<T> list, T[] array)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            list.CopyTo(array);
        }

        /// <summary>
        /// Copies the entire <see cref="List{T}"/> to a compatible one-dimensional
        /// array, starting at the specified index of the target array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(List<T> list, T[] array, int arrayIndex)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            list.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Copies a range of elements from the <see cref="List{T}"/> to a compatible
        /// one-dimensional array, starting at the specified index of the target array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(List<T> list, int index, T[] array, int arrayIndex, int count)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            list.CopyTo(index, array, arrayIndex, count);
        }

        /// <summary>
        /// Determines whether the <see cref="List{T}"/> contains elements that
        /// match the conditions defined by the specified predicate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Exists<T>(List<T> list, Predicate<T> match)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.Exists(match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the first occurrence within the entire <see cref="List{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Find<T>(List<T> list, Predicate<T> match)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.Find(match);
        }

        /// <summary>
        /// Retrieves all the elements that match the conditions defined by the specified predicate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> FindAll<T>(List<T> list, Predicate<T> match)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.FindAll(match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the first occurrence within the
        /// range of elements in the <see cref="List{T}"/> that starts at the specified
        /// index and contains the specified number of elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindIndex<T>(List<T> list, int startIndex, int count, Predicate<T> match)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.FindIndex(startIndex, count, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the first occurrence within the
        /// range of elements in the <see cref="List{T}"/> that extends from the specified
        /// index to the last element.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindIndex<T>(List<T> list, int startIndex, Predicate<T> match)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.FindIndex(startIndex, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the first occurrence within the
        /// entire <see cref="List{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindIndex<T>(List<T> list, Predicate<T> match)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.FindIndex(match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the last occurrence within the entire <see cref="List{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindLast<T>(List<T> list, Predicate<T> match)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.FindLast(match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the last occurrence within the
        /// range of elements in the <see cref="List{T}"/> that contains the specified
        /// number of elements and ends at the specified index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindLastIndex<T>(List<T> list, int startIndex, int count, Predicate<T> match)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.FindLastIndex(startIndex, count, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the last occurrence within the
        /// range of elements in the <see cref="List{T}"/> that extends from the first
        /// element to the specified index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindLastIndex<T>(List<T> list, int startIndex, Predicate<T> match)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.FindLastIndex(startIndex, match);
        }

        /// <summary>
        /// Searches for an element that matches the conditions defined by the specified
        /// predicate, and returns the zero-based index of the last occurrence within the
        /// entire <see cref="List{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindLastIndex<T>(List<T> list, Predicate<T> match)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.FindLastIndex(match);
        }

        /// <summary>
        /// Performs the specified action on each element of the <see cref="List{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ForEach<T>(List<T> list, Action<T> action)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            list.ForEach(action);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="List{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T>.Enumerator GetEnumerator<T>(List<T> list)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.GetEnumerator();
        }

        /// <summary>
        /// Creates a shallow copy of a range of elements in the source <see cref="List{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<T> GetRange<T>(List<T> list, int index, int count)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.GetRange(index, count);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first
        /// occurrence within the range of elements in the <see cref="List{T}"/> that starts
        /// at the specified index and contains the specified number of elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(List<T> list, T item, int index, int count)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.IndexOf(item, index, count);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first
        /// occurrence within the range of elements in the <see cref="List{T}"/> that extends
        /// from the specified index to the last element.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(List<T> list, T item, int index)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.IndexOf(item, index);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the first
        /// occurrence within the entire <see cref="List{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int IndexOf<T>(List<T> list, T item)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.IndexOf(item);
        }

        /// <summary>
        /// Inserts an element into the <see cref="List{T}"/> at the specified index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Insert<T>(List<T> list, int index, T item)
        {
            (list as Mock<T>)?.CheckDataRace(true);
            list.Insert(index, item);
        }

        /// <summary>
        /// Inserts the elements of a collection into the <see cref="List{T}"/>
        /// at the specified index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void InsertRange<T>(List<T> list, int index, IEnumerable<T> collection)
        {
            (list as Mock<T>)?.CheckDataRace(true);
            list.InsertRange(index, collection);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the last
        /// occurrence within the entire <see cref="List{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf<T>(List<T> list, T item)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.LastIndexOf(item);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the last
        /// occurrence within the range of elements in the <see cref="List{T}"/> that extends
        /// from the first element to the specified index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf<T>(List<T> list, T item, int index)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.LastIndexOf(item, index);
        }

        /// <summary>
        /// Searches for the specified object and returns the zero-based index of the last
        /// occurrence within the range of elements in the <see cref="List{T}"/> that contains
        /// the specified number of elements and ends at the specified index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LastIndexOf<T>(List<T> list, T item, int index, int count)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.LastIndexOf(item, index, count);
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="List{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Remove<T>(List<T> list, T item)
        {
            (list as Mock<T>)?.CheckDataRace(true);
            return list.Remove(item);
        }

        /// <summary>
        /// Removes all the elements that match the conditions defined by the specified predicate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RemoveAll<T>(List<T> list, Predicate<T> match)
        {
            (list as Mock<T>)?.CheckDataRace(true);
            return list.RemoveAll(match);
        }

        /// <summary>
        /// Removes the element at the specified index of the <see cref="List{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveAt<T>(List<T> list, int index)
        {
            (list as Mock<T>)?.CheckDataRace(true);
            list.RemoveAt(index);
        }

        /// <summary>
        /// Removes a range of elements from the <see cref="List{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void RemoveRange<T>(List<T> list, int index, int count)
        {
            (list as Mock<T>)?.CheckDataRace(true);
            list.RemoveRange(index, count);
        }

        /// <summary>
        /// Reverses the order of the elements in the specified range.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Reverse<T>(List<T> list, int index, int count)
        {
            (list as Mock<T>)?.CheckDataRace(true);
            list.Reverse(index, count);
        }

        /// <summary>
        /// Reverses the order of the elements in the entire <see cref="List{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Reverse<T>(List<T> list)
        {
            (list as Mock<T>)?.CheckDataRace(true);
            list.Reverse();
        }

        /// <summary>
        /// Sorts the elements in the entire <see cref="List{T}"/> using the
        /// specified <see cref="Comparison{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<T>(List<T> list, Comparison<T> comparison)
        {
            (list as Mock<T>)?.CheckDataRace(true);
            list.Sort(comparison);
        }

        /// <summary>
        /// Sorts the elements in a range of elements in <see cref="List{T}"/>
        /// using the specified comparer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<T>(List<T> list, int index, int count, IComparer<T> comparer)
        {
            (list as Mock<T>)?.CheckDataRace(true);
            list.Sort(index, count, comparer);
        }

        /// <summary>
        /// Sorts the elements in the entire <see cref="List{T}"/> using the default comparer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<T>(List<T> list)
        {
            (list as Mock<T>)?.CheckDataRace(true);
            list.Sort();
        }

        /// <summary>
        /// Sorts the elements in the entire <see cref="List{T}"/> using the specified comparer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Sort<T>(List<T> list, IComparer<T> comparer)
        {
            (list as Mock<T>)?.CheckDataRace(true);
            list.Sort(comparer);
        }

        /// <summary>
        /// Copies the elements of the <see cref="List{T}"/> to a new array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T[] ToArray<T>(List<T> list)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.ToArray();
        }

        /// <summary>
        /// Sets the capacity to the actual number of elements in the <see cref="List{T}"/>,
        /// if that number is less than a threshold value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TrimExcess<T>(List<T> list)
        {
            (list as Mock<T>)?.CheckDataRace(true);
            list.TrimExcess();
        }

        /// <summary>
        /// Determines whether every element in the <see cref="List{T}"/> matches
        /// the conditions defined by the specified predicate.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TrueForAll<T>(List<T> list, Predicate<T> match)
        {
            (list as Mock<T>)?.CheckDataRace(false);
            return list.TrueForAll(match);
        }

        /// <summary>
        /// Implements a <see cref="List{T}"/> that can be controlled during testing.
        /// </summary>
        private class Mock<T> : List<T>
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
            /// Initializes a new instance of the <see cref="Mock{T}"/> class.
            /// </summary>
            internal Mock()
                : base() => this.Setup();

            /// <summary>
            /// Initializes a new instance of the <see cref="Mock{T}"/> class.
            /// </summary>
            internal Mock(IEnumerable<T> collection)
                : base(collection) => this.Setup();

            /// <summary>
            /// Initializes a new instance of the <see cref="Mock{T}"/> class.
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
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void CheckDataRace(bool isWriteAccess)
            {
                var runtime = CoyoteRuntime.Current;
                if (isWriteAccess)
                {
                    runtime.Assert(this.WriterCount is 0, $"Found write/write data race on '{typeof(List<T>)}'.");
                    runtime.Assert(this.ReaderCount is 0, $"Found read/write data race on '{typeof(List<T>)}'.");
                    Interlocked.Increment(ref this.WriterCount);

                    if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
                    {
                        runtime.ScheduleNextOperation(AsyncOperationType.Default);
                    }
                    else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                    {
                        runtime.DelayOperation();
                    }

                    Interlocked.Decrement(ref this.WriterCount);
                }
                else
                {
                    runtime.Assert(this.WriterCount is 0, $"Found read/write data race on '{typeof(List<T>)}'.");
                    Interlocked.Increment(ref this.ReaderCount);

                    if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
                    {
                        runtime.ScheduleNextOperation(AsyncOperationType.Default);
                    }
                    else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                    {
                        runtime.DelayOperation();
                    }

                    Interlocked.Decrement(ref this.ReaderCount);
                }
            }
        }
    }
}
