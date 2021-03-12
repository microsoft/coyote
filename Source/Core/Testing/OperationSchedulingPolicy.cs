// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Testing
{
    /// <summary>
    /// Supported policies for scheduling the execution of operations.
    /// </summary>
    internal enum OperationSchedulingPolicy
    {
        /// <summary>
        /// Policy that uses the default uncontrolled scheduler for executing operations.
        /// </summary>
        None = 0,

        /// <summary>
        /// Policy that fuzzes the schedule of operations to reorder interleavings.
        /// </summary>
        Fuzzing,

        /// <summary>
        /// Policy that controls the schedule of operations to systematically explore interleavings.
        /// </summary>
        Systematic
    }
}
