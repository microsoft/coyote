// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Provides a set of static methods for declaring points in the execution where interleavings
    /// between operations should be explored during testing.
    /// </summary>
    /// <remarks>
    /// These methods are no-op in production.
    /// </remarks>
    public static class SchedulingPoint
    {
        /// <summary>
        /// Explores a possible interleaving with another controlled operation.
        /// </summary>
        public static void Interleave()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                runtime.ScheduleNextOperation(SchedulingPointType.Default, isSuppressible: false);
            }
        }

        /// <summary>
        /// Yields execution to another controlled operation.
        /// </summary>
        /// <remarks>
        /// Invoking this method can lower the scheduling priority of the currently executing
        /// operation when certain exploration strategies are used.
        /// </remarks>
        public static void Yield()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                runtime.ScheduleNextOperation(SchedulingPointType.Yield, isSuppressible: false, isYielding: true);
            }
        }

        /// <summary>
        /// Suppresses interleavings until <see cref="Resume"/> is invoked.
        /// </summary>
        /// <remarks>
        /// This method does not suppress interleavings that happen when an operation is waiting
        /// some other operation to complete, when an operation completes and the scheduler
        /// switches to a new operation, or interleavings from uncontrolled concurrency.
        /// </remarks>
        public static void Suppress()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                runtime.SuppressScheduling();
            }
        }

        /// <summary>
        /// Resumes interleavings that were suppressed by invoking <see cref="Suppress"/>.
        /// </summary>
        public static void Resume()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                runtime.ResumeScheduling();
            }
        }

        /// <summary>
        /// Enable a previously disabled operation.
        /// </summary>
        public static void Enable(string msg)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                runtime.EnableOps(msg);
            }
        }

        /// <summary>
        /// Disabled the current operation with the specified message.
        /// </summary>
        public static void Disable()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                runtime.DisableCurrentOp();
            }
        }

        /// <summary>
        /// Move to the next phase (for debugging).
        /// </summary>
        public static void NextPhase(int phase)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                runtime.MoveNextPhase(phase);
            }
        }

        /// <summary>
        /// Prints debug information and optionally fails.
        /// </summary>
        public static void SetDebugInfo(string msg)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                runtime.SetDebugInfo(msg);
            }
        }

        /// <summary>
        /// Prints debug information and optionally fails.
        /// </summary>
        public static void WriteDebugInfo(bool fail)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                runtime.WriteDebugInfo(fail);
            }
        }
    }
}
