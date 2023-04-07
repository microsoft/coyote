// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Reflection;
using Microsoft.Coyote.Runtime;
using SystemSpinWait = System.Threading.SpinWait;
using SystemThread = System.Threading.Thread;

namespace Microsoft.Coyote.Rewriting.Types.Threading
{
    /// <summary>
    /// Provides support for spin-based waiting that can be controlled during testing.
    /// </summary>
    /// <remarks>This type is intended for compiler use rather than use directly in code.</remarks>
    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    public static class SpinWait
    {
        /// <summary>
        /// Used to invoke methods of the specified struct instance without boxing it.
        /// </summary>
        private delegate void StructInvoker(ref SystemSpinWait instance, int count);

        /// <summary>
        /// Performs a single spin.
        /// </summary>
        public static void SpinOnce(ref SystemSpinWait instance)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                // We model 'SpinOnce' by yielding the current operation.
                runtime.ScheduleNextOperation(current, SchedulingPointType.Yield, isYielding: true);
                IncrementCounter(ref instance);
            }
            else
            {
                instance.SpinOnce();
            }
        }

#if NET
        /// <summary>
        /// Performs a single spin and calls <see cref="SystemThread.Sleep(int)"/> after a minimum spin count.
        /// </summary>
        public static void SpinOnce(ref SystemSpinWait instance, int sleep1Threshold)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                // We model 'SpinOnce' by yielding the current operation.
                runtime.ScheduleNextOperation(current, SchedulingPointType.Yield, isYielding: true);
                IncrementCounter(ref instance);
            }
            else
            {
                instance.SpinOnce(sleep1Threshold);
            }
        }
#endif

        /// <summary>
        /// Spins until the specified condition is satisfied.
        /// </summary>
        public static void SpinUntil(Func<bool> condition)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                runtime.PauseOperationUntil(current, condition);
            }
            else
            {
                SystemSpinWait.SpinUntil(condition);
            }
        }

        /// <summary>
        /// Spins until the specified condition is satisfied or until the specified timeout is expired.
        /// </summary>
        public static bool SpinUntil(Func<bool> condition, int millisecondsTimeout)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                // TODO: consider introducing the notion of a PausedOnResourceOrDelay to model timeouts!
                runtime.PauseOperationUntil(current, condition);
                return true;
            }
            else
            {
                return SystemSpinWait.SpinUntil(condition, millisecondsTimeout);
            }
        }

        /// <summary>
        /// Spins until the specified condition is satisfied or until the specified timeout is expired.
        /// </summary>
        public static bool SpinUntil(Func<bool> condition, TimeSpan timeout)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Interleaving &&
                runtime.TryGetExecutingOperation(out ControlledOperation current))
            {
                // TODO: consider introducing the notion of a PausedOnResourceOrDelay to model timeouts!
                runtime.PauseOperationUntil(current, condition);
                return true;
            }
            else
            {
                return SystemSpinWait.SpinUntil(condition, timeout);
            }
        }

        /// <summary>
        /// Increments the spin count for the specified instance.
        /// </summary>
        private static void IncrementCounter(ref SystemSpinWait instance)
        {
            int currentCount = instance.Count;
            int newCount = currentCount == int.MaxValue ? 10 : currentCount + 1;

            // Use reflection to increment the count, as this is a private property.
            PropertyInfo property = instance.GetType().GetProperty("Count");
            StructInvoker propertySetter = (StructInvoker)Delegate.CreateDelegate(typeof(StructInvoker), property.GetSetMethod(true));
            propertySetter(ref instance, newCount);
        }

        /// <summary>
        /// Resets the spin counter.
        /// </summary>
        public static void Reset(ref SystemSpinWait instance)
        {
            instance.Reset();
        }
    }
}
