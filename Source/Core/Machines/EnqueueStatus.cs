// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Machines
{
    /// <summary>
    /// The status returned as the result of an enqueue operation.
    /// </summary>
    internal enum EnqueueStatus
    {
        /// <summary>
        /// The event handler is already running.
        /// </summary>
        EventHandlerRunning = 0,

        /// <summary>
        /// The event handler is not running.
        /// </summary>
        EventHandlerNotRunning,

        /// <summary>
        /// The event was used to wake a machine at a receive statement.
        /// </summary>
        Received,

        /// <summary>
        /// There is no next event available to dequeue and handle.
        /// </summary>
        NextEventUnavailable,

        /// <summary>
        /// The event was dropped.
        /// </summary>
        Dropped
    }
}
