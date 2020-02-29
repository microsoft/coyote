// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

namespace Microsoft.Coyote.Machines
{
    /// <summary>
    /// The status of the machine event handler.
    /// </summary>
    internal enum EventHandlerStatus
    {
        /// <summary>
        /// The machine has dequeued an event.
        /// </summary>
        EventDequeued = 0,

        /// <summary>
        /// The machine has handled an event.
        /// </summary>
        EventHandled,

        /// <summary>
        /// The machine has dequeued an event that cannot be handled.
        /// </summary>
        EventUnhandled
    }
}
