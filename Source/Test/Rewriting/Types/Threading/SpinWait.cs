// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
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
        /// Gets the number of times <see cref="SystemSpinWait.SpinOnce()"/> has been called on this instance.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static int get_Count(ref SystemSpinWait instance)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            return instance.Count;
        }

        /// <summary>
        /// Gets whether the next call to <see cref="SystemSpinWait.SpinOnce()"/> will yield the
        /// processor, triggering a forced context switch.
        /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores
#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable IDE1006 // Naming Styles
        public static bool get_NextSpinWillYield(ref SystemSpinWait instance)
#pragma warning restore IDE1006 // Naming Styles
#pragma warning restore SA1300 // Element should begin with upper-case letter
#pragma warning restore CA1707 // Identifiers should not contain underscores
        {
            return instance.NextSpinWillYield;
        }

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
        /// Resets the spin counter.
        /// </summary>
        public static void Reset(ref SystemSpinWait instance)
        {
            instance.Reset();
        }
    }
}
