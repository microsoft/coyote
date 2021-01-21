// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Runtime.CompilerServices;
using Microsoft.Coyote.Runtime;
using SystemInterlocked = System.Threading.Interlocked;

namespace Microsoft.Coyote.SystematicTesting.Interception
{
    /// <summary>
    /// Provides controlled atomic operations for variables that are shared by multiple threads.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class Interlocked
    {
        /// <summary>
        /// Increments a specified variable and stores the result, as an atomic operation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Increment(ref int location)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
            }

            return SystemInterlocked.Increment(ref location);
        }

        /// <summary>
        /// Increments a specified variable and stores the result, as an atomic operation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Increment(ref long location)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
            }

            return SystemInterlocked.Increment(ref location);
        }

        /// <summary>
        /// Decrement a specified variable and stores the result, as an atomic operation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Decrement(ref int location)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
            }

            return SystemInterlocked.Decrement(ref location);
        }

        /// <summary>
        /// Decrement a specified variable and stores the result, as an atomic operation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Decrement(ref long location)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
            }

            return SystemInterlocked.Decrement(ref location);
        }

        /// <summary>
        /// Compares two values for equality and, if they are equal, replaces the first value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CompareExchange(ref int location1, int value, int comparand)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
            }

            return SystemInterlocked.CompareExchange(ref location1, value, comparand);
        }

        /// <summary>
        /// Compares two values for equality and, if they are equal, replaces the first value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long CompareExchange(ref long location1, long value, long comparand)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
            }

            return SystemInterlocked.CompareExchange(ref location1, value, comparand);
        }

        /// <summary>
        /// Compares two values for equality and, if they are equal, replaces the first value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CompareExchange(ref float location1, float value, float comparand)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
            }

            return SystemInterlocked.CompareExchange(ref location1, value, comparand);
        }

        /// <summary>
        /// Compares two values for equality and, if they are equal, replaces the first value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double CompareExchange(ref double location1, double value, double comparand)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
            }

            return SystemInterlocked.CompareExchange(ref location1, value, comparand);
        }

        /// <summary>
        /// Compares two values for equality and, if they are equal, replaces the first value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object CompareExchange(ref object location1, object value, object comparand)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
            }

            return SystemInterlocked.CompareExchange(ref location1, value, comparand);
        }

        /// <summary>
        /// Compares two values for equality and, if they are equal, replaces the first value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr CompareExchange(ref IntPtr location1, IntPtr value, IntPtr comparand)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
            }

            return SystemInterlocked.CompareExchange(ref location1, value, comparand);
        }

        /// <summary>
        /// Compares two values for equality and, if they are equal, replaces the first value.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T CompareExchange<T>(ref T location1, T value, T comparand)
            where T : class
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
            }

            return SystemInterlocked.CompareExchange(ref location1, value, comparand);
        }

        /// <summary>
        /// Adds two integers and replaces the first integer with the sum, as an atomic operation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Add(ref int location1, int value)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
            }

            return SystemInterlocked.Add(ref location1, value);
        }

        /// <summary>
        /// Adds two integers and replaces the first integer with the sum, as an atomic operation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Add(ref long location1, long value)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
            }

            return SystemInterlocked.Add(ref location1, value);
        }

        /// <summary>
        /// Sets a variable to a specified value as an atomic operation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Exchange(ref int location1, int value)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
            }

            return SystemInterlocked.Exchange(ref location1, value);
        }

        /// <summary>
        /// Sets a variable to a specified value as an atomic operation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Exchange(ref long location1, long value)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
            }

            return SystemInterlocked.Exchange(ref location1, value);
        }

        /// <summary>
        /// Sets a variable to a specified value as an atomic operation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Exchange(ref float location1, float value)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
            }

            return SystemInterlocked.Exchange(ref location1, value);
        }

        /// <summary>
        /// Sets a variable to a specified value as an atomic operation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double Exchange(ref double location1, double value)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
            }

            return SystemInterlocked.Exchange(ref location1, value);
        }

        /// <summary>
        /// Sets a variable to a specified value as an atomic operation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object Exchange(ref object location1, object value)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
            }

            return SystemInterlocked.Exchange(ref location1, value);
        }

        /// <summary>
        /// Sets a variable to a specified value as an atomic operation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static IntPtr Exchange(ref IntPtr location1, IntPtr value)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
            }

            return SystemInterlocked.Exchange(ref location1, value);
        }

        /// <summary>
        /// Sets a variable to a specified value as an atomic operation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T Exchange<T>(ref T location1, T value)
            where T : class
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
            }

            return SystemInterlocked.Exchange(ref location1, value);
        }

        /// <summary>
        /// Synchronizes memory access.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MemoryBarrier() => SystemInterlocked.MemoryBarrier();

        /// <summary>
        /// Returns a 64-bit value, loaded as an atomic operation.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Read(ref long location)
        {
            if (CoyoteRuntime.IsExecutionControlled)
            {
                CoyoteRuntime.Current.ScheduleNextOperation();
            }

            return SystemInterlocked.Read(ref location);
        }

#if NET5_0 || NETCOREAPP3_1 || NETSTANDARD2_1
        /// <summary>
        /// Provides a process-wide memory barrier that ensures that reads and writes from any CPU cannot move across the barrier.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MemoryBarrierProcessWide() => SystemInterlocked.MemoryBarrierProcessWide();
#endif
    }
}
