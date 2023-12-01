// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.Serialization;
using Microsoft.Coyote.Runtime;
using SystemGenerics = System.Collections.Generic;
using SystemInterlocked = System.Threading.Interlocked;

namespace Microsoft.Coyote.Rewriting.Types.Collections.Generic
{
#pragma warning disable CA1000 // Do not declare static members on generic types
    /// <summary>
    /// Provides methods for creating generic hashsets that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class HashSet<T>
    {
        /// <summary>
        /// Initializes a hash set instance class that is empty and uses the
        /// default equality comparer for the set type.
        /// </summary>
        public static SystemGenerics.HashSet<T> Create() =>
            CoyoteRuntime.IsExecutionControlled ?
            new Wrapper() :
            new SystemGenerics.HashSet<T>();

        /// <summary>
        /// Initializes a hash set instance class that uses the default equality comparer
        /// for the set type, contains elements copied from the specified collection, and
        /// has sufficient capacity to accommodate the number of elements copied.
        /// </summary>
        public static SystemGenerics.HashSet<T> Create(SystemGenerics.IEnumerable<T> collection) =>
            CoyoteRuntime.IsExecutionControlled ?
            new Wrapper(collection) :
            new SystemGenerics.HashSet<T>(collection);

        /// <summary>
        /// Initializes a hash set instance class that is empty and uses the default
        /// equality comparer for the set type.
        /// </summary>
        public static SystemGenerics.HashSet<T> Create(SystemGenerics.IEqualityComparer<T> comparer) =>
            CoyoteRuntime.IsExecutionControlled ?
            new Wrapper(comparer) :
            new SystemGenerics.HashSet<T>(comparer);

        /// <summary>
        /// Initializes a hash set instance class that uses the specified equality comparer for the
        /// set type, contains elements copied from the specified collection, and has sufficient
        /// capacity to accommodate the number of elements copied.
        /// </summary>
        public static SystemGenerics.HashSet<T> Create(SystemGenerics.IEnumerable<T> collection,
            SystemGenerics.IEqualityComparer<T> comparer) =>
            CoyoteRuntime.IsExecutionControlled ?
            new Wrapper(collection, comparer) :
            new SystemGenerics.HashSet<T>(collection, comparer);

#if NET || NETCOREAPP3_1
        /// <summary>
        /// Initializes a hash set instance class that is empty, but has reserved
        /// space for 'capacity' items and and uses the default equality comparer for the set type.
        /// </summary>
        public static SystemGenerics.HashSet<T> Create(int capacity) =>
            CoyoteRuntime.IsExecutionControlled ?
            new Wrapper(capacity) :
            new SystemGenerics.HashSet<T>(capacity);

        /// <summary>
        /// Initializes a hash set instance class that uses the specified
        /// equality comparer for the set type, and has sufficient capacity to accommodate 'capacity' elements.
        /// </summary>
        public static SystemGenerics.HashSet<T> Create(int capacity,
            SystemGenerics.IEqualityComparer<T> comparer) =>
            CoyoteRuntime.IsExecutionControlled ?
            new Wrapper(capacity, comparer) :
            new SystemGenerics.HashSet<T>(capacity, comparer);
#endif

        /// <summary>
        /// Gets the equality comparer object that is used to determine equality for the values in the set.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static SystemGenerics.IEqualityComparer<T> get_Comparer(SystemGenerics.HashSet<T> instance)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.Comparer;
        }

