// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Interception
{
    /// <summary>
    /// Provides methods for creating generic hashsets that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ControlledHashSet
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HashSet{T}"/> class
        /// that is empty and uses the default equality comparer for the set type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashSet<T> Create<T>() => new Mock<T>();

        /// <summary>
        /// Initializes a new instance of the <see cref="HashSet{T}"/> class that uses the default equality
        /// comparer for the set type, contains elements copied from the specified collection, and has
        /// sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashSet<T> Create<T>(IEnumerable<T> collection) => new Mock<T>(collection);

        /// <summary>
        /// Initializes a new instance of the <see cref="HashSet{T}"/> class
        /// that is empty and uses the default equality comparer for the set type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashSet<T> Create<T>(IEqualityComparer<T> comparer) => new Mock<T>(comparer);

        /// <summary>
        /// Initializes a new instance of the <see cref="HashSet{T}"/> class that uses the specidifed equality
        /// comparer for the set type, contains elements copied from the specified collection, and has
        /// sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashSet<T> Create<T>(IEnumerable<T> collection, IEqualityComparer<T> comparer) => new Mock<T>(collection, comparer);

        /// <summary>
        /// Initializes a new instance of the <see cref="HashSet{T}"/> class with serialized data.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashSet<T> Create<T>(SerializationInfo info, StreamingContext context) => new Mock<T>(info, context);

#if !NETSTANDARD2_0 && !NETFRAMEWORK
        /// <summary>
        /// Initializes a new instance of the <see cref="HashSet{T}"/> class that is empty, but has reserved
        /// space for 'capacity' items and and uses the default equality comparer for the set type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashSet<T> Create<T>(int capacity) => new Mock<T>(capacity);

        /// <summary>
        /// Initializes a new instance of the <see cref="HashSet{T}"/> class that uses the specified
        /// equality comparer for the set type, and has sufficient capacity to accommodate 'capacity' elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashSet<T> Create<T>(int capacity, IEqualityComparer<T> comparer) => new Mock<T>(capacity, comparer);
#endif

        /// <summary>
        /// Gets the <see cref="IEqualityComparer{T}"/> object that is used to determine equality for the values in the set.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static IEqualityComparer<T> get_Comparer<T>(HashSet<T> hashSet)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.Comparer;
        }

        /// <summary>
        /// Gets the number of elements that are contained in the <see cref="HashSet{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static int get_Count<T>(HashSet<T> hashSet)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.Count;
        }

        /// <summary>
        /// Adds the specified element to the <see cref="HashSet{T}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Add<T>(HashSet<T> hashSet, T item)
        {
            (hashSet as Mock<T>)?.CheckDataRace(true);
            return hashSet.Add(item);
        }

        /// <summary>
        /// Removes all elements from a <see cref="HashSet{T}"/> object.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear<T>(HashSet<T> hashSet)
        {
            (hashSet as Mock<T>)?.CheckDataRace(true);
            hashSet.Clear();
        }

        /// <summary>
        /// Determines whether a <see cref="HashSet{T}"/> object contains the specified element.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<T>(HashSet<T> hashSet, T item)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.Contains(item);
        }

        /// <summary>
        /// Copies the elements of a <see cref="HashSet{T}"/> object to an array.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(HashSet<T> hashSet, T[] array)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            hashSet.CopyTo(array);
        }

        /// <summary>
        /// Copies the elements of a <see cref="HashSet{T}"/> object to an array, starting at the specified array index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(HashSet<T> hashSet, T[] array, int arrayIndex)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            hashSet.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Copies the specified number of elements of a <see cref="HashSet{T}"/> object to an array, starting at the specified array index.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<T>(HashSet<T> hashSet, T[] array, int arrayIndex, int count)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            hashSet.CopyTo(array, arrayIndex, count);
        }

        /// <summary>
        /// Removes all elements in the specified collection from the current <see cref="HashSet{T}"/> object.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExceptWith<T>(HashSet<T> hashSet, IEnumerable<T> other)
        {
            (hashSet as Mock<T>)?.CheckDataRace(true);
            hashSet.ExceptWith(other);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a <see cref="HashSet{T}"/> object.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static HashSet<T>.Enumerator GetEnumerator<T>(HashSet<T> hashSet)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.GetEnumerator();
        }

        /////////////////////////////// TODO: Is this requried? NetStandard should be 2.0 or higher. Do we need to specify this explicitly? ///////////////////////////////

        /// <summary>
        /// Implements the <see cref="ISerializable"/> interface and returns the data needed to
        /// serialize a <see cref="HashSet{T}"/> object.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetObjectData<T>(HashSet<T> hashSet, SerializationInfo info, StreamingContext context)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            hashSet.GetObjectData(info, context);
        }

        /// <summary>
        /// Modifies the current <see cref="HashSet{T}"/> object to contain only elements that are present
        /// in that object and in the specified collection.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IntersecWith<T>(HashSet<T> hashSet, IEnumerable<T> other)
        {
            (hashSet as Mock<T>)?.CheckDataRace(true);
            hashSet.IntersectWith(other);
        }

        /// <summary>
        /// Determines whether a <see cref="HashSet{T}"/> object is a proper subset of the specified collection.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsProperSubsetOf<T>(HashSet<T> hashSet, IEnumerable<T> other)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.IsProperSubsetOf(other);
        }

        /// <summary>
        /// Determines whether a <see cref="HashSet{T}"/> object is a proper superset of the specified collection.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsProperSupersetOf<T>(HashSet<T> hashSet, IEnumerable<T> other)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.IsProperSupersetOf(other);
        }

        /// <summary>
        /// Determines whether a <see cref="HashSet{T}"/> object is a subset of the specified collection.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSubsetOf<T>(HashSet<T> hashSet, IEnumerable<T> other)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.IsSubsetOf(other);
        }

        /// <summary>
        /// Determines whether a <see cref="HashSet{T}"/> object is a superset of the specified collection.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsSupersetOf<T>(HashSet<T> hashSet, IEnumerable<T> other)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.IsSupersetOf(other);
        }

        // TODO: Is this requried?

        /// <summary>
        /// Implements the <see cref="ISerializable"/> interface and raises the deserialization event when the deserialization is complete.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OnDeserialization<T>(HashSet<T> hashSet, object sender)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            hashSet.OnDeserialization(sender);
        }

        /// <summary>
        /// Determines whether a <see cref="HashSet{T}"/> object and a specified collection share common elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Overlaps<T>(HashSet<T> hashSet, IEnumerable<T> other)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.Overlaps(other);
        }

        /// <summary>
        /// Removes the specified element from a <see cref="HashSet{T}"/> object.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Remove<T>(HashSet<T> hashSet, T item)
        {
            (hashSet as Mock<T>)?.CheckDataRace(true);
            return hashSet.Remove(item);
        }

        /// <summary>
        /// Removes the specified element from a <see cref="HashSet{T}"/> object.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int RemoveWhere<T>(HashSet<T> hashSet, Predicate<T> match)
        {
            (hashSet as Mock<T>)?.CheckDataRace(true);
            return hashSet.RemoveWhere(match);
        }

        /// <summary>
        /// Determines whether a <see cref="HashSet{T}"/> object and the specified collection contain the same elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool SetEquals<T>(HashSet<T> hashSet, IEnumerable<T> other)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.SetEquals(other);
        }

        /// <summary>
        /// Modifies the current <see cref="HashSet{T}"/> object to contain only elements that are present either in
        /// that object or in the specified collection, but not both.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SymmetricExceptWith<T>(HashSet<T> hashSet, IEnumerable<T> other)
        {
            (hashSet as Mock<T>)?.CheckDataRace(true);
            hashSet.SymmetricExceptWith(other);
        }

        /// <summary>
        /// Sets the capacity of a <see cref="HashSet{T}"/> object to the actual number of elements it
        /// contains, rounded up to a nearby, implementation-specific value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TrimExcess<T>(HashSet<T> hashSet)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            hashSet.TrimExcess();
        }

        /// <summary>
        /// Modifies the current <see cref="HashSet{T}"/> object to contain all elements that are
        /// present in itself, the specified collection, or both.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void UnionWith<T>(HashSet<T> hashSet, IEnumerable<T> other)
        {
            (hashSet as Mock<T>)?.CheckDataRace(true);
            hashSet.UnionWith(other);
        }

