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
        /// Explores a possible interleaving with another operation during testing.
        /// </summary>
        public static void Interleave()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                runtime.ScheduleNextOperation(AsyncOperationType.Default, false, true);
            }
        }

        /// <summary>
        /// Lowers the scheduling priority of the currently executing operation during testing.
        /// </summary>
        public static void Deprioritize()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                runtime.ScheduleNextOperation(AsyncOperationType.Yield, true, true);
            }
        }

        /// <summary>
        /// Supresses interleavings during testing until <see cref="Resume"/> is invoked.
        /// </summary>
        /// <remarks>
        /// This method does not supress interleavings that happen when an operation is waiting
        /// some other operation to complete, when an operation completes and the scheduler
        /// switches to a new operation, or interleavings from uncontrolled concurrency.
        /// </remarks>
        public static void Supress()
        {
            var runtime = CoyoteRuntime.Current;
            if (runtime.SchedulingPolicy is SchedulingPolicy.Systematic)
            {
                runtime.SupressScheduling();
            }
        }

        /// <summary>
        /// Resumes interleavings during testing due to an invoked <see cref="Supress"/>.
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
