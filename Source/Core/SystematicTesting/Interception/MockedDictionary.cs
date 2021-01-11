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
    /// <summary>
    /// Provides methods for dictionary that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class MockDictionary
    {
        internal class RWData : object
        {
            public int ReaderCount { get; set; }
            public int WriterCount { get; set; }

            public RWData()
            {
                this.ReaderCount = 0;
                this.WriterCount = 0;
            }

            public RWData(int rc, int wc)
            {
                this.ReaderCount = rc;
                this.WriterCount = wc;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static object DetectDataRace(object obj, bool isWrite)
        {
            bool is_dictionary = obj is IDictionary && obj.GetType().IsGenericType &&
                obj.GetType().GetGenericTypeDefinition().IsAssignableFrom(typeof(Dictionary<,>));

            if (CoyoteRuntime.IsExecutionControlled && is_dictionary)
            {
                // If this object is already been tracked
                if (CoyoteRuntime.Current.Cwt.TryGetValue(obj, out object rwCount))
                {
                    RWData rwCountKV = rwCount as RWData;

                    Debug.Assert(rwCountKV.ReaderCount >= 0 && rwCountKV.WriterCount >= 0, "Invariant failed");

                    if (isWrite)
                    {
                        // If I am a Writer
                        if (rwCountKV.ReaderCount > 0 || rwCountKV.WriterCount > 0)
                        {
                            CoyoteRuntime.Current.NotifyAssertionFailure($"Race found between a writer and a reader/writer on object: {obj}");
                        }
                        else
                        {
                            rwCountKV.WriterCount++;
                            CoyoteRuntime.Current.Cwt.Remove(obj);
                            CoyoteRuntime.Current.Cwt.Add(obj, rwCountKV);

                            CoyoteRuntime.Current.ScheduleNextOperation();

                            // Re-retrive the count of readers and writers
                            CoyoteRuntime.Current.Cwt.TryGetValue(obj, out rwCount);
                            rwCountKV = rwCount as RWData;
                            Debug.Assert(rwCountKV.ReaderCount >= 0 && rwCountKV.WriterCount >= 0, "Invariant failed");

                            rwCountKV.WriterCount--;
                            CoyoteRuntime.Current.Cwt.Remove(obj);
                            CoyoteRuntime.Current.Cwt.Add(obj, rwCountKV);
                        }
                    }
                    else
                    {
                        // If I am a Reader
                        if (rwCountKV.WriterCount > 0)
                        {
                            CoyoteRuntime.Current.NotifyAssertionFailure($"Race found between a reader and a writer on object: {obj}");
                        }
                        else
                        {
                            rwCountKV.ReaderCount++;
                            CoyoteRuntime.Current.Cwt.Remove(obj);
                            CoyoteRuntime.Current.Cwt.Add(obj, rwCountKV);

                            CoyoteRuntime.Current.ScheduleNextOperation();

                            // Re-retrive the count of readers and writers
                            CoyoteRuntime.Current.Cwt.TryGetValue(obj, out rwCount);
                            rwCountKV = rwCount as RWData;
                            Debug.Assert(rwCountKV.ReaderCount >= 0 && rwCountKV.WriterCount >= 0, "Invariant failed");

                            rwCountKV.ReaderCount--;
                            CoyoteRuntime.Current.Cwt.Remove(obj);
                            CoyoteRuntime.Current.Cwt.Add(obj, rwCountKV);
                        }
                    }
                }
                else
                {
                    RWData newKV = null;
                    newKV = isWrite ? new RWData(0, 1) : new RWData(1, 0);
                    CoyoteRuntime.Current.Cwt.Add(obj, newKV);

                    CoyoteRuntime.Current.ScheduleNextOperation();

                    // Re-Retrieve the saved Reader/Writer Count values
                    CoyoteRuntime.Current.Cwt.TryGetValue(obj, out object rwCount_);
                    newKV = rwCount_ as RWData;
                    Debug.Assert(newKV.ReaderCount >= 0 && newKV.WriterCount >= 0, "Invariant failed");

                    _ = isWrite ? newKV.WriterCount-- : newKV.ReaderCount--;
                    CoyoteRuntime.Current.Cwt.Remove(obj);
                    CoyoteRuntime.Current.Cwt.Add(obj, newKV);
                }
            }

            return obj;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add<TKey, TValue>(object obj, TKey key, TValue value)
        {
            DetectDataRace(obj, true);

            var dict = obj as Dictionary<TKey, TValue>;
            dict.Add(key, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear<TKey, TValue>(object obj)
        {
            DetectDataRace(obj, true);

            var dict = obj as Dictionary<TKey, TValue>;
            dict.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsValue<TKey, TValue>(object obj, TValue value)
        {
            DetectDataRace(obj, false);

            var dict = obj as Dictionary<TKey, TValue>;
            return dict.ContainsValue(value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsKey<TKey, TValue>(object obj, TKey key)
        {
            DetectDataRace(obj, false);

            var dict = obj as Dictionary<TKey, TValue>;
            return dict.ContainsKey(key);
        }

#if !NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureCapacity<TKey, TValue>(object obj, int size)
        {
            DetectDataRace(obj, true);

            var dict = obj as Dictionary<TKey, TValue>;
            dict.EnsureCapacity(size);
        }
#endif

        // Udit: Don't know what to do with enumerators. They will read the dictionary but we can't put context switches there.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator<TKey, TValue>(object obj)
        {
            DetectDataRace(obj, false);

            var dict = obj as Dictionary<TKey, TValue>;
            return dict.GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetType<TKey, TValue>(Dictionary<TKey, TValue> obj)
        {
            return obj.GetType();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OnDeserialization<TKey, TValue>(object obj, object sender)
        {
            DetectDataRace(obj, true);

            var dict = obj as Dictionary<TKey, TValue>;
            dict.OnDeserialization(sender);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Remove<TKey, TValue>(object obj, TKey key)
        {
            DetectDataRace(obj, true);

            var dict = obj as Dictionary<TKey, TValue>;
            return dict.Remove(key);
        }

#if !NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Remove<TKey, TValue>(object obj, TKey key, out TValue value)
        {
            DetectDataRace(obj, true);

            var dict = obj as Dictionary<TKey, TValue>;
            return dict.Remove(key, out value);
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToString<TKey, TValue>(object obj)
        {
            DetectDataRace(obj, false);

            var dict = obj as Dictionary<TKey, TValue>;
            return obj.ToString();
        }

#if !NETSTANDARD2_0

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TrimExcess<TKey, TValue>(object obj)
        {
            DetectDataRace(obj, true);

            var dict = obj as Dictionary<TKey, TValue>;
            dict.TrimExcess();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TrimExcess<TKey, TValue>(object obj, int size)
        {
            DetectDataRace(obj, true);

            var dict = obj as Dictionary<TKey, TValue>;
            dict.TrimExcess(size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdd<TKey, TValue>(object obj, TKey key, TValue value)
        {
            DetectDataRace(obj, true);

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
            DetectDataRace(obj, false);

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
            DetectDataRace(obj, false);

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
            DetectDataRace(obj, true);

            var dict = obj as Dictionary<TKey, TValue>;
            dict[key] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValue<TKey, TValue>(object obj, TKey key, out TValue value)
        {
            DetectDataRace(obj, false);

            var dict = obj as Dictionary<TKey, TValue>;
            return dict.TryGetValue(key, out value);
        }
    }
}
