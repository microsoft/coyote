// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
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
        /*
        public TValue this[Dictionary<TKey, TValue> a, TKey key]
        {
            get
            {
                return a[key];
            }
            set
            {
                a[key] = value;
            }
        }
        */

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Add(Dictionary<dynamic, dynamic> obj, dynamic key, dynamic value)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.DetectRace(obj.GetHashCode(), false, obj.ToString());
            }

            obj.Add(key, value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Clear(ref Dictionary<dynamic, dynamic> obj)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.DetectRace(obj.GetHashCode(), false);
            }

            obj.Clear();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsValue(ref Dictionary<dynamic, dynamic> obj, dynamic value)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.DetectRace(obj.GetHashCode(), true);
            }

            return obj.ContainsValue(value);
        }

#if !NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void EnsureCapacity(ref Dictionary<dynamic, dynamic> obj, int size)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.DetectRace(obj.GetHashCode(), false);
            }

            obj.EnsureCapacity(size);
        }
#endif

        // Udit: Don't know what to do with enumerators. They will read the dictionary but we can't put context switches there.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IEnumerator<KeyValuePair<object, object>> GetEnumerator(ref Dictionary<dynamic, dynamic> obj)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.DetectRace(obj.GetHashCode(), true);
            }

            return obj.GetEnumerator();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Type GetType(ref Dictionary<dynamic, dynamic> obj)
        {
            return obj.GetType();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void OnDeserialization(ref Dictionary<dynamic, dynamic> obj, dynamic sender)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.DetectRace(obj.GetHashCode(), true);
            }

            obj.OnDeserialization(sender);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Remove(ref Dictionary<dynamic, dynamic> obj, dynamic key)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.DetectRace(obj.GetHashCode(), false);
            }

            return obj.Remove(key);
        }

#if !NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Remove(ref Dictionary<dynamic, dynamic> obj, dynamic key, out dynamic value)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.DetectRace(obj.GetHashCode(), false);
            }

            return obj.Remove(key, out value);
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static string ToString(ref Dictionary<dynamic, dynamic> obj)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.DetectRace(obj.GetHashCode(), true);
            }

            return obj.ToString();
        }

#if !NETSTANDARD2_0

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TrimExcess(ref Dictionary<dynamic, dynamic> obj)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.DetectRace(obj.GetHashCode(), false);
            }

            obj.TrimExcess();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TrimExcess(ref Dictionary<dynamic, dynamic> obj, int size)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.DetectRace(obj.GetHashCode(), false);
            }

            obj.TrimExcess(size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryAdd(ref Dictionary<dynamic, dynamic> obj, dynamic key, dynamic value)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.DetectRace(obj.GetHashCode(), false);
            }

            return obj.TryAdd(key, value);
        }
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // get_Count is a compiler emitted function for obj.Count, so can't change its
        // name format.
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
        public static int get_Count(Dictionary<dynamic, dynamic> obj)
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.DetectRace(obj.GetHashCode(), true);
            }

            return obj.Count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // get_Item is a compiler emitted function for _ = obj[_], so can't change its
        // name format.
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
        public static dynamic get_Item(Dictionary<dynamic, dynamic> obj, dynamic key)
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.DetectRace(obj.GetHashCode(), true);
            }

            return obj[key];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // set_Item is a compiler emitted function for obj[_] = _, so can't change its
        // name format.
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
        public static void set_Item(Dictionary<dynamic, dynamic> obj, dynamic key, dynamic value)
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.DetectRace(obj.GetHashCode(), false);
            }

            obj[key] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool TryGetValue(ref Dictionary<dynamic, dynamic> obj, dynamic key, out dynamic value)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.DetectRace(obj.GetHashCode(), true);
            }

            return obj.TryGetValue(key, out value);
        }
    }
}
