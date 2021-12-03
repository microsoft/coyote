// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Microsoft.Coyote.Runtime;

using SystemGenerics = System.Collections.Generic;
using SystemInterlocked = System.Threading.Interlocked;

namespace Microsoft.Coyote.Rewriting.Types.Collections.Generic
{
    /// <summary>
    /// Provides methods for creating generic hashsets that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class HashSet
    {
        /// <summary>
        /// Initializes a hash set instance class that is empty and uses the
        /// default equality comparer for the set type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemGenerics.HashSet<T> Create<T>() => new Mock<T>();

        /// <summary>
        /// Initializes a hash set instance class that uses the default equality comparer
        /// for the set type, contains elements copied from the specified collection, and
        /// has sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemGenerics.HashSet<T> Create<T>(SystemGenerics.IEnumerable<T> collection) =>
            new Mock<T>(collection);

        /// <summary>
        /// Initializes a hash set instance class that is empty and uses the default
        /// equality comparer for the set type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemGenerics.HashSet<T> Create<T>(SystemGenerics.IEqualityComparer<T> comparer) =>
            new Mock<T>(comparer);

        /// <summary>
        /// Initializes a hash set instance class that uses the specified equality comparer for the
        /// set type, contains elements copied from the specified collection, and has sufficient
        /// capacity to accommodate the number of elements copied.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemGenerics.HashSet<T> Create<T>(SystemGenerics.IEnumerable<T> collection,
            SystemGenerics.IEqualityComparer<T> comparer) =>
            new Mock<T>(collection, comparer);

        /// <summary>
        /// Initializes a hash set instance class with serialized data.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemGenerics.HashSet<T> Create<T>(SerializationInfo info, StreamingContext context) =>
            new Mock<T>(info, context);

#if NET || NETCOREAPP3_1
        /// <summary>
        /// Initializes a hash set instance class that is empty, but has reserved
        /// space for 'capacity' items and and uses the default equality comparer for the set type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemGenerics.HashSet<T> Create<T>(int capacity) => new Mock<T>(capacity);

        /// <summary>
        /// Initializes a hash set instance class that uses the specified
        /// equality comparer for the set type, and has sufficient capacity to accommodate 'capacity' elements.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SystemGenerics.HashSet<T> Create<T>(int capacity,
            SystemGenerics.IEqualityComparer<T> comparer) =>
            new Mock<T>(capacity, comparer);
