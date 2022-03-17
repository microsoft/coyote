// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;

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
                runtime.ScheduleNextOperation(SchedulingPointType.Interleave, isSuppressible: false);
            }
        }

        /// <summary>
        /// Attempts to yield execution to another controlled operation.
        /// </summary>
        /// <remarks>
        /// Invoking this method might lower the scheduling priority of the currently executing
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

#pragma warning disable CA1801 // Parameter not used
        /// <summary>
        /// Explores a possible interleaving due to a 'READ' operation on the specified shared state.
        /// </summary>
        /// <param name="state">The shared state that is being read represented as a string.</param>
        /// <param name="comparer">
        /// Checks if the read shared state is equal with another shared state that is being accessed concurrently.
        /// </param>
        public static void Read(string state, IEqualityComparer<string> comparer = default)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                ControlledOperation op = runtime.GetExecutingOperation();
                op.LastAccessedSharedState = state;
                runtime.ScheduleNextOperation(SchedulingPointType.Read, isSuppressible: false);
                op.LastAccessedSharedState = string.Empty;
            }
        }

        /// <summary>
        /// Explores a possible interleaving due to a 'WRITE' operation on the specified shared state.
        /// </summary>
        /// <param name="state">The shared state that is being written represented as a string.</param>
        /// <param name="comparer">
        /// Checks if the written shared state is equal with another shared state that is being accessed concurrently.
        /// </param>
        public static void Write(string state, IEqualityComparer<string> comparer = default)
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                ControlledOperation op = runtime.GetExecutingOperation();
                op.LastAccessedSharedState = state;
                runtime.ScheduleNextOperation(SchedulingPointType.Write, isSuppressible: false);
                op.LastAccessedSharedState = string.Empty;
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
    }
}