        /// <summary>
        /// Gets the number of elements that are contained in the hash set.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static int get_Count(SystemGenerics.HashSet<T> instance)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.Count;
        }

        /// <summary>
        /// Adds the specified element to the hash set.
        /// </summary>
        public static bool Add(SystemGenerics.HashSet<T> instance, T item)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            return instance.Add(item);
        }

        /// <summary>
        /// Removes all elements from a hash set object.
        /// </summary>
        public static void Clear(SystemGenerics.HashSet<T> instance)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.Clear();
        }

        /// <summary>
        /// Determines whether a hash set object contains the specified element.
        /// </summary>
        public static bool Contains(SystemGenerics.HashSet<T> instance, T item)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.Contains(item);
        }

        /// <summary>
        /// Copies the elements of a hash set object to an array.
        /// </summary>
        public static void CopyTo(SystemGenerics.HashSet<T> instance, T[] array)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            instance.CopyTo(array);
        }

        /// <summary>
        /// Copies the elements of a hash set object to an array, starting at the specified array index.
        /// </summary>
        public static void CopyTo(SystemGenerics.HashSet<T> instance, T[] array, int arrayIndex)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            instance.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Copies the specified number of elements of a hash set object to an array, starting at the specified array index.
        /// </summary>
        public static void CopyTo(SystemGenerics.HashSet<T> instance, T[] array, int arrayIndex, int count)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            instance.CopyTo(array, arrayIndex, count);
        }

        /// <summary>
        /// Removes all elements in the specified collection from the current hash set object.
        /// </summary>
        public static void ExceptWith(SystemGenerics.HashSet<T> instance, SystemGenerics.IEnumerable<T> other)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.ExceptWith(other);
        }

        /// <summary>
        /// Returns an enumerator that iterates through a hash set object.
        /// </summary>
        public static SystemGenerics.HashSet<T>.Enumerator GetEnumerator(SystemGenerics.HashSet<T> instance)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.GetEnumerator();
        }

        /// <summary>
        /// Implements the <see cref="ISerializable"/> interface and returns the data needed to
        /// serialize a hash set object.
        /// </summary>
#if NET8_0_OR_GREATER
        [Obsolete("This method is obsolete due to BinaryFormatter obsoleted", DiagnosticId = "SYSLIB0051")]
#endif
        public static void GetObjectData(SystemGenerics.HashSet<T> instance, SerializationInfo info,
            StreamingContext context)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            instance.GetObjectData(info, context);
        }

        /// <summary>
        /// Modifies the current hash set object to contain only elements that are present
        /// in that object and in the specified collection.
        /// </summary>
        public static void IntersecWith(SystemGenerics.HashSet<T> instance, SystemGenerics.IEnumerable<T> other)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.IntersectWith(other);
        }

        /// <summary>
        /// Determines whether a hash set object is a proper subset of the specified collection.
        /// </summary>
        public static bool IsProperSubsetOf(SystemGenerics.HashSet<T> instance, SystemGenerics.IEnumerable<T> other)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.IsProperSubsetOf(other);
        }

        /// <summary>
        /// Determines whether a hash set object is a proper superset of the specified collection.
        /// </summary>
        public static bool IsProperSupersetOf(SystemGenerics.HashSet<T> instance, SystemGenerics.IEnumerable<T> other)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.IsProperSupersetOf(other);
        }

        /// <summary>
        /// Determines whether a hash set object is a subset of the specified collection.
        /// </summary>
        public static bool IsSubsetOf(SystemGenerics.HashSet<T> instance, SystemGenerics.IEnumerable<T> other)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.IsSubsetOf(other);
        }

        /// <summary>
        /// Determines whether a hash set object is a superset of the specified collection.
        /// </summary>
        public static bool IsSupersetOf(SystemGenerics.HashSet<T> instance, SystemGenerics.IEnumerable<T> other)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.IsSupersetOf(other);
        }

        // TODO: Is this requried?

        /// <summary>
        /// Implements the <see cref="ISerializable"/> interface and raises the deserialization
        /// event when the deserialization is complete.
        /// </summary>
        public static void OnDeserialization(SystemGenerics.HashSet<T> instance, object sender)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            instance.OnDeserialization(sender);
        }

        /// <summary>
        /// Determines whether a hash set object and a specified collection share common elements.
        /// </summary>
        public static bool Overlaps(SystemGenerics.HashSet<T> instance, SystemGenerics.IEnumerable<T> other)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.Overlaps(other);
        }

        /// <summary>
        /// Removes the specified element from a hash set object.
        /// </summary>
        public static bool Remove(SystemGenerics.HashSet<T> instance, T item)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            return instance.Remove(item);
        }

        /// <summary>
        /// Removes the specified element from a hash set object.
        /// </summary>
        public static int RemoveWhere(SystemGenerics.HashSet<T> instance, Predicate<T> match)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            return instance.RemoveWhere(match);
        }

        /// <summary>
        /// Determines whether a hash set object and the specified collection contain the same elements.
        /// </summary>
        public static bool SetEquals(SystemGenerics.HashSet<T> instance, SystemGenerics.IEnumerable<T> other)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.SetEquals(other);
        }

        /// <summary>
        /// Modifies the current hash set object to contain only elements that are present either in
        /// that object or in the specified collection, but not both.
        /// </summary>
        public static void SymmetricExceptWith(SystemGenerics.HashSet<T> instance, SystemGenerics.IEnumerable<T> other)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.SymmetricExceptWith(other);
        }

        /// <summary>
        /// Sets the capacity of a hash set object to the actual number of elements it
        /// contains, rounded up to a nearby, implementation-specific value.
        /// </summary>
        public static void TrimExcess(SystemGenerics.HashSet<T> instance)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            instance.TrimExcess();
        }

        /// <summary>
        /// Modifies the current hash set object to contain all elements that are
        /// present in itself, the specified collection, or both.
        /// </summary>
        public static void UnionWith(SystemGenerics.HashSet<T> instance, SystemGenerics.IEnumerable<T> other)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.UnionWith(other);
        }

