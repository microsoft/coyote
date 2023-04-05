// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using Microsoft.Coyote.Runtime;
using SystemVolatile = System.Threading.Volatile;

namespace Microsoft.Coyote.Rewriting.Types.Threading
{
    /// <summary>
    /// Provides methods for performing volatile memory operations.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class Volatile
    {
        /// <summary>
        /// Reads the value of the specified field. On systems that require it, inserts a
        /// memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static bool Read(ref bool location)
        {
            ExploreInterleaving();
            return SystemVolatile.Read(ref location);
        }

        /// <summary>
        /// Reads the value of the specified field. On systems that require it, inserts a
        /// memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static nuint Read(ref nuint location)
        {
            ExploreInterleaving();
            return SystemVolatile.Read(ref location);
        }

        /// <summary>
        /// Reads the value of the specified field. On systems that require it, inserts a
        /// memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static ulong Read(ref ulong location)
        {
            ExploreInterleaving();
            return SystemVolatile.Read(ref location);
        }

        /// <summary>
        /// Reads the value of the specified field. On systems that require it, inserts a
        /// memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static uint Read(ref uint location)
        {
            ExploreInterleaving();
            return SystemVolatile.Read(ref location);
        }

        /// <summary>
        /// Reads the value of the specified field. On systems that require it, inserts a
        /// memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static ushort Read(ref ushort location)
        {
            ExploreInterleaving();
            return SystemVolatile.Read(ref location);
        }

        /// <summary>
        /// Reads the value of the specified field. On systems that require it, inserts a
        /// memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static float Read(ref float location)
        {
            ExploreInterleaving();
            return SystemVolatile.Read(ref location);
        }

        /// <summary>
        /// Reads the value of the specified field. On systems that require it, inserts a
        /// memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static sbyte Read(ref sbyte location)
        {
            ExploreInterleaving();
            return SystemVolatile.Read(ref location);
        }

        /// <summary>
        /// Reads the object reference from the specified field. On systems that require it, inserts
        /// a memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static T Read<T>(ref T location)
            where T : class
        {
            ExploreInterleaving();
            return SystemVolatile.Read(ref location);
        }

        /// <summary>
        /// Reads the value of the specified field. On systems that require it, inserts a
        /// memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static long Read(ref long location)
        {
            ExploreInterleaving();
            return SystemVolatile.Read(ref location);
        }

        /// <summary>
        /// Reads the value of the specified field. On systems that require it, inserts a
        /// memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static int Read(ref int location)
        {
            ExploreInterleaving();
            return SystemVolatile.Read(ref location);
        }

        /// <summary>
        /// Reads the value of the specified field. On systems that require it, inserts a
        /// memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static short Read(ref short location)
        {
            ExploreInterleaving();
            return SystemVolatile.Read(ref location);
        }

        /// <summary>
        /// Reads the value of the specified field. On systems that require it, inserts a
        /// memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static double Read(ref double location)
        {
            ExploreInterleaving();
            return SystemVolatile.Read(ref location);
        }

        /// <summary>
        /// Reads the value of the specified field. On systems that require it, inserts a
        /// memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static byte Read(ref byte location)
        {
            ExploreInterleaving();
            return SystemVolatile.Read(ref location);
        }

        /// <summary>
        /// Reads the value of the specified field. On systems that require it, inserts a
        /// memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static nint Read(ref nint location)
        {
            ExploreInterleaving();
            return SystemVolatile.Read(ref location);
        }

        /// <summary>
        /// Writes the specified value to the specified field. On systems that require it, inserts
        /// a memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static void Write(ref ulong location, ulong value)
        {
            ExploreInterleaving();
            SystemVolatile.Write(ref location, value);
        }

        /// <summary>
        /// Writes the specified value to the specified field. On systems that require it, inserts
        /// a memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static void Write(ref uint location, uint value)
        {
            ExploreInterleaving();
            SystemVolatile.Write(ref location, value);
        }

        /// <summary>
        /// Writes the specified value to the specified field. On systems that require it, inserts
        /// a memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static void Write(ref ushort location, ushort value)
        {
            ExploreInterleaving();
            SystemVolatile.Write(ref location, value);
        }

        /// <summary>
        /// Writes the specified value to the specified field. On systems that require it, inserts
        /// a memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static void Write(ref float location, float value)
        {
            ExploreInterleaving();
            SystemVolatile.Write(ref location, value);
        }

        /// <summary>
        /// Writes the specified value to the specified field. On systems that require it, inserts
        /// a memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static void Write(ref sbyte location, sbyte value)
        {
            ExploreInterleaving();
            SystemVolatile.Write(ref location, value);
        }

        /// <summary>
        /// Writes the specified value to the specified field. On systems that require it, inserts
        /// a memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static void Write(ref nint location, nint value)
        {
            ExploreInterleaving();
            SystemVolatile.Write(ref location, value);
        }

        /// <summary>
        /// Writes the specified value to the specified field. On systems that require it, inserts
        /// a memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static void Write(ref short location, short value)
        {
            ExploreInterleaving();
            SystemVolatile.Write(ref location, value);
        }

        /// <summary>
        /// Writes the specified value to the specified field. On systems that require it, inserts
        /// a memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static void Write(ref int location, int value)
        {
            ExploreInterleaving();
            SystemVolatile.Write(ref location, value);
        }

        /// <summary>
        /// Writes the specified value to the specified field. On systems that require it, inserts
        /// a memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static void Write(ref double location, double value)
        {
            ExploreInterleaving();
            SystemVolatile.Write(ref location, value);
        }

        /// <summary>
        /// Writes the specified value to the specified field. On systems that require it, inserts
        /// a memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static void Write(ref byte location, byte value)
        {
            ExploreInterleaving();
            SystemVolatile.Write(ref location, value);
        }

        /// <summary>
        /// Writes the specified value to the specified field. On systems that require it, inserts
        /// a memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static void Write(ref bool location, bool value)
        {
            ExploreInterleaving();
            SystemVolatile.Write(ref location, value);
        }

        /// <summary>
        /// Writes the specified value to the specified field. On systems that require it, inserts
        /// a memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static void Write(ref nuint location, nuint value)
        {
            ExploreInterleaving();
            SystemVolatile.Write(ref location, value);
        }

        /// <summary>
        /// Writes the specified value to the specified field. On systems that require it, inserts
        /// a memory barrier that prevents the processor from reordering memory operations.
        /// </summary>
        public static void Write(ref long location, long value)
        {
            ExploreInterleaving();
            SystemVolatile.Write(ref location, value);
        }

        /// <summary>
        /// Writes the specified object reference to the specified field. On systems that
        /// require it, inserts a memory barrier that prevents the processor from reordering
        /// memory operations.
        /// </summary>
        public static void Write<T>(ref T location, T value)
            where T : class
        {
            ExploreInterleaving();
            SystemVolatile.Write(ref location, value);
        }

        /// <summary>
        /// Asks the runtime to explore a possible interleaving.
        /// </summary>
        private static void ExploreInterleaving()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.Configuration.IsVolatileOperationRaceCheckingEnabled &&
                runtime.SchedulingPolicy != SchedulingPolicy.None &&
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
        }
    }
}
