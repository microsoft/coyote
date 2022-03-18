// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Runtime
{
    /// <summary>
    /// The execution status of the runtime.
    /// </summary>
    internal enum ExecutionStatus
    {
        /// <summary>
        /// The runtime is still executing.
        /// </summary>
        Running = 0,

        /// <summary>
        /// The execution terminated because the path was fully explored.
        /// </summary>
        PathExplored,

        /// <summary>
        /// The execution terminated because the exploration bound was reached.
        /// </summary>
        BoundReached,

        /// <summary>
        /// The execution terminated because of a potential deadlock.
        /// </summary>
        Deadlocked,

        /// <summary>
        /// The execution terminated because of uncontrolled concurrency.
        /// </summary>
        ConcurrencyUncontrolled,

        /// <summary>
        /// The execution terminated because a bug was found.
        /// </summary>
        BugFound
    }
}
