// ------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See License.txt in the repo root for license information.
// ------------------------------------------------------------------------------------------------

namespace Microsoft.Coyote.Runtime
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
