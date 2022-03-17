// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// Supported policies for scheduling the execution of operations.
    /// </summary>
    internal enum SchedulingPolicy
    {
        /// <summary>
        /// Policy that uses the default uncontrolled scheduler for executing operations.
        /// </summary>
        None = 0,

        /// <summary>
        /// Policy that injects controlled delays to fuzz the schedule of operations.
        /// </summary>
        Fuzzing,

        /// <summary>
        /// Policy that controls the lifetime and schedule of operations to serialize
        /// the execution and explore interleavings.
        /// </summary>
        Interleaving
    }
}
