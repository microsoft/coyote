// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// The reason why the runtime detached the scheduler during a testing iteration.
    /// </summary>
    internal enum SchedulerDetachmentReason
    {
        /// <summary>
        /// The runtime detached the scheduler because the execution path was fully explored.
        /// </summary>
        PathExplored = 0,

        /// <summary>
        /// The runtime detached the scheduler because an exploration bound was reached.
        /// </summary>
        BoundReached,

        /// <summary>
        /// The runtime detached the scheduler because uncontrolled concurrency was detected.
        /// </summary>
        UncontrolledConcurrencyDetected,

        /// <summary>
        /// The runtime detached the scheduler because a was found bug in the execution path.
        /// </summary>
        BugFound
    }
}
