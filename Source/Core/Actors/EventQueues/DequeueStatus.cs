// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// The status returned as the result of an <see cref="Actor"/> dequeue operation.
    /// </summary>
    public enum DequeueStatus
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
        /// No dequeue has happened.
        /// </summary>
        NoDequeue,

        /// <summary>
        /// No event available to dequeue.
        /// </summary>
        Unavailable
    }
}
