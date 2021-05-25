// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Provides a set of static methods for fuzzing operations.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class FuzzingProvider
    {
        /// <summary>
        /// Creates an action that can be fuzzed when executed.
        /// </summary>
        internal static Action CreateAction(Action action)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing)
            {
                return new Action(() =>
                {
                    runtime.DelayOperation();
                    action();
                });
            }

            return action;
        }

        /// <summary>
        /// Creates a function that can be fuzzed when executed.
        /// </summary>
        internal static Func<T> CreateFunc<T>(Func<T> function)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing)
            {
                return new Func<T>(() =>
                {
                    runtime.DelayOperation();
                    return function();
                });
            }

            return function;
        }

        /// <summary>
        /// Creates a nondeterministic delay.
        /// </summary>
        internal static int CreateDelay(int millisecondsDelay)
        {
            if (millisecondsDelay == 0)
            {
                return 0;
            }

            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing)
            {
                return runtime.GetNondeterministicDelay(millisecondsDelay);
            }

            return millisecondsDelay;
        }

        /// <summary>
        /// Creates a nondeterministic delay.
        /// </summary>
        internal static TimeSpan CreateDelay(TimeSpan delay)
        {
            if (delay.TotalMilliseconds == 0)
            {
                return TimeSpan.Zero;
            }

            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Fuzzing)
            {
                return TimeSpan.FromMilliseconds(runtime.GetNondeterministicDelay(Convert.ToInt32(delay.TotalMilliseconds)));
            }

            return delay;
        }
    }
}