#endif

        /// <summary>
        /// Gets the equality comparer object that is used to determine equality for the values in the set.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static SystemGenerics.IEqualityComparer<T> get_Comparer<T>(SystemGenerics.HashSet<T> hashSet)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.Comparer;
        }

        /// <summary>
        /// Gets the number of elements that are contained in the hash set.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static int get_Count<T>(SystemGenerics.HashSet<T> hashSet)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.Count;
        }

        /// <summary>
        /// Adds the specified element to the hash set.
        /// </summary>
        public static bool Add<T>(SystemGenerics.HashSet<T> hashSet, T item)
        {
            (hashSet as Mock<T>)?.CheckDataRace(true);
            return hashSet.Add(item);
        }

        /// <summary>
        /// Removes all elements from a hash set object.
        /// </summary>
        public static void Clear<T>(SystemGenerics.HashSet<T> hashSet)
        {
            (hashSet as Mock<T>)?.CheckDataRace(true);
            hashSet.Clear();
        }

        /// <summary>
        /// Determines whether a hash set object contains the specified element.
        /// </summary>
        public static bool Contains<T>(SystemGenerics.HashSet<T> hashSet, T item)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.Contains(item);
        }

        /// <summary>
        /// Copies the elements of a hash set object to an array.
        /// </summary>
        public static void CopyTo<T>(SystemGenerics.HashSet<T> hashSet, T[] array)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            hashSet.CopyTo(array);
        }

        /// <summary>
        /// Copies the elements of a hash set object to an array, starting at the specified array index.
        /// </summary>
        public static void CopyTo<T>(SystemGenerics.HashSet<T> hashSet, T[] array, int arrayIndex)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            hashSet.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Copies the specified number of elements of a hash set object to an array, starting at the specified array index.
        /// </summary>
        public static void CopyTo<T>(SystemGenerics.HashSet<T> hashSet, T[] array, int arrayIndex, int count)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            hashSet.CopyTo(array, arrayIndex, count);
        }

        /// <summary>
        /// Removes all elements in the specified collection from the current hash set object.
        /// </summary>
        public static void ExceptWith<T>(SystemGenerics.HashSet<T> hashSet, SystemGenerics.IEnumerable<T> other)
        {
            (hashSet as Mock<T>)?.CheckDataRace(true);
            hashSet.ExceptWith(other);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a hash set object.
        /// </summary>
        public static SystemGenerics.HashSet<T>.Enumerator GetEnumerator<T>(SystemGenerics.HashSet<T> hashSet)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.GetEnumerator();
        }

        /// <summary>
        /// Implements the <see cref="ISerializable"/> interface and returns the data needed to
        /// serialize a hash set object.
        /// </summary>
        public static void GetObjectData<T>(SystemGenerics.HashSet<T> hashSet, SerializationInfo info,
            StreamingContext context)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            hashSet.GetObjectData(info, context);
        }

        /// <summary>
        /// Modifies the current hash set object to contain only elements that are present
        /// in that object and in the specified collection.
        /// </summary>
        public static void IntersecWith<T>(SystemGenerics.HashSet<T> hashSet, SystemGenerics.IEnumerable<T> other)
        {
            (hashSet as Mock<T>)?.CheckDataRace(true);
            hashSet.IntersectWith(other);
        }

        /// <summary>
        /// Determines whether a hash set object is a proper subset of the specified collection.
        /// </summary>
        public static bool IsProperSubsetOf<T>(SystemGenerics.HashSet<T> hashSet, SystemGenerics.IEnumerable<T> other)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.IsProperSubsetOf(other);
        }

        /// <summary>
        /// Determines whether a hash set object is a proper superset of the specified collection.
        /// </summary>
        public static bool IsProperSupersetOf<T>(SystemGenerics.HashSet<T> hashSet, SystemGenerics.IEnumerable<T> other)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.IsProperSupersetOf(other);
        }

        /// <summary>
        /// Determines whether a hash set object is a subset of the specified collection.
        /// </summary>
        public static bool IsSubsetOf<T>(SystemGenerics.HashSet<T> hashSet, SystemGenerics.IEnumerable<T> other)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.IsSubsetOf(other);
        }

        /// <summary>
        /// Determines whether a hash set object is a superset of the specified collection.
        /// </summary>
        public static bool IsSupersetOf<T>(SystemGenerics.HashSet<T> hashSet, SystemGenerics.IEnumerable<T> other)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.IsSupersetOf(other);
        }

        // TODO: Is this requried?

        /// <summary>
        /// Implements the <see cref="ISerializable"/> interface and raises the deserialization
        /// event when the deserialization is complete.
        /// </summary>
        public static void OnDeserialization<T>(SystemGenerics.HashSet<T> hashSet, object sender)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            hashSet.OnDeserialization(sender);
        }

        /// <summary>
        /// Determines whether a hash set object and a specified collection share common elements.
        /// </summary>
        public static bool Overlaps<T>(SystemGenerics.HashSet<T> hashSet, SystemGenerics.IEnumerable<T> other)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.Overlaps(other);
        }

        /// <summary>
        /// Removes the specified element from a hash set object.
        /// </summary>
        public static bool Remove<T>(SystemGenerics.HashSet<T> hashSet, T item)
        {
            (hashSet as Mock<T>)?.CheckDataRace(true);
            return hashSet.Remove(item);
        }

        /// <summary>
        /// Removes the specified element from a hash set object.
        /// </summary>
        public static int RemoveWhere<T>(SystemGenerics.HashSet<T> hashSet, Predicate<T> match)
        {
            (hashSet as Mock<T>)?.CheckDataRace(true);
            return hashSet.RemoveWhere(match);
        }

        /// <summary>
        /// Determines whether a hash set object and the specified collection contain the same elements.
        /// </summary>
        public static bool SetEquals<T>(SystemGenerics.HashSet<T> hashSet, SystemGenerics.IEnumerable<T> other)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.SetEquals(other);
        }

        /// <summary>
        /// Modifies the current hash set object to contain only elements that are present either in
        /// that object or in the specified collection, but not both.
        /// </summary>
        public static void SymmetricExceptWith<T>(SystemGenerics.HashSet<T> hashSet, SystemGenerics.IEnumerable<T> other)
        {
            (hashSet as Mock<T>)?.CheckDataRace(true);
            hashSet.SymmetricExceptWith(other);
        }

        /// <summary>
        /// Sets the capacity of a hash set object to the actual number of elements it
        /// contains, rounded up to a nearby, implementation-specific value.
        /// </summary>
        public static void TrimExcess<T>(SystemGenerics.HashSet<T> hashSet)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            hashSet.TrimExcess();
        }

        /// <summary>
        /// Modifies the current hash set object to contain all elements that are
        /// present in itself, the specified collection, or both.
        /// </summary>
        public static void UnionWith<T>(SystemGenerics.HashSet<T> hashSet, SystemGenerics.IEnumerable<T> other)
        {
            (hashSet as Mock<T>)?.CheckDataRace(true);
            hashSet.UnionWith(other);
        }

