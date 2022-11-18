// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Runtime.Serialization;
using Microsoft.Coyote.Runtime;
using SystemGenerics = System.Collections.Generic;
using SystemInterlocked = System.Threading.Interlocked;

namespace Microsoft.Coyote.Rewriting.Types.Collections.Generic
{
#pragma warning disable CA1000 // Do not declare static members on generic types
    /// <summary>
    /// Provides methods for creating generic dictionaries that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class Dictionary<TKey, TValue>
    {
        /// <summary>
        /// Initializes a new dictionary instance class that is empty, has the default initial
        /// capacity, and uses the default equality comparer for the key type.
        /// </summary>
        public static SystemGenerics.Dictionary<TKey, TValue> Create() =>
            CoyoteRuntime.IsExecutionControlled ?
            new Wrapper() :
            new SystemGenerics.Dictionary<TKey, TValue>();

        /// <summary>
        /// Initializes a new dictionary instance class that contains elements copied from the
        /// specified dictionary and uses the default equality comparer for the key type.
        /// </summary>
        public static SystemGenerics.Dictionary<TKey, TValue> Create(
            SystemGenerics.IDictionary<TKey, TValue> dictionary) =>
            CoyoteRuntime.IsExecutionControlled ?
            new Wrapper(dictionary) :
            new SystemGenerics.Dictionary<TKey, TValue>(dictionary);

        /// <summary>
        /// Initializes a new dictionary instance class that is empty, has the default
        /// initial capacity, and uses the specified equality comparer.
        /// </summary>
        public static SystemGenerics.Dictionary<TKey, TValue> Create(
            SystemGenerics.IEqualityComparer<TKey> comparer) =>
            CoyoteRuntime.IsExecutionControlled ?
            new Wrapper(comparer) :
            new SystemGenerics.Dictionary<TKey, TValue>(comparer);

        /// <summary>
        /// Initializes a new dictionary instance class that is empty, has the specified initial
        /// capacity, and uses the default equality comparer for the key type.
        /// </summary>
        public static SystemGenerics.Dictionary<TKey, TValue> Create(int capacity) =>
            CoyoteRuntime.IsExecutionControlled ?
            new Wrapper(capacity) :
            new SystemGenerics.Dictionary<TKey, TValue>(capacity);

        /// <summary>
        /// Initializes a new dictionary instance class that contains elements copied from the specified dictionary
        /// and uses the specified equality comparer.
        /// </summary>
        public static SystemGenerics.Dictionary<TKey, TValue> Create(
            SystemGenerics.IDictionary<TKey, TValue> dictionary,
            SystemGenerics.IEqualityComparer<TKey> comparer) =>
            CoyoteRuntime.IsExecutionControlled ?
            new Wrapper(dictionary, comparer) :
            new SystemGenerics.Dictionary<TKey, TValue>(dictionary, comparer);

        /// <summary>
        /// Initializes a new dictionary instance class that is empty, has the specified initial
        /// capacity, and uses the specified equality comparer.
        /// </summary>
        public static SystemGenerics.Dictionary<TKey, TValue> Create(
            int capacity, SystemGenerics.IEqualityComparer<TKey> comparer) =>
            CoyoteRuntime.IsExecutionControlled ?
            new Wrapper(capacity, comparer) :
            new SystemGenerics.Dictionary<TKey, TValue>(capacity, comparer);

#if NET || NETCOREAPP3_1
        /// <summary>
        /// Initializes a new dictionary instance class that contains elements copied
        /// from the specified enumerable.
        /// </summary>
        public static SystemGenerics.Dictionary<TKey, TValue> Create(
            SystemGenerics.IEnumerable<SystemGenerics.KeyValuePair<TKey, TValue>> collection) =>
            CoyoteRuntime.IsExecutionControlled ?
            new Wrapper(collection) :
            new SystemGenerics.Dictionary<TKey, TValue>(collection);

        /// <summary>
        /// Initializes a new dictionary instance class that contains elements copied
        /// from the specified enumerable and uses the specified equality comparer.
        /// </summary>
        public static SystemGenerics.Dictionary<TKey, TValue> Create(
            SystemGenerics.IEnumerable<SystemGenerics.KeyValuePair<TKey, TValue>> collection,
            SystemGenerics.IEqualityComparer<TKey> comparer) =>
            CoyoteRuntime.IsExecutionControlled ?
            new Wrapper(collection, comparer) :
            new SystemGenerics.Dictionary<TKey, TValue>(collection, comparer);
#endif

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static TValue get_Item(SystemGenerics.Dictionary<TKey, TValue> instance, TKey key)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance[key];
        }

