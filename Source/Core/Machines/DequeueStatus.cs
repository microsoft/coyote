// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.Coyote.Machines
{
    /// <summary>
    /// The status returned as the result of a dequeue operation.
    /// </summary>
    internal enum DequeueStatus
    {
        /// <summary>
        /// An event was successfully dequeued.
        /// </summary>
        Success = 0,

        /// <summary>
        /// The raised event was dequeued.
        /// </summary>
        Raised,

        /// <summary>
        /// The default event was dequeued.
        /// </summary>
        Default,

        /// <summary>
        /// No event available to dequeue.
        /// </summary>
        NotAvailable
    }
}
