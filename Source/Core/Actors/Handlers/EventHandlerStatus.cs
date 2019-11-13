// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Actors
{
    /// <summary>
    /// The status of the actor event handler.
    /// </summary>
    internal enum EventHandlerStatus
    {
        /// <summary>
        /// The actor has dequeued an event.
        /// </summary>
        EventDequeued = 0,

        /// <summary>
        /// The actor has handled an event.
        /// </summary>
        EventHandled,

        /// <summary>
        /// The actor has dequeued an event that cannot be handled.
        /// </summary>
        EventUnhandled
    }
}
