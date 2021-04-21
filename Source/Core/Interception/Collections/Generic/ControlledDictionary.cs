// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.Interception
{
    /// <summary>
    /// Provides methods for creating generic dictionaries that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class ControlledDictionary
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Dictionary{TKey, TValue}"/> class
        /// that is empty, has the default initial capacity, and uses the default equality
        /// comparer for the key type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TValue> Create<TKey, TValue>() => new Mock<TKey, TValue>();

        /// <summary>
        /// Initializes a new instance of the <see cref="Dictionary{TKey, TValue}"/> class
        /// that contains elements copied from the specified <see cref="Dictionary{TKey, TValue}"/>
        /// and uses the default equality comparer for the key type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TValue> Create<TKey, TValue>(IDictionary<TKey, TValue> dictionary) =>
            new Mock<TKey, TValue>(dictionary);

        /// <summary>
        /// Initializes a new instance of the <see cref="Dictionary{TKey, TValue}"/> class
        /// that is empty, has the default initial capacity, and uses the specified
        /// <see cref="IEqualityComparer{TKey}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TValue> Create<TKey, TValue>(IEqualityComparer<TKey> comparer) =>
            new Mock<TKey, TValue>(comparer);

        /// <summary>
        /// Initializes a new instance of the <see cref="Dictionary{TKey, TValue}"/> class
        /// that is empty, has the specified initial capacity, and uses the default equality
        /// comparer for the key type.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TValue> Create<TKey, TValue>(int capacity)
        {
            return new Mock<TKey, TValue>(capacity);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Dictionary{TKey, TValue}"/> class
        /// that contains elements copied from the specified <see cref="Dictionary{TKey, TValue}"/>
        /// and uses the specified <see cref="IEqualityComparer{TKey}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TValue> Create<TKey, TValue>(IDictionary<TKey, TValue> dictionary,
            IEqualityComparer<TKey> comparer) =>
            new Mock<TKey, TValue>(dictionary, comparer);

        /// <summary>
        /// Initializes a new instance of the <see cref="Dictionary{TKey, TValue}"/> class
        /// that is empty, has the specified initial capacity, and uses the specified
        /// <see cref="IEqualityComparer{TKey}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TValue> Create<TKey, TValue>(int capacity, IEqualityComparer<TKey> comparer) =>
            new Mock<TKey, TValue>(capacity, comparer);

#if !NETSTANDARD2_0
        /// <summary>
        /// Initializes a new instance of the <see cref="Dictionary{TKey, TValue}"/> class
        /// that contains elements copied from the specified <see cref="IEnumerable{T}"/>.
        /// </summary>
        public static Dictionary<TKey, TValue> Create<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> collection) =>
            new Mock<TKey, TValue>(collection);

        /// <summary>
        /// Initializes a new instance of the <see cref="Dictionary{TKey, TValue}"/> class
        /// that contains elements copied from the specified <see cref="IEnumerable{T}"/>
        /// and uses the specified <see cref="IEqualityComparer{TKey}"/>.
        /// </summary>
        public static Dictionary<TKey, TValue> Create<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> collection,
            IEqualityComparer<TKey> comparer) =>
            new Mock<TKey, TValue>(collection, comparer);