        /// <summary>
        /// Sets the value associated with the specified key.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static void set_Item(SystemGenerics.Dictionary<TKey, TValue> instance,
            TKey key, TValue value)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance[key] = value;
        }

        /// <summary>
        /// Gets a collection containing the keys in the dictionary.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static SystemGenerics.Dictionary<TKey, TValue>.KeyCollection get_Keys(
            SystemGenerics.Dictionary<TKey, TValue> instance)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.Keys;
        }

        /// <summary>
        /// Gets a collection containing the values in the dictionary.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static SystemGenerics.Dictionary<TKey, TValue>.ValueCollection get_Values(
            SystemGenerics.Dictionary<TKey, TValue> instance)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.Values;
        }

        /// <summary>
        /// Gets the number of key/value pairs contained in the dictionary.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static int get_Count(SystemGenerics.Dictionary<TKey, TValue> instance)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.Count;
        }

        /// <summary>
        /// Adds the specified key and value to the dictionary.
        /// </summary>
        public static void Add(SystemGenerics.Dictionary<TKey, TValue> instance, TKey key, TValue value)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.Add(key, value);
        }

        /// <summary>
        /// Removes all keys and values from the dictionary.
        /// </summary>
        public static void Clear(SystemGenerics.Dictionary<TKey, TValue> instance)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.Clear();
        }

        /// <summary>
        /// Determines whether the dictionary contains the specified key.
        /// </summary>
        public static bool ContainsKey(SystemGenerics.Dictionary<TKey, TValue> instance, TKey key)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.ContainsKey(key);
        }

        /// <summary>
        /// Determines whether the dictionary contains a specific value.
        /// </summary>
        public static bool ContainsValue(SystemGenerics.Dictionary<TKey, TValue> instance, TValue value)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.ContainsValue(value);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the dictionary.
        /// </summary>
        public static SystemGenerics.Dictionary<TKey, TValue>.Enumerator GetEnumerator(
            SystemGenerics.Dictionary<TKey, TValue> instance)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.GetEnumerator();
        }

        /// <summary>
        /// Removes the value with the specified key from the dictionary,
        /// and copies the element to the value parameter.
        /// </summary>
        public static bool Remove(SystemGenerics.Dictionary<TKey, TValue> instance, TKey key)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            return instance.Remove(key);
        }

        /// <summary>
        /// Gets the value associated with the specified key.
        /// </summary>
        public static bool TryGetValue(SystemGenerics.Dictionary<TKey, TValue> instance,
            TKey key, out TValue value)
        {
            (instance as Wrapper)?.CheckDataRace(false);
            return instance.TryGetValue(key, out value);
        }

        /// <summary>
        /// Implements the <see cref="ISerializable"/> interface and returns the data needed
        /// to serialize the dictionary instance.
        /// </summary>
        public static void GetObjectData(SystemGenerics.Dictionary<TKey, TValue> instance,
            SerializationInfo info, StreamingContext context)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.GetObjectData(info, context);
        }

        /// <summary>
        /// Implements the <see cref="ISerializable"/> interface and raises
        /// the deserialization event when the deserialization is complete.
        /// </summary>
        public static void OnDeserialization(SystemGenerics.Dictionary<TKey, TValue> instance, object sender)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.OnDeserialization(sender);
        }