#if !NETSTANDARD2_0 && !NETFRAMEWORK
        /// <summary>
        /// Ensures that this <see cref="HashSet{T}"/> object can hold the specified number of elements without growing.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int EnsureCapacity<T>(HashSet<T> hashSet, int capacity)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.EnsureCapacity(capacity);
        }

        /// <summary>
        /// Searches the set for a given value and returns the equal value it finds, if any.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValue<T>(HashSet<T> hashSet, T equalValue, out T actualValue)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.TryGetValue(equalValue, out actualValue);
        }
#endif

        /// <summary>
        /// Implements a <see cref="HashSet{T}"/> that can be controlled during testing.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        private class Mock<T> : HashSet<T>
        {
            /// <summary>
            /// Count of read accesses to the dictionary.
            /// </summary>
            private volatile int ReaderCount;

            /// <summary>
            /// Count of write accesses to the dictionary.
            /// </summary>
            private volatile int WriterCount;

            /// <summary>
            /// Initializes a new instance of the <see cref="Mock{T}"/> class.
            /// </summary>
            internal Mock()
                : base() => this.Setup();

            internal Mock(IEnumerable<T> collection)
                : base(collection) => this.Setup();

            internal Mock(IEnumerable<T> collection, IEqualityComparer<T> comparer)
                : base(collection, comparer) => this.Setup();

            internal Mock(IEqualityComparer<T> comparer)
                : base(comparer) => this.Setup();

            internal Mock(SerializationInfo info, StreamingContext context)
                : base(info, context) => this.Setup();

#if !NETSTANDARD2_0 && !NETFRAMEWORK
            internal Mock(int capacity)
                : base(capacity) => this.Setup();

            internal Mock(int capacity, IEqualityComparer<T> comparer)
                : base(capacity, comparer) => this.Setup();
#endif

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
                void Interleave()
                {
                    if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
                    {
                        runtime.ScheduleNextOperation(AsyncOperationType.Default);
                    }
                    else if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing)
                    {
                        runtime.DelayOperation();
                    }
                }

                if (isWriteAccess)
                {
                    runtime.Assert(this.WriterCount is 0, $"Found write/write data race on '{typeof(HashSet<T>)}'.");
                    runtime.Assert(this.ReaderCount is 0, $"Found read/write data race on '{typeof(HashSet<T>)}'.");

                    Interlocked.Increment(ref this.WriterCount);
                    Interleave();
                    Interlocked.Decrement(ref this.WriterCount);
                }
                else
                {
                    runtime.Assert(this.WriterCount is 0, $"Found read/write data race on '{typeof(HashSet<T>)}'.");

                    Interlocked.Increment(ref this.ReaderCount);
                    Interleave();
                    Interlocked.Decrement(ref this.ReaderCount);
                }
            }
        }
    }
}