#if NET || NETCOREAPP3_1
        /// <summary>
        /// Ensures that this hash set object can hold the specified number of elements without growing.
        /// </summary>
        public static int EnsureCapacity(SystemGenerics.HashSet<T> instance, int capacity)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.EnsureCapacity(capacity);
        }

        /// <summary>
        /// Searches the set for a given value and returns the equal value it finds, if any.
        /// </summary>
        public static bool TryGetValue(SystemGenerics.HashSet<T> instance, T equalValue, out T actualValue)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.TryGetValue(equalValue, out actualValue);
        }
#endif

        /// <summary>
        /// Wraps a hash set so that it can be controlled during testing.
        /// </summary>
        private class Wrapper : SystemGenerics.HashSet<T>
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
            internal Wrapper(SystemGenerics.IEnumerable<T> collection, SystemGenerics.IEqualityComparer<T> comparer)
                : base(collection, comparer) => this.Setup();

            /// <summary>
            /// Initializes a new instance of the <see cref="Wrapper"/> class.
            /// </summary>
            internal Wrapper(SystemGenerics.IEqualityComparer<T> comparer)
                : base(comparer) => this.Setup();

            /// <summary>
            /// Initializes a new instance of the <see cref="Wrapper"/> class.
            /// </summary>
#if NET8_0_OR_GREATER
            [Obsolete("This method is obsolete due to BinaryFormatter obsoleted", DiagnosticId = "SYSLIB0051")]
#endif
            internal Wrapper(SerializationInfo info, StreamingContext context)
                : base(info, context) => this.Setup();

#if NET || NETCOREAPP3_1
            /// <summary>
            /// Initializes a new instance of the <see cref="Wrapper"/> class.
            /// </summary>
            internal Wrapper(int capacity)
                : base(capacity) => this.Setup();

            /// <summary>
            /// Initializes a new instance of the <see cref="Wrapper"/> class.
            /// </summary>
            internal Wrapper(int capacity, SystemGenerics.IEqualityComparer<T> comparer)
                : base(capacity, comparer) => this.Setup();
#endif

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
                        $"Found write/write data race on '{typeof(SystemGenerics.HashSet<T>)}'.");
                    runtime.Assert(this.ReaderCount is 0,
                        $"Found read/write data race on '{typeof(SystemGenerics.HashSet<T>)}'.");
                    SystemInterlocked.Increment(ref this.WriterCount);
                }
                else
                {
                    runtime.Assert(this.WriterCount is 0,
                        $"Found read/write data race on '{typeof(SystemGenerics.HashSet<T>)}'.");
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
