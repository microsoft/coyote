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
        public static void Interleave() => CoyoteRuntime.Current.ScheduleNextOperation(AsyncOperationType.Default, false, true);

        /// <summary>
        /// Lowers the scheduling priority of the currently executing operation during testing.
        /// </summary>
        public static void Deprioritize() => CoyoteRuntime.Current.ScheduleNextOperation(AsyncOperationType.Default, true, true);
    }
}
