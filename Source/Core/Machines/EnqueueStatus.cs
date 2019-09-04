// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.Coyote.Runtime
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
