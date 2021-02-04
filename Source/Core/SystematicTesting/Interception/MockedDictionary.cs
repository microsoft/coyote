// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.SystematicTesting.Interception
{
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class StaticMockDictionaryWrapper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TValue> Create<TKey, TValue>() => CoyoteRuntime.IsExecutionControlled ?
         new MockDictionary<TKey, TValue>() : new Dictionary<TKey, TValue>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TValue> Create<TKey, TValue>(IDictionary<TKey, TValue> dictionary) => CoyoteRuntime.IsExecutionControlled ?
         new MockDictionary<TKey, TValue>(dictionary) : new Dictionary<TKey, TValue>(dictionary);

#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TValue> Create<TKey, TValue>(IEqualityComparer<TKey>? comparer) => CoyoteRuntime.IsExecutionControlled ?
         new MockDictionary<TKey, TValue>(comparer) : new Dictionary<TKey, TValue>(comparer);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TValue> Create<TKey, TValue>(System.Collections.Generic.IDictionary<TKey, TValue> dictionary, System.Collections.Generic.IEqualityComparer<TKey>? comparer)
         => CoyoteRuntime.IsExecutionControlled ? new MockDictionary<TKey, TValue>(dictionary, comparer) :
            new Dictionary<TKey, TValue>(dictionary, comparer);

        /*
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TValue> Create<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>>? collection, IEqualityComparer<TKey>? comparer)
        {
            return new MockDictionary<TKey, TValue>(collection, comparer);
        }
        */

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TValue> Create<TKey, TValue>(int capacity, IEqualityComparer<TKey>? comparer)
        {
            return new MockDictionary<TKey, TValue>(capacity, comparer);
        }

        /*
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TValue> Create<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>>? collection)
        {
            return new MockDictionary<TKey, TValue>(collection);
        }
        */
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TValue> Create<TKey, TValue>(SerializationInfo info, StreamingContext context)
        {
            return new MockDictionary<TKey, TValue>(info, context);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Dictionary<TKey, TValue> Create<TKey, TValue>(int capacity)
        {
            return new MockDictionary<TKey, TValue>(capacity);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add<TKey, TValue>(object obj, TKey key, TValue value)
        {
            if (obj is MockDictionary<TKey, TValue> mockDictObj)
            {
                mockDictObj.DetectDataRace(true);
            }

            var dict = obj as Dictionary<TKey, TValue>;
            dict.Add(key, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear<TKey, TValue>(object obj)
        {
            if (obj is MockDictionary<TKey, TValue> mockDictObj)
            {
                mockDictObj.DetectDataRace(true);
            }

            var dict = obj as Dictionary<TKey, TValue>;
            dict.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsValue<TKey, TValue>(object obj, TValue value)
        {
            if (obj is MockDictionary<TKey, TValue> mockDictObj)
            {
                mockDictObj.DetectDataRace(false);
            }

            var dict = obj as Dictionary<TKey, TValue>;
            return dict.ContainsValue(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsKey<TKey, TValue>(object obj, TKey key)
        {
            if (obj is MockDictionary<TKey, TValue> mockDictObj)
            {
                mockDictObj.DetectDataRace(false);
            }

            var dict = obj as Dictionary<TKey, TValue>;
            return dict.ContainsKey(key);
        }

#if !NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureCapacity<TKey, TValue>(object obj, int size)
        {
            if (obj is MockDictionary<TKey, TValue> mockDictObj)
            {
                mockDictObj.DetectDataRace(true);
            }

            var dict = obj as Dictionary<TKey, TValue>;
            dict.EnsureCapacity(size);
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetType<TKey, TValue>(Dictionary<TKey, TValue> obj)
        {
            return obj.GetType();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OnDeserialization<TKey, TValue>(object obj, object sender)
        {
            if (obj is MockDictionary<TKey, TValue> mockDictObj)
            {
                mockDictObj.DetectDataRace(true);
            }

            var dict = obj as Dictionary<TKey, TValue>;
            dict.OnDeserialization(sender);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Remove<TKey, TValue>(object obj, TKey key)
        {
            if (obj is MockDictionary<TKey, TValue> mockDictObj)
            {
                mockDictObj.DetectDataRace(true);
            }

            var dict = obj as Dictionary<TKey, TValue>;
            return dict.Remove(key);
        }

#if !NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Remove<TKey, TValue>(object obj, TKey key, out TValue value)
        {
            if (obj is MockDictionary<TKey, TValue> mockDictObj)
            {
                mockDictObj.DetectDataRace(true);
            }

            var dict = obj as Dictionary<TKey, TValue>;
            return dict.Remove(key, out value);
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToString<TKey, TValue>(object obj)
        {
            if (obj is MockDictionary<TKey, TValue> mockDictObj)
            {
                mockDictObj.DetectDataRace(false);
            }

            var dict = obj as Dictionary<TKey, TValue>;
            return obj.ToString();
        }

#if !NETSTANDARD2_0

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TrimExcess<TKey, TValue>(object obj)
        {
            if (obj is MockDictionary<TKey, TValue> mockDictObj)
            {
                mockDictObj.DetectDataRace(true);
            }

            var dict = obj as Dictionary<TKey, TValue>;
            dict.TrimExcess();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TrimExcess<TKey, TValue>(object obj, int size)
        {
            if (obj is MockDictionary<TKey, TValue> mockDictObj)
            {
                mockDictObj.DetectDataRace(true);
            }

            var dict = obj as Dictionary<TKey, TValue>;
            dict.TrimExcess(size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdd<TKey, TValue>(object obj, TKey key, TValue value)
        {
            if (obj is MockDictionary<TKey, TValue> mockDictObj)
            {
                mockDictObj.DetectDataRace(true);
            }

            var dict = obj as Dictionary<TKey, TValue>;
            return dict.TryAdd(key, value);
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // get_Count is a compiler emitted function for obj.Count, so can't change its
        // name format.
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
        public static int get_Count<TKey, TValue>(object obj)
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            if (obj is MockDictionary<TKey, TValue> mockDictObj)
            {
                mockDictObj.DetectDataRace(false);
            }

            var dict = obj as Dictionary<TKey, TValue>;
            return dict.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // get_Item is a compiler emitted function for _ = obj[_], so can't change its
        // name format.
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
        public static TValue get_Item<TKey, TValue>(object obj, TKey key)
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            if (obj is MockDictionary<TKey, TValue> mockDictObj)
            {
                mockDictObj.DetectDataRace(false);
            }

            var dict = obj as Dictionary<TKey, TValue>;
            return dict[key];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // set_Item is a compiler emitted function for obj[_] = _, so can't change its
        // name format.
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
        public static void set_Item<TKey, TValue>(object obj, TKey key, TValue value)
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            Debug.Assert(false, "aa");

            if (obj is MockDictionary<TKey, TValue> mockDictObj)
            {
                mockDictObj.DetectDataRace(true);
            }

            var dict = obj as Dictionary<TKey, TValue>;
            dict[key] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValue<TKey, TValue>(object obj, TKey key, out TValue value)
        {
            if (obj is MockDictionary<TKey, TValue> mockDictObj)
            {
                mockDictObj.DetectDataRace(false);
            }

            var dict = obj as Dictionary<TKey, TValue>;
            return dict.TryGetValue(key, out value);
        }
    }

    /// <summary>
    /// Provides methods for dictionary that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class MockDictionary<TKey, TValue> : Dictionary<TKey, TValue>
    {
        internal class RWData : object
        {
            public int ReaderCount { get; set; }
            public int WriterCount { get; set; }

            public RWData(int rc = 0, int wc = 0)
            {
                this.ReaderCount = rc;
                this.WriterCount = wc;
            }

            public void CheckInvariant()
            {
                Debug.Assert(this.ReaderCount >= 0 && this.WriterCount >= 0, "Invariant failed");
            }
        }

        private RWData AuxInfo;

        private void Init()
        {
            this.AuxInfo = new RWData(0, 0);
        }

        public MockDictionary()
            : base() => this.Init();

        public MockDictionary(IDictionary<TKey, TValue> dictionary)
            : base(dictionary) => this.Init();

        public MockDictionary(SerializationInfo info, StreamingContext context)
            : base(info, context) => this.Init();

        public MockDictionary(int capacity)
            : base(capacity) => this.Init();

        public MockDictionary(int capacity, IEqualityComparer<TKey> comparer)
            : base(capacity, comparer) => this.Init();

        /*
        public MockDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer)
            : base(collection, comparer) => this.Init();
        */

        public MockDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
            : base(dictionary, comparer) => this.Init();

        public MockDictionary(IEqualityComparer<TKey> comparer)
            : base(comparer) => this.Init();

        /*
        public MockDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection)
            : base(collection) => this.Init();
        */

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void DetectDataRace(bool isWrite)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                this.AuxInfo.CheckInvariant();

                // For a Writer
                if (isWrite)
                {
                    // If I am a Writer
                    if (this.AuxInfo.ReaderCount > 0 || this.AuxInfo.WriterCount > 0)
                    {
                        CoyoteRuntime.Current.NotifyAssertionFailure($"Race found between a writer and a reader/writer on object: {this}");
                    }
                    else
                    {
                        this.AuxInfo.WriterCount++;

                        CoyoteRuntime.Current.ScheduleNextOperation();

                        this.AuxInfo.CheckInvariant();
                        this.AuxInfo.WriterCount--;
                    }
                }

                // For a Reader
                else
                {
                    // If I am a Reader
                    if (this.AuxInfo.WriterCount > 0)
                    {
                        CoyoteRuntime.Current.NotifyAssertionFailure($"Race found between a reader and a writer on object: {this}");
                    }
                    else
                    {
                        this.AuxInfo.ReaderCount++;

                        CoyoteRuntime.Current.ScheduleNextOperation();

                        this.AuxInfo.CheckInvariant();
                        this.AuxInfo.ReaderCount--;
                    }
                }
            }
        }
    }
}