#if NET || NETCOREAPP3_1
        /// <summary>
        /// Ensures that this hash set object can hold the specified number of elements without growing.
        /// </summary>
        public static int EnsureCapacity<T>(SystemGenerics.HashSet<T> hashSet, int capacity)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.EnsureCapacity(capacity);
        }

        /// <summary>
        /// Searches the set for a given value and returns the equal value it finds, if any.
        /// </summary>
        public static bool TryGetValue<T>(SystemGenerics.HashSet<T> hashSet, T equalValue, out T actualValue)
        {
            (hashSet as Mock<T>)?.CheckDataRace(false);
            return hashSet.TryGetValue(equalValue, out actualValue);
        }
#endif

        /// <summary>
        /// Implements a hash set that can be controlled during testing.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        private class Mock<T> : SystemGenerics.HashSet<T>
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

            internal Mock(SystemGenerics.IEnumerable<T> collection)
                : base(collection) => this.Setup();

            internal Mock(SystemGenerics.IEnumerable<T> collection, SystemGenerics.IEqualityComparer<T> comparer)
                : base(collection, comparer) => this.Setup();

            internal Mock(SystemGenerics.IEqualityComparer<T> comparer)
                : base(comparer) => this.Setup();

            internal Mock(SerializationInfo info, StreamingContext context)
                : base(info, context) => this.Setup();

#if NET || NETCOREAPP3_1
            internal Mock(int capacity)
                : base(capacity) => this.Setup();

            internal Mock(int capacity, SystemGenerics.IEqualityComparer<T> comparer)
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
                    runtime.Assert(this.WriterCount is 0,
                        $"Found write/write data race on '{typeof(SystemGenerics.HashSet<T>)}'.");
                    runtime.Assert(this.ReaderCount is 0,
                        $"Found read/write data race on '{typeof(SystemGenerics.HashSet<T>)}'.");

                    SystemInterlocked.Increment(ref this.WriterCount);
                    Interleave();
                    SystemInterlocked.Decrement(ref this.WriterCount);
                }
                else
                {
                    runtime.Assert(this.WriterCount is 0,
                        $"Found read/write data race on '{typeof(SystemGenerics.HashSet<T>)}'.");

                    SystemInterlocked.Increment(ref this.ReaderCount);
                    Interleave();
                    SystemInterlocked.Decrement(ref this.ReaderCount);
                }
            }
        }
    }
}
