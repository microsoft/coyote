// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Coyote.Runtime;

namespace Microsoft.Coyote.SystematicTesting.Interception
{
    // TODO: Add support for all the methods of List.
    public static class StaticMockListWrapper
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TKey> Create<TKey>() => CoyoteRuntime.IsExecutionControlled ?
         new MockListCollection<TKey>() : new List<TKey>();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TKey> Create<TKey>(int capacity) => CoyoteRuntime.IsExecutionControlled ?
         new MockListCollection<TKey>(capacity) : new List<TKey>(capacity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static List<TKey> Create<TKey>(IEnumerable<TKey> collection) => CoyoteRuntime.IsExecutionControlled ?
         new MockListCollection<TKey>(collection) : new List<TKey>(collection);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add<TKey>(object obj, TKey key)
        {
            if (obj is MockListCollection<TKey> mockListObj)
            {
                mockListObj.DetectDataRace(true);
            }

            var list = obj as List<TKey>;
            list.Add(key);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AddRange<TKey>(object obj, IEnumerable<TKey> ienum)
        {
            if (obj is MockListCollection<TKey> mockListObj)
            {
                mockListObj.DetectDataRace(true);
            }

            var list = obj as List<TKey>;
            list.AddRange(ienum);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BinarySearch<TKey>(object obj, TKey item)
        {
            if (obj is MockListCollection<TKey> mockListObj)
            {
                mockListObj.DetectDataRace(false);
            }

            var list = obj as List<TKey>;
            list.BinarySearch(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BinarySearch<TKey>(object obj, TKey item, IComparer<TKey> comparer)
        {
            if (obj is MockListCollection<TKey> mockListObj)
            {
                mockListObj.DetectDataRace(false);
            }

            var list = obj as List<TKey>;
            list.BinarySearch(item, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void BinarySearch<TKey>(object obj, int index, int count, TKey item, IComparer<TKey> comparer)
        {
            if (obj is MockListCollection<TKey> mockListObj)
            {
                mockListObj.DetectDataRace(false);
            }

            var list = obj as List<TKey>;
            list.BinarySearch(index, count, item, comparer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear<TKey>(object obj)
        {
            if (obj is MockListCollection<TKey> mockListObj)
            {
                mockListObj.DetectDataRace(true);
            }

            var list = obj as List<TKey>;
            list.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains<TKey>(object obj, TKey item)
        {
            if (obj is MockListCollection<TKey> mockListObj)
            {
                mockListObj.DetectDataRace(false);
            }

            var list = obj as List<TKey>;
            return list.Contains(item);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static System.Collections.Generic.List<TOutput> ConvertAll<TKey, TOutput>(object obj, Converter<TKey, TOutput> converter)
        {
            if (obj is MockListCollection<TKey> mockListObj)
            {
                mockListObj.DetectDataRace(false);
            }

            var list = obj as List<TKey>;
            return list.ConvertAll<TOutput>(converter);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<TKey>(object obj, TKey[] obj1, int size)
        {
            if (obj is MockListCollection<TKey> mockListObj)
            {
                mockListObj.DetectDataRace(false);
            }

            var list = obj as List<TKey>;
            list.CopyTo(obj1, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<TKey>(object obj, int val1, TKey[] obj1, int size, int val2)
        {
            if (obj is MockListCollection<TKey> mockListObj)
            {
                mockListObj.DetectDataRace(false);
            }

            var list = obj as List<TKey>;
            list.CopyTo(val1, obj1, size, val2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CopyTo<TKey>(object obj, TKey[] obj1)
        {
            if (obj is MockListCollection<TKey> mockListObj)
            {
                mockListObj.DetectDataRace(false);
            }

            var list = obj as List<TKey>;
            list.CopyTo(obj1);
        }

        /*
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Exists<TKey>(object obj, Predicate<TKey> p)
        {
            if (obj is MockListCollection<TKey> mockListObj)
            {
                mockListObj.DetectDataRace(false);
            }

            var list = obj as List<TKey>;
            list.Exists(p);
        }
        */

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // get_Count is a compiler emitted function for obj.Count, so can't change its
        // name format.
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
        public static int get_Count<TKey>(object obj)
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            if (obj is MockListCollection<TKey> mockListObj)
            {
                mockListObj.DetectDataRace(false);
            }

            var list = obj as List<TKey>;
            return list.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // get_Item is a compiler emitted function for _ = obj[_], so can't change its
        // name format.
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
        public static TKey get_Item<TKey>(object obj, int key)
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            if (obj is MockListCollection<TKey> mockListObj)
            {
                mockListObj.DetectDataRace(false);
            }

            var list = obj as List<TKey>;
            return list[key];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // set_Item is a compiler emitted function for obj[_] = _, so can't change its
        // name format.
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
        public static void set_Item<TKey>(object obj, int key, TKey value)
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            if (obj is MockListCollection<TKey> mockListObj)
            {
                mockListObj.DetectDataRace(true);
            }

            var list = obj as List<TKey>;
            list[key] = value;
        }
    }

    /// <summary>
    /// Provides methods for list that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public class MockListCollection<TKey> : List<TKey>
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

        public MockListCollection()
            : base() => this.Init();

        public MockListCollection(IEnumerable<TKey> collection)
            : base(collection) => this.Init();

        public MockListCollection(int capacity)
            : base(capacity) => this.Init();

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
