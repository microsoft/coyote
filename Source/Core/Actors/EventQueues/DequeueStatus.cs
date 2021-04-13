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
        /// No dequeue has happened.
        /// </summary>
        None = 0,

        /// <summary>
        /// An event was successfully dequeued.
        /// </summary>
        Success,

        /// <summary>
        /// The raised event was dequeued.
        /// </summary>
        RaisedEvent,

        /// <summary>
        /// The default event was dequeued.
        /// </summary>
        DefaultEvent,

        /// <summary>
        /// No event available to dequeue.
        /// </summary>
        UnavailableEvent
    }
}