#endif

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static TValue get_Item<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (dictionary as Mock<TKey, TValue>)?.CheckDataRace(false);
            return dictionary[key];
        }

        /// <summary>
        /// Sets the value associated with the specified key.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static void set_Item<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (dictionary as Mock<TKey, TValue>)?.CheckDataRace(true);
            dictionary[key] = value;
        }

        /// <summary>
        /// Gets a collection containing the keys in the <see cref="Dictionary{TKey, TValue}"/>.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TValue>.KeyCollection get_Keys<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (dictionary as Mock<TKey, TValue>)?.CheckDataRace(false);
            return dictionary.Keys;
        }

        /// <summary>
        /// Gets a collection containing the values in the <see cref="Dictionary{TKey, TValue}"/>.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TValue>.ValueCollection get_Values<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (dictionary as Mock<TKey, TValue>)?.CheckDataRace(false);
            return dictionary.Values;
        }

        /// <summary>
        /// Gets the number of key/value pairs contained in the <see cref="Dictionary{TKey, TValue}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static int get_Count<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (dictionary as Mock<TKey, TValue>)?.CheckDataRace(false);
            return dictionary.Count;
        }

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            (dictionary as Mock<TKey, TValue>)?.CheckDataRace(true);
            dictionary.Add(key, value);
        }

        /// <summary>
        /// Removes all keys and values from the <see cref="Dictionary{TKey, TValue}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        {
            (dictionary as Mock<TKey, TValue>)?.CheckDataRace(true);
            dictionary.Clear();
        }

        /// <summary>
        /// Determines whether the <see cref="Dictionary{TKey, TValue}"/> contains the specified key.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsKey<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key)
        {
            (dictionary as Mock<TKey, TValue>)?.CheckDataRace(false);
            return dictionary.ContainsKey(key);
        }

        /// <summary>
        /// Determines whether the <see cref="Dictionary{TKey, TValue}"/> contains a specific value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsValue<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TValue value)
        {
            (dictionary as Mock<TKey, TValue>)?.CheckDataRace(false);
            return dictionary.ContainsValue(value);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="Dictionary{TKey, TValue}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TValue>.Enumerator GetEnumerator<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        {
            (dictionary as Mock<TKey, TValue>)?.CheckDataRace(false);
            return dictionary.GetEnumerator();
        }

        /// <summary>
        /// Removes the value with the specified key from the <see cref="Dictionary{TKey, TValue}"/>,
        /// and copies the element to the value parameter.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Remove<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key)
        {
            (dictionary as Mock<TKey, TValue>)?.CheckDataRace(true);
            return dictionary.Remove(key);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValue<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key, out TValue value)
        {
            (dictionary as Mock<TKey, TValue>)?.CheckDataRace(false);
            return dictionary.TryGetValue(key, out value);
        }

        /// <summary>
        /// Implements the <see cref="ISerializable"/> interface and returns the data needed
        /// to serialize the <see cref="Dictionary{TKey, TValue}"/> instance.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void GetObjectData<TKey, TValue>(Dictionary<TKey, TValue> dictionary, SerializationInfo info,
            StreamingContext context)
        {
            (dictionary as Mock<TKey, TValue>)?.CheckDataRace(true);
            dictionary.GetObjectData(info, context);
        }

        /// <summary>
        /// Implements the <see cref="ISerializable"/> interface and raises
        /// the deserialization event when the deserialization is complete.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OnDeserialization<TKey, TValue>(Dictionary<TKey, TValue> dictionary, object sender)
        {
            (dictionary as Mock<TKey, TValue>)?.CheckDataRace(true);
            dictionary.OnDeserialization(sender);
        }

#if !NETSTANDARD2_0
        /// <summary>
        /// Ensures that the dictionary can hold up to a specified number of entries without
        /// any further expansion of its backing storage.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureCapacity<TKey, TValue>(Dictionary<TKey, TValue> dictionary, int size)
        {
            (dictionary as Mock<TKey, TValue>)?.CheckDataRace(true);
            dictionary.EnsureCapacity(size);
        }

        /// <summary>
        /// Removes the value with the specified key from the <see cref="Dictionary{TKey, TValue}"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Remove<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key, out TValue value)
        {
            (dictionary as Mock<TKey, TValue>)?.CheckDataRace(true);
            return dictionary.Remove(key, out value);
        }

        /// <summary>
        /// Sets the capacity of this dictionary to what it would be if it had been originally
        /// initialized with all its entries.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TrimExcess<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
        {
            (dictionary as Mock<TKey, TValue>)?.CheckDataRace(true);
            dictionary.TrimExcess();
        }

        /// <summary>
        /// Sets the capacity of this dictionary to hold up a specified number of entries
        /// without any further expansion of its backing storage.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TrimExcess<TKey, TValue>(Dictionary<TKey, TValue> dictionary, int size)
        {
            (dictionary as Mock<TKey, TValue>)?.CheckDataRace(true);
            dictionary.TrimExcess(size);
        }

        /// <summary>
        /// Attempts to add the specified key and value to the dictionary.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdd<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            (dictionary as Mock<TKey, TValue>)?.CheckDataRace(true);
            return dictionary.TryAdd(key, value);
        }
#endif

        /// <summary>
        /// Implements a <see cref="Dictionary{TKey, TValue}"/> that can be controlled during testing.
        /// </summary>
        /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        private class Mock<TKey, TValue> : Dictionary<TKey, TValue>
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
            /// Initializes a new instance of the <see cref="Mock{TKey, TValue}"/> class.
            /// </summary>
            internal Mock()
                : base() => this.Setup();

            internal Mock(IDictionary<TKey, TValue> dictionary)
                : base(dictionary) => this.Setup();

            internal Mock(int capacity)
                : base(capacity) => this.Setup();

            internal Mock(int capacity, IEqualityComparer<TKey> comparer)
                : base(capacity, comparer) => this.Setup();

            internal Mock(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
                : base(dictionary, comparer) => this.Setup();

            internal Mock(IEqualityComparer<TKey> comparer)
                : base(comparer) => this.Setup();

#if !NETSTANDARD2_0
            internal Mock(IEnumerable<KeyValuePair<TKey, TValue>> collection)
                : base(collection) => this.Setup();

            internal Mock(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
                : base(collection, comparer) => this.Setup();
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
                if (isWriteAccess)
                {
                    runtime.Assert(this.WriterCount is 0, $"Found write/write data race on '{typeof(Dictionary<TKey, TValue>)}'.");
                    runtime.Assert(this.ReaderCount is 0, $"Found read/write data race on '{typeof(Dictionary<TKey, TValue>)}'.");
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
                    runtime.Assert(this.WriterCount is 0, $"Found read/write data race on '{typeof(Dictionary<TKey, TValue>)}'.");
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