#if NET || NETCOREAPP3_1
        /// <summary>
        /// Ensures that the dictionary can hold up to a specified number of entries without
        /// any further expansion of its backing storage.
        /// </summary>
        public static void EnsureCapacity(SystemGenerics.Dictionary<TKey, TValue> instance, int size)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.EnsureCapacity(size);
        }

        /// <summary>
        /// Removes the value with the specified key from the dictionary.
        /// </summary>
        public static bool Remove(SystemGenerics.Dictionary<TKey, TValue> instance, TKey key, out TValue value)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            return instance.Remove(key, out value);
        }

        /// <summary>
        /// Sets the capacity of this dictionary to what it would be if it had been originally
        /// initialized with all its entries.
        /// </summary>
        public static void TrimExcess(SystemGenerics.Dictionary<TKey, TValue> instance)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.TrimExcess();
        }

        /// <summary>
        /// Sets the capacity of this dictionary to hold up a specified number of entries
        /// without any further expansion of its backing storage.
        /// </summary>
        public static void TrimExcess(SystemGenerics.Dictionary<TKey, TValue> instance, int size)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            instance.TrimExcess(size);
        }

        /// <summary>
        /// Attempts to add the specified key and value to the dictionary.
        /// </summary>
        public static bool TryAdd(SystemGenerics.Dictionary<TKey, TValue> instance, TKey key, TValue value)
        {
            (instance as Wrapper)?.CheckDataRace(true);
            return instance.TryAdd(key, value);
        }
#endif

        /// <summary>
        /// Wraps a dictionary so that it can be controlled during testing.
        /// </summary>
        private class Wrapper : SystemGenerics.Dictionary<TKey, TValue>
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
            internal Wrapper(SystemGenerics.IDictionary<TKey, TValue> dictionary)
                : base(dictionary) => this.Setup();

            /// <summary>
            /// Initializes a new instance of the <see cref="Wrapper"/> class.
            /// </summary>
            internal Wrapper(int capacity)
                : base(capacity) => this.Setup();

            /// <summary>
            /// Initializes a new instance of the <see cref="Wrapper"/> class.
            /// </summary>
            internal Wrapper(int capacity, SystemGenerics.IEqualityComparer<TKey> comparer)
                : base(capacity, comparer) => this.Setup();

            /// <summary>
            /// Initializes a new instance of the <see cref="Wrapper"/> class.
            /// </summary>
            internal Wrapper(SystemGenerics.IDictionary<TKey, TValue> dictionary,
                SystemGenerics.IEqualityComparer<TKey> comparer)
                : base(dictionary, comparer) => this.Setup();

            /// <summary>
            /// Initializes a new instance of the <see cref="Wrapper"/> class.
            /// </summary>
            internal Wrapper(SystemGenerics.IEqualityComparer<TKey> comparer)
                : base(comparer) => this.Setup();

#if NET || NETCOREAPP3_1
            /// <summary>
            /// Initializes a new instance of the <see cref="Wrapper"/> class.
            /// </summary>
            internal Wrapper(SystemGenerics.IEnumerable<SystemGenerics.KeyValuePair<TKey, TValue>> collection)
                : base(collection) => this.Setup();

            /// <summary>
            /// Initializes a new instance of the <see cref="Wrapper"/> class.
            /// </summary>
            internal Wrapper(SystemGenerics.IEnumerable<SystemGenerics.KeyValuePair<TKey, TValue>> collection,
                SystemGenerics.IEqualityComparer<TKey> comparer)
                : base(collection, comparer) => this.Setup();
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
                        $"Found write/write data race on '{typeof(SystemGenerics.Dictionary<TKey, TValue>)}'.");
                    runtime.Assert(this.ReaderCount is 0,
                        $"Found read/write data race on '{typeof(SystemGenerics.Dictionary<TKey, TValue>)}'.");
                    SystemInterlocked.Increment(ref this.WriterCount);
                }
                else
                {
                    runtime.Assert(this.WriterCount is 0,
                        $"Found read/write data race on '{typeof(SystemGenerics.Dictionary<TKey, TValue>)}'.");
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
