// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using Microsoft.Coyote.Runtime;
using SystemInterlocked = System.Threading.Interlocked;

namespace Microsoft.Coyote.Rewriting.Types.Threading
{
    /// <summary>
    /// Provides atomic operations for variables that are shared by multiple threads.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class Interlocked
    {
        /// <summary>
        /// Adds two 32-bit integers and replaces the first integer with the sum, as an atomic operation.
        /// </summary>
        public static int Add(ref int location1, int value)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Add(ref location1, value);
        }

        /// <summary>
        /// Adds two 64-bit integers and replaces the first integer with the sum, as an atomic operation.
        /// </summary>
        public static long Add(ref long location1, long value)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Add(ref location1, value);
        }

        /// <summary>
        /// Adds two 32-bit unsigned integers and replaces the first integer with the sum, as an atomic operation.
        /// </summary>
        public static uint Add(ref uint location1, uint value)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Add(ref location1, value);
        }

        /// <summary>
        /// Adds two 64-bit unsigned integers and replaces the first integer with the sum, as an atomic operation.
        /// </summary>
        public static ulong Add(ref ulong location1, ulong value)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Add(ref location1, value);
        }

        /// <summary>
        /// Bitwise "ands" two 32-bit signed integers and replaces the first integer with
        /// the result, as an atomic operation.
        /// </summary>
        public static int And(ref int location1, int value)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Add(ref location1, value);
        }

        /// <summary>
        /// Bitwise "ands" two 64-bit signed integers and replaces the first integer with
        /// the result, as an atomic operation.
        /// </summary>
        public static long And(ref long location1, long value)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Add(ref location1, value);
        }

        /// <summary>
        /// Bitwise "ands" two 32-bit unsigned integers and replaces the first integer with
        /// the result, as an atomic operation.
        /// </summary>
        public static uint And(ref uint location1, uint value)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Add(ref location1, value);
        }

        /// <summary>
        /// Bitwise "ands" two 64-bit unsigned integers and replaces the first integer with
        /// the result, as an atomic operation.
        /// </summary>
        public static ulong And(ref ulong location1, ulong value)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Add(ref location1, value);
        }

        /// <summary>
        /// Compares two 64-bit unsigned integers for equality and, if they are equal, replaces
        /// the first value.
        /// </summary>
        public static ulong CompareExchange(ref ulong location1, ulong value, ulong comparand)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.CompareExchange(ref location1, value, comparand);
        }

        /// <summary>
        /// Compares two 32-bit unsigned integers for equality and, if they are equal, replaces
        /// the first value.
        /// </summary>
        public static uint CompareExchange(ref uint location1, uint value, uint comparand)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.CompareExchange(ref location1, value, comparand);
        }

        /// <summary>
        /// Compares two single-precision floating point numbers for equality and, if they
        /// are equal, replaces the first value.
        /// </summary>
        public static float CompareExchange(ref float location1, float value, float comparand)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.CompareExchange(ref location1, value, comparand);
        }

        /// <summary>
        /// Compares two objects for reference equality and, if they are equal, replaces
        /// the first object.
        /// </summary>
        public static object CompareExchange(ref object location1, object value, object comparand)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.CompareExchange(ref location1, value, comparand);
        }

        /// <summary>
        /// Compares two double-precision floating point numbers for equality and, if they
        /// are equal, replaces the first value.
        /// </summary>
        public static double CompareExchange(ref double location1, double value, double comparand)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.CompareExchange(ref location1, value, comparand);
        }

        /// <summary>
        /// Compares two 64-bit signed integers for equality and, if they are equal, replaces
        /// the first value.
        /// </summary>
        public static long CompareExchange(ref long location1, long value, long comparand)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.CompareExchange(ref location1, value, comparand);
        }

        /// <summary>
        /// Compares two 32-bit signed integers for equality and, if they are equal, replaces
        /// the first value.
        /// </summary>
        public static int CompareExchange(ref int location1, int value, int comparand)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.CompareExchange(ref location1, value, comparand);
        }

        /// <summary>
        /// Compares two instances of the specified reference type T for reference equality
        /// and, if they are equal, replaces the first one.
        /// </summary>
        public static T CompareExchange<T>(ref T location1, T value, T comparand)
            where T : class
        {
            Operation.ScheduleNext();
            return SystemInterlocked.CompareExchange(ref location1, value, comparand);
        }

        /// <summary>
        /// Compares two platform-specific handles or pointers for equality and, if they
        /// are equal, replaces the first one.
        /// </summary>
        public static IntPtr CompareExchange(ref IntPtr location1, IntPtr value, IntPtr comparand)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.CompareExchange(ref location1, value, comparand);
        }

        /// <summary>
        /// Decrements a specified variable and stores the result, as an atomic operation.
        /// </summary>
        public static uint Decrement(ref uint location)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Decrement(ref location);
        }

        /// <summary>
        /// Decrements a specified variable and stores the result, as an atomic operation.
        /// </summary>
        public static ulong Decrement(ref ulong location)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Decrement(ref location);
        }

        /// <summary>
        /// Decrements a specified variable and stores the result, as an atomic operation.
        /// </summary>
        public static int Decrement(ref int location)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Decrement(ref location);
        }

        /// <summary>
        /// Decrements the specified variable and stores the result, as an atomic operation.
        /// </summary>
        public static long Decrement(ref long location)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Decrement(ref location);
        }

        /// <summary>
        /// Sets a double-precision floating point number to a specified value and returns
        /// the original value, as an atomic operation.
        /// </summary>
        public static double Exchange(ref double location1, double value)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Exchange(ref location1, value);
        }

        /// <summary>
        /// Sets a 32-bit signed integer to a specified value and returns the original value,
        /// as an atomic operation.
        /// </summary>
        public static int Exchange(ref int location1, int value)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Exchange(ref location1, value);
        }

        /// <summary>
        /// Sets a 64-bit signed integer to a specified value and returns the original value,
        /// as an atomic operation.
        /// </summary>
        public static long Exchange(ref long location1, long value)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Exchange(ref location1, value);
        }

        /// <summary>
        /// Sets a platform-specific handle or pointer to a specified value and returns the
        /// original value, as an atomic operation.
        /// </summary>
        public static IntPtr Exchange(ref IntPtr location1, IntPtr value)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Exchange(ref location1, value);
        }

        /// <summary>
        /// Sets an object to a specified value and returns a reference to the original object,
        /// as an atomic operation.
        /// </summary>
        public static object Exchange(ref object location1, object value)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Exchange(ref location1, value);
        }

        /// <summary>
        /// Sets a single-precision floating point number to a specified value and returns
        /// the original value, as an atomic operation.
        /// </summary>
        public static float Exchange(ref float location1, float value)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Exchange(ref location1, value);
        }

        /// <summary>
        /// Sets a 32-bit unsigned integer to a specified value and returns the original
        /// value, as an atomic operation.
        /// </summary>
        public static uint Exchange(ref uint location1, uint value)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Exchange(ref location1, value);
        }

        /// <summary>
        /// Sets a 64-bit unsigned integer to a specified value and returns the original
        /// value, as an atomic operation.
        /// </summary>
        public static ulong Exchange(ref ulong location1, ulong value)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Exchange(ref location1, value);
        }

        /// <summary>
        /// Sets a variable of the specified type T to a specified value and returns the
        /// original value, as an atomic operation.
        /// </summary>
        public static T Exchange<T>(ref T location1, T value)
            where T : class
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Exchange(ref location1, value);
        }

        /// <summary>
        /// Increments a specified variable and stores the result, as an atomic operation.
        /// </summary>
        public static ulong Increment(ref ulong location)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Increment(ref location);
        }

        /// <summary>
        /// Increments a specified variable and stores the result, as an atomic operation.
        /// </summary>
        public static uint Increment(ref uint location)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Increment(ref location);
        }

        /// <summary>
        /// Increments a specified variable and stores the result, as an atomic operation.
        /// </summary>
        public static int Increment(ref int location)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Increment(ref location);
        }

        /// <summary>
        /// Increments a specified variable and stores the result, as an atomic operation.
        /// </summary>
        public static long Increment(ref long location)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Increment(ref location);
        }

        /// <summary>
        /// Bitwise "ors" two 32-bit signed integers and replaces the first integer with
        /// the result, as an atomic operation.
        /// </summary>
        public static int Or(ref int location1, int value)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Or(ref location1, value);
        }

        /// <summary>
        /// Bitwise "ors" two 64-bit signed integers and replaces the first integer with
        /// the result, as an atomic operation.
        /// </summary>
        public static long Or(ref long location1, long value)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Or(ref location1, value);
        }

        /// <summary>
        /// Bitwise "ors" two 32-bit unsigned integers and replaces the first integer with
        /// the result, as an atomic operation.
        /// </summary>
        public static uint Or(ref uint location1, uint value)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Or(ref location1, value);
        }

        /// <summary>
        /// Bitwise "ors" two 64-bit unsigned integers and replaces the first integer with
        /// the result, as an atomic operation.
        /// </summary>
        public static ulong Or(ref ulong location1, ulong value)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Or(ref location1, value);
        }

        /// <summary>
        /// Returns a 64-bit value, loaded as an atomic operation.
        /// </summary>
        public static long Read(ref long location)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Read(ref location);
        }

        /// <summary>
        /// Returns a 64-bit unsigned value, loaded as an atomic operation.
        /// </summary>
        public static ulong Read(ref ulong location)
        {
            Operation.ScheduleNext();
            return SystemInterlocked.Read(ref location);
        }
    }
}
